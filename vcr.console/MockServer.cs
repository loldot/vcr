using System.Net.Http;
using Vcr.Core.HAR.Version1_2;

namespace Vcr.Console;

public class MockServer
{
    private Dictionary<Route, Response> routeData = new();
    public static MockServer Create(HttpArchive archive)
    {
        var mockServer = new MockServer();
        foreach(var entry in archive.Log.Entries)
        {
            var route = new Route(new HttpMethod(entry.Request.Method), entry.Request.Url.PathAndQuery);
            mockServer.routeData.Add(route, entry.Response);
        }
        return mockServer;
    }

    public Response? GetResponse(HttpMethod method, string relativeUrl) => GetResponse(new Route(method, relativeUrl));
    private Response? GetResponse(Route route) 
    {
        routeData.TryGetValue(route, out var response);
        return response;
    }

    record Route(HttpMethod Method, string relativeUrl);
}