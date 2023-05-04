using System.Runtime.CompilerServices;

namespace Vcr.Core;

public class HttpRecorder : IDisposable
{
    private readonly VcrTestInterceptor interceptor;

    public HttpRecorder([CallerMemberName] string name = "")
    {
        if (!name.EndsWith(".har")) name = $"{name}.har";

        var vcrDataDir = Path.Combine(Environment.CurrentDirectory, $"../../../.vcrdata");
        if (!Directory.Exists(vcrDataDir)) Directory.CreateDirectory(vcrDataDir);
        
        interceptor = new VcrTestInterceptor(Path.Combine(vcrDataDir, name));
    }

    public IHttpClientFactory HttpClientFactory => new VcrTestInterceptorFactory(interceptor);

    public void Dispose() => interceptor?.Dispose();
}