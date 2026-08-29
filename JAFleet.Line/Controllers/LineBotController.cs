using Line.OpenApi.Messaging.Webhook;
using Line.OpenApi.Messaging.Webhook.Generated.Models;
using Microsoft.AspNetCore.Mvc;
using JAFleet.Line.Infrastructure;
using JAFleet.Commons.Data;

namespace JAFleet.Line.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private readonly JAFleetContext _context;
        private readonly IServiceScopeFactory _services;
        private readonly WebhookRequestParser _parser;
        public LineBotController(JAFleetContext context, IServiceScopeFactory serviceScopeFactory, WebhookRequestParser parser)
        {
            _context = context;
            _services = serviceScopeFactory;
            _parser = parser;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            //署名検証は生のリクエストボディに対して行うため、モデルバインドせずに読み取る
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            var body = ms.ToArray();
            var signature = Request.Headers["x-line-signature"].FirstOrDefault() ?? string.Empty;

            CallbackRequest callback;
            try
            {
                //署名検証とデシリアライズをまとめて行う
                callback = await _parser.ParseAsync(body, signature);
            }
            catch (WebhookSignatureException)
            {
                return Unauthorized();
            }
            catch (WebhookPayloadException)
            {
                return BadRequest();
            }

            var app = new LineBotApp(LineMessagingClientManager.GetInstance(),_context, _services);
            await app.RunAsync(callback.Events ?? []);
            return new OkResult();
        }
    }
}
