using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChampBot.Bot.Discord;

public class DiscordHostedService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly DiscordOptions _opts;
    private readonly InteractionModuleCatalog _catalog;
    private readonly ILogger<DiscordHostedService> _log;

    public DiscordHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        DiscordOptions opts,
        InteractionModuleCatalog catalog,
        ILogger<DiscordHostedService> log
        )
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _opts = opts;
        _catalog = catalog;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += msg => { _log.LogInformation("[Discord] {Message}", msg.ToString()); return Task.CompletedTask; };
        _interactions.Log += msg => { _log.LogInformation("[Interactions] {Message}", msg.ToString()); return Task.CompletedTask; };

        _client.Ready += OnReady;
        _client.InteractionCreated += HandleInteraction;
        
        await _client.LoginAsync(TokenType.Bot, _opts.Token);
        await _client.StartAsync();
    }

    private async Task OnReady()
    {
        await _interactions.AddModulesAsync(_catalog.Assembly, _services);

        if (ulong.TryParse(_opts.DevGuildId, out var guildId) && guildId != 0)
        {
            await _interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
            _log.LogInformation("Registred Discord slash-commands to guild {GuildId}", guildId);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true);
            _log.LogInformation("Registred Discord slash-commands globally");
        }
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Handling interaction {interaction.Id}");
            if (interaction.Type is InteractionType.ApplicationCommand or InteractionType.MessageComponent)
                try
                {
                    await interaction.RespondAsync("Something went wrong.");
                }
                catch
                {
                    /* ingored */
                }

        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }
    
}