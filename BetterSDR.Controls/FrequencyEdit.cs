using System;
using System.Drawing;
using System.Windows.Forms;

namespace BetterSDR.Controls {
    public partial class FrequencyEdit : UserControl {
        public bool EntryModeActive { get; set; }
        public int StepSize { get; set; }
        public EntryMode EntryMode { get; set; }
        public long Frequency {
            get => _frequency;
            set {
                if (_frequency == value) return;
                _frequency = value;
                UpdateDigitsValues();
            }
        }

        // -- Internal state tracking
        private long _frequency;
        private readonly FrequencyEditDigit[] _digits = new FrequencyEditDigit[Constants.DigitCount];
        private readonly FrequencyEditSeperator[] _separators = new FrequencyEditSeperator[Constants.DigitSeparatorCount];

        public FrequencyEdit() {
            InitializeComponent();
            Configure();
        }

        #region Initialization
        private void Configure() {
            // -- Remove any previously existing controls
            RemoveExistingImages();
            // -- Re Generate them
            CreateChildControls();
            CalculateDigitWeight();
            this.Height = 22;
            UpdateDigitMask();
        }

        private void CreateChildControls() {
            var images = GenerateNumbers();
            var digitWidth = images[0].Width;
            var digitHeight = images[0].Height;
            var sepWidth = images[11].Width;
            int xPos = 0, yPos = 0, sepIndex = 0;

            for (var i = Constants.DigitCount - 1; i >= 0; i--) {
                // -- Add a comma every 3rd digit
                if ((i + 1) % 3 == 0 && i != (Constants.DigitCount - 1)) {
                    var separator = new FrequencyEditSeperator {
                        BackgroundImage = images[11],
                        Width = sepWidth,
                        Height = digitHeight,
                        Location = new Point(xPos-5, yPos)
                    };
                    Controls.Add(separator);
                    _separators[sepIndex++] = separator;
                    xPos += (sepWidth-5);
                }

                var newDigit = new FrequencyEditDigit(i) {
                    Location = new Point(xPos, yPos),
                    Width = digitWidth,
                    Height = digitHeight,
                    ImageList = images
                };

                Controls.Add(newDigit);
                _digits[i] = newDigit;
                xPos += digitWidth;
            }
        }

        private void CalculateDigitWeight() {
            long weight = 1L;
            for (var i = 0; i < Constants.DigitCount; i++) {
                _digits[i].Weight = weight;
                weight *= 10;
            }
        }

        private void RemoveExistingImages() {
            for (var i = 0; i < Constants.DigitCount; i++) {
                if (_digits[i] == null || !Controls.Contains(_digits[i])) continue;
                Controls.Remove(_digits[i]);
                _digits[i] = null;
            }

            for (var i = 0; i < Constants.DigitSeparatorCount; i++) {
                if (_separators[i] == null || !Controls.Contains(_digits[i])) continue;
                Controls.Remove(_separators[i]);
                _separators[i] = null;
            }
        }

        /// <summary>
        /// Generates an image of each number 0-9 to render to the FrequencyEditDigit controls
        /// </summary>
        /// <returns>Bitmap array of number images</returns>
        private Bitmap[] GenerateNumbers() {
            Bitmap[] numbers = new Bitmap[Constants.DigitImageSplitCount];
            var font = new Font("Noto Mono", 22f);
            var fontBrush = new SolidBrush(Color.Black);
            var newImage = new Bitmap(50, 22);
            var drawer = Graphics.FromImage(newImage);

            for (var i = 0; i < Constants.DigitImageSplitCount; i++) {
                var character = "" + Constants.DigitString[i];
                SizeF size = drawer.MeasureString(character, font);
                var characterImage = new Bitmap((int)size.Width, (int)size.Height);
                var characterDrawer = Graphics.FromImage(characterImage);
                characterDrawer.DrawString(character, font, fontBrush, 0.0f, 0.0f);
                numbers[i] = characterImage;
            }

            return numbers;
        }
        #endregion
        /// <summary>
        /// Redraws the child elements to reflect the current frequency value of the control
        /// </summary>
        private void UpdateDigitsValues() {
            if (_digits[0] == null)
                return;

            var currentFrequency = _frequency;

            for (var i = Constants.DigitCount - 1; i >= 0; i--) {
                var digit = currentFrequency / _digits[i].Weight;
                _digits[i].DisplayedDigit = (int)digit;
                currentFrequency -= (_digits[i].DisplayedDigit * _digits[i].Weight);
            }

            UpdateDigitMask();
        }

        /// <summary>
        /// Masks (Greys out) numbers/separators if it is a zero.
        /// </summary>
        private void UpdateDigitMask() {
            var frequency = _frequency;

            if (frequency < 0)
                return;

            for (var i = 1; i < Constants.DigitCount; i++) {
                if ((i + 1) % 3 == 0 && i != Constants.DigitCount - 1) {
                    var separatorIndex = i / 3;
                    if (_separators[separatorIndex] != null) {
                        _separators[separatorIndex].Masked = (_digits[i + 1].Weight > frequency);
                    }
                }
                if (_digits[i] != null)
                    _digits[i].Masked = (_digits[i].Weight > frequency);
            }
        }

        private void renderTimer_Tick(object sender, EventArgs e) {
            foreach (var ctrl in Controls)
                if (ctrl is IRenderable renderable)
                    renderable.Render();
        }
    }
}
