using System.Text;
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
    public static class AircraftReply
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
    }
}
