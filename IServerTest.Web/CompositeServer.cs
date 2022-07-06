using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IServerTest.Web;

public class CompositeServer : IServer
{
    readonly ILogger<CompositeServer> _logger;
    readonly KestrelServer _kestrelServer;
    readonly UdpServer _udpServer;

    public IFeatureCollection Features =>
        new FeatureCollection();

    public CompositeServer(
        UdpServer udpServer,
        KestrelServer kestrelServer,
        ILogger<CompositeServer> logger)
    {
        ArgumentNullException.ThrowIfNull(udpServer, nameof(udpServer));
        ArgumentNullException.ThrowIfNull(kestrelServer, nameof(kestrelServer));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _udpServer = udpServer;
        _kestrelServer = kestrelServer;
        _logger = logger;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _kestrelServer?.Dispose();
        _udpServer?.Dispose();
    }

    public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        _logger.LogDebug("Starting Kestrel Server...");
        await _kestrelServer.StartAsync(application, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Kestrel Server started successfully");

        _logger.LogDebug("Starting UDP Server...");
        await _udpServer.StartAsync(application, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("UDP Server started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping Kestrel Server....");
        await _kestrelServer.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Kestrel Server stopped successfully");

        _logger.LogDebug("Stopping UDP Server....");
        await _udpServer.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("UDP Server stopped successfully");
    }
}

public static class UdpServerExtensions
{
    public static IWebHostBuilder UseUdpServer(this IWebHostBuilder builder, Action<UdpServerOptions> options)
    {
        builder.ConfigureServices(sc =>
        {
            sc.Configure(options);
            sc.AddSingleton<IServer, CompositeServer>();
            sc.AddSingleton<KestrelServer>();
            sc.AddSingleton<UdpServer>();
            sc.AddSingleton<IRequestTransformer<HttpRequestMessage>, HttpRequestMessageTransformer>();
            sc.AddSingleton<UdpSocketTransport>();
        });

        return builder;
    }
}
