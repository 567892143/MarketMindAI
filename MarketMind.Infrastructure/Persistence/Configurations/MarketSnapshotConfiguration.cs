using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketMind.Infrastructure.Persistence.Configurations;

public class MarketSnapshotConfiguration
    : IEntityTypeConfiguration<MarketSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketSnapshot> builder)
    {
        builder.ToTable("market_snapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.DisplayName)
            .HasMaxLength(100);

        builder.Property(s => s.Module)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Price)
            .HasPrecision(18, 4);

        builder.Property(s => s.ChangePercent)
            .HasPrecision(8, 4);

        builder.Property(s => s.ChangeAbsolute)
            .HasPrecision(18, 4);

        builder.Property(s => s.Volume)
            .HasPrecision(20, 2);

        // Ignore computed properties — not stored in DB
        builder.Ignore(s => s.IsBullish);
        builder.Ignore(s => s.IsBearish);
        builder.Ignore(s => s.DirectionLabel);

        // Latest snapshot per symbol query is very common
        builder.HasIndex(s => new { s.Symbol, s.CapturedAt })
            .HasDatabaseName("ix_market_snapshots_symbol_captured");
    }
}