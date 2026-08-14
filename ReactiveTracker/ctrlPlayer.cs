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

        private PlayerPresenter _presenter;

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

        // IPlayerView
        public void SetSingleCount(int count)    => lblSingleCount.Text = count.ToString();
        public void SetGroupCount(int count)     => lblGroupCount.Text = count.ToString();
        public void SetSingleTimer(int seconds)  => lblSingleTimer.Text = seconds > 0 ? $"{seconds}s" : string.Empty;
        public void SetGroupTimer(int seconds)   => lblGroupTimer.Text = seconds > 0 ? $"{seconds}s" : string.Empty;

        public void StartSingle(int count) => _presenter?.StartSingle(count);
        public void StartGroup(int count)  => _presenter?.StartGroup(count);
        public void TriggerSingle()        => _presenter?.UseSingle();
        public void TriggerGroup()         => _presenter?.UseGroup();
    }
}
