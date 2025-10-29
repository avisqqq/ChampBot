using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ChampBot.Common;
using ChampBot.Common.Time;
using ChampBot.Domain;
using ChampBot.Infra;
using ChampBot.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ChampBot.Bot.Discord;

public class DiscordCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IServiceProvider _services;
    private readonly BotSettings _settings;
    private readonly Service _challengeService;
    private readonly IClock _clock;

    public DiscordCommands(
        IServiceProvider services,
        BotSettings settings,
        Service challengeService,
        IClock clock
        )
    {
        _services = services;
        _settings = settings;
        _challengeService = challengeService;
        _clock = clock;
    }

    private static async Task<UserConfig> EnsureUserRow(BotDb db, BotSettings settings, CancellationToken ct)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.SteamId64 == settings.SteamID64, ct);
        if (u == null)
        {
            u = new UserConfig
            {
                ChatId = 0,
                TelegramUserid = 0,
                SteamId64 = settings.SteamID64,
                DotaAccountId32 = settings.Dota2Account32
            };
            db.Users.Add(u);
            await db.SaveChangesAsync(ct);
        }
        return u;
    }
    
    // /start : sends a small help
    [SlashCommand("start", "Show available commands and current state.")]
    public async Task StartAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDb>();
        var ct = CancellationToken.None;
        var u = await EnsureUserRow(db,  _settings, ct);
        var eb = new EmbedBuilder()
            .WithTitle("Commands")
            .WithDescription($"Use the slash commands below.")
            .AddField("Today", "/today", true);
        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }

    [SlashCommand("today", "Show today's games/wins/losses and remaining")]
    public async Task TodayAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDb>();
        var ct = CancellationToken.None;
        var api = scope.ServiceProvider.GetRequiredService<OpenDotaClient>();
        
        var u = await EnsureUserRow(db, _settings, ct);
        var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: 7, ct);
        await db.SaveChangesAsync();

        var today = _challengeService.ComputeToday(recent);

        var msg =
            $"**TODAY**\n" +
            $"Games: {today.Games}\n" +
            $"Wins: {today.Wins}\n" +
            $"Losses {today.Losses}\n";
        await RespondAsync(msg);
    }
}