using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OoLunar.Willow.Models;
using OoLunar.Willow.Models.Actions;
using OoLunar.Willow.Payloads;
using OoLunar.Willow.Server.Models;
using OoLunar.Willow.Server.Models.Actions;

namespace OoLunar.Willow.Server
{
    /// <summary>
    /// Handles incoming QUIC streams, authenticates a user and idles the connection waiting for commands.
    /// </summary>
    public sealed class StreamHandler
    {
        /// <summary>
        /// The database used to fetch user information.
        /// </summary>
        public DatabaseContext Database { get; init; }

        /// <summary>
        /// The QUIC connection tied to this stream handler.
        /// </summary>
        public QuicConnection Connection { get; init; }

        /// <summary>
        /// Used when creating actions.
        /// </summary>
        public IServiceProvider ServiceProvider { get; init; }

        /// <summary>
        /// One of many streams tied to a singular connection.
        /// </summary>
        // TODO: This should be a list of streams, not a single stream. There should also be a way to continously accept new streams in case the old stream is closed.
        public QuicStream? Stream { get; private set; }

        /// <summary>
        /// After the authentication process is complete, this will be the user's information.
        /// </summary>
        public UserModel? User { get; private set; }

        /// <summary>
        /// Which count of the keep alive request this is.
        /// </summary>
        public byte ExpectedKeepAliveId { get; private set; }

        private static readonly IReadOnlyDictionary<ActionFlags, Type> Actions = new Dictionary<ActionFlags, Type>
        {
            [ActionFlags.ExecuteCommand] = typeof(ExecuteCommandAction),
            [ActionFlags.AlterSettings] = typeof(AlterSettingsAction),
            [ActionFlags.SetCommand] = typeof(SetCommandServerAction)
        };

        /// <summary>
        /// Creates a new stream handler.
        /// </summary>
        /// <param name="database">The database used to grab the user's information.</param>
        /// <param name="connection">The QUIC connection to handle.</param>
        public StreamHandler(IServiceProvider serviceProvider, DatabaseContext database, QuicConnection connection)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Sends the server version, accepts the client version, and then authenticates the user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to test if the server is closing.</param>
        /// <remarks>The Hello process should not be interrupted. If the cancellation token is cancelled, the connection should stay open, finish the hello process, then close the stream during LoginAsync.</remarks>
        public async Task HelloAsync(CancellationToken cancellationToken = default)
        {
            // See remarks for the use of CancellationToken.None
            Stream = await Connection.AcceptInboundStreamAsync(CancellationToken.None);
            if (Stream.WritesClosed.IsCompleted)
            {
                // The stream was closed before we could accept it.
                Stream.Close();
                return;
            }

            await JsonSerializer.SerializeAsync(Stream, new HelloPayload(), cancellationToken: CancellationToken.None);
            HelloModel? helloModel = await DeserializeAsync<HelloModel>();
            if (helloModel is null)
            {
                Database.Logins.Add(new LoginModel(null, null, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidHello, "Hello was null");
                await Database.SaveChangesAsync(CancellationToken.None);
                return;
            }

            // Authenticate the user
            await LoginAsync(helloModel, cancellationToken);
        }

        /// <summary>
        /// Authenticates the user and idles the connection.
        /// </summary>
        /// <param name="hello">The hello payload that the QUIC connection has previously sent.</param>
        /// <param name="cancellationToken">The cancellation token used to test if the server is closing.</param>
        /// <remarks>The login process shouldn't be interrupted. We first test if the cancellation token is cancelled before we start authenticating the user. If it is, send the close code. If it isn't, authenticate the user and pass the cancellation token to the idle method.</remarks>
        public async Task LoginAsync(HelloModel hello, CancellationToken cancellationToken = default)
        {
            if (Stream is null)
            {
                throw new InvalidOperationException("Stream is null");
            }
            else if (cancellationToken.IsCancellationRequested)
            {
                await ErrorAndCloseAsync(CloseCode.ServerShutdown, "Server is shutting down.");
                return;
            }

            User = await Database.Users.FirstOrDefaultAsync(user => user.Id == hello.Id, CancellationToken.None);
            if (User is null)
            {
                Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidLogin, "User does not exist");
                await Database.SaveChangesAsync(CancellationToken.None);
            }
            else if (User.PasswordHash != hello.PasswordHash)
            {
                Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidLogin, "Invalid password");
                await Database.SaveChangesAsync(CancellationToken.None);
            }

            // Welcomes the user, sending the successful login information
            await JsonSerializer.SerializeAsync(Stream, new WelcomePayload(User!, await Database.Logins.LastOrDefaultAsync(login => login.Id == User!.Id && login.Successful, CancellationToken.None), Database.Commands.Where(command => command.UserId == User!.Id)), cancellationToken: CancellationToken.None);

            // Track when the user logged in at
            Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, true));
            await Database.SaveChangesAsync(CancellationToken.None);

            // Send the user their account
            await JsonSerializer.SerializeAsync(Stream, User, cancellationToken: CancellationToken.None);

            // Wait for commands
            await IdleAsync(cancellationToken);
        }

        /// <summary>
        /// Idles the connection waiting for commands.
        /// </summary>
        /// <param name="cancellationToken">Used to close the connection.</param>
        public async Task IdleAsync(CancellationToken cancellationToken = default)
        {
            if (Stream is null)
            {
                throw new InvalidOperationException("Stream is null");
            }
            else if (User is null)
            {
                throw new InvalidOperationException("User is null");
            }

            cancellationToken.Register(async () => await ErrorAndCloseAsync(CloseCode.ServerShutdown, "Server is shutting down."));

            while (!cancellationToken.IsCancellationRequested)
            {
                ServerActionModel? action = await DeserializeAsync<ServerActionModel>();
                if (action is null || !Actions.TryGetValue(action.Action, out Type? actionType))
                {
                    await JsonSerializer.SerializeAsync(Stream, new ErrorPayload(ErrorCode.InvalidAction, $"Unknown action type: {(int?)action?.Action}", action), cancellationToken: CancellationToken.None);
                    continue;
                }
                else if (action.Data is null)
                {
                    await JsonSerializer.SerializeAsync(Stream, new ErrorPayload(ErrorCode.InvalidData, "Data cannot be null", action), cancellationToken: CancellationToken.None);
                    continue;
                }
                else if (actionType is null || !actionType.IsSubclassOf(action.Data.GetType()))
                {
                    await JsonSerializer.SerializeAsync(Stream, new ErrorPayload(ErrorCode.ServerError, $"Failed to correctly handle action type: {(int?)action?.Action}", action), cancellationToken: CancellationToken.None);
                    continue;
                }
                else // else for scope
                {
                    action.Data.InjectDependencies(User, Connection, ServiceProvider);
                    object? result = await action.Data.ExecuteAsync(action.CorrelationId, cancellationToken);
                    await JsonSerializer.SerializeAsync(Stream, new ActionModel(result is ErrorPayload ? ActionFlags.Error : ActionFlags.Result, result), cancellationToken: CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Gracefully closes the connection with an error code.
        /// </summary>
        /// <param name="errorCode">The error code to give to the user.</param>
        /// <param name="message">An optional message to be passed during development. Should not be sent when in production.</param>
        /// <param name="cancellationToken">Unsure.</param>
        private async Task ErrorAndCloseAsync(CloseCode errorCode, string? message = null)
        {
            if (Stream is null)
            {
                throw new InvalidOperationException("Stream is null");
            }

            await JsonSerializer.SerializeAsync(Stream, new ClosePayload(errorCode, message), cancellationToken: CancellationToken.None);
            await Stream.DisposeAsync();
        }

        private ValueTask<T?> DeserializeAsync<T>()
        {
            if (Stream is null)
            {
                throw new InvalidOperationException("Stream is null");
            }

            MemoryStream memoryStream = new();
            Stream.CopyTo(memoryStream);
            return JsonSerializer.DeserializeAsync<T>(memoryStream, cancellationToken: CancellationToken.None);
        }
    }
}
