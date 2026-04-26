using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketMind.Infrastructure.Persistence.Configurations;

public class FoSnapshotConfiguration
    : IEntityTypeConfiguration<FoSnapshot>
{
    public void Configure(EntityTypeBuilder<FoSnapshot> builder)
    {
        builder.ToTable("fo_snapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.PutCallRatio)
            .HasPrecision(8, 4);

        builder.Property(s => s.MaxPain)
            .HasPrecision(18, 2);

        // OiStrike is stored as owned JSON collection
        builder.OwnsMany(s => s.TopStrikes, strike =>
        {
            strike.ToJson();
            strike.Property(o => o.Strike).HasPrecision(18, 2);
        });

        // Ignore computed property
        builder.Ignore(s => s.PcrSignal);

        builder.HasIndex(s => new { s.Symbol, s.CapturedAt })
            .HasDatabaseName("ix_fo_snapshots_symbol_captured");
    }
}