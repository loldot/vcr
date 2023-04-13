using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vcr.Core;

public class Vcr : IDisposable
{
    private readonly VcrTestInterceptor interceptor;

    public Vcr(string name)
    {
        if (!name.EndsWith(".har")) name = $"{name}.har";

        var vcrDataDir = Path.Combine(Environment.CurrentDirectory, $"../../../.vcrdata");
        if (!Directory.Exists(vcrDataDir)) Directory.CreateDirectory(vcrDataDir);
        
        interceptor = new VcrTestInterceptor(Path.Combine(vcrDataDir, name));
    }

    public IHttpClientFactory HttpClientFactory => new VcrTestInterceptorFactory(interceptor);


    // public static IServiceScope Record(IServiceCollection services, string recordingPath)
    // {
    //     var defaultHandlerFactory = services.FirstOrDefault(x => x.ServiceType == typeof(IHttpMessageHandlerFactory));
    //     if (defaultHandlerFactory is not null)
    //         services.Remove(defaultHandlerFactory);

    //     services.AddSingleton<IHttpMessageHandlerFactory>(x => new VcrTestInterceptorFactory(recordingPath));

    //     var serviceProvider = services.BuildServiceProvider();
    //     return serviceProvider.CreateScope();
    // }

    public void Dispose()
    {
        interceptor?.Dispose();
    }
}