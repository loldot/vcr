using System.Net;
using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;


public sealed class VcrTestInterceptor : DelegatingHandler
{
    private readonly string recordingPath;
    public HttpArchive? Archive { get; private set; }

    private HarBuilder recorder = new HarBuilder();
    private MockServer server = new MockServer();
    private bool isAttached = false;

    public VcrTestInterceptor(string recordingPath)
        : base(new HttpClientHandler())
    {
        this.recordingPath = recordingPath;
    }

    private async Task Attach()
    {
        if (isAttached) return;

        Archive = await HttpArchive.Load(recordingPath);
        recorder = new HarBuilder(Archive);
        server = new MockServer(Archive, MockServer.RoutingModes.Absolute);
        isAttached = true;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Attach();

        var storedResponse = server.GetResponse(request.Method, request.RequestUri!.AbsoluteUri);

        if (storedResponse != null) return storedResponse.ToHttpResponseMessage();

        var entry = recorder.WithEntry();
        entry.WithRequest(request);

        var httpResponse = await base.SendAsync(request, cancellationToken);
        entry.WithResponse(httpResponse);

        return httpResponse;
    }

    protected override void Dispose(bool disposing)
    {
        recorder.SaveToFileSync(recordingPath);
        base.Dispose(disposing);
    }
}

public class VcrTestInterceptorFactory : IHttpClientFactory, IHttpMessageHandlerFactory
{
    private readonly VcrTestInterceptor interceptor;

    public VcrTestInterceptorFactory(VcrTestInterceptor interceptor)
    {
        this.interceptor = interceptor;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(interceptor);
    }

    public HttpMessageHandler CreateHandler(string name) => interceptor;
}