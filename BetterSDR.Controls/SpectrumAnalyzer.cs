using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterSDR.Controls {
    public enum BandType {
        Lower,
        Upper,
        Center
    }

    public partial class SpectrumAnalyzer : UserControl {
        private const int AxisMargin = 30;
        private const float MaxZoom = 4.0f;
        public int SpectrumWidth { get; set; }
        public int FilterBandwidth { get; set; }
        public int FilterOffset { get; set; }
        public bool EnableFilter { get; set; }
        public bool UseSmoothing { get; set; }
        public double Attack { get; set; }
        public double Decay { get; set; }
        public int StepSize { get; set; }
        public bool UseSnap { get; set; }
        public bool ShowMaxLine { get; set; }

        public int Zoom { get; set; }

        #region Spectrum Analyzer Settings

        public Color BackgroundColor { get; set; }
        public Color GridlinesColor { get; set; }
        public Color LabelColor { get; set; }
        public Color AxisColor { get; set; }
        public Color SpectrumColor { get; set; }

        #endregion
        private int _displayRange = 130;
        private int _displayOffset = 0;
        private long _spectrumWidth = 48000;
        private float _scale = 0.5f;
        private int _stepSize = 1000;
        private long _displayCenterFrequency = 0;
        private Bitmap _background;
        private Graphics _backgroundGraphics;
        private byte[] _scaledPowerSpectrum;
        private byte[] _spectrum;
        private byte[] _maxSpectrum;
        private long _frequency;
        private long _centerFrequency;
        private BandType _bandType;

        public SpectrumAnalyzer() {
            InitializeComponent();

            _background = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppPArgb);
            _backgroundGraphics = Graphics.FromImage(_background);
            LoadDefaults();

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            var length = ClientRectangle.Width - 2 * AxisMargin;
            _maxSpectrum = new byte[length];
            _spectrum = new byte[length];

            DrawBackground();
        }

        private void LoadDefaults() {
            BackgroundColor = Color.Black;
            AxisColor = Color.DarkGray;
            LabelColor = Color.Silver;
            GridlinesColor = Color.FromArgb(80, 80, 80);
            SpectrumColor = Color.Aqua;
            Attack = 0.9;
            Decay = 0.3;

            _backgroundGraphics.CompositingMode = CompositingMode.SourceOver;
            _backgroundGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            _backgroundGraphics.SmoothingMode = SmoothingMode.None;
            _backgroundGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            _backgroundGraphics.InterpolationMode = InterpolationMode.High;
        }

        #region Background Rendering
        private void RenderBackgroundGrid() {
            var gridPen = new Pen(GridlinesColor);
            var font = new Font("Arial", 8f);
            var fontBrush = new SolidBrush(LabelColor);
            gridPen.DashStyle = DashStyle.Dash;

            var powerMarkerCount = _displayRange / 10;
            var yIncrement = (ClientRectangle.Height - 2 * AxisMargin) / (float)powerMarkerCount;


            var displayOffset = _displayOffset / 10 * 10;

            for (var i = 0; i <= powerMarkerCount; i++) {
                // -- Draws the labels for each power line
                var db = (displayOffset - (powerMarkerCount - i) * 10).ToString();
                var sizeF = _backgroundGraphics.MeasureString(db, font);
                var x = AxisMargin - sizeF.Width - 3;
                var y = ClientRectangle.Height - AxisMargin - i * yIncrement - sizeF.Height / 2f;

                _backgroundGraphics.DrawString(db, font, fontBrush, x, y);

                if (i == 0) continue;
                // -- Draws horizontal lines for the dB level.
                var y1 = (int)(ClientRectangle.Height - AxisMargin - i * yIncrement);
                var x2 = (ClientRectangle.Width - AxisMargin);

                _backgroundGraphics.DrawLine(gridPen, AxisMargin, y1, x2, y1);

            }
        }

        private void RenderAxis() {
            var axisPen = new Pen(AxisColor);
            _backgroundGraphics.DrawLine(axisPen, AxisMargin, AxisMargin, AxisMargin, ClientRectangle.Height - AxisMargin);
            _backgroundGraphics.DrawLine(axisPen, AxisMargin, ClientRectangle.Height - AxisMargin, ClientRectangle.Width - AxisMargin, ClientRectangle.Height - AxisMargin);
        }
        public static string GetFrequencyDisplay(long frequency) {
            string result;
            if (frequency == 0)
                result = "DC";
            else if (Math.Abs(frequency) > 1500000000)
                result = $"{frequency / 1000000000.0:#,0.000 000}GHz";
            else if (Math.Abs(frequency) > 30000000)
                result = $"{frequency / 1000000.0:0,0.000#}MHz";
            else if (Math.Abs(frequency) > 1000)
                result = $"{frequency / 1000.0:#,#.###}kHz";
            else
                result = $"{frequency}Hz";

            return result;
        }

        private void RenderFrequencyMarkers() {
            var gridPen = new Pen(GridlinesColor);
            var font = new Font("Arial", 8f);
            var fontBrush = new SolidBrush(LabelColor);

            var baseLabelLength = (int)_backgroundGraphics.MeasureString("1,000,000.000kHz", font).Width;
            var frequencyStep = (int)(_spectrumWidth / _scale * baseLabelLength / (ClientRectangle.Width - 2 * AxisMargin));
            var stepSnap = _stepSize;
            frequencyStep = frequencyStep / stepSnap * stepSnap + stepSnap;

            var lineCount = (int)(_spectrumWidth / _scale / frequencyStep) + 4;
            var xIncrement = (ClientRectangle.Width - 2.0f * AxisMargin) * frequencyStep * _scale / _spectrumWidth;
            var centerShift = (int)((_displayCenterFrequency % frequencyStep) * (ClientRectangle.Width - 2.0 * AxisMargin) * _scale / _spectrumWidth);

            for (var i = -lineCount / 2; i < lineCount / 2; i++) {
                var x = (ClientRectangle.Width - 2 * AxisMargin) / 2 + AxisMargin + xIncrement * i - centerShift;

                // -- Draw vertical divider lines
                if (x >= AxisMargin && x <= ClientRectangle.Width - AxisMargin)
                    _backgroundGraphics.DrawLine(gridPen, x, AxisMargin, x, ClientRectangle.Height - AxisMargin);

                if (!(x >= AxisMargin) || !(x <= ClientRectangle.Width - AxisMargin)) continue;
                // -- Draw frequency label at bottom of graph
                var frequency = _displayCenterFrequency + i * frequencyStep - _displayCenterFrequency % frequencyStep;
                var fString = GetFrequencyDisplay(frequency);
                var sizeF = _backgroundGraphics.MeasureString(fString, font);

                x -= sizeF.Width / 2f;
                _backgroundGraphics.DrawString(fString, font, fontBrush, x, ClientRectangle.Height - AxisMargin + 8f);
            }
        }

        private void DrawBackground() {
            _backgroundGraphics.Clear(BackgroundColor);

            if (_spectrumWidth > 0) {
                RenderFrequencyMarkers();
            }

            RenderBackgroundGrid();
            // -- Draws the primary graph lines
            RenderAxis();

            pbPlot.Image = _background;
            pbPlot.Refresh();
        }

        #endregion

        #region Utility Functions

        private void ApplyZoom() {
            _scale = (float)Math.Pow(10, Zoom * MaxZoom * 100.0f);

            if (_spectrumWidth <= 0)
                return;

            //_displayCenterFrequency = GetDisplayCenterFrequency();
        }
        //private long GetDisplayCenterFrequency()
        //{
        //    var f = _frequency;

        //    switch (_bandType)
        //    {
        //        case BandType.Lower:
        //            f -= _filterBand
        //    }
        //}

        /// <summary>
        /// Fancy copy from a double to a byte.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="length"></param>
        /// <param name="minPower"></param>
        /// <param name="maxPower"></param>
        private static void ScaleFFT(double[] src, ref byte[] dest, int length, float minPower, float maxPower)
        {
            var scale = byte.MaxValue / (maxPower - minPower);

            for (var i = 0; i < length; i++) {
                var magnitude = src[i];

                if (magnitude < minPower) {
                    magnitude = minPower;
                }
                else if (magnitude > maxPower) {
                    magnitude = maxPower;
                }

                dest[i] = (byte)((magnitude - minPower) * scale);
            }
        }

        private static byte LargeR(float r, int i, byte[] srcPtr, int offset)
        {
            var n = (int)Math.Ceiling(r);
            var k = (int)(i * r - n * 0.5f);
            var max = (byte)0;
            
            for (var j = 0; j < n; j++) {
                var index = k + j + offset;
                if (index >= 0 && index < srcPtr.Length) {
                    if (max < srcPtr[index]) {
                        max = srcPtr[index];
                    }
                }
            }

            return max;
        }
        /// <summary>
        /// I... Really don't know.
        /// </summary>
        /// <param name="srcPtr"></param>
        /// <param name="dstPtr"></param>
        /// <param name="sourceLength"></param>
        /// <param name="destinationLength"></param>
        /// <param name="scale"></param>
        /// <param name="offset"></param>
        public static void SmoothCopy(byte[] srcPtr, ref byte[] dstPtr, int sourceLength, int destinationLength, float scale, int offset) {
            var r = sourceLength / scale / destinationLength;
            for (var i = 0; i < destinationLength; i++)
            {
                if (r > 1.0f)
                {
                    dstPtr[i] = LargeR(r, i, srcPtr, offset);
                    continue;
                }

                var index = (int)(r * i + offset);
                if (index >= 0 && index < sourceLength) {
                    dstPtr[i] = srcPtr[index];
                }
            }
        }

        #endregion
        public void Render(double[] spectrum) {
            if (_scaledPowerSpectrum == null || _scaledPowerSpectrum.Length != spectrum.Length)
                _scaledPowerSpectrum = new byte[spectrum.Length];

            var displayOffset = _displayOffset / 10 * 10;
            var displayRange = _displayRange / 10 * 10;
            var scaledLength = (int) (spectrum.Length / _scale);
            var offset = (int) ((spectrum.Length - scaledLength) / 2.0 +
                                spectrum.Length * (double) (_displayCenterFrequency - _centerFrequency) /
                                _spectrumWidth);

            ScaleFFT(spectrum, ref _scaledPowerSpectrum, spectrum.Length, displayOffset - displayRange, displayOffset);
            var temp = new byte[_spectrum.Length];

            if (UseSmoothing)
            {
                
                SmoothCopy(_scaledPowerSpectrum, ref temp, spectrum.Length, temp.Length, _scale, offset);

                for (var i = 0; i < _scaledPowerSpectrum.Length; i++)
                {
                    var ratio = _spectrum[i] < temp[i] ? Attack : Decay;
                    _spectrum[i] = (byte) Math.Round(_spectrum[i] * (1 - ratio) + temp[i] * ratio);
                }
            }
            else {
                SmoothCopy(_scaledPowerSpectrum, ref _spectrum, spectrum.Length, temp.Length, _scale, offset);
            }

            for (var i = 0; i < _spectrum.Length; i++)
            {
                if (_maxSpectrum[i] < _spectrum[i])
                    _maxSpectrum[i] = _spectrum[i];
            }

            Draw();
            pbPlot.Refresh();
        }

        private void Draw() {
            DrawBackground();
            DrawSpectrum();
        }

        private void ShiftImage() {

        }

        private void DrawSpectrum() {
            if (_spectrum == null || _spectrum.Length == 0)
                return;

            var xIncrement = (ClientRectangle.Width - 2 * AxisMargin) / (float)_spectrum.Length;
            var yIncrement = (ClientRectangle.Height - 2 * AxisMargin) / (float)byte.MaxValue;

            var spectrumPen = new Pen(SpectrumColor);
            var points = new Point[_spectrum.Length + 2];
            for (var i = 0; i < _spectrum.Length; i++) {
                var x = (int)(AxisMargin + i * xIncrement);
                var y = (int)(ClientRectangle.Height - AxisMargin - _spectrum[i] * yIncrement);
                points[i + 1] = new Point(x, y);
            }

            points[0] = points[1];
            points[points.Length - 1] = points[points.Length - 2];
            _backgroundGraphics.DrawLines(spectrumPen, points);
        }
        private void pbPlot_SizeChanged(object sender, EventArgs e) {

        }

        private void pbPlot_MouseDoubleClick(object sender, MouseEventArgs e) {

        }

        private void pbPlot_MouseDown(object sender, MouseEventArgs e) {

        }

        private void pbPlot_MouseMove(object sender, MouseEventArgs e) {

        }

        private void pbPlot_MouseUp(object sender, MouseEventArgs e) {

        }
    }
}
