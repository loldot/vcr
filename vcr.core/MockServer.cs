using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;

public class MockServer
{
    private Dictionary<Route, ResponseGenerator> routeData = new();

    public RoutingModes RoutingMode { get; }

    public enum RoutingModes
    {
        Absolute,
        PathAndQuery
    }

    public MockServer(RoutingModes routingMode = RoutingModes.PathAndQuery) 
    { 
        RoutingMode = routingMode;
    }
    public MockServer(HttpArchive? archive, RoutingModes routingMode = RoutingModes.PathAndQuery)
    {
        RoutingMode = routingMode;

        if (archive is not null)
        {
            foreach (var entry in archive.Log.Entries)
            {
                var path = this.RoutingMode switch
                {
                    RoutingModes.Absolute => entry.Request.Url.AbsoluteUri,
                    RoutingModes.PathAndQuery => entry.Request.Url.PathAndQuery,
                    _ => throw new NotImplementedException()
                };

                var route = new Route(new HttpMethod(entry.Request.Method), path);
                
                if(!routeData.ContainsKey(route))
                {
                    routeData[route] = new ResponseGenerator();
                }

                routeData[route].Add(entry.Response);
            }
        }
    }

    public Response? GetResponse(HttpMethod method, string url) => GetResponse(new Route(method, url));
    private Response? GetResponse(Route route)
    {
        routeData.TryGetValue(route, out var response);
        return response?.GetResponse();
    }

    record Route(HttpMethod Method, string url);
}