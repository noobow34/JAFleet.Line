using JAFleet.Line.Infrastructure;
using Line.OpenApi.Messaging.Generated.Api.Models;

namespace JAFleet.Line.Constants
{
    public static class ReplyMessage
    {
        public static readonly Message[] FOLLOW_MESSAGE = new Message[]
        {
            LineMessageFactory.Text("フォローありがとうございます。\n" +
                                                "このアカウントのでは使用方法は以下の画像および説明をご確認ください。\n" +
                                                "・JA-Fleetサイトに登録されている飛行機は詳細情報と写真を検索できます\n" +
                                                "・JA-Fleetサイトに登録されていない場合は写真のみ検索できます\n" +
                                                "【検索例】\n" +
                                                "●JAレジ\n" +
                                                "801a,JA301J\n" +
                                                "（JAレジは、'JA'をつけなくてもOK）\n" +
                                                "●それ以外\n" +
                                                "80-1111,n501dn, A6-BLA\n" +
                                                "（省略不可）\n" +
                                                "※すべて大文字小文字区別せず"),
            LineMessageFactory.Text("管理人にメッセージを送るには1行目に「メッセージ」と書いて、2行目以降にメッセージを入力してください。"),
            LineMessageFactory.Image("https://ja-fleet-line.noobow.me/howtouse.jpg", "https://ja-fleet-line.noobow.me/howtouse.jpg")
        };

        public static readonly TextMessage SEND_MESSAGE = LineMessageFactory.Text("メッセージを受信しました。\n" +
                                                                            "ありがとうございましたm(_ _)m\n" +
                                                                            "なお、仕組み上LINEでの返信はできないため、\n" +
                                                                            "返信が必要な場合はメールアドレスやTwitterのアカウントなどを記載ください。");

        public static readonly TextMessage NOT_FOUND = LineMessageFactory.Text("見つかりませんでした。\n" +
                                                                       "----------\n" +
                                                                        "検索方法を確認するには「検索方法」と入力してください。\n" +
                                                                        "管理人にメッセージを送るには1行目に「メッセージ」と書いて、2行目以降にメッセージを入力してください。");

        public static readonly TextMessage HOWTO_SEARCH = LineMessageFactory.Text("【検索対象】\n"+
                                                                "JA-Fleet登録データ：JAレジで運航中のもの、2018/09以降に抹消されてもの\n" +
                                                                "写真のみ検索：Jetphotosサイトに登録されているもの\n" +
                                                                "【検索例】\n" +
                                                                "●JAレジ\n" +
                                                                "801a,JA301J\n" +
                                                                "（JAレジは、'JA'をつけなくてもOK）\n" +
                                                                "●それ以外\n" +
                                                                "80-1111,n501dn, A6-BLA\n" +
                                                                "（省略不可）\n" +
                                                                "※すべて大文字小文字区別せず");
    }
}
