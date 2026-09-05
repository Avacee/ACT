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

        private Color _inactiveBackColor = Color.Black;
        private Color _activeBgColor = Color.Green;
        private Color _warningBgColor = Color.Red;
        private Color _textColor = Color.White;
        private PlayerPresenter _presenter;
        private int _singleCount;
        private int _groupCount;
        private int _singleSeconds;
        private int _groupSeconds;
        private int _warningCountThreshold = 1;
        private int _warningSecondsThreshold = 5;
        private Font _scaledFont;
        private float _scaledFontSize = -1f;

        public ctrlPlayer()
        {
            InitializeComponent();

            Resize += (s, e) => ScaleFontToControlSize();
            lblSingleCount.TextChanged += (s, e) => ScaleFontToControlSize();
            lblSingleTimer.TextChanged += (s, e) => ScaleFontToControlSize();
            lblGroupCount.TextChanged += (s, e) => ScaleFontToControlSize();
            lblGroupTimer.TextChanged += (s, e) => ScaleFontToControlSize();
            Disposed += (s, e) => _scaledFont?.Dispose();

            ScaleFontToControlSize();
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

        public void SetAppearance(Color backgroundColor, Color textColor, Color activeColor, Color thresholdColor)
        {
            _inactiveBackColor = backgroundColor;
            _textColor = textColor;
            _activeBgColor = activeColor;
            _warningBgColor = thresholdColor;

            lblSingleCount.ForeColor = _textColor;
            lblSingleTimer.ForeColor = _textColor;
            lblGroupCount.ForeColor = _textColor;
            lblGroupTimer.ForeColor = _textColor;

            ScaleFontToControlSize();
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

        private void ScaleFontToControlSize()
        {
            if (lblSingleCount == null || lblSingleTimer == null || lblGroupCount == null || lblGroupTimer == null)
                return;

            var labels = new[] { lblSingleCount, lblSingleTimer, lblGroupCount, lblGroupTimer };
            if (labels.Any(l => l.ClientSize.Width <= 0 || l.ClientSize.Height <= 0))
                return;

            var baseFont = lblSingleCount.Font;
            var minSize = 6f;
            var maxSize = Math.Max(minSize, Math.Min(48f, Height * 0.65f));
            var bestSize = minSize;

            for (float size = maxSize; size >= minSize; size -= 0.5f)
            {
                var fitsAll = true;
                using (var testFont = new Font(baseFont.FontFamily, size, baseFont.Style, GraphicsUnit.Point))
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        var label = labels[i];
                        var text = string.IsNullOrEmpty(label.Text) ? "0" : label.Text;
                        var measured = TextRenderer.MeasureText(text, testFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                        if (measured.Width > label.ClientSize.Width || measured.Height > label.ClientSize.Height)
                        {
                            fitsAll = false;
                            break;
                        }
                    }
                }

                if (fitsAll)
                {
                    bestSize = size;
                    break;
                }
            }

            if (Math.Abs(bestSize - _scaledFontSize) < 0.1f)
                return;

            _scaledFont?.Dispose();
            _scaledFont = new Font(baseFont.FontFamily, bestSize, baseFont.Style, GraphicsUnit.Point);
            _scaledFontSize = bestSize;

            for (int i = 0; i < labels.Length; i++)
                labels[i].Font = _scaledFont;
        }

        public void StartSingle(int count) => _presenter?.StartSingle(count);
        public void StartGroup(int count)  => _presenter?.StartGroup(count);
        public void TriggerSingle()        => _presenter?.UseSingle();
        public void TriggerGroup()         => _presenter?.UseGroup();
    }
}
