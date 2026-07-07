using Microsoft.AspNetCore.SignalR;

namespace inventory_dashboard.Hubs;

public class StockHub : Hub
{
    // This demonstrates real-time communication
    public async Task SendStockAlert(string productName, int stock)
    {
        await Clients.All.SendAsync("ReceiveAlert", productName, stock);
    }
}