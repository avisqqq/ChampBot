using ChampBot.Domain;
using Telegram.Bot.Types.ReplyMarkups;

namespace ChampBot.Bot;

public class Keyboards
{
    public static InlineKeyboardMarkup Main(UserConfig u)
    {
        return new(new[]
            {
               InlineKeyboardButton.WithCallbackData("🔄 Today", "today"),
            });
    }
}