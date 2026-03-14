using FX5u_Web_HMI_App; // Add this
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Hubs
{
    public class PlcHub : Hub
    {
        private readonly PageStateTracker _tracker;
        private readonly ISLMPService _slmpService;

        public PlcHub(PageStateTracker tracker, ISLMPService slmpService)
        {
            _tracker = tracker;
            _slmpService = slmpService;

        }

        public async Task RequestCurrentNavigationState()
        {
            var navResult = await _slmpService.ReadInt16Async("D1");
            if (navResult.IsSuccess)
            {
                if (navResult.Content == 11)
                {
                    // Tell ONLY the specific browser that just asked to navigate
                    await Clients.Caller.SendAsync("NavigateToPage", "/PowerMainScreen");
                }
                // You can add other page states here later if D1 == 12, 13, etc.
            }
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