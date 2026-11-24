using System.Text;
using System.Text.RegularExpressions;
using JAFleet.Commons.Data;
using JAFleet.Commons.Util;

namespace JAFleet.Line.Infrastructure
{
    /// <summary>
    /// JA-Fleet に登録のある機体への返信。
    /// ① 機体情報（値のある項目だけ）② JA-Fleet の機体詳細のリンク ③ Flightradar24 の機体ページのリンク、の3通。
    /// リンクは1通に1つずつにして、LINE がそれぞれにリンクのプレビュー（OGP のサムネ）を付けられるようにする。
    /// 写真は FR24 のページで見てもらう（JetPhotos の画像を直接送るのはやめた。撮影者の許可が無いため）。
    /// </summary>
    public static partial class AircraftReply
    {
        public const string JAFLEET_DETAIL_URL = "https://ja-fleet.noobow.me/AD/";
        public const string FR24_AIRCRAFT_URL = "https://www.flightradar24.com/data/aircraft/";

        public static List<string> BuildTexts(AircraftViewBase av)
        {
            string reg = av.RegistrationNumber ?? string.Empty;
            return
            [
                BuildInfo(av),
                $"JA-Fleet で詳しく見る\n{JAFLEET_DETAIL_URL}{Uri.EscapeDataString(reg)}",
                $"Flightradar24 で写真と飛行履歴を見る\n{FR24_AIRCRAFT_URL}{Uri.EscapeDataString(reg.ToLowerInvariant())}",
            ];
        }

        /// <summary>見出し（レジ・型式・航空会社）のあとに、値のある項目だけを並べる</summary>
        public static string BuildInfo(AircraftViewBase av)
        {
            var sb = new StringBuilder();
            sb.Append("✈ ").Append(av.RegistrationNumber);
            string? type = av.TypeDetailName ?? av.TypeName;
            string head = string.Join(" / ", new[] { type, av.AirlineNameJpShort }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (head.Length > 0)
            {
                sb.Append('\n').Append(head);
            }
            sb.Append('\n');
            Line(sb, "運用状況", av.Operation);
            Line(sb, "登録年月日", av.RegisterDate == null ? null : JaDateUtil.Format(av.RegisterDate));
            Line(sb, "抹消年月日", av.DeregisterDate == null ? null : JaDateUtil.Format(av.DeregisterDate));
            Line(sb, "製造番号", av.SerialNumber);
            Line(sb, "コンフィグ", av.SeatConfig);
            Line(sb, "Wi-Fi", av.Wifi);
            Line(sb, "特別塗装", av.SpecialLivery);
            Line(sb, "備考", av.Remarks);
            return sb.ToString().TrimEnd('\n');
        }

        private static void Line(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                sb.Append('\n').Append(label).Append('：').Append(value.Trim());
            }
        }

        /// <summary>
        /// JA-Fleet に登録の無いレジへの返信（説明と FR24 のリンクの2通）。レジとして読めない入力なら null（「見つかりませんでした」を返す）。
        /// 写真の画像は送らない。LINE は画像をサーバーで預かるうえ、撮影者の表記もリンクも付けられないので、
        /// どの写真サイトの条件も満たせない。FR24 のページなら写真と飛行履歴が見られる。
        /// </summary>
        public static List<string>? BuildUnregisteredTexts(string input)
        {
            string? reg = NormalizeUnregistered(input);
            if (reg == null)
            {
                return null;
            }
            return
            [
                $"{reg} は JA-Fleet に登録がありません（本邦機ではないか、まだ登録前・記録より前の機体です）。\n写真と飛行履歴は Flightradar24 で見られます。",
                $"Flightradar24 で写真と飛行履歴を見る\n{FR24_AIRCRAFT_URL}{Uri.EscapeDataString(reg.ToLowerInvariant())}",
            ];
        }

        /// <summary>
        /// 登録の無いレジの入力を、リンクに使うレジにする。
        /// 「801A」のような JA を省いた本邦機の書き方なら JA を付け、それ以外（N501DN・A6-BLA・80-1111 など）は入力のまま。
        /// 英数字とハイフン以外を含む・長すぎる入力は null
        /// </summary>
        public static string? NormalizeUnregistered(string input)
        {
            string s = input.Trim().ToUpperInvariant();
            if (!RegLike().IsMatch(s))
            {
                return null;
            }
            return JaSuffix().IsMatch(s) ? "JA" + s : s;
        }

        [GeneratedRegex(@"^[A-Z0-9][A-Z0-9-]{1,9}$")]
        private static partial Regex RegLike();

        /// <summary>JA を省いた本邦機の記号（数字3〜4桁＋英字0〜2文字、または数字2桁＋英字2文字）</summary>
        [GeneratedRegex(@"^(\d{3,4}[A-Z]{0,2}|\d{2}[A-Z]{2})$")]
        private static partial Regex JaSuffix();
    }
}
