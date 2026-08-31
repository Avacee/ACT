using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReactiveTracker.Presenters;

namespace ReactiveTracker
{
    // Passes WM_NCHITTEST up to the parent so frmReactiveOverlay handles dragging/resizing
    public class DraggableTableLayoutPanel : TableLayoutPanel
    {
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST   = 0x0084;
            const int HTTRANSPARENT = -1;

            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)HTTRANSPARENT;
            else
                base.WndProc(ref m);
        }
    }

    public partial class ctrlPlayer : UserControl, IPlayerView
    {
        private const int WM_NCHITTEST   = 0x0084;
        private const int HTTRANSPARENT = -1;

        private readonly Color _inactiveBackColor = Color.Black;
        private readonly Color _activeBgColor = Color.Green;
        private readonly Color _warningBgColor = Color.Red;
        private PlayerPresenter _presenter;
        private int _singleCount;
        private int _groupCount;
        private int _singleSeconds;
        private int _groupSeconds;
        private int _warningCountThreshold = 1;
        private int _warningSecondsThreshold = 5;

        public ctrlPlayer()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)HTTRANSPARENT;
            else
                base.WndProc(ref m);
        }

        public void SetPresenter(PlayerPresenter presenter)
        {
            _presenter = presenter;
        }

        public void SetWarningThresholds(int countThreshold, int secondsThreshold)
        {
            _warningCountThreshold = countThreshold < 0 ? 0 : countThreshold;
            _warningSecondsThreshold = secondsThreshold < 0 ? 0 : secondsThreshold;
            UpdateAlertState();
        }

        // IPlayerView
        public void SetSingleCount(int count)
        {
            _singleCount = count;
            lblSingleCount.Text = count.ToString();
            UpdateAlertState();
        }

        public void SetGroupCount(int count)
        {
            _groupCount = count;
            lblGroupCount.Text = count.ToString();
            UpdateAlertState();
        }

        public void SetSingleTimer(int seconds)
        {
            _singleSeconds = seconds;
            lblSingleTimer.Text = seconds > 0 ? $"{seconds}s" : string.Empty;
            UpdateAlertState();
        }

        public void SetGroupTimer(int seconds)
        {
            _groupSeconds = seconds;
            lblGroupTimer.Text = seconds > 0 ? $"{seconds}s" : string.Empty;
            UpdateAlertState();
        }

        private void UpdateAlertState()
        {
            // Determine states
            var isSingleInactive = _singleCount == 0 || _singleSeconds == 0;
            var isGroupInactive = _groupCount == 0 || _groupSeconds == 0;

            var isSingleWarning = _singleCount > 0 && (_singleCount <= _warningCountThreshold || (_singleSeconds > 0 && _singleSeconds <= _warningSecondsThreshold));
            var isGroupWarning = _groupCount > 0 && (_groupCount <= _warningCountThreshold || (_groupSeconds > 0 && _groupSeconds <= _warningSecondsThreshold));

            var isSingleActive = _singleCount > _warningCountThreshold && _singleSeconds > _warningSecondsThreshold;
            var isGroupActive = _groupCount > _warningCountThreshold && _groupSeconds > _warningSecondsThreshold;

            // Apply colors: Red > Green > Black (priority order)
            Color singleColor = isSingleWarning ? _warningBgColor : (isSingleActive ? _activeBgColor : _inactiveBackColor);
            Color groupColor = isGroupWarning ? _warningBgColor : (isGroupActive ? _activeBgColor : _inactiveBackColor);

            SetBgColor(lblSingleCount, singleColor);
            SetBgColor(lblSingleTimer, singleColor);
            SetBgColor(lblGroupCount, groupColor);
            SetBgColor(lblGroupTimer, groupColor);
        }

        private void SetBgColor(Label label, Color color)
        {
            if (label.BackColor != color)
                label.BackColor = color;
        }

        public void StartSingle(int count) => _presenter?.StartSingle(count);
        public void StartGroup(int count)  => _presenter?.StartGroup(count);
        public void TriggerSingle()        => _presenter?.UseSingle();
        public void TriggerGroup()         => _presenter?.UseGroup();
    }
}
