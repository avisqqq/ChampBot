namespace ChampBot.Domain;

public record Hero(
    int id,
    int games,
    int wins,
    int losses
)
{
    public double WinRate => games > 0 ? (double)wins / games * 100 : 0;
}