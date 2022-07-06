using App.Metrics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace IServerTest.Web;

public class UdpServer : IServer
{
    readonly UdpClient _listener = new();
    readonly UdpSocketTransport _transport;
    readonly IPEndPoint _listenEndpoint;
    readonly ILogger<UdpServer> _logger;
    readonly IMetrics _metrics;
    readonly IRequestTransformer<HttpRequestMessage> _messageTransformer;

    public IFeatureCollection Features => new FeatureCollection();

    public UdpServer(
        UdpSocketTransport transport,
        IOptions<UdpServerOptions> udpOptions,
        IRequestTransformer<HttpRequestMessage> messageTransformer,
        ILogger<UdpServer> logger,
        IMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(udpOptions, nameof(udpOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(messageTransformer, nameof(messageTransformer));
        ArgumentNullException.ThrowIfNull(transport, nameof(transport));

        _listenEndpoint = new IPEndPoint(udpOptions.Value.Address, udpOptions.Value.Port);
        // _listener = new(_listenEndpoint);
        _messageTransformer = messageTransformer;
        _logger = logger;
        _metrics = metrics;
        _transport = transport;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _listener?.Dispose();
    }

    public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        _logger.LogInformation(
            "UDP server starting to listen on: IP address '{address}' Port '{port}'", _listenEndpoint.Address, _listenEndpoint.Port);

        await ListenAndServe(application, cancellationToken);
    }

    async Task ListenAndServe<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        var sw = new Stopwatch();

        while (!cancellationToken.IsCancellationRequested)
        {
            // var request = await _listener.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            var request = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            sw.Start();

            //_logger.LogDebug(
            //    "Recieved udp request from remote host: '{address}' on port '{port}'. " +
            //    "Message size: '{size}' bytes", request.RemoteEndPoint.Address, request.RemoteEndPoint.Port, request.Buffer.Length);

            // var httpRequest = await _messageTransformer.DecodeHttpRequest(request.Buffer).ConfigureAwait(false);
            var httpRequest = await _messageTransformer.DecodeHttpRequest(request).ConfigureAwait(false);

            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(await MapFromHttpRequestMessage(httpRequest, cancellationToken).ConfigureAwait(false));
            features.Set<IHttpResponseFeature>(new HttpResponseFeature());

            var ctx = application.CreateContext(features);
            try
            {
                await application.ProcessRequestAsync(ctx);

                // TODO transform IHttpResponseFeature into udp response and send back to client
            }
            finally
            {
                application.DisposeContext(ctx, null);
            }

            sw.Stop();
            _metrics.Measure.Meter.Mark(UdpMetricsRegistry.ServerRequestCount);
            _metrics.Measure.Timer.Time(UdpMetricsRegistry.ServerE2ETime, sw.ElapsedMilliseconds);
            sw.Reset();
        };
    }

    async Task<IHttpRequestFeature> MapFromHttpRequestMessage(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
        var requestFeature = new HttpRequestFeature();

        if (httpRequest?.Content is not null)
        {
            requestFeature!.Body = await httpRequest.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("Skipping setting Http Request body, body is null");
        }

        if (httpRequest?.Headers is not null)
        {
            foreach (var (key, value) in httpRequest!.Headers)
            {
                requestFeature!.Headers.TryAdd(key, new StringValues(value.ToArray()));
            }
        }
        else
        {
            _logger.LogDebug("Skipped setting Http Headers, headers are null");
        }

        if (httpRequest?.Method is not null)
        {
            requestFeature!.Method = httpRequest.Method.ToString();
        }
        else
        {
            // TODO short circuit request and send back udp bad response?
            _logger.LogDebug("Skipped setting Http Method, http methos is null");
        }

        if (httpRequest?.RequestUri is not null)
        {
            requestFeature!.Protocol = httpRequest.RequestUri.Scheme;
            requestFeature!.QueryString = httpRequest.RequestUri.Query;
            requestFeature!.Path = httpRequest.RequestUri.LocalPath;
        }

        return requestFeature;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No-Op, currently no stopping code needed
        return Task.CompletedTask;
    }
}

