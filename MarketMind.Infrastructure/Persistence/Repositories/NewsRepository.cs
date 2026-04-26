using MarketMind.Application.Interfaces;
using MarketMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketMind.Infrastructure.Persistence.Repositories;

public class NewsRepository(AppDbContext db) : INewsRepository
{
    public async Task SaveAsync(
        NewsArticle article,
        CancellationToken ct = default)
    {
        db.NewsArticles.Add(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveManyAsync(
        IEnumerable<NewsArticle> articles,
        CancellationToken ct = default)
    {
        db.NewsArticles.AddRange(articles);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<NewsArticle>> GetUnembeddedAsync(
        int limit = 50,
        CancellationToken ct = default) =>
        await db.NewsArticles
            .Where(a => !a.IsEmbedded)
            .OrderBy(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task UpdateEmbeddingAsync(
        NewsArticle article,
        CancellationToken ct = default)
    {
        db.NewsArticles.Update(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSentimentAsync(
        NewsArticle article,
        CancellationToken ct = default)
    {
        db.NewsArticles.Update(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<NewsArticle>> GetBySectorAsync(
        string sector,
        DateTime date,
        int limit = 10,
        CancellationToken ct = default) =>
        await db.NewsArticles
            .Where(a => a.AffectedSectors.Contains(sector) &&
                        a.PublishedAt.Date == date.Date)
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<List<NewsArticle>> GetByDateAsync(
        DateTime date,
        CancellationToken ct = default) =>
        await db.NewsArticles
            .Where(a => a.PublishedAt.Date == date.Date)
            .OrderByDescending(a => a.SentimentScore)
            .ToListAsync(ct);

    public async Task<bool> ExistsByUrlAsync(
        string url,
        CancellationToken ct = default) =>
        await db.NewsArticles
            .AnyAsync(a => a.Url == url, ct);
}