# ChampBot

ChampBot is a small student project — a learning bot for Dota 2 stats that integrates with Telegram and Discord and uses OpenDota for match data. This repository is intended for study and experimentation.

## Status
- Student project / learning exercise.
- Not intended for production use.
- Secrets (tokens, IDs) must be set before running.

## Required configuration
Set values either in `appsettings.dev.json` (recommended for local development, see .gitignore) or via environment variables.

Required keys (matching [Program.cs](Program.cs)):
- `TelegramBotToken` — Telegram bot token.
- `Steam:SteamId64` — your 64-bit Steam ID (unsigned integer string).
- `TimeZoneId` — optional, default is `Europe/Kyiv`.
- `Discord:Token` — Discord bot token.
- `Discord:DevGuildId` — optional guild ID for registering dev slash commands.

Example `appsettings.dev.json` (DO NOT commit):
```json
{
  "TelegramBotToken": "YOUR_TELEGRAM_TOKEN",
  "Steam": {
    "SteamId64": "7656119XXXXXXXXXX"
  },
  "TimeZoneId": "Europe/Kyiv",
  "Discord": {
    "Token": "YOUR_DISCORD_TOKEN",
    "DevGuildId": "123456789012345678"
  }
}
```
