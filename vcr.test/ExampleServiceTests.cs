using FluentAssertions;
using Xunit;
using Vcr.Core;

namespace Vcr.Test.Example;

public class ExampleServiceTests
{
    [Fact]
    public async Task IpShouldBeXXX()
    {
        using var vcr = new Recorder();

        var exampleSvc = new ExampleService(vcr.HttpClientFactory);
        var ip = await exampleSvc.GetIp();

        ip.Should().Be("193.161.64.241");
    }

    [Fact]
    public async Task IpShouldBe123_123_123_123()
    {
        using var vcr = new Recorder("IpShouldBe123.123.123.123.har");

        var exampleSvc = new ExampleService(vcr.HttpClientFactory);
        var ip = await exampleSvc.GetIp();

        ip.Should().Be("123.123.123.123");
    }
}