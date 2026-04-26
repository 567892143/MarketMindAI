using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketMind.Infrastructure.Persistence.Configurations;

public class AIAnalysisConfiguration
    : IEntityTypeConfiguration<AIAnalysis>
{
    public void Configure(EntityTypeBuilder<AIAnalysis> builder)
    {
        builder.ToTable("ai_analyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.TargetKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ResponseText)
            .IsRequired();

        builder.Property(a => a.SourceArticleIds)
            .HasColumnType("uuid[]");

        // Lookup by type + key = most common query pattern
        builder.HasIndex(a => new { a.Type, a.TargetKey })
            .HasDatabaseName("ix_ai_analyses_type_key");

        builder.HasIndex(a => a.ValidUntil)
            .HasDatabaseName("ix_ai_analyses_valid_until");
    }
}