using Microsoft.Extensions.Http;
using System.Net;
using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;


public sealed class VcrTestInterceptor : DelegatingHandler
{
    private readonly string recordingPath;
    public HttpArchive? Archive { get; private set; }

    private HarBuilder recorder = new HarBuilder();
    private MockServer server = new MockServer();
    private bool isIntercepting = false;

    public VcrTestInterceptor(string recordingPath)
        : this(recordingPath, new HttpClientHandler()) { }

    public VcrTestInterceptor(string recordingPath, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        this.recordingPath = recordingPath;
    }

    private async Task Intercept()
    {
        if (isIntercepting) return;

        Archive = await HttpArchive.Load(recordingPath);
        recorder = new HarBuilder(Archive);
        server = new MockServer(Archive, MockServer.RoutingModes.Absolute);
        isIntercepting = true;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Intercept();

        var storedResponse = server.GetResponse(request.Method, request.RequestUri!.AbsoluteUri);

        if (storedResponse is not null) return storedResponse.ToHttpResponseMessage();

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

    public HttpClient CreateClient(string name) => new(interceptor);
    public HttpMessageHandler CreateHandler(string name) => interceptor;
}