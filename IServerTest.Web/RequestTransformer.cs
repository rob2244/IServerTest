using System.Buffers;
using System.Net.Http.Headers;

namespace IServerTest.Web;

public interface IRequestTransformer<TRequest>
{
    Task<ReadOnlyMemory<byte>> EncodeHttpRequest(TRequest request);

    Task<TRequest> DecodeHttpRequest(Stream stream);

    Task<TRequest> DecodeHttpRequest(ArraySegment<byte> bytes);
}

public class HttpRequestMessageTransformer : IRequestTransformer<HttpRequestMessage>
{
    public async Task<HttpRequestMessage> DecodeHttpRequest(ArraySegment<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));

        var content = new ByteArrayContent(bytes!.Array!);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/http; msgtype=request");

        return await content.ReadAsHttpRequestMessageAsync().ConfigureAwait(false);
    }

    public async Task<HttpRequestMessage> DecodeHttpRequest(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var content = new StreamContent(stream);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/http; msgtype=request");

        return await content.ReadAsHttpRequestMessageAsync().ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> EncodeHttpRequest(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var content = new HttpMessageContent(request);
        return new ReadOnlyMemory<byte>(await content.ReadAsByteArrayAsync().ConfigureAwait(false));
    }
}