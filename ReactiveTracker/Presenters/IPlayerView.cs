namespace ReactiveTracker.Presenters
{
    public interface IPlayerView
    {
        void SetSingleCount(int count);
        void SetGroupCount(int count);
        void SetSingleTimer(int secondsRemaining);
        void SetGroupTimer(int secondsRemaining);
    }
}
