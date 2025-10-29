using ChampBot.Common.Time;
using ChampBot.Common;
using ChampBot.Domain;
using ChampBot.Infra;
using ChampBot.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;
using ChampBot.Telegram;

namespace ChampBot.Bot;

public class TelegramPollingService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceProvider _sp;
    private readonly BotSettings _settings;
    private readonly Service _challenge;
    private readonly IClock _clock;

    public TelegramPollingService(ITelegramBotClient bot,
        IServiceProvider sp,
        BotSettings settings,
        Service challenge,
        IClock clock)
    {
        _bot = bot;
        _sp = sp;
        _settings = settings;
        _challenge = challenge;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _bot.GetMe(cancellationToken: stoppingToken);
        Console.WriteLine($"Bot @{me.Username} is online.");
        _bot.StartReceiving(HandleUpdate, HandleError, new ReceiverOptions(), stoppingToken);
    }

    //EnsureUserRow Method
    private async Task<UserConfig> EnsureUserRow(BotDb db, long chatId, long? telegramUserId, CancellationToken ct)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.SteamId64 == _settings.SteamID64, ct);
        if (u == null)
        {
            u = new UserConfig
            {
                ChatId = chatId,
                TelegramUserid = telegramUserId ?? 0,
                SteamId64 = _settings.SteamID64,
                DotaAccountId32 = _settings.Dota2Account32
            };
            db.Users.Add(u);
        }
        else if (u.ChatId != chatId)
        {
            u.ChatId = chatId;
            await db.SaveChangesAsync(ct);
        }

        return u;
    }

    private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } cq)
        {
            _ = bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct); // fire-and-forget is fine
            var chatId = cq.Message!.Chat.Id;
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDb>();
            var api = scope.ServiceProvider.GetRequiredService<OpenDotaClient>();
            var user = await EnsureUserRow(db, chatId, cq.From.Id, ct);
            // Handle button presses (callbalck queries)
            switch (cq.Data)
            {
                case "today": await CmdToday(user, db, api, chatId, ct); break;
            }


        }

        //Handle text commands
        if (update.Type == UpdateType.Message && update.Message?.Text is { } text)
        {
            var chatId = update.Message.Chat.Id;

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDb>();
            var api = scope.ServiceProvider.GetRequiredService<OpenDotaClient>();
            var user = await EnsureUserRow(db, chatId, update.Message.From?.Id, ct);
            if (text.StartsWith("/start"))
            {
                await _bot.SendMessage(chatId,
                    "Dota Goal Bot *\nTap a button*:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: Keyboards.Main(user),
                    cancellationToken: ct);
                return;
            }

            if (text.Equals("/info", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("*Available commands:*");
                sb.AppendLine();

                foreach (var (cmd, desc) in Info.All)
                    sb.AppendLine($"{cmd} — {desc}");

                await _bot.SendMessage(chatId,
                    sb.ToString(),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: Keyboards.Main(user),
                    cancellationToken: ct
                );
            }
            if (text.StartsWith("/calc"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out var count) && count > 0)
                {
                    var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: count, ct);
                    int matches = _challenge.CalcMatches(recent);
                    await _bot.SendMessage(chatId, "Match count: " + matches, cancellationToken: ct);
                }
                else
                {
                    await _bot.SendMessage(chatId, "Bad requrest", cancellationToken: ct);
                }

                return;
            }
            if (text.StartsWith("/wr", StringComparison.OrdinalIgnoreCase))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int count = 0;
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsed))
                    count = parsed;
                await CmdWR(user, db, api, chatId, ct, count);
                return;
            }
            if (text.StartsWith("/time", StringComparison.OrdinalIgnoreCase))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int count = 0;
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsed))
                    count = parsed;
                await CmdTime(user, api, chatId, ct, count);
                return;
            }

            if (text.StartsWith("/wl", StringComparison.OrdinalIgnoreCase))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int count = 0;
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsed))
                    count = parsed;
                await CmdWRdiff(user, db, api, chatId, ct, count);
                return;
            }
            if (text.Equals("/today", StringComparison.OrdinalIgnoreCase)) { await CmdToday(user, db, api, chatId, ct); return; }
        }

    }
    //HadleUPDATE

    //HandleError
    private Task HandleError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Task.CompletedTask;
    }

    // Commands
    private async Task CmdToday(UserConfig u, BotDb db, OpenDotaClient api, long chatId, CancellationToken ct)
    {
        var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: 2, ct);
        await db.SaveChangesAsync(ct);
        var today = _challenge.ComputeToday(recent);
        var msg = "*TODAY*\n" +
                  $"Games: {today.Games}\n" +
                  $"Wins *{today.Wins}*\n" +
                  $"Losses *{today.Losses}*";
        await _bot.SendMessage(chatId, msg,
            parseMode: ParseMode.Markdown,
            replyMarkup: Keyboards.Main(u),
            cancellationToken: ct
        );
    }
    private async Task CmdWRdiff(UserConfig u, BotDb db, OpenDotaClient api, long chatId, CancellationToken ct, int d)
    {
        // 1. Fetch recent matches
        var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: d, ct);

        // 2. Calculate WR stats
        var heroesStats = _challenge.wl_diff(recent);
        string msg = $"Games diff: {heroesStats}\n" +
            $"MMR: {heroesStats * 25}";



        // 5. Send message
        await _bot.SendMessage(
        chatId,
        msg,
        parseMode: ParseMode.Markdown,
        replyMarkup: Keyboards.Main(u),
        cancellationToken: ct
    );
    }

    private async Task CmdWR(UserConfig u, BotDb db, OpenDotaClient api, long chatId, CancellationToken ct, int d)
    {
        // 1. Fetch recent matches
        var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: d, ct);

        // 2. Calculate WR stats
        var heroesStats = _challenge.WR(recent);

        // 3. Get all hero info from OpenDota API (so we can map hero_id -> hero_name)
        var heroes = await api.GetHeroesAsync(ct);
        var heroDict = heroes.ToDictionary(h => h.id, h => h.localized_name);
        int totalGames = heroesStats.Sum(h => h.games);
        int totalWins = heroesStats.Sum(h => h.wins);
        int totalLosses = heroesStats.Sum(h => h.losses);

        // 4. Build formatted message
        var sb = new StringBuilder();
        foreach (var hero in heroesStats
        .OrderByDescending(h => h.WinRate)
        .ThenByDescending(h => h.games))
        {
            if (!heroDict.TryGetValue(hero.id, out var name))
                name = $"Hero #{hero.id}";

            double wr = Math.Round(hero.WinRate, 2);
            sb.AppendLine($"**{name}** — {hero.wins}W / {hero.losses}L ({hero.games} games, {wr}%)");
        }
        sb.AppendLine($"**-----** — {totalWins}W / {totalLosses}L ({totalGames} games, {Math.Round(((double)totalWins / totalGames * 100), 2)}%)");
        sb.AppendLine("//Stat for last 30 days");


        // 5. Send message
        await _bot.SendMessage(
        chatId,
        sb.ToString(),
        parseMode: ParseMode.Markdown,
        replyMarkup: Keyboards.Main(u),
        cancellationToken: ct
    );
    }
    private async Task CmdTime(UserConfig u, OpenDotaClient api, long chatId, CancellationToken ct, int d)
    {
        var recent = await api.GetMatchesAsync(_settings.Dota2Account32, days: d, ct);
        double time = _challenge.totalTime(recent);
        await _bot.SendMessage(
                chatId,
                $"Total hours: {Math.Round(time, 2)} for {d} days \nHours/day: {Math.Round(time / d, 2)}",
                replyMarkup: Keyboards.Main(u),
                cancellationToken: ct
                );
    }
}
