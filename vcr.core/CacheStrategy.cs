using System.Text;

namespace Vcr.Core;

public record CacheKey(int HashCode, string Value);

public class DefaultCacheStrategy
{
    private readonly string[] varyHeaders = Array.Empty<string>();

    public DefaultCacheStrategy() { }
    public DefaultCacheStrategy(IEnumerable<string> varyHeaders)
    {
        this.varyHeaders = varyHeaders.Order().ToArray();
    }


    public CacheKey GetCacheKey(HttpRequestMessage request)
    {
        var value = new StringBuilder();
        var hash = HashCode.Combine(request.Method, request.RequestUri);

        value.AppendLine($"Method: {request.Method}");
        value.AppendLine($"Url: {request.RequestUri}");

        foreach (var header in varyHeaders)
        {
            if (request.Headers.TryGetValues(header, out var values))
            {
                hash = HashCode.Combine(hash, header);
                foreach (var v in values)
                {
                    hash = HashCode.Combine(hash, v);
                    value.AppendLine($"{header}:{value}");
                }
            }
        }

        return new CacheKey(hash, value.ToString());
    }
}