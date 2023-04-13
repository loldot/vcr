using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;


public sealed class VcrTestInterceptor : DelegatingHandler, IAsyncDisposable
{
    private readonly string recordingPath;
    private HttpArchive? archive;
    private HarBuilder recorder;
    private MockServer server;
    private bool isAttached = false;

    public VcrTestInterceptor(string recordingPath)
        : base(new HttpClientHandler())
    {
        this.recordingPath = recordingPath;
    }

    private async Task Attach()
    {
        if (isAttached) return;

        archive = await HttpArchive.Load(recordingPath);
        recorder = new HarBuilder(archive);
        server = new MockServer(archive);
        isAttached = true;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Attach();

        var storedResponse = server.GetResponse(request.Method, request.RequestUri!.PathAndQuery);

        if (storedResponse != null) return storedResponse.ToHttpResponseMessage();

        var entry = recorder.WithEntry();
        entry.WithRequest(request);

        var httpResponse = await base.SendAsync(request, cancellationToken);
        entry.WithResponse(httpResponse);

        return httpResponse;
    }

    protected override void Dispose(bool disposing)
    {
        DisposeAsync().ConfigureAwait(false);
        base.Dispose(disposing);
    }

    public async ValueTask DisposeAsync()
    {
        await recorder.SaveToFile(recordingPath);
    }
}
