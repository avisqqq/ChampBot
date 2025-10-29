using System.Net.Http.Json;
using ChampBot.Domain;

namespace ChampBot.Infra;

public class OpenDotaClient
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.opendota.com/api/")
    };

    // === Player ===
    public Task<List<PlayerMatch>> GetMatchesAsync(int accountId32, int days, CancellationToken ct) =>
        _http.GetFromJsonAsync<List<PlayerMatch>>($"players/{accountId32}/matches?date={days}", ct)!;

    public async Task RefreshPlayerAsync(int accountId32, CancellationToken ct)
    {
        var res = await _http.PostAsync($"players/{accountId32}/refresh", content: null, ct);
        res.EnsureSuccessStatusCode();
    }

    // === Heroes ===
    public Task<List<HeroInfo>> GetHeroesAsync(CancellationToken ct) =>
        _http.GetFromJsonAsync<List<HeroInfo>>("heroes", ct)!;
}
