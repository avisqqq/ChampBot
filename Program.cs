using ChampBot.Bot;
using ChampBot.Common;
using ChampBot.Common.Time;
using ChampBot.Domain;
using ChampBot.Infra;
using ChampBot.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using ChampBot.Bot.Discord;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
      cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
      cfg.AddJsonFile("appsettings.dev.json", optional: true, reloadOnChange: true);

      cfg.AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
      // --- Telegram Config ---
      var token = ctx.Configuration["TelegramBotToken"];
      var tzId = ctx.Configuration["TimeZoneId"] ?? "Europe/Kyiv";
      var steam64Str = ctx.Configuration["Steam:SteamId64"];
      if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(steam64Str))
        throw new InvalidOperationException("TelegramBotToken and Steam:SteamId64 must be set in appsettings.json");
      if (!ulong.TryParse(steam64Str, out var steam64))
        throw new InvalidOperationException("Steam:SteamId64 must be an unsigned integer string.");
      // --- Discord Config ---
      var discordToken = ctx.Configuration["Discord:Token"];
      var devGuildIdStr = ctx.Configuration["Discord:DevGuildId"];
      if (string.IsNullOrWhiteSpace(discordToken) || string.IsNullOrWhiteSpace(devGuildIdStr))
        throw new InvalidOperationException("Discord:Token or Discord:DevGuildIdStr must be set in appesettings.json");
      // --- EF Core ---
      services.AddDbContext<BotDb>(o => o.UseSqlite("Data Source=bot.db"));


      // --- Common/Domain ---
      services.AddSingleton<IClock>(new SystemClock(tzId));
      services.AddSingleton(new BotSettings(steam64));
      services.AddSingleton<Service>();

      // --- Infra ---
      services.AddSingleton<OpenDotaClient>();

      // --- Telegram ---
      services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
      services.AddHostedService<TelegramPollingService>();
      // ---Discord ---

      services.AddSingleton(new DiscordOptions (discordToken, devGuildIdStr));

      services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
      {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.DirectMessages,
        AlwaysDownloadUsers = false,
        LogGatewayIntentWarnings = false
      }));
      services.AddSingleton(sp => 
        new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));
      
      services.AddSingleton(sp => new InteractionModuleCatalog(Assembly.GetExecutingAssembly()));

      services.AddHostedService<DiscordHostedService>();
    }).Build();

using (var scope = host.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<BotDb>();
  db.Database.EnsureCreated();
}
await host.RunAsync();
