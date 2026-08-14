using ReactiveTracker.Models;

namespace ReactiveTracker.Presenters
{
    public class PlayerPresenter
    {
        private readonly PlayerModel _model;
        private readonly IPlayerView _view;

        public PlayerPresenter(PlayerModel model, IPlayerView view)
        {
            _model = model;
            _view = view;

            _model.SingleCountChanged += (s, v) => _view.SetSingleCount(v);
            _model.GroupCountChanged  += (s, v) => _view.SetGroupCount(v);
            _model.SingleTimerChanged += (s, v) => _view.SetSingleTimer(v);
            _model.GroupTimerChanged  += (s, v) => _view.SetGroupTimer(v);
        }

        public void StartSingle(int count) => _model.StartSingle(count);
        public void StartGroup(int count)  => _model.StartGroup(count);
        public void UseSingle()            => _model.UseSingle();
        public void UseGroup()             => _model.UseGroup();
        public void Reset()                => _model.Reset();
    }
}
