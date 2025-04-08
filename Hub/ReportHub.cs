using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Cloud9_2.Hubs
{
public class ReportHub : Hub
{
    public async Task SwitchReport(string reportName)
    {
        // Notify the client to switch to the specified report
        await Clients.Caller.SendAsync("ReceiveReport", reportName);
    }
}
}