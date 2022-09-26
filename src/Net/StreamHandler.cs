using System;
using System.Net.Quic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OoLunar.Willow.Database;
using OoLunar.Willow.Database.Models;
using OoLunar.Willow.Net.Payloads;

namespace OoLunar.Willow.Net
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
        /// One of many streams tied to a singular connection.
        /// </summary>
        // TODO: This should be a list of streams, not a single stream. There should also be a way to continously accept new streams in case the old stream is closed.
        public QuicStream? Stream { get; private set; }

        /// <summary>
        /// After the authentication process is complete, this will be the user's information.
        /// </summary>
        public UserModel? User { get; private set; }

        /// <summary>
        /// Creates a new stream handler.
        /// </summary>
        /// <param name="database">The database used to grab the user's information.</param>
        /// <param name="connection">The QUIC connection to handle.</param>
        public StreamHandler(DatabaseContext database, QuicConnection connection)
        {
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

            await JsonSerializer.SerializeAsync(Stream, new HelloPayload(), cancellationToken: CancellationToken.None);
            HelloModel? helloModel = await JsonSerializer.DeserializeAsync<HelloModel>(Stream, cancellationToken: CancellationToken.None);
            if (helloModel == null)
            {
                Database.Logins.Add(new LoginModel(null, null, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidHello, "Hello was null", CancellationToken.None);
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
            if (cancellationToken.IsCancellationRequested)
            {
                await ErrorAndCloseAsync(CloseCode.ServerShutdown, "Server is shutting down.", CancellationToken.None);
                return;
            }

            User = await Database.Users.FirstOrDefaultAsync(user => user.Id == hello.Id, CancellationToken.None);
            if (User == null)
            {
                Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidLogin, "User does not exist", CancellationToken.None);
                await Database.SaveChangesAsync(CancellationToken.None);
            }
            else if (User.PasswordHash != hello.PasswordHash)
            {
                Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, false));
                await ErrorAndCloseAsync(CloseCode.InvalidLogin, "Invalid password", CancellationToken.None);
                await Database.SaveChangesAsync(CancellationToken.None);
            }

            // Track when the user logged in at
            Database.Logins.Add(new LoginModel(hello.Id, hello.UserAgent, Connection.RemoteEndPoint, DateTimeOffset.UtcNow, true));
            await Database.SaveChangesAsync(CancellationToken.None);

            // Wait for commands
            await IdleAsync(cancellationToken);
        }

        /// <summary>
        /// Idles the connection waiting for commands.
        /// </summary>
        /// <param name="cancellationToken">Used to close the connection.</param>
        public async Task IdleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(async () => await ErrorAndCloseAsync(CloseCode.ServerShutdown, "Server is shutting down.", CancellationToken.None));

            while (!cancellationToken.IsCancellationRequested)
            {
                // Idle the connection, waiting for a new payload.
                while (Stream!.ReadByte() == -1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
                // Back up one byte for full deserialization.
                Stream.Position = 0;

                // TODO: Change from execute command model to execute action model, which can contain commands.
                await JsonSerializer.DeserializeAsync<ExecuteCommandModel>(Stream, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Gracefully closes the connection with an error code.
        /// </summary>
        /// <param name="errorCode">The error code to give to the user.</param>
        /// <param name="message">An optional message to be passed during development. Should not be sent when in production.</param>
        /// <param name="cancellationToken">Unsure.</param>
        private async Task ErrorAndCloseAsync(CloseCode errorCode, string? message = null, CancellationToken cancellationToken = default)
        {
            if (Stream == null)
            {
                throw new InvalidOperationException("Stream is null");
            }

            // FIXME: Should cancellationToken be used here if the connection is supposed to close gracefully?
            await JsonSerializer.SerializeAsync(Stream, new ClosePayload(errorCode, message), cancellationToken: cancellationToken);
            await Stream.DisposeAsync();
        }
    }
}
