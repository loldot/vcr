using Vcr.Core;
using Xunit;
using FluentAssertions;
using Vcr.Core.HAR.Version1_2;
using System.Text.Json;
using System.Net.Http.Json;

namespace Vcr.Test;


public class Interceptor
{
    [Fact]
    public async Task ShouldRecordResponse()
    {
        var harPath = $"{Guid.NewGuid()}.har";
        var interceptor = new VcrTestInterceptor(harPath);

        var http = new HttpClient(interceptor);
        var response = await http.GetAsync("https://api.ipify.org?format=json");

        http.Dispose();

        response.Should().NotBeNull();
        File.Exists(harPath).Should().BeTrue();

        File.Delete(harPath);
    }

    [Fact]
    public async Task ShouldRecordValidResponse()
    {
        var harPath = $"{Guid.NewGuid()}.har";
        var interceptor = new VcrTestInterceptor(harPath);

        var http = new HttpClient(interceptor);
        var response = await http.GetAsync("https://api.ipify.org?format=json");

        http.Dispose();

        response.Should().NotBeNull();
        File.Exists(harPath).Should().BeTrue();

        var har = await HttpArchive.Load(harPath);

        har.Should().NotBeNull();
        har!.Log.Entries.Should().HaveCount(1);

        var json = har!.Log.Entries[0].Response.Content.Text;
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        dict.Should().NotBeNull();
        dict!["ip"].Should().NotBeNull();

        File.Delete(harPath);
    }

    [Fact]
    public async Task ShouldUseRecordedResponse()
    {
        var harPath = "./ip.har";
        var interceptor = new VcrTestInterceptor(harPath);

        var http = new HttpClient(interceptor);
        var dict = await http.GetFromJsonAsync<Dictionary<string, string>>("https://api.ipify.org?format=json");
        dict!["ip"].Should().Be("123.123.123.123");
    }


    [Fact]
    public async Task ShouldSequenceRecordedResponses()
    {
        var harPath = "./ip-with-mutated-response.har";
        var interceptor = new VcrTestInterceptor(harPath);

        var http = new HttpClient(interceptor);

        var dict = await http.GetFromJsonAsync<Dictionary<string, string>>("https://api.ipify.org?format=json");
        dict!["ip"].Should().Be("123.123.123.123");

        dict = await http.GetFromJsonAsync<Dictionary<string, string>>("https://api.ipify.org?format=json");
        dict!["ip"].Should().Be("127.0.0.1");
    }


    [Fact]
    public async Task ShouldUpdateArchive()
    {
        var originalHarPath = "./ip.har";
        var harPath = $"{Path.GetTempFileName()}.har";
        File.Copy(originalHarPath, harPath, overwrite: true);

        var interceptor = new VcrTestInterceptor(harPath);

        var http = new HttpClient(interceptor);
        var newUrl = new Uri("https://api.ipify.org");
        var response = await http.GetAsync(newUrl);
        var ip = await response.Content.ReadAsStringAsync();

        http.Dispose();

        var archive = await HttpArchive.Load(harPath);

        archive!.Log.Entries.Should().HaveCount(2);

        var newEntry = archive.Log.Entries.First(x => x.Request.Url == newUrl);
        newEntry.Response.Content.Text.Should().Be(ip);

        File.Delete(harPath);
    }
}