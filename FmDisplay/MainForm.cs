using BetterSDR.Common;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using BetterSDR.RTLSDR;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Drawing;
using Complex = System.Numerics.Complex;
using Fourier = BetterSDR.Common.Fourier;

namespace BetterSDR {
    public partial class MainForm : Form {
        private RtlDevice _rtlDevice;
        private double[] _qFft;
        private Complex[] _rawPcm;
        private readonly object _pcmLock = new object();
        private Thread _renderThread;

        public MainForm() {
            InitializeComponent();
            InitializePlot();
            frequencyEdit1.Frequency = 0100300000;
        }

        private void InitializePlot() {
            if (this.InvokeRequired) {
                this.Invoke(new BlankEventArgs(InitializePlot));
                return;
            }

            if (_qFft == null)
                return;

            iPlot.plt.Clear();

            double fftSpacing = (double)_rtlDevice.SampleRate / _qFft.Length;
            iPlot.plt.Clear();
            PlottableSignal signal = iPlot.plt.PlotSignal(_qFft, fftSpacing, markerSize: 0, useParallel: false);
            signal.fillType = FillType.FillBelow;
            signal.fillColor1 = Color.Blue;
            signal.gradientFillColor1 = Color.Transparent; 
            iPlot.plt.PlotHLine(0, Color.Black);
            iPlot.plt.YLabel("Power");
            iPlot.plt.XLabel("Frequency");
            iPlot.plt.Style(Style.Gray1);
            iPlot.plt.Colorset(Colorset.OneHalfDark);
            iPlot.Render();
        }

        private void button1_Click(object sender, EventArgs e) {
            var myDevice = new RtlDevice(1);

            _rtlDevice = myDevice;
            _rtlDevice.SampleRate = (uint)(2.048 * 1000000.0);
            _rtlDevice.UseOffsetTuning = false;
            _rtlDevice.SamplingMode = 0;
            _rtlDevice.FrequencyCorrection = 0;
            _rtlDevice.UseRtlAGC = true;
            _rtlDevice.UseTunerAGC = false;
            _rtlDevice.TunerGain = 496;
            _rtlDevice.DataAvailable += MyDevice_DataAvailable;
            // 1.090.000.000
            // 0.100.300.000
            myDevice.Frequency = 0100300000;
            myDevice.Start();

            if (_renderThread == null) {
                _renderThread = new Thread(() => {
                    while (true) {
                        if (_rtlDevice == null) {
                            Thread.Sleep(2);
                            continue;
                        }

                        updateFFT();

                        if (iPlot.plt.GetPlottables().Count == 0)
                            InitializePlot();

                        iPlot.Render();
                        Thread.Sleep(1);
                    }
                });

                _renderThread.Start();
                
            }
        }

        private void MyDevice_DataAvailable() {
            var readLength = (int)(_rtlDevice.SampleRate / 2);

            if (readLength > _rtlDevice.Buffer.Length)
                readLength = _rtlDevice.Buffer.Length;

            Complex[] mySample = _rtlDevice.Buffer.Read(readLength);
            lock (_pcmLock) {
                _rawPcm = mySample;
            }
        }
        public delegate void BlankEventArgs();
        private void updateFFT() {
            if (this.InvokeRequired) {
                this.Invoke(new BlankEventArgs(updateFFT));
                return;
            }

            if (_rawPcm == null)
                return;

            Complex[] myCopy;

            lock (_pcmLock) {
                double[] blackWindow = Window.Blackman(_rawPcm.Length);
                myCopy = new Complex[_rawPcm.Length];

                for (var i = 0; i < _rawPcm.Length; i++) {
                    double newReal = _rawPcm[i].Real * blackWindow[i];
                    double newImag = _rawPcm[i].Imaginary * blackWindow[i];

                    myCopy[i] = new Complex(newReal, newImag);
                }
            }

            // http://www.designnews.com/author.asp?section_id=1419&doc_id=236273&piddl_msgid=522392
            var fftGain = (float)(10.0 * Math.Log10((double)myCopy.Length / 2));
            float compensation = 24.0f - fftGain + -60.0f;

            Fourier.ForwardTransform(myCopy, myCopy.Length);

            if (_qFft == null)
                _qFft = new double[myCopy.Length];

            Fourier.SpectrumPower(myCopy, ref _qFft, myCopy.Length, compensation);
            spectrumAnalyzer1.Render(_qFft);

            //var scaledPower = new byte[_rawPcm.Length];
            //Fourier.ScaleFFT(_qFft, ref scaledPower, scaledPower.Length, 0, 2000);

            //var temp = new byte[scaledPower.Length];
            //Fourier.SmoothCopy(scaledPower, ref temp, _qFft.Length, scaledPower.Length, 1.0f, 0);

            //for (var i = 0; i < _qFft.Length; i++) {
            //    // -- ? Attack : Decay
            //    double ratio = _qFft[i] < temp[i] ? 0.9 : 0.3;
            //    _qFft[i] = Math.Round(_qFft[i] * (1 - ratio) + temp[i] * ratio);
            //}
        }

        private void Form1_Load(object sender, EventArgs e) {

        }

        private void frequencyEdit1_FrequencyUpdated(long frequency) {
            _rtlDevice.Frequency = (uint) frequency;
        }
    }
}
