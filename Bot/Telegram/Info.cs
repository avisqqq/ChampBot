
namespace ChampBot.Telegram;

public class Info
{
    public static readonly (string Command, string Description)[] All =
    {
        ("/start", "Show main menu"),
        ("/calc <days>", "Count matches played in last N days"),
        ("/wr <days>", "Show hero win rate stats for N days"),
        ("/wl <days>", "Show total of played games(Games/MMR Diff) for N days"),
        ("/time <days>", "Show total playtime for N days"),
        ("/today", "Show today's stats"),
        ("/info", "Show this help message")
    };
    
}