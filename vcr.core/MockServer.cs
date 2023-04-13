using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;

public class MockServer
{
    private Dictionary<Route, Response> routeData = new();

    public MockServer() { }
    public MockServer(HttpArchive? archive)
    {
        if (archive is not null)
        {
            foreach (var entry in archive.Log.Entries)
            {
                var route = new Route(new HttpMethod(entry.Request.Method), entry.Request.Url.PathAndQuery);
                routeData.Add(route, entry.Response);
            }
        }
    }

    public Response? GetResponse(HttpMethod method, string relativeUrl) => GetResponse(new Route(method, relativeUrl));
    private Response? GetResponse(Route route)
    {
        routeData.TryGetValue(route, out var response);
        return response;
    }

    record Route(HttpMethod Method, string relativeUrl);
}