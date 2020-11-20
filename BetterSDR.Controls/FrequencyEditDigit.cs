using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterSDR.Controls {
    public partial class FrequencyEditDigit : UserControl {
        public bool IsCursorInside { get; private set; }
        public long Weight { get; set; }

        private int _displayedDigit;
        public int DisplayedDigit {
            get => _displayedDigit;
            set {
                if (value < 0 || value > 9) return;
                if (_displayedDigit == value) return;

                _displayedDigit = value;
                _isRenderNeeded = true;
            }
        }
        public bool Masked {
            get => _masked;
            set {
                if (_masked == value) return;
                _masked = value;
                _isRenderNeeded = true;
            }
        }
        private bool _highlight;

        public bool Highlight {
            get => _highlight;
            set {
                _highlight = value;
                _isRenderNeeded = true;
            }
        }
        // -- State tracking fields
        private bool _isRenderNeeded;
        private bool _isUpperHalf;
        private bool _masked;
        private int _lastMouseY;
        private bool _isLastUpperHalf;
        private int _digitIndex;


        private readonly ImageAttributes _maskedAttributes = new ImageAttributes();
        public Bitmap[] ImageList { get; set; }

        public FrequencyEditDigit(int digitIndex) {
            InitializeComponent();
            _digitIndex = digitIndex;
            _displayedDigit = 0;
            var cm = new ColorMatrix {Matrix33 = Constants.MaskedDigitTransparency};
            _maskedAttributes.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
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
            DrawNumber(e);
            DrawMouseover(e);
            DrawHighlight(e);
        }

        private void DrawHighlight(PaintEventArgs e) {
            if (!_highlight) return;

            var transparentColor = new SolidBrush(Color.FromArgb(25, Color.Red));
            e.Graphics.FillRectangle(transparentColor, new Rectangle(0, 0, ClientRectangle.Width, ClientRectangle.Height));
        }

        private void DrawNumber(PaintEventArgs e) {
            if (ImageList == null || DisplayedDigit >= ImageList.Length)
                return;

            var img = ImageList[DisplayedDigit];
            var attributes = ((_masked && !IsCursorInside) || !Parent.Enabled) ? _maskedAttributes : null;
            var rectangle = new Rectangle(0, 0, img.Width, img.Height);
            e.Graphics.DrawImage(img, rectangle, 0.0f, 0.0f, img.Width, img.Height, GraphicsUnit.Pixel, attributes);
        }

        private void DrawMouseover(PaintEventArgs e) {
            if (!IsCursorInside || ((FrequencyEdit)base.Parent).EntryModeActive)
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
