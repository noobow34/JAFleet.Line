using Line.OpenApi.Messaging;
using Line.OpenApi.Messaging.Generated.Api.Models;

namespace JAFleet.Line.Infrastructure
{
    /// <summary>
    /// Line.OpenApi.MessagingのMessagingClientに対する、よく使う操作のショートカット
    /// </summary>
    public static class MessagingClientExtensions
    {
        /// <summary>
        /// 応答メッセージを送信する
        /// </summary>
        public static Task<ReplyMessageResponse?> ReplyMessageAsync(this MessagingClient client, string replyToken, IEnumerable<Message> messages)
            => client.Api.V2.Bot.Message.Reply.PostAsync(new ReplyMessageRequest
            {
                ReplyToken = replyToken,
                Messages = [.. messages]
            });

        /// <summary>
        /// ユーザーのプロフィールを取得する
        /// </summary>
        public static Task<UserProfileResponse?> GetUserProfileAsync(this MessagingClient client, string userId)
            => client.Api.V2.Bot.Profile[userId].GetAsync();
    }
}
