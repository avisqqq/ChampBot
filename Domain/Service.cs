using ChampBot.Common.Time;

namespace ChampBot.Domain;

public sealed class Service
{
    private IClock _clock;
    public Service(IClock clock) => _clock = clock;

    public static bool IsWin(PlayerMatch m)
    {
        bool radiant = m.player_slot < 128;
        return (radiant && m.radiant_win) || (!radiant && !m.radiant_win);
    }

    public long TodayMidnightUnix()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, _clock.TimeZone).Date;
        var midnightUTC = TimeZoneInfo.ConvertTimeToUtc(local, _clock.TimeZone);
        return new DateTimeOffset(midnightUTC).ToUnixTimeSeconds();
    }

    public TodayStats ComputeToday(IEnumerable<PlayerMatch> recent)
    {
        var cutoff = TodayMidnightUnix();
        var today = recent.Where(m => m.start_time >= cutoff).ToList();
        int wins = today.Count(IsWin);
        return new TodayStats(today.Count, wins, today.Count - wins);
        ;
    }
    public int CalcMatches(IEnumerable<PlayerMatch> recent)
    {
        return recent.Count();
    }

    public List<Hero> WR(IEnumerable<PlayerMatch> recent)
    {
        return recent.GroupBy(m => m.hero_id)
            .Select(g =>
            {
                int wins = g.Count(m =>
                    (m.player_slot < 128 && m.radiant_win) || (m.player_slot >= 128 && !m.radiant_win));

                int losses = g.Count() - wins;

                return new Hero(id: g.Key, games: g.Count(), wins: wins, losses: losses);
            })
            .ToList();
    }

    public int wl_diff(IEnumerable<PlayerMatch> recent)
    {
        int wins = recent.Count(m =>
            (m.player_slot < 128 && m.radiant_win) || (m.player_slot >= 128 && !m.radiant_win));
        int losses = recent.Count() - wins;
        return wins - losses;
    }

    public double totalTime(IEnumerable<PlayerMatch> recent)
    {
        double total = recent.Sum(m => m.duration);
        return total / 3600.0;
    }
}