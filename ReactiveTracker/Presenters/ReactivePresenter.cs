using System;
using ReactiveTracker.Models;

namespace ReactiveTracker.Presenters
{
    public class ReactivePresenter
    {
        private readonly PlayerPresenter[] _playerPresenters;

        public int PlayerCount => _playerPresenters.Length;

        public ReactivePresenter(PlayerModel[] models, IPlayerView[] views)
        {
            if (models == null) throw new ArgumentNullException("models");
            if (views == null)  throw new ArgumentNullException("views");
            if (models.Length != views.Length)
                throw new ArgumentException("models and views must have the same length.");

            _playerPresenters = new PlayerPresenter[models.Length];
            for (int i = 0; i < models.Length; i++)
                _playerPresenters[i] = new PlayerPresenter(models[i], views[i]);
        }

        public PlayerPresenter GetPlayerPresenter(int playerIndex) => _playerPresenters[playerIndex];

        public void StartSingle(int playerIndex, int count) => _playerPresenters[playerIndex].StartSingle(count);
        public void StartGroup(int playerIndex, int count)  => _playerPresenters[playerIndex].StartGroup(count);
        public void UseSingle(int playerIndex)              => _playerPresenters[playerIndex].UseSingle();
        public void UseGroup(int playerIndex)               => _playerPresenters[playerIndex].UseGroup();
        public void ResetPlayer(int playerIndex)            => _playerPresenters[playerIndex].Reset();

        public void ResetAll()
        {
            foreach (var presenter in _playerPresenters)
                presenter.Reset();
        }
    }
}
