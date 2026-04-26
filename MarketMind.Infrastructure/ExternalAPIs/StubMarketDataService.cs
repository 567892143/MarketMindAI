using MarketMind.Application.DTOs;
using MarketMind.Application.Interfaces;
using MarketMind.Domain.Enums;

namespace MarketMind.Infrastructure.ExternalAPIs;

// Returns realistic fake data — replaced by YahooFinanceService in Phase 8
public class StubMarketDataService : IMarketDataService
{
    private static readonly Random Rng = new();

    private static readonly Dictionary<string, (string Display, MarketModule Module, decimal Base)> Instruments = new()
    {
        ["NIFTY"]      = ("NIFTY 50",      MarketModule.Equities,    24312m),
        ["SENSEX"]     = ("SENSEX",         MarketModule.Equities,    79876m),
        ["BANKNIFTY"]  = ("BANK NIFTY",     MarketModule.Equities,    52840m),
        ["GIFT_NIFTY"] = ("GIFT NIFTY",     MarketModule.Equities,    24454m),
        ["NASDAQ"]     = ("Nasdaq",          MarketModule.USMarkets,   18240m),
        ["SP500"]      = ("S&P 500",         MarketModule.USMarkets,    5248m),
        ["DOW"]        = ("Dow Jones",       MarketModule.USMarkets,   38920m),
        ["CRUDE"]      = ("Crude Oil",       MarketModule.Commodities,    87m),
        ["GOLD"]       = ("Gold",            MarketModule.Commodities,  2340m),
        ["SILVER"]     = ("Silver",          MarketModule.Commodities,    27m),
        ["USDINR"]     = ("USD/INR",         MarketModule.Forex,        83.4m),
        ["DXY"]        = ("DXY Index",       MarketModule.Forex,        104m),
    };

    public Task<MarketSnapshotDto> GetSnapshotAsync(
        string symbol,
        CancellationToken ct = default)
    {
        var snap = BuildSnapshot(symbol);
        return Task.FromResult(snap);
    }

    public Task<List<MarketSnapshotDto>> GetModuleSnapshotsAsync(
        MarketModule module,
        CancellationToken ct = default)
    {
        var snaps = Instruments
            .Where(i => i.Value.Module == module)
            .Select(i => BuildSnapshot(i.Key))
            .ToList();

        return Task.FromResult(snaps);
    }

    public Task<List<TopStockDto>> GetTopStocksForSectorAsync(
        string sector,
        CancellationToken ct = default)
    {
        // Realistic stub stocks per sector
        var stocks = sector.ToUpper() switch
        {
            "IT" or "IT SERVICES" => new List<TopStockDto>
            {
                new("TCS",     3842m, +1.2m,  "Bullish"),
                new("INFY",    1456m, +0.8m,  "Bullish"),
                new("WIPRO",    482m, +0.3m,  "Neutral"),
                new("HCLTECH", 1234m, +1.6m,  "Bullish"),
                new("TECHM",   1089m, -0.2m,  "Caution"),
            },
            "BANKING" or "FINANCIALS" => new List<TopStockDto>
            {
                new("HDFCBANK", 1678m, +0.9m, "Bullish"),
                new("ICICIBANK", 1245m, +1.1m,"Bullish"),
                new("KOTAKBANK", 1789m, -0.3m,"Neutral"),
                new("SBIN",      812m, +0.6m, "Bullish"),
                new("AXISBANK",  1123m, +0.4m,"Neutral"),
            },
            "ENERGY" or "OIL AND GAS" => new List<TopStockDto>
            {
                new("RELIANCE", 2984m, +1.2m, "Bullish"),
                new("ONGC",      284m, -0.8m, "Bearish"),
                new("BPCL",      628m, -1.2m, "Bearish"),
                new("HPCL",      512m, -0.9m, "Caution"),
                new("IOC",       178m, -0.4m, "Neutral"),
            },
            _ => new List<TopStockDto>
            {
                new("STOCK1", 1200m, +0.5m, "Bullish"),
                new("STOCK2",  890m, -0.3m, "Neutral"),
                new("STOCK3", 2340m, +1.1m, "Bullish"),
            }
        };

        return Task.FromResult(stocks);
    }

    public Task<List<OhlcPointDto>> GetIntradayOhlcAsync(
        string symbol,
        DateTime date,
        CancellationToken ct = default)
    {
        // Generate realistic 15-min candles for a trading day
        // 9:15 AM to 3:30 PM = 25 candles
        var candles = new List<OhlcPointDto>();
        var basePrice = 24312m;
        var current   = basePrice;
        var startTime = date.Date.AddHours(9).AddMinutes(15);

        for (int i = 0; i < 25; i++)
        {
            var open   = current;
            var change = (decimal)(Rng.NextDouble() * 100 - 50);
            var close  = open + change;
            var high   = Math.Max(open, close) + (decimal)(Rng.NextDouble() * 30);
            var low    = Math.Min(open, close) - (decimal)(Rng.NextDouble() * 30);
            var volume = (long)(Rng.NextDouble() * 500000 + 100000);

            candles.Add(new OhlcPointDto(
                startTime.AddMinutes(i * 15),
                open, high, low, close, volume));

            current = close;
        }

        return Task.FromResult(candles);
    }

    // Adds realistic random variation to base price
    private static MarketSnapshotDto BuildSnapshot(string symbol)
    {
        if (!Instruments.TryGetValue(symbol.ToUpper(), out var info))
            info = ("Unknown", MarketModule.Equities, 1000m);

        var changePct = (decimal)(Rng.NextDouble() * 4 - 2); // -2% to +2%
        var changeAbs = info.Base * changePct / 100;
        var price     = info.Base + changeAbs;

        return new MarketSnapshotDto(
            Symbol:        symbol.ToUpper(),
            DisplayName:   info.Display,
            Module:        info.Module,
            Price:         Math.Round(price, 2),
            ChangePercent: Math.Round(changePct, 2),
            ChangeAbsolute:Math.Round(changeAbs, 2),
            IsBullish:     changePct > 0,
            DirectionLabel:changePct > 0 ? "▲" : changePct < 0 ? "▼" : "–",
            CapturedAt:    DateTime.UtcNow
        );
    }
}