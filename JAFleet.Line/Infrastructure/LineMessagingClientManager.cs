using Line.OpenApi.Core.Authentication;
using Line.OpenApi.Messaging;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http.Headers;
using System.Text;

namespace JAFleet.Line.Infrastructure
{
    public static class LineMessagingClientManager
    {
        private static MessagingClient? _client;

        public static MessagingClient GetInstance()
        {
            if(_client == null)
            {
                string channelAccessToken = Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN") ?? "";
#if DEBUG
                //デバッグ時はローカルのスタブ（line-simulator）に向ける
                //HttpClientのBaseAddressがKiotaのベースURLになる
                var httpClient = new HttpClient(new SimulatorResponseHandler(new HttpClientHandler()))
                {
                    BaseAddress = new Uri("http://localhost:8080")
                };
                var authProvider = new BaseBearerTokenAuthenticationProvider(
                    new StaticChannelAccessTokenProvider(channelAccessToken, ["localhost"]));
                _client = new MessagingClient(authProvider, httpClient);
#else
    _client = MessagingClient.CreateWithStaticToken(channelAccessToken);
#endif
            }
            return _client;
        }

#if DEBUG
        /// <summary>
        /// line-simulator向けの応答補正
        /// </summary>
        /// <remarks>
        /// line-simulatorはreplyに対してres.sendStatus(200)（text/plainの"OK"）を、
        /// profileなどの転送に対してres.send(body)（text/html）を返す。
        /// KiotaはContent-Typeを見てデシリアライズするため、そのままでは
        /// InvalidOperationException（xxx does not support structured data）になる。
        /// 本物のLINE APIはapplication/jsonを返すのでリリースビルドでは不要。
        /// </remarks>
        private sealed class SimulatorResponseHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
        {
            private const string JsonMediaType = "application/json";

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode
                    || response.Content.Headers.ContentType?.MediaType is null or JsonMediaType)
                {
                    return response;
                }

                var body = (await response.Content.ReadAsStringAsync(cancellationToken)).TrimStart();
                //中身がJSONならContent-Typeだけ直す（profileなどLINE APIからの転送）
                //JSONでなければ空のJSONに差し替える（replyの"OK"など）
                var json = body.StartsWith('{') || body.StartsWith('[') ? body : "{}";
                response.Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue(JsonMediaType));
                return response;
            }
        }
#endif
    }
}
