
namespace ChampBot.Domain;

public class UserConfig
{

    //Telegram configuration
    public int Id { get; set; }
    public long TelegramUserid { get; set; }
    public long ChatId { get; set; }

    //Player Identeficator
    public ulong SteamId64 { get; set; }
    public int DotaAccountId32 { get; set; }

    public long LastSeenMatchId { get; set; }
}