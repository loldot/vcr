using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Vcr.Console;
using Vcr.Core.HAR.Version1_2;

var handler = new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

var fileOption = new Option<FileInfo?>(
    name: "--file",
    description: "The http archive file (.har)");
fileOption.IsRequired = true;

var rootCommand = new RootCommand("VCR: A command line utility to interact with .har files.");

var replayCommand = new Command("replay", "Replay all requests from an archive file.");
replayCommand.AddOption(fileOption);
replayCommand.SetHandler(Replay, fileOption);
rootCommand.AddCommand(replayCommand);

var verifyCommand = new Command("verify", "Replay all requests from an archive file and verify that the server responds in the same way");
verifyCommand.AddOption(fileOption);
verifyCommand.SetHandler(Verify, fileOption);
rootCommand.AddCommand(verifyCommand);

var serveCommand = new Command("serve", "Create a mock http server that will accept requests matching method/relative path from requests in the .har file and replicate the responses.");
serveCommand.AddOption(fileOption);
serveCommand.SetHandler(async (context) =>
        {
            FileInfo fileOptionValue = context.ParseResult.GetValueForOption(fileOption);
            var token = context.GetCancellationToken();
            await Serve(fileOptionValue, token);
        });
rootCommand.AddCommand(serveCommand);

return await rootCommand.InvokeAsync(args);

HttpRequestMessage RecreateRequest(Request request)
{
    var httpRequestMessage = new HttpRequestMessage(
        new HttpMethod(request.Method),
        request.Url
    );

    foreach (var header in request.Headers)
    {
        if (header.Name.StartsWith("content-", StringComparison.InvariantCultureIgnoreCase)) continue;

        httpRequestMessage.Headers.Add(header.Name, header.Value);
    }

    if (request.PostData is not null)
    {
        httpRequestMessage.Content = new StringContent(request.PostData.Text);
        httpRequestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.PostData.MimeType); ;
    }

    return httpRequestMessage;
}

async Task Replay(FileInfo file)
{
    Console.WriteLine($"Replaying {file.FullName}");

    using var fs = File.OpenRead(file.FullName);
    using var http = new HttpClient(handler);

    var har = await JsonSerializer.DeserializeAsync<HttpArchive>(fs, options);
    foreach (var entry in har!.Log.Entries)
    {
        var request = RecreateRequest(entry.Request);
        var response = await http.SendAsync(request);

        LogInformation($"{entry.Request.Method.ToUpper()} {entry.Request.Url}");

        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}

async Task Verify(FileInfo file)
{
    LogInformation($"Replaying {file.FullName}");

    using var fs = File.OpenRead(file.FullName);
    using var http = new HttpClient(handler);

    var har = await JsonSerializer.DeserializeAsync<HttpArchive>(fs, options);

    int totalCount = 0, failedCount = 0;

    foreach (var entry in har!.Log.Entries)
    {
        var request = RecreateRequest(entry.Request);
        var response = await http.SendAsync(request);


        var areEqual = await CompareResponse(response, entry.Response);
        if (!areEqual)
        {
            failedCount++;
            LogError($"Response from {entry.Request.Url.AbsoluteUri} did not match log entry");
        }
        totalCount++;
    }

    Console.WriteLine();
    LogInformation($"Verification of {file.Name} completed ({totalCount - failedCount} / {totalCount} successful requests).");
    if (failedCount > 0)
    {
        LogError("Verification failed.");
    }
}

async Task Serve(FileInfo file, CancellationToken ct)
{
    const string prefix = "http://*:8080/";

    MockServer server;
    using (var fs = File.OpenRead(file.FullName))
    {
        var har = await JsonSerializer.DeserializeAsync<HttpArchive>(fs, options);
        server = MockServer.Create(har);
    }

    using var listener = new HttpListener();

    listener.Prefixes.Add(prefix);
    listener.Start();
    LogInformation($"Started listening on {prefix}");
    while (!ct.IsCancellationRequested)
    {
        var context = await listener.GetContextAsync();
        LogInformation($"{context.Request.HttpMethod}: {context.Request.Url}");

        var response = server.GetResponse(HttpMethod.Get, context.Request.Url!.PathAndQuery);
        if (response is null)
        {
            context.Response.StatusCode = 404;
        }
        else
        {
            byte[] data = response.Content.GetBytes();
            context.Response.ContentEncoding = Encoding.UTF8;
            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
        }

        context.Response.Close();
    }
    listener.Stop();
    LogInformation("Server stopped");
}

async Task<bool> CompareResponse(HttpResponseMessage actualResponse, Response expectedResponse)
{
    if ((int)actualResponse.StatusCode != expectedResponse.Status) return false;

    var responseContent = await actualResponse.Content.ReadAsByteArrayAsync();
    var expectedContent = expectedResponse.Content.GetBytes();

    return ((ReadOnlySpan<byte>)responseContent).SequenceEqual(expectedContent);
}


void LogInformation(string message)
{
    Console.WriteLine($"[INF] {message}");
}

void LogWarning(string warning)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Error.WriteLine($"[WRN] {warning}");
    Console.ResetColor();
}

void LogError(string errorMessage)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"[ERR] {errorMessage}");
    Console.ResetColor();
}
