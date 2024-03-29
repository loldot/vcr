using System.Net.Http.Json;

namespace Vcr.Test.Example;

class ExampleService
{
    private readonly HttpClient http;

    public ExampleService(IHttpClientFactory httpClientFactory)
    {
        http = httpClientFactory.CreateClient();
    }

    public async Task<string> GetIp()
    {
        var ipInfo = await http.GetFromJsonAsync<Dictionary<string, string>>("https://api.ipify.org?format=json");
        return ipInfo!["ip"];
    }
}
