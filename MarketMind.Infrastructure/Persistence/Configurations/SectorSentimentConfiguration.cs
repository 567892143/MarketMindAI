using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketMind.Infrastructure.Persistence.Configurations;

public class SectorSentimentConfiguration
    : IEntityTypeConfiguration<SectorSentiment>
{
    public void Configure(EntityTypeBuilder<SectorSentiment> builder)
    {
        builder.ToTable("sector_sentiments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Sector)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Overall)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Today's sector sentiment is the most common query
        builder.HasIndex(s => new { s.Sector, s.Date })
            .HasDatabaseName("ix_sector_sentiments_sector_date");
    }
}