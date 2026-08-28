using EnumStringValues;
using JAFleet.Commons.Scraping;
using JAFleet.Commons.Constants;
using JAFleet.Commons.EF;
using JAFleet.Line.Constants;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Noobow.Commons.Constants;
using Noobow.Commons.Utils;

namespace JAFleet.Line
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        private readonly JAFleetContext _context;
        private readonly IServiceScopeFactory _services;

        public LineBotApp(LineMessagingClient lineMessagingClient,JAFleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            this.messagingClient = lineMessagingClient;
            _context = context;
            _services = serviceScopeFactory;
        }

        /// <summary>
        /// フォローイベント
        /// （使用方法を返信して、ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnFollowAsync(FollowEvent ev)
        {
            await messagingClient.ReplyMessageAsync(ev.ReplyToken, ReplyMessage.FOLLOW_MESSAGE);

            //ユーザーに返信してからログを処理
            DateTime? followDate = DateTime.Now;
            string userId = ev.Source.UserId;

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
        protected override async Task OnUnfollowAsync(UnfollowEvent ev)
        {
            await Task.Run(() =>
            {
                DateTime? unfollowDate = DateTime.Now;
                string userId = ev.Source.UserId;

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
        /// <returns></returns>
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId, ((TextEventMessage)ev.Message).IsCheck);
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
            var reply = new List<ISendMessage>();

            var compareTarget = DateTime.Now;

            if (userMessage.Contains(CommandConstant.MESSAGE))
            {
                await messagingClient.ReplyMessageAsync(replyToken, [ReplyMessage.SEND_MESSAGE]);
                string messageBody = userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty);
                var m = new Message
                {
                    Sender = userId,
                    MessageDetail = messageBody,
                    MessageType = Commons.Constants.MessageType.LINE,
                    RecieveDate = DateTime.Now
                };
                _context.Messagess.Add(m);
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

                    reply.Add(new TextMessage(aircraftInfo));
                }

                if (!string.IsNullOrEmpty(av?.PhotoDirectLarge))
                {
                    reply.Add(new ImageMessage(av.PhotoDirectLarge, av.PhotoDirectSmall));
                }
                else if (av ==null)
                {
                    var ap2 = await AircraftDataExtractor.GetAircraftPhotoAnyRegistrationNumberAsync(upperedReg!, _context);
                    if (ap2 != null)
                    {
                        reply.Add(new ImageMessage(ap2.PhotoDirectLarge, ap2.PhotoDirectSmall));
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
                        lineuser.UserName = profile.DisplayName;
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
                        UserName = profile.DisplayName,
                        LastAccess = processDate,
                        ProfileUpdateTime = processDate
                    };
                    _context.LineUsers.Add(user);
                }
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// プロフィールとプロフィール画像を取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns></returns>
        private async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            UserProfile retprofile;

            retprofile = await messagingClient.GetUserProfileAsync(userId);

            return retprofile;
        }

    }
}