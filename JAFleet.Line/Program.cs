using JAFleet.Commons.EF;
using JAFleet.Line.Middleware;
using JAFleet.Line.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var config = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.json").Build();

Console.WriteLine($"SLACK_BOT_TOKEN:{Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN")?.Length ?? 0}");
string connectionString = Environment.GetEnvironmentVariable("JAFLEET_LINE_CONNECTION_STRING") ?? "";
Console.WriteLine($"JAFLEET_LINE_CONNECTION_STRING:{connectionString?.Length ?? 0}");
string lineChannelSecret = Environment.GetEnvironmentVariable("LINE_CHANNEL_SECRET") ?? "";
Console.WriteLine($"LINE_CHANNEL_SECRET:{lineChannelSecret?.Length ?? 0}");
Console.WriteLine($"LINE_CHANNEL_ACCESS_TOKEN:{Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN")?.Length ?? 0}");


builder.Services.AddDbContextPool<JAFleetContext>(
    options => options.UseNpgsql(connectionString).ConfigureWarnings(warnings =>
    {
        warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
        warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning);
    })
);
builder.Services.AddMvc().AddNewtonsoftJson();
builder.Services.Configure<AppSettings>(config);
builder.WebHost.UseUrls("http://localhost:6500");

var app = builder.Build();

app.UseLineValidationMiddleware(lineChannelSecret!);
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}"
);


app.Run();