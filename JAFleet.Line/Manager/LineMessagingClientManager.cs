using Line.Messaging;

namespace JAFleet.Line.Manager
{
    public static class LineMessagingClientManager
    {
        private static LineMessagingClient? _client;

        public static LineMessagingClient GetInstance()
        {
            if(_client == null)
            {
                string channelAccessToken = Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN") ?? "";
#if DEBUG
                _client = new LineMessagingClient(channelAccessToken,"http://localhost:8080");
#else
    _client = new LineMessagingClient(channelAccessToken);
#endif
            }
            return _client;
        }
    }
}
