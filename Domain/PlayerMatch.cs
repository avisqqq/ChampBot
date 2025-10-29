namespace ChampBot.Domain;

public record PlayerMatch(
    long match_id,
    double duration,
    bool radiant_win,
    int  player_slot,
    long start_time,
    int hero_id
    );
