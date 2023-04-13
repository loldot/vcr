using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Vcr.Core.HAR.Version1_2;

namespace Vcr.Core;

public class HarBuilder
{
    private static string AssemblyName = Assembly.GetExecutingAssembly().FullName;
    private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    private readonly HttpArchive archive = new HttpArchive();
    public HarBuilder()
    {
        archive.Log.Creator = new Creator(AssemblyName, AssemblyVersion);
    }

    public HarBuilder(HttpArchive? existingArchive)
    {
        if (existingArchive is not null)
        {
            this.archive = existingArchive;
        }
        this.archive.Log.Creator = new Creator(AssemblyName, AssemblyVersion);
    }

    public HarEntryBuilder WithEntry()
    {
        var builder = new HarEntryBuilder();
        archive.Log.Entries.Add(builder.Entry);
        return builder;
    }


    public async Task SaveToFile(string path)
    {
        if (!Path.GetExtension(path).Equals(".har")) path = path + ".har";

        var options = new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        using var fs = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(fs, this.archive, options);
    }


    public class HarEntryBuilder
    {
        public Entry Entry { get; } = new();

        public HarEntryBuilder WithRequest(HttpRequestMessage request)
        {
            Entry.Request.Method = request.Method.ToString();
            Entry.Request.Url = request.RequestUri!;

            if (request.Content is not null)
            {
                Entry.Request.BodySize = request.Content.Headers.ContentLength ?? 0;
                Entry.Request.PostData = new PostData
                {
                    MimeType = request.Content.Headers.ContentType.ToString(),
                    Text = request.Content.ReadAsStringAsync().Result,
                };
            }

            if (!string.IsNullOrEmpty(request.RequestUri.Query))
            {
                Entry.Request.QueryString = new List<Header>();
                var queryParams = HttpUtility.ParseQueryString(request.RequestUri.Query);
                foreach (var key in queryParams.AllKeys!)
                {
                    Entry.Request.QueryString.Add(new Header(key!, queryParams[key!]));
                }
            }

            return this;
        }

        public HarEntryBuilder WithResponse(HttpResponseMessage response)
        {
            Entry.Response.Status = (int)response.StatusCode;
            Entry.Response.StatusText = response.StatusCode.ToString();
            Entry.Response.HttpVersion = response.Version.ToString();

            Entry.Response.Headers = ParseHeaders(response.Headers);
            Entry.Response.Content = ParseContent(response.Content);
            return this;
        }

        private List<Header> ParseHeaders(HttpResponseHeaders headers)
        {
            return headers.Select(x => new Header
            {
                Name = x.Key,
                Value = string.Join(";", x.Value)
            }).ToList();
        }

        private Content ParseContent(HttpContent content)
        {
            var stringContent = content.ReadAsStringAsync().Result;
            var encoding = ParseEncoding(content.Headers.ContentEncoding);
            return new Content
            {
                Size = stringContent.Length,
                Compression = 0,
                Encoding = encoding,
                MimeType = content.Headers.ContentType?.ToString(),
                Text = stringContent
            };
        }

        private string ParseEncoding(ICollection<string> contentEncoding) => string.Join(',', contentEncoding);
    }
}