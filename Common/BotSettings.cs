namespace ChampBot.Common;

public class BotSettings
{
    public ulong SteamID64 { get; }
    public int Dota2Account32 { get; }

    public BotSettings(ulong steamId64)
    {
        SteamID64 = steamId64;
        Dota2Account32 = (int)(steamId64 - 76561197960265728UL);
    }
}