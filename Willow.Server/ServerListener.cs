using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OoLunar.Willow.Server
{
    public class ServerListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServerListener> _logger;
        private QuicListener? _listener;

        public ServerListener(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<ServerListener> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (!QuicListener.IsSupported)
            {
                _logger.LogCritical("QuicListener is not supported on this platform.");
                throw new NotSupportedException("QUIC is not supported on this platform.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = stoppingToken.Register(async () =>
            {
                _logger.LogInformation("Stopping server");
                if (_listener != null)
                {
                    await _listener.DisposeAsync();
                }
            });

            QuicListenerOptions options = new()
            {
                ListenEndPoint = IPEndPoint.TryParse(_configuration["ListenEndPoint"]!, out IPEndPoint? listenEndPoint) ? listenEndPoint : new IPEndPoint(IPAddress.Any, 443),
                ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2, SslApplicationProtocol.Http3 },
                ConnectionOptionsCallback = (QuicConnection connection, SslClientHelloInfo clientHelloInfo, CancellationToken cancellationToken) =>
                {
                    _logger.LogDebug("Connection established from {RemoteEndPoint} on {LocalEndPoint}", connection.RemoteEndPoint, connection.LocalEndPoint);
                    return ValueTask.FromResult(new QuicServerConnectionOptions()
                    {
                        ServerAuthenticationOptions = new SslServerAuthenticationOptions()
                        {
                            ServerCertificate = new X509Certificate(_configuration["CertificatePath"]!)
                        }
                    });
                },
                ListenBacklog = 100,
            };

            _listener ??= await QuicListener.ListenAsync(options, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                QuicConnection connection = await _listener.AcceptConnectionAsync(stoppingToken);
                // TODO: Not Task.Run lmao
                try
                {
                    Task.Run(() => new StreamHandler(_serviceProvider, _serviceProvider.GetRequiredService<DatabaseContext>(), connection).HelloAsync(stoppingToken), stoppingToken).Start();
                }
                catch (Exception error)
                {
                    _logger.LogError(error, "Error while handling connection from {ConnectionEndPoint}", connection.RemoteEndPoint);
                }
            }
        }
    }
}
