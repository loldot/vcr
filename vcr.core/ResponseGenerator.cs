using Vcr.Core.HAR.Version1_2;

class ResponseGenerator
{
    private readonly Queue<Response> responses = new Queue<Response>();

    public Response? GetResponse()
    {
        if(responses.Count > 1) return responses.Dequeue();
        
        return responses.FirstOrDefault();
    }

    public void Add(Response response)
    {
        responses.Enqueue(response);
    }
}