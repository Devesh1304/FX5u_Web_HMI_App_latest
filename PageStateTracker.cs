using System.Collections.Concurrent;

namespace FX5u_Web_HMI_App
{
    public class PageStateTracker
    {
        // Key: PageName, Value: Number of connected clients
        private readonly ConcurrentDictionary<string, int> _activePages = new();

        public void ClientJoined(string pageName)
        {
            _activePages.AddOrUpdate(pageName, 1, (key, count) => count + 1);
        }

        public void ClientLeft(string pageName)
        {
            _activePages.AddOrUpdate(pageName, 0, (key, count) => Math.Max(0, count - 1));
        }

        public bool IsPageActive(string pageName)
        {
            return _activePages.TryGetValue(pageName, out int count) && count > 0;
        }
    }
}