using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace MarketMind.Infrastructure.Persistence.Configurations;

public class NewsArticleConfiguration
    : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("news_articles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Headline)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.RawText)
            .IsRequired();

        builder.Property(a => a.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.Sentiment)
            .HasConversion<string>()
            .HasMaxLength(20);

          var embeddingConverter = new ValueConverter<float[]?, Vector?>(
    v => v == null ? null : new Vector(v),
    v => v == null ? null : v.ToArray());

var embeddingComparer = new ValueComparer<float[]?>(
    (a, b) => a != null && b != null && a.SequenceEqual(b),
    v => v == null ? 0 : v.Aggregate(0, HashCode.Combine),
    v => v == null ? null : v.ToArray());

builder.Property(a => a.Embedding)
    .HasColumnType("vector(768)")
    .HasConversion(embeddingConverter, embeddingComparer)
    .IsRequired(false);

        builder.Property(a => a.AffectedSectors)
            .HasColumnType("text[]");

        // Index for deduplication checks
        builder.HasIndex(a => a.Url)
            .IsUnique()
            .HasDatabaseName("ix_news_articles_url");

        // Index for date-based queries (Why Engine, daily news fetch)
        builder.HasIndex(a => a.PublishedAt)
            .HasDatabaseName("ix_news_articles_published_at");

        // Index for unembedded article queries
        builder.HasIndex(a => a.IsEmbedded)
            .HasDatabaseName("ix_news_articles_is_embedded");

        // pgvector IVFFlat index for cosine similarity search
        // Created separately in migration — see migration notes below
    }
}