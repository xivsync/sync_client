using System;
using System.Collections.Concurrent;
using System.Threading;

namespace XIVSync.Security
{
    internal enum ThrottleState { Off, Slow, SustainedSweep }

    /// <summary>
    /// Yuhi-note: This is the “don’t strip-mine the vault” governor. Players won’t notice;
    /// bulk scrapers will—just enough to be annoying.
    /// </summary>
    internal sealed class ReadGovernor
    {
        private readonly TimeSpan _window = TimeSpan.FromSeconds(60);
        private readonly int _filesPerMinute;
        private readonly long _bytesPerMinute;
        private readonly int _distinctThreshold;
        private readonly ConcurrentQueue<(DateTime ts, string path, long bytes)> _events = new();
        private long _bytes;

        public ReadGovernor(int filesPerMinute = 120, long bytesPerMinute = 1_610_612_736, int distinctThreshold = 200)
        {
            _filesPerMinute = filesPerMinute;
            _bytesPerMinute = bytesPerMinute;   // ~1.5 GiB/min
            _distinctThreshold = distinctThreshold;
        }

        public void Note(string path, long bytes)
        {
            var now = DateTime.UtcNow;
            _events.Enqueue((now, path, bytes));
            Interlocked.Add(ref _bytes, bytes);
            Trim(now);
        }

        public ThrottleState State()
        {
            var now = DateTime.UtcNow; Trim(now);
            var count = _events.Count;
            var bytes = Interlocked.Read(ref _bytes);
            if (count > _filesPerMinute || bytes > _bytesPerMinute) return ThrottleState.Slow;

            // Cheap distinct check: broad sweeps hit lots of unique paths quickly.
            int seen = 0; var set = new ConcurrentDictionary<string, byte>();
            foreach (var e in _events)
                if (set.TryAdd(e.path, 0) && ++seen > _distinctThreshold) return ThrottleState.SustainedSweep;

            return ThrottleState.Off;
        }

        public static void Apply(ThrottleState s)
        {
            if (s == ThrottleState.Slow) Thread.Sleep(30);         // invisible nudge
            else if (s == ThrottleState.SustainedSweep) Thread.Sleep(120); // “hey, slow down”
        }

        private void Trim(DateTime now)
        {
            while (_events.TryPeek(out var e) && now - e.ts > _window)
                if (_events.TryDequeue(out var d))
                    Interlocked.Add(ref _bytes, -d.bytes);
        }
    }
}