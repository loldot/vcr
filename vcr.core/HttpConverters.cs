using System.Net;
using System.Net.Http.Headers;
using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;

public static class HttpConverters
{
    public static HttpResponseMessage ToHttpResponseMessage(this Response response)
    {
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = (HttpStatusCode)response.Status,
            Content = new ByteArrayContent(response.Content.GetBytes())
        };

        httpResponse.Content.Headers.ContentLength = response.BodySize;
        httpResponse.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(response.Content.MimeType);
        
        foreach(var header in httpResponse.Headers)
        {
            httpResponse.Headers.Add(header.Key, header.Value);
        }

        return httpResponse;
    }
}