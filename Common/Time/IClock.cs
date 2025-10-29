namespace ChampBot.Common.Time;

public interface IClock
{
    DateTime UtcNow { get; }
    TimeZoneInfo TimeZone{get; }
}