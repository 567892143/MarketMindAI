using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace MarketMind.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<NewsArticle>     NewsArticles     => Set<NewsArticle>();
    public DbSet<MarketSnapshot>  MarketSnapshots  => Set<MarketSnapshot>();
    public DbSet<AIAnalysis>      AIAnalyses       => Set<AIAnalysis>();
    public DbSet<SectorSentiment> SectorSentiments => Set<SectorSentiment>();
    public DbSet<FoSnapshot>      FoSnapshots      => Set<FoSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");


        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        // Snake_case naming — PostgreSQL convention
        optionsBuilder.UseSnakeCaseNamingConvention();
        base.OnConfiguring(optionsBuilder);
    }
}