using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketMind.Infrastructure.ExternalAPIs;

public class YahooFinanceService(
    HttpClient httpClient,
    ILogger<YahooFinanceService> logger) : IMarketDataService
{
    // Symbol map — our internal symbol → Yahoo Finance symbol
    private static readonly Dictionary<string, (string YahooSymbol, string Display, MarketModule Module)>
        SymbolMap = new()
        {
            ["NIFTY"]      = ("^NSEI",    "NIFTY 50",    MarketModule.Equities),
            ["SENSEX"]     = ("^BSESN",   "SENSEX",      MarketModule.Equities),
            ["BANKNIFTY"]  = ("^NSEBANK", "BANK NIFTY",  MarketModule.Equities),
            ["GIFT_NIFTY"] = ("^NSEI", "GIFT NIFTY",  MarketModule.Equities),
            ["NASDAQ"]     = ("^IXIC",    "Nasdaq",       MarketModule.USMarkets),
            ["SP500"]      = ("^GSPC",    "S&P 500",      MarketModule.USMarkets),
            ["DOW"]        = ("^DJI",     "Dow Jones",    MarketModule.USMarkets),
            ["CRUDE"]      = ("CL=F",     "Crude Oil",    MarketModule.Commodities),
            ["GOLD"]       = ("GC=F",     "Gold",         MarketModule.Commodities),
            ["SILVER"]     = ("SI=F",     "Silver",       MarketModule.Commodities),
            ["USDINR"]     = ("USDINR=X", "USD/INR",      MarketModule.Forex),
            ["DXY"]        = ("DX-Y.NYB", "DXY Index",    MarketModule.Forex),
        };

    private static readonly Dictionary<MarketModule, string[]> ModuleSymbols = new()
    {
        [MarketModule.Equities]    = ["NIFTY", "SENSEX", "BANKNIFTY", "GIFT_NIFTY"],
        [MarketModule.USMarkets]   = ["NASDAQ", "SP500", "DOW"],
        [MarketModule.Commodities] = ["CRUDE", "GOLD", "SILVER"],
        [MarketModule.Forex]       = ["USDINR", "DXY"],
        [MarketModule.Derivatives] = [],
    };

    public async Task<MarketSnapshotDto> GetSnapshotAsync(
        string symbol,
        CancellationToken ct = default)
    {
        if (!SymbolMap.TryGetValue(symbol.ToUpper(), out var info))
        {
            logger.LogWarning("Symbol {Symbol} not found in map", symbol);
            return BuildFallback(symbol);
        }

        try
        {
            var url  = $"https://query1.finance.yahoo.com/v8/finance/chart/{info.YahooSymbol}?interval=1d&range=1d";
            var json = await httpClient.GetStringAsync(url, ct);
            return ParseYahooResponse(json, symbol, info.Display, info.Module);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Yahoo Finance failed for {Symbol} — returning fallback", symbol);
            return BuildFallback(symbol, info.Display, info.Module);
        }
    }

    public async Task<List<MarketSnapshotDto>> GetModuleSnapshotsAsync(
        MarketModule module,
        CancellationToken ct = default)
    {
        if (!ModuleSymbols.TryGetValue(module, out var symbols) || symbols.Length == 0)
            return [];

        // Fetch all symbols in parallel
        var tasks = symbols.Select(s => GetSnapshotAsync(s, ct));
        var results = await Task.WhenAll(tasks);
        return [.. results];
    }

    public async Task<List<TopStockDto>> GetTopStocksForSectorAsync(
        string sector,
        CancellationToken ct = default)
    {
        // Top stocks per sector — fetch from Yahoo in parallel
        var sectorStocks = GetSectorStockSymbols(sector);
        var tasks = sectorStocks.Select(s => FetchTopStockAsync(s.Symbol, s.Name, ct));
        var results = await Task.WhenAll(tasks);
        return [.. results];
    }

    public async Task<List<OhlcPointDto>> GetIntradayOhlcAsync(
        string symbol,
        DateTime date,
        CancellationToken ct = default)
    {
        if (!SymbolMap.TryGetValue(symbol.ToUpper(), out var info))
            return [];

        try
        {
            // 15-minute interval for intraday
            var url  = $"https://query1.finance.yahoo.com/v8/finance/chart/{info.YahooSymbol}?interval=15m&range=1d";
            var json = await httpClient.GetStringAsync(url, ct);
            return ParseOhlcResponse(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OHLC fetch failed for {Symbol}", symbol);
            return [];
        }
    }

    // ── Parsers ───────────────────────────────────────────────────

    private static MarketSnapshotDto ParseYahooResponse(
        string json,
        string symbol,
        string displayName,
        MarketModule module)
    {
        using var doc  = JsonDocument.Parse(json);
        var chart      = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0];

        var meta       = chart.GetProperty("meta");
        var price      = meta.GetProperty("regularMarketPrice").GetDecimal();
        var prevClose  = meta.GetProperty("chartPreviousClose").GetDecimal();
        var changeAbs  = price - prevClose;
        var changePct  = prevClose != 0
            ? Math.Round(changeAbs / prevClose * 100, 2)
            : 0m;

        return new MarketSnapshotDto(
            Symbol:         symbol.ToUpper(),
            DisplayName:    displayName,
            Module:         module,
            Price:          Math.Round(price, 2),
            ChangePercent:  changePct,
            ChangeAbsolute: Math.Round(changeAbs, 2),
            IsBullish:      changeAbs > 0,
            DirectionLabel: changeAbs > 0 ? "▲" : changeAbs < 0 ? "▼" : "–",
            CapturedAt:     DateTime.UtcNow
        );
    }

    private static List<OhlcPointDto> ParseOhlcResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result    = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0];

        var timestamps = result.GetProperty("timestamp").EnumerateArray().ToList();
        var indicators = result.GetProperty("indicators").GetProperty("quote")[0];

        var opens   = indicators.GetProperty("open").EnumerateArray().ToList();
        var highs   = indicators.GetProperty("high").EnumerateArray().ToList();
        var lows    = indicators.GetProperty("low").EnumerateArray().ToList();
        var closes  = indicators.GetProperty("close").EnumerateArray().ToList();
        var volumes = indicators.GetProperty("volume").EnumerateArray().ToList();

        var candles = new List<OhlcPointDto>();

        for (int i = 0; i < timestamps.Count; i++)
        {
            // Skip null candles
            if (closes[i].ValueKind == JsonValueKind.Null) continue;

            var time = DateTimeOffset
                .FromUnixTimeSeconds(timestamps[i].GetInt64())
                .UtcDateTime;

            candles.Add(new OhlcPointDto(
                Time:   time,
                Open:   opens[i].ValueKind   != JsonValueKind.Null ? opens[i].GetDecimal()   : 0,
                High:   highs[i].ValueKind   != JsonValueKind.Null ? highs[i].GetDecimal()   : 0,
                Low:    lows[i].ValueKind    != JsonValueKind.Null ? lows[i].GetDecimal()    : 0,
                Close:  closes[i].GetDecimal(),
                Volume: volumes[i].ValueKind != JsonValueKind.Null ? volumes[i].GetInt64()   : 0
            ));
        }

        return candles;
    }

    private async Task<TopStockDto> FetchTopStockAsync(
        string yahooSymbol,
        string displayName,
        CancellationToken ct)
    {
        try
        {
            var url  = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?interval=1d&range=1d";
            var json = await httpClient.GetStringAsync(url, ct);

            using var doc = JsonDocument.Parse(json);
            var meta      = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta");

            var price     = meta.GetProperty("regularMarketPrice").GetDecimal();
            var prevClose = meta.GetProperty("chartPreviousClose").GetDecimal();
            var changePct = prevClose != 0
                ? Math.Round((price - prevClose) / prevClose * 100, 2)
                : 0m;

            var signal = changePct switch
            {
                > 1.5m  => "Bullish",
                > 0m    => "Bullish",
                < -1.5m => "Bearish",
                < 0m    => "Caution",
                _       => "Neutral"
            };

            return new TopStockDto(displayName, Math.Round(price, 2), changePct, signal);
        }
        catch
        {
            return new TopStockDto(displayName, 0m, 0m, "Neutral");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static MarketSnapshotDto BuildFallback(
        string symbol,
        string display = "",
        MarketModule module = MarketModule.Equities) =>
        new(symbol.ToUpper(), display.Length > 0 ? display : symbol,
            module, 0m, 0m, 0m, false, "–", DateTime.UtcNow);

    private static List<(string Symbol, string Name)> GetSectorStockSymbols(string sector) =>
        sector.ToUpper() switch
        {
            "IT" or "IT SERVICES" =>
            [
                ("TCS.NS",     "TCS"),
                ("INFY.NS",    "INFY"),
                ("WIPRO.NS",   "WIPRO"),
                ("HCLTECH.NS", "HCLTECH"),
                ("TECHM.NS",   "TECHM"),
            ],
            "BANKING" or "FINANCIALS" =>
            [
                ("HDFCBANK.NS",  "HDFCBANK"),
                ("ICICIBANK.NS", "ICICIBANK"),
                ("KOTAKBANK.NS", "KOTAKBANK"),
                ("SBIN.NS",      "SBIN"),
                ("AXISBANK.NS",  "AXISBANK"),
            ],
            "ENERGY" or "OIL AND GAS" =>
            [
                ("RELIANCE.NS", "RELIANCE"),
                ("ONGC.NS",     "ONGC"),
                ("BPCL.NS",     "BPCL"),
                ("HPCL.NS",     "HPCL"),
                ("IOC.NS",      "IOC"),
            ],
            "PHARMA" =>
            [
                ("SUNPHARMA.NS", "SUNPHARMA"),
                ("DRREDDY.NS",   "DRREDDY"),
                ("CIPLA.NS",     "CIPLA"),
                ("DIVISLAB.NS",  "DIVISLAB"),
                ("APOLLOHOSP.NS","APOLLOHOSP"),
            ],
            "AUTO" =>
            [
                ("MARUTI.NS",    "MARUTI"),
                ("TATAMOTORS.NS","TATAMOTORS"),
                ("M&M.NS",       "M&M"),
                ("BAJAJ-AUTO.NS","BAJAJ-AUTO"),
                ("EICHERMOT.NS", "EICHERMOT"),
            ],
            _ =>
            [
                ("RELIANCE.NS", "RELIANCE"),
                ("HDFCBANK.NS", "HDFCBANK"),
                ("TCS.NS",      "TCS"),
            ]
        };
}