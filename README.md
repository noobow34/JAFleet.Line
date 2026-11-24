# JAFleet.Line

JA-Fleet の LINE 公式アカウントのボット（Webhook）。レジを送ると機体の情報を返す。
DB と機体の情報は JAFleet と同じもの（`JAFleet.Commons`）を使う。

## 返信

| 送られてきたもの | 返信 |
| --- | --- |
| JA-Fleet に登録のあるレジ（`JA` は省略してよい） | 下の3通 |
| JA-Fleet に登録の無いレジ | airliners.net で写真を探して画像を返す。無ければ「見つかりませんでした」（※見直し予定） |
| 「メッセージ」を含む文 | 受け付けた旨を返し、DB（`message`）と Slack に送る |
| 「検索方法」を含む文 | 検索の使い方 |

**登録のある機体の3通**（`Infrastructure/AircraftReply.cs`）

1. 機体情報。見出しにレジ・型式・航空会社、そのあとに値のある項目だけ（運用状況・登録年月日・抹消年月日・製造番号・
   コンフィグ・Wi-Fi・特別塗装・備考）。日付は `yyyy/MM/dd`
2. JA-Fleet の機体詳細のリンク（`https://ja-fleet.noobow.me/AD/<レジ>`）
3. Flightradar24 の機体ページのリンク（`https://www.flightradar24.com/data/aircraft/<レジの小文字>`）

リンクは1通に1つずつにして、LINE がそれぞれにリンクのプレビュー（OGP のサムネ）を付けられるようにしている。
写真は FR24 のページで見てもらう。以前は DB の JetPhotos の画像（`aircraft_photo`）を画像メッセージで送っていたが、
JetPhotos の規約は写真をほかで使う許可を出していない（著作権は撮影者）のでやめた。

### 登録の無いレジ（見直し予定）

airliners.net の `/search` を使っているが、airliners.net は robots.txt で全ボットに `/search` を禁止し、
ボットのアクセスは有料（TollBit）に回している。代わりを決めるまでの暫定。
