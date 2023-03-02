namespace Vcr.Core.HAR.Version1_2;

public class HttpArchive
{
    public Log Log { get; set; } = new();
}

public class Log
{
    public string Version { get; set; } = "1.2";
    public Creator Creator { get; set; } = new();
    public List<Page> Pages { get; set; } = new();
    public List<Entry> Entries { get; set; } = new();
}

public class Creator
{
    public Creator() : this("Unknown Creator", "0.0.0") { }
    public Creator(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; set; }
    public string Version { get; set; }
}

public class Entry
{
    public DateTimeOffset StartedDateTime { get; set; }
    public long Time { get; set; }
    public Request Request { get; set; } = new();
    public Response Response { get; set; } = new();
    public Cache Cache { get; set; }
    public Timings Timings { get; set; }
    public string Pageref { get; set; }
}

public class Cache
{
    public CacheState BeforeRequest { get; set; }
    public CacheState AfterRequest { get; set; }
    public string Comment { get; set; }
}

public partial class CacheState
{
    public DateTimeOffset Expires { get; set; }
    public string LastAccess { get; set; }
    public string ETag { get; set; }
    public long HitCount { get; set; } = 0;
    public string Comment { get; set; } = "";
}

public class Cookie
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Path { get; set; }
    public string Domain { get; set; }
    public DateTimeOffset Expires { get; set; }
    public bool Httponly { get; set; } = false;
    public bool Secure { get; set; } = false;
    public string Comment { get; set; } = "";
}

public class Request
{
    public string Method { get; set; }
    public Uri Url { get; set; }
    public string HttpVersion { get; set; }
    public List<Header> Headers { get; set; }
    public List<Header> QueryString { get; set; }
    public List<Cookie> Cookies { get; set; }
    public long HeadersSize { get; set; }
    public long BodySize { get; set; }
    public PostData? PostData { get; set; }
}

public class PostData
{
    public string MimeType { get; set; }
    public List<PostParam>? Params { get; set; }
    public string Text { get; set; }
    public string? Comment { get; set; }
}
public class PostParam
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string? FileName { get; set; }
    public string ContentType { get; set; }
}

public class Header
{
    public Header()
    {
    }

    public Header(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}

public class Response
{
    public int Status { get; set; }
    public string StatusText { get; set; }
    public string HttpVersion { get; set; }
    public List<Header> Headers { get; set; }
    public List<Cookie> Cookies { get; set; }
    public Content Content { get; set; }
    public string RedirectUrl { get; set; }
    public long HeadersSize { get; set; }
    public long BodySize { get; set; }
}

public class Content
{
    public long Size { get; set; }
    public string? MimeType { get; set; }
    public string Text { get; set; }
    public long? Compression { get; set; }
    public string? Encoding { get; set; }

    public byte[] GetBytes()
    {
        return Encoding?.ToLower() switch
        {
            "base64" => Convert.FromBase64String(Text),
            _ => System.Text.Encoding.UTF8.GetBytes(Text)
        };
    }
}

public class Timings
{
    public long Blocked { get; set; }
    public long Dns { get; set; }
    public long Connect { get; set; }
    public long Send { get; set; }
    public long Wait { get; set; }
    public long Receive { get; set; }
    public long Ssl { get; set; }
}

public class Page
{
    public DateTimeOffset StartedDateTime { get; set; }
    public string Id { get; set; }
    public Uri Title { get; set; }
    public PageTimings PageTimings { get; set; }
}

public class PageTimings
{
    public long OnContentLoad { get; set; }
    public long OnLoad { get; set; }
}