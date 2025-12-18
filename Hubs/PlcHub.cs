using FX5u_Web_HMI_App; // Add this
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Hubs
{
    public class PlcHub : Hub
    {
        private readonly PageStateTracker _tracker;

        public PlcHub(PageStateTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task JoinPage(string pageName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, pageName);
            _tracker.ClientJoined(pageName);
            Context.Items["CurrentPage"] = pageName;
        }

        public async Task LeavePage(string pageName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, pageName);
            _tracker.ClientLeft(pageName);
            Context.Items.Remove("CurrentPage");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items.TryGetValue("CurrentPage", out var pageNameObj))
            {
                var pageName = pageNameObj as string;
                if (!string.IsNullOrEmpty(pageName))
                {
                    _tracker.ClientLeft(pageName);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}