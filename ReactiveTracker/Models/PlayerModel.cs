using System;
using System.Diagnostics;
using System.Threading;

namespace ReactiveTracker.Models
{
    public class PlayerModel
    {
        private const int CountdownSeconds = 30;

        private readonly SynchronizationContext _syncContext;

        public string Name { get; set; } = string.Empty;

        private int _singleCount;
        private int _groupCount;
        private int _singleSecondsRemaining;
        private int _groupSecondsRemaining;
        private System.Threading.Timer _singleTimer;
        private System.Threading.Timer _groupTimer;

        public int SingleCount
        {
            get => _singleCount;
            private set
            {
                _singleCount = value < 0 ? 0 : value;
                SingleCountChanged?.Invoke(this, _singleCount);

                if (_singleCount == 0)
                    StopSingleTimer();
            }
        }

        public int GroupCount
        {
            get => _groupCount;
            private set
            {
                _groupCount = value < 0 ? 0 : value;
                GroupCountChanged?.Invoke(this, _groupCount);

                if (_groupCount == 0)
                    StopGroupTimer();
            }
        }

        public int SingleSecondsRemaining
        {
            get => _singleSecondsRemaining;
            private set
            {
                _singleSecondsRemaining = value;
                SingleTimerChanged?.Invoke(this, _singleSecondsRemaining);
                //Debug.WriteLine($"Name: {Name}, SingleSecondsRemaining: {_singleSecondsRemaining}");
            }
        }

        public int GroupSecondsRemaining
        {
            get => _groupSecondsRemaining;
            private set
            {
                _groupSecondsRemaining = value;
                GroupTimerChanged?.Invoke(this, _groupSecondsRemaining);
                //Debug.WriteLine($"Name: {Name}, GroupSecondsRemaining: {_groupSecondsRemaining}");
            }
        }

        public event EventHandler<int> SingleCountChanged;
        public event EventHandler<int> GroupCountChanged;
        public event EventHandler<int> SingleTimerChanged;
        public event EventHandler<int> GroupTimerChanged;

        public PlayerModel()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public void StartSingle(int count)
        {
            StopSingleTimer();

            _singleCount = count;
            SingleSecondsRemaining = CountdownSeconds;
            SingleCountChanged?.Invoke(this, _singleCount);

            _singleTimer = new System.Threading.Timer(_ =>
                _syncContext.Post(__ => OnSingleTick(), null), null, 1000, 1000);
        }

        public void StartGroup(int count)
        {
            StopGroupTimer();

            _groupCount = count;
            GroupSecondsRemaining = CountdownSeconds;
            GroupCountChanged?.Invoke(this, _groupCount);

            _groupTimer = new System.Threading.Timer(_ =>
                _syncContext.Post(__ => OnGroupTick(), null), null, 1000, 1000);
        }

        public void UseSingle()
        {
            if (_singleCount > 0)
                SingleCount--;
        }

        public void UseGroup()
        {
            if (_groupCount > 0)
                GroupCount--;
        }

        private void OnSingleTick()
        {
            SingleSecondsRemaining--;

            if (_singleSecondsRemaining <= 0)
            {
                SingleCount = 0;
            }
        }
        public void ExpireSingle()
        {
            SingleCount = 0;
        }

        private void OnGroupTick()
        {
            GroupSecondsRemaining--;

            if (_groupSecondsRemaining <= 0)
            {
                GroupCount = 0;
            }
        }

        private void StopSingleTimer()
        {
            if (_singleTimer == null)
                return;

            _singleTimer.Dispose();
            _singleTimer = null;
            SingleSecondsRemaining = 0;
        }

        private void StopGroupTimer()
        {
            if (_groupTimer == null)
                return;

            _groupTimer.Dispose();
            _groupTimer = null;
            GroupSecondsRemaining = 0;
        }

        public void Reset()
        {
            StopSingleTimer();
            StopGroupTimer();
            SingleCount = 0;
            GroupCount = 0;
        }
    }
}
