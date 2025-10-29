using ChampBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChampBot.Infra.Persistence;


public class BotDb(DbContextOptions<BotDb> options) : DbContext(options)
{
    public DbSet<UserConfig> Users => Set<UserConfig>();
}