using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace IServerTest.Web
{
    public class UdpSocketTransport
    {
        readonly Socket _socket;
        readonly IPEndPoint _endpoint;
        readonly ILogger<UdpSocketTransport> _logger;
        readonly IRequestTransformer<HttpRequestMessage> _requestTransformer;
        const int MinBufferSize = 1024;

        public UdpSocketTransport(
            IOptions<UdpServerOptions> udpOptions,
            ILogger<UdpSocketTransport> logger,
            IRequestTransformer<HttpRequestMessage> requestTransformer)
        {
            ArgumentNullException.ThrowIfNull(udpOptions, nameof(udpOptions));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _endpoint = new IPEndPoint(udpOptions.Value.Address, udpOptions.Value.Port);
            _socket.Bind(_endpoint);

            _logger = logger;
            _requestTransformer = requestTransformer;

        }

        public async Task<Stream> ReceiveAsync(CancellationToken cancellationToken)
        {
                var pipe = new Pipe();
                await ReadFromSocket(pipe.Writer, cancellationToken);
                // Disposing of the stream should automatically close the pipe reader
                return pipe.Reader.AsStream();
        }

        async Task ReadFromSocket(PipeWriter wr, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var memory = wr.GetMemory(MinBufferSize);
                    var response = await _socket.ReceiveFromAsync(memory, SocketFlags.None, _endpoint, cancellationToken);

                    if (response.ReceivedBytes == 0) break;

                    wr.Advance(response.ReceivedBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error message: {message}", ex.Message);
                    break;
                }

                var result = await wr.FlushAsync(cancellationToken);
                if (result.IsCompleted) break;
            }

            await wr.CompleteAsync();
        }
    }
}
