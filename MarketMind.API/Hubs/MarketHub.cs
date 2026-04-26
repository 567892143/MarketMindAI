using Microsoft.AspNetCore.SignalR;

namespace MarketMind.API.Hubs;

// Hub is the connection point — clients connect here
// Think of it as a switchboard
// Each method = a command a client can call on the server
public class MarketHub : Hub
{
    // ── Group subscriptions ───────────────────────────────────────
    // Client calls these to opt into specific data streams
    // Groups mean we only push data to clients who want it

    /// <summary>
    /// Subscribe to live NIFTY, SENSEX, BANK NIFTY updates.
    /// Server pushes "PriceUpdate" every 15 seconds.
    /// </summary>
    public async Task SubscribeToIndices()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "indices");
        
        // Immediately acknowledge — client knows subscription worked
        await Clients.Caller.SendAsync("Subscribed", new
        {
            Group     = "indices",
            Message   = "You will receive index updates every 15 seconds",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Subscribe to GIFT NIFTY updates — most important pre-market signal.
    /// Active 7:00 AM to 9:15 AM IST.
    /// </summary>
    public async Task SubscribeToGiftNifty()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "gift-nifty");
        await Clients.Caller.SendAsync("Subscribed", new
        {
            Group     = "gift-nifty",
            Message   = "You will receive GIFT NIFTY updates every 15 seconds",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Subscribe to commodities — Crude, Gold, Silver.
    /// </summary>
    public async Task SubscribeToCommodities()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "commodities");
        await Clients.Caller.SendAsync("Subscribed", new
        {
            Group     = "commodities",
            Message   = "You will receive commodity updates every 30 seconds",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Subscribe to all streams at once — used by dashboard on load.
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "indices");
        await Groups.AddToGroupAsync(Context.ConnectionId, "gift-nifty");
        await Groups.AddToGroupAsync(Context.ConnectionId, "commodities");

        await Clients.Caller.SendAsync("Subscribed", new
        {
            Group     = "all",
            Message   = "Subscribed to all market streams",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unsubscribe from a group — called when user leaves dashboard.
    /// </summary>
    public async Task Unsubscribe(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    // ── Connection lifecycle ──────────────────────────────────────
    // These are called automatically by SignalR

    public override async Task OnConnectedAsync()
    {
        // Log connection — useful for debugging
        Console.WriteLine(
            $"[SignalR] Client connected: {Context.ConnectionId} at {DateTime.UtcNow:HH:mm:ss}");

        // Send welcome message with server time
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            ServerTime   = DateTime.UtcNow,
            Message      = "Connected to MarketMind real-time feed"
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine(
            $"[SignalR] Client disconnected: {Context.ConnectionId}");

        // Groups are automatically cleaned up on disconnect
        // No manual removal needed

        await base.OnDisconnectedAsync(exception);
    }
}