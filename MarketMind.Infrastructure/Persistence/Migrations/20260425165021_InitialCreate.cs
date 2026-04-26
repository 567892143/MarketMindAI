using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace MarketMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ai_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    response_text = table.Column<string>(type: "text", nullable: false),
                    source_article_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_analyses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fo_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    put_call_ratio = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    max_pain = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    top_strikes = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fo_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    change_percent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    change_absolute = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: true),
                    day_high = table.Column<decimal>(type: "numeric", nullable: true),
                    day_low = table.Column<decimal>(type: "numeric", nullable: true),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sentiment_score = table.Column<float>(type: "real", nullable: false),
                    affected_sectors = table.Column<string[]>(type: "text[]", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    is_embedded = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sector_sentiments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bullish_score = table.Column<float>(type: "real", nullable: false),
                    overall = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    article_count = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sector_sentiments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_type_key",
                table: "ai_analyses",
                columns: new[] { "type", "target_key" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_valid_until",
                table: "ai_analyses",
                column: "valid_until");

            migrationBuilder.CreateIndex(
                name: "ix_fo_snapshots_symbol_captured",
                table: "fo_snapshots",
                columns: new[] { "symbol", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_market_snapshots_symbol_captured",
                table: "market_snapshots",
                columns: new[] { "symbol", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_is_embedded",
                table: "news_articles",
                column: "is_embedded");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_published_at",
                table: "news_articles",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_url",
                table: "news_articles",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sector_sentiments_sector_date",
                table: "sector_sentiments",
                columns: new[] { "sector", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analyses");

            migrationBuilder.DropTable(
                name: "fo_snapshots");

            migrationBuilder.DropTable(
                name: "market_snapshots");

            migrationBuilder.DropTable(
                name: "news_articles");

            migrationBuilder.DropTable(
                name: "sector_sentiments");
        }
    }
}
