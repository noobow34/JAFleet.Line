using JAFleet.Commons.Data;
using JAFleet.Line.Infrastructure;
using Line.OpenApi.Messaging.Webhook.Generated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Noobow.Commons.Extensions;
using System.Diagnostics;

namespace JAFleet.Line.Controllers
{
    public class CheckController : Controller
    {
        private readonly JAFleetContext _context;
        private readonly IServiceScopeFactory _services;

        public CheckController(JAFleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _services = serviceScopeFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Content(_context.AircraftViews.Count().ToString());
        }

        [HttpPost]
        public async Task<IActionResult> IndexAsync()
        {
            int count = _context.Aircrafts.Count();
            int randomIndex = new Random().Next(count);
            Stopwatch sw = new();
            sw.Start();

            Aircraft a = _context.Aircrafts.AsNoTracking()
                .Skip(randomIndex)
                .Take(1)
                .First();

            //死活監視用のダミーイベント
            //（LINEからのWebhookではないため署名がなく、WebhookRequestParserは通せないので直接組み立てる）
            var events = new List<Event>
            {
                new MessageEvent
                {
                    Type = "message",
                    ReplyToken = "dummyToken",
                    Timestamp = 1462629479859,
                    Source = new UserSource { Type = "user", UserId = string.Empty },
                    Message = new TextMessageContent
                    {
                        Type = "text",
                        Id = "325708",
                        Text = a.RegistrationNumber
                    }
                }
            };

            var app = new LineBotApp(LineMessagingClientManager.GetInstance(), _context, _services);
            await app.RunAsync(events, isCheck: true);

            sw.Stop();
            this.JournalWriteLine($"Check:{a.RegistrationNumber!}:{sw.Elapsed}");

            return new OkResult();
        }
    }
}
