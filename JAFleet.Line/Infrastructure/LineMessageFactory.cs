using Line.OpenApi.Messaging.Generated.Api.Models;

namespace JAFleet.Line.Infrastructure
{
    /// <summary>
    /// 送信メッセージの生成ヘルパー
    /// Line.OpenApi.Messagingのメッセージオブジェクトは判別子(type)を自動設定しないため、ここで設定する
    /// </summary>
    public static class LineMessageFactory
    {
        public static TextMessage Text(string text)
            => new() { Type = "text", Text = text };

        public static ImageMessage Image(string? originalContentUrl, string? previewImageUrl)
            => new() { Type = "image", OriginalContentUrl = originalContentUrl, PreviewImageUrl = previewImageUrl };
    }
}
