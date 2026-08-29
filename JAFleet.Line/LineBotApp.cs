using EnumStringValues;
using JAFleet.Commons.Scraping;
using JAFleet.Commons.Constants;
using JAFleet.Commons.Data;
using JAFleet.Line.Constants;
using JAFleet.Line.Infrastructure;
using Line.OpenApi.Messaging;
using Line.OpenApi.Messaging.Generated.Api.Models;
using Line.OpenApi.Messaging.Webhook.Generated.Models;
using Noobow.Commons.Constants;
using Noobow.Commons.Utils;
//JAFleet.Commons.Data.Message（DBエンティティ）と名前が衝突するため別名を付ける
using LineMessage = Line.OpenApi.Messaging.Generated.Api.Models.Message;

namespace JAFleet.Line
{
    internal class LineBotApp
    {
        private MessagingClient messagingClient { get; }
        private readonly JAFleetContext _context;
        private readonly IServiceScopeFactory _services;

        public LineBotApp(MessagingClient lineMessagingClient,JAFleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            this.messagingClient = lineMessagingClient;
            _context = context;
            _services = serviceScopeFactory;
        }

        /// <summary>
        /// Webhookイベントを種類ごとに振り分ける
        /// </summary>
        /// <param name="events">Webhookイベント</param>
        /// <param name="isCheck">死活監視からの呼び出しの場合はtrue（返信を行わない）</param>
        /// <returns></returns>
        public async Task RunAsync(IEnumerable<Event> events, bool isCheck = false)
        {
            foreach (var ev in events)
            {
                switch (ev)
                {
                    case MessageEvent message:
                        await OnMessageAsync(message, isCheck);
                        break;
                    case FollowEvent follow:
                        await OnFollowAsync(follow);
                        break;
                    case UnfollowEvent unfollow:
                        await OnUnfollowAsync(unfollow);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// フォローイベント
        /// （使用方法を返信して、ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        private async Task OnFollowAsync(FollowEvent ev)
        {
            await messagingClient.ReplyMessageAsync(ev.ReplyToken!, ReplyMessage.FOLLOW_MESSAGE);

            //ユーザーに返信してからログを処理
            DateTime? followDate = DateTime.Now;
            string userId = GetUserId(ev);

            var profile = await GetUserProfileAsync(userId);
            Log log = new()
            {
                LogDate = followDate,
                LogType = LogType.LINE_FOLLOW,
                LogDetail = profile?.DisplayName,
                UserId = userId
            };
            _context.Logs.Add(log);
            //LINE_USERにユーザーを記録
            var lineuser = _context.LineUsers.SingleOrDefault(p => p.UserId == userId);
            if(lineuser != null)
            {
                //すでに存在するユーザーの場合（再フォローの場合など）
                lineuser.FollowDate = followDate;
            }
            else
            {
                //新規ユーザー（普通こっち）
                LineUser user = new()
                {
                    UserId = userId,
                    UserName = profile?.DisplayName,
                    FollowDate = followDate,
                    ProfileUpdateTime = followDate
                };
                _context.LineUsers.Add(user);
            }
            _context.SaveChanges();

        }

        /// <summary>
        /// アンフォローイベント
        /// （ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        private async Task OnUnfollowAsync(UnfollowEvent ev)
        {
            await Task.Run(() =>
            {
                DateTime? unfollowDate = DateTime.Now;
                string userId = GetUserId(ev);

                //LINE_USERにユーザーを記録
                using var serviceScope = _services.CreateScope();
                using var context = serviceScope.ServiceProvider.GetService<JAFleetContext>();
                var lineuser = _context.LineUsers.SingleOrDefault(p => p.UserId == userId);
                if (lineuser != null)
                {
                    //ユーザーがLINE_USERテーブルに存在する場合
                    lineuser.UnfollowDate = unfollowDate;
                }
                else
                {
                    //ユーザーがLINE_USERテーブルに存在しない場合（初期のユーザーなど）
                    var unfollowedUser = new LineUser
                    {
                        UserId = userId,
                        UnfollowDate = unfollowDate
                    };
                }

                Log log = new()
                {
                    LogDate = unfollowDate,
                    LogType = LogType.LINE_UNFOLLOW,
                    LogDetail = (lineuser?.UserName) ?? userId,
                    UserId = userId
                };

                _context.Logs.Add(log);
                _context.SaveChanges();
            });

        }

        /// <summary>
        /// メッセージ発生
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="isCheck">死活監視からの呼び出しの場合はtrue</param>
        /// <returns></returns>
        private async Task OnMessageAsync(MessageEvent ev, bool isCheck)
        {
            switch (ev.Message)
            {
                case TextMessageContent text:
                    await HandleTextAsync(ev.ReplyToken!, text.Text!, GetUserId(ev), isCheck);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// テキストメッセージ処理
        /// </summary>
        /// <param name="replyToken"></param>
        /// <param name="userMessage"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task HandleTextAsync(string replyToken, string userMessage, string userId, bool isCheck)
        {
            string? upperedReg = userMessage.Split("\n")?[0].ToUpper();
            string? jaAddUpperedReg = upperedReg;
            string? firstLine = userMessage.Split("\n")?[0];
            var reply = new List<LineMessage>();

            var compareTarget = DateTime.Now;

            if (userMessage.Contains(CommandConstant.MESSAGE))
            {
                await messagingClient.ReplyMessageAsync(replyToken, [ReplyMessage.SEND_MESSAGE]);
                string messageBody = userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty);
                var m = new Commons.Data.Message
                {
                    Sender = userId,
                    MessageDetail = messageBody,
                    MessageType = Commons.Constants.MessageType.LINE,
                    RecieveDate = DateTime.Now
                };
                _context.Messages.Add(m);
                _context.SaveChanges();
                await SlackUtil.PostAsync(SlackChannelEnum.jafleet.GetStringValue(), "【JA-Fleet from LINE】\n" +
                                    "ユーザー：" + (_context.LineUsers.Find(userId)?.UserName ?? userId) + "\n" +
                                    userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty));
            }
            else if (userMessage.Contains(CommandConstant.HOWTOSEARCH))
            {
                await messagingClient.ReplyMessageAsync(replyToken, [ReplyMessage.HOWTO_SEARCH]);
            }
            else
            {
                //通常の利用（レジ）
                if (!(upperedReg!.StartsWith("JA")))
                {
                    jaAddUpperedReg = "JA" + upperedReg;
                }

                AircraftView? av = null;

                av = _context.AircraftViews.Where(p => p.RegistrationNumber == jaAddUpperedReg).FirstOrDefault();

                if (av != null)
                {
                    //JA-Fleetのデータベースで見つかった場合
                    string aircraftInfo = $"{av.RegistrationNumber} \n " +
                        $" 航空会社:{av.AirlineNameJpShort} \n " +
                        $" 型式:{av.TypeDetailName ?? av.TypeName} \n " +
                        $" 製造番号:{av.SerialNumber} \n " +
                        $" 登録年月日:{av.RegisterDate} \n " +
                        $" 運用状況:{av.Operation} \n " +
                        $" コンフィグ:{av.SeatConfig}\n " +
                        $" WiFi:{av.Wifi} \n " +
                        $" 特別塗装:{av.SpecialLivery} \n " +
                        $" 備考:{av.Remarks}";

                    reply.Add(LineMessageFactory.Text(aircraftInfo));
                }

                if (!string.IsNullOrEmpty(av?.PhotoDirectLarge))
                {
                    reply.Add(LineMessageFactory.Image(av.PhotoDirectLarge, av.PhotoDirectSmall));
                }
                else if (av ==null)
                {
                    var ap2 = await AircraftDataExtractor.GetAircraftPhotoAnyRegistrationNumberAsync(upperedReg!, _context);
                    if (ap2 != null)
                    {
                        reply.Add(LineMessageFactory.Image(ap2.PhotoDirectLarge, ap2.PhotoDirectSmall));
                    }
                    else
                    {
                        reply.Add(ReplyMessage.NOT_FOUND);
                    }
                }

                //チェック処理の場合は返信しない
                if (isCheck)
                {
                    return;
                }

                await messagingClient.ReplyMessageAsync(replyToken, reply);

                var processDate = DateTime.Now;
                //ユーザーに返信してからログを処理
                Log log = new()
                {
                    LogDate = processDate,
                    LogType = LogType.LINE,
                    LogDetail = firstLine,
                    UserId = userId
                };

                //Log登録
                _context.Logs.Add(log);

                //LineUser登録または更新
                var lineuser = _context.LineUsers.SingleOrDefault(p => p.UserId == userId);
                if (lineuser != null)
                {
                    //ユーザーのレコードがある
                    lineuser.LastAccess = processDate;
                    if(lineuser.ProfileUpdateTime == null ||(DateTime.Now - lineuser.ProfileUpdateTime) > new TimeSpan(7, 0, 0, 0))
                    {
                        //前回アクセスから1週間以上
                        var profile = await GetUserProfileAsync(userId);
                        lineuser.UserName = profile?.DisplayName;
                        lineuser.ProfileUpdateTime = processDate;
                    }
                }
                else
                {
                    //ユーザーのレコードがない
                    //ユーザー情報を取得
                    var profile = await GetUserProfileAsync(userId);
                    LineUser user = new()
                    {
                        UserId = userId,
                        UserName = profile?.DisplayName,
                        LastAccess = processDate,
                        ProfileUpdateTime = processDate
                    };
                    _context.LineUsers.Add(user);
                }
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// イベントの送信元ユーザーIDを取得
        /// </summary>
        /// <param name="ev">Webhookイベント</param>
        /// <returns></returns>
        private static string GetUserId(Event ev)
            => (ev.Source as UserSource)?.UserId ?? string.Empty;

        /// <summary>
        /// プロフィールとプロフィール画像を取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns></returns>
        private async Task<UserProfileResponse?> GetUserProfileAsync(string userId)
        {
            UserProfileResponse? retprofile;

            retprofile = await messagingClient.GetUserProfileAsync(userId);

            return retprofile;
        }

    }
}
