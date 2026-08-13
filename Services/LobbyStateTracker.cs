using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RiftRoulette.Services
{
    public class LobbyStateTracker
    {
        // Almacena el último "ping" de cada usuario (userId -> Timestamp)
        private readonly ConcurrentDictionary<int, DateTime> _lastSeenCache = new();

        public void UpdateLastSeen(int userId)
        {
            _lastSeenCache[userId] = DateTime.UtcNow;
        }

        public void RemoveUser(int userId)
        {
            _lastSeenCache.TryRemove(userId, out _);
        }

        public Dictionary<int, DateTime> GetAllLastSeen()
        {
            return new Dictionary<int, DateTime>(_lastSeenCache);
        }
    }
}
