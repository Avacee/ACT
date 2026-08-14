using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ReactiveTracker
{
    public partial class frmReactiveOverlay : Form
    {
        private const int BorderSize = 4;

        // SetWindowPos
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE    = 0x0002;
        private const uint SWP_NOSIZE    = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int  WM_WINDOWPOSCHANGING = 0x0046;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        // WM_NCHITTEST result codes
        private const int HTCLIENT      = 1;
        private const int HTCAPTION     = 2;
        private const int HTLEFT        = 10;
        private const int HTRIGHT       = 11;
        private const int HTTOP         = 12;
        private const int HTTOPLEFT     = 13;
        private const int HTTOPRIGHT    = 14;
        private const int HTBOTTOM      = 15;
        private const int HTBOTTOMLEFT  = 16;
        private const int HTBOTTOMRIGHT = 17;

        private ctrlPlayer[] _playerControls;

        public frmReactiveOverlay()
        {
            InitializeComponent();
            this.Padding = new Padding(BorderSize);
            this.HandleCreated += (s, e) =>
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public void SetPlayerControls(ctrlPlayer[] controls)
        {
            if (_playerControls != null)
            {
                foreach (var ctrl in _playerControls)
                    this.Controls.Remove(ctrl);
            }

            _playerControls = controls;

            foreach (var ctrl in _playerControls)
            {
                ctrl.BackColor = this.BackColor;
                this.Controls.Add(ctrl);
                ctrl.BringToFront();
            }

            LayoutPlayerControls();
        }

        private void LayoutPlayerControls()
        {
            if (_playerControls == null || _playerControls.Length == 0)
                return;

            int innerX = BorderSize;
            int innerY = BorderSize;
            int innerW = this.ClientSize.Width  - BorderSize * 2;
            int innerH = this.ClientSize.Height - BorderSize * 2;

            int sectionW = innerW / _playerControls.Length;

            for (int i = 0; i < _playerControls.Length; i++)
            {
                _playerControls[i].SetBounds(
                    innerX + i * sectionW,
                    innerY,
                    sectionW,
                    innerH);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPlayerControls();
            this.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            base.WndProc(ref m);

            if (m.Msg == WM_WINDOWPOSCHANGING)
            {
                var pos = (WINDOWPOS)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));
                pos.hwndInsertAfter = HWND_TOPMOST;
                System.Runtime.InteropServices.Marshal.StructureToPtr(pos, m.LParam, false);
            }

            if (m.Msg == WM_NCHITTEST)
            {
                var cursor = this.PointToClient(System.Windows.Forms.Cursor.Position);
                int x = cursor.X;
                int y = cursor.Y;
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;

                bool left   = x < BorderSize;
                bool right  = x >= w - BorderSize;
                bool top    = y < BorderSize;
                bool bottom = y >= h - BorderSize;

                if (top    && left)  m.Result = (IntPtr)HTTOPLEFT;
                else if (top    && right)  m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left)  m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right)  m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left)             m.Result = (IntPtr)HTLEFT;
                else if (right)            m.Result = (IntPtr)HTRIGHT;
                else if (top)              m.Result = (IntPtr)HTTOP;
                else if (bottom)           m.Result = (IntPtr)HTBOTTOM;
                else if (m.Result == (IntPtr)HTCLIENT)
                    m.Result = (IntPtr)HTCAPTION; // drag anywhere on the client area
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.DimGray, BorderSize))
            {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1));
            }

            using (var dividerPen = new Pen(Color.DimGray, 1))
            {
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;
                float sectionWidth = w / 6f;

                for (int i = 1; i <= 5; i++)
                {
                    int x = (int)(sectionWidth * i);
                    e.Graphics.DrawLine(dividerPen, x, BorderSize, x, h - BorderSize);
                }
            }
        }

            }
        }
