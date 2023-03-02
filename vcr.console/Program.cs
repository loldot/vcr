using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Vcr.Console;
using Vcr.Core;
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

var url = new Option<Uri>("--url");
var recordCommand = new Command("record", "record a request to an http server");
recordCommand.AddOption(fileOption);
recordCommand.AddOption(url);
recordCommand.SetHandler(Record, url, fileOption);
rootCommand.AddCommand(recordCommand);

var addressOption = new Option<string?>(
    name: "--address", 
    description: "address to listen on: i.e. http://localhost:9000/ (default) http://*:8000/");
addressOption.SetDefaultValue("http://localhost:9000");

var serveCommand = new Command("serve", "Create a mock http server that will accept requests matching method/relative path from requests in the .har file and replicate the responses.");
serveCommand.AddOption(fileOption);
serveCommand.AddOption(addressOption);

serveCommand.SetHandler(async (context) =>
        {
            FileInfo fileOptionValue = context.ParseResult.GetValueForOption(fileOption)!;
            string addressOptionValue = context.ParseResult.GetValueForOption(addressOption)!;
            var token = context.GetCancellationToken();
            await Serve(fileOptionValue, addressOptionValue, token);
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

async Task Serve(FileInfo file, string address, CancellationToken ct)
{
    MockServer server;
    using (var fs = File.OpenRead(file.FullName))
    {
        var har = await JsonSerializer.DeserializeAsync<HttpArchive>(fs, options);
        server = MockServer.Create(har);
    }

    using var listener = new HttpListener();


    address = address.EndsWith('/') ? address : address + '/';
    listener.Prefixes.Add(address);
    listener.Start();
    LogInformation($"Started listening on {address}");
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

async Task Record(Uri url, FileInfo file)
{
    var harBuilder = new HarBuilder();
    var httpclient = new HttpClient(handler);
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    var entry = harBuilder.WithEntry();

    entry.WithRequest(request);
    var response = await httpclient.SendAsync(request);
    entry.WithResponse(response);

    await harBuilder.SaveToFile(file.FullName);
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
