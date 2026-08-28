namespace JAFleet.Line.Infrastructure
{
    public class HttpClientManager
    {
        private static HttpClient _client = new();

        public static HttpClient GetInstance()
        {
            return _client;
        }
    }
}
