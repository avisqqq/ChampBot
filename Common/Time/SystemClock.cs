namespace ChampBot.Common.Time;

public class SystemClock : IClock
{
    private readonly TimeZoneInfo _tz;
    public SystemClock(string tzID = "Europe/Kyiv") => _tz = TimeZoneInfo.FindSystemTimeZoneById(tzID);
    
    public DateTime UtcNow => DateTime.UtcNow;
    public TimeZoneInfo TimeZone => _tz;

}