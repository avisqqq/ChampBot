namespace ChampBot.Domain;

public record HeroInfo(
    int id,
    string name,
    string localized_name,
    string primary_attr,
    string attack_type,
    List<string> roles
);
