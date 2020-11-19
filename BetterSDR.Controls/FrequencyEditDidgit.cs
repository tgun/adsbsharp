using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterSDR.Controls {
    public partial class FrequencyEditDidgit : UserControl {
        public bool IsCursorInside { get; set; }
        public int DisplayedDigit { get; set; }

        // -- State tracking fields
        private bool _isRenderNeeded;
        private bool _isUpperHalf;
        private int _lastMouseY;
        private bool _isLastUpperHalf;
        private int _digitIndex;

        private bool _highlight;

        // -- mehroihasd
        public Bitmap[] ImageList { get; set; }

        public FrequencyEditDidgit(int digitIndex) {
            InitializeComponent();
            _digitIndex = digitIndex;
        }

        #region Mouse Events

        private void FrequencyEditDidgit_MouseDown(object sender, MouseEventArgs e) {

        }

        private void FrequencyEditDidgit_MouseEnter(object sender, EventArgs e) {
            IsCursorInside = true;
            _isRenderNeeded = true;
            Focus();
        }

        private void FrequencyEditDidgit_MouseLeave(object sender, EventArgs e) {
            IsCursorInside = false;
            _isRenderNeeded = true;
        }

        private void FrequencyEditDidgit_MouseMove(object sender, MouseEventArgs e) {
            _isLastUpperHalf = e.Y <= ClientRectangle.Height / 2;
            _lastMouseY = e.Y;

            if (_isUpperHalf != _isLastUpperHalf) {
                _isRenderNeeded = true;
            }

            _isLastUpperHalf = _isUpperHalf;
        }

        private void FrequencyEditDidgit_MouseUp(object sender, MouseEventArgs e) {

        }

        private void FrequencyEditDidgit_Scroll(object sender, ScrollEventArgs e) {

        }

        #endregion

        private void FrequencyEditDidgit_Paint(object sender, PaintEventArgs e) {
            if (ImageList != null && DisplayedDigit < ImageList.Length) {
                var img = ImageList[DisplayedDigit];

            }

            DrawMouseover(e);
        }

        private void DrawNumber() {

        }

        private void DrawMouseover(PaintEventArgs e) {
            if (!IsCursorInside || ((FrequencyEdit) base.Parent).EntryModeActive) 
                return;

            bool isUpperHalf = (_lastMouseY <= ClientRectangle.Height / 2);
            Color transparentColor = Color.FromArgb(100, isUpperHalf ? Color.Red : Color.Blue);

            using (var transparentBrush = new SolidBrush(transparentColor)) {
                Rectangle rect;

                if (isUpperHalf)
                    rect = new Rectangle(0, 0, ClientRectangle.Width, ClientRectangle.Height / 2);
                else
                    rect = new Rectangle(0, ClientRectangle.Height / 2, ClientRectangle.Width, ClientRectangle.Height);

                e.Graphics.FillRectangle(transparentBrush, rect);
            }
        }
    }
}
