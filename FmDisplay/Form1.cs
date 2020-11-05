using libRtlSdrSharp;
using MathNet.Numerics;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace FmDisplay {
    public partial class Form1 : Form {
        private RtlDevice _rtlDevice;
        double[] dataFft;
        double[] iFft;
        double[] qFft;
        double[] noFft;
        Int16[] dataPcm;
        Complex[] rawPcm;
        public Form1() {
            InitializeComponent();
            timer1.Interval = 1;
            initializePlot();
        }

        private void initializePlot() {
            
            if (iFft != null) {
                iPlot.plt.Clear();

                double fftSpacing = (double)_rtlDevice.SampleRate / iFft.Length;
                // -- plot for I branc
                iPlot.plt.Clear();
                var signal = iPlot.plt.PlotSignal(qFft, sampleRate: fftSpacing, markerSize: 0);
                signal.fillType = ScottPlot.FillType.FillBelow;
                signal.fillColor1 = Color.Blue;
                signal.gradientFillColor1 = Color.Transparent;

                iPlot.plt.PlotHLine(0, color: Color.Black, lineWidth: 1);
                iPlot.plt.YLabel("Power");
                iPlot.plt.XLabel("Frequency");
                iPlot.Render();
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            var mySdrDevices = RtlDevice.GetAvailableDevices();
            var myDevice = new RtlDevice(0);

            _rtlDevice = myDevice;
            // 1.090.000.000
            // 0.100.300.000
            myDevice.Frequency = 0100300000;
            myDevice.UseRtlAGC = false;

            myDevice.DataAvailable += MyDevice_DataAvailable;
            myDevice.Start();
            trackBar1.Maximum = 50;
            trackBar1.Minimum = 0;
            trackBar1.Value = myDevice.TunerGain;
        }

        private void MyDevice_DataAvailable() {
            Complex[] mySample = new Complex[0];

            lock (_rtlDevice.BufferLock) {
                if (_rtlDevice.SampleBufferDataReady > 0) {
                    _rtlDevice.SampleBufferDataOut &= (15);
                    mySample = new Complex[_rtlDevice.SampleBuffer[_rtlDevice.SampleBufferDataOut].Length];
                    Array.Copy(_rtlDevice.SampleBuffer[_rtlDevice.SampleBufferDataOut], mySample, mySample.Length);
                }
            }

            if (dataPcm == null)
                dataPcm = new Int16[mySample.Length];
            if (rawPcm == null)
                rawPcm = new Complex[mySample.Length];
            lock (rawPcm) {


                for (int i = 0; i < mySample.Length; i++) {
                    rawPcm[i] = mySample[i];
                    if (checkBox2.Checked)
                        dataPcm[i] = (short)mySample[i].Magnitude;
                    else
                        dataPcm[i] = (short)(mySample[i].Real * mySample[i].Real + mySample[i].Imaginary * mySample[i].Imaginary);
                }
            }
        }

        private void updateFFT() {
            if (rawPcm == null)
                return;

            // the PCM size to be analyzed with FFT must be a power of 2
            var myCopy = new Complex[0];
            lock (rawPcm)
            {
                var blackWindow = Window.Blackman((int) (rawPcm.Length));
                myCopy = new Complex[rawPcm.Length];

                for (var i = 0; i < rawPcm.Length; i++)
                {
                    var newReal = rawPcm[i].Real * blackWindow[i];
                    newReal = newReal * 10 - 1275;
                    var newImag = rawPcm[i].Imaginary * blackWindow[i];
                    newImag = newImag * 10 - 1275;
                    myCopy[i] = new Complex(newReal, newImag);
                }
            }

            // http://www.designnews.com/author.asp?section_id=1419&doc_id=236273&piddl_msgid=522392
            var fftGain = (float)(10.0 * Math.Log10((double)myCopy.Length / 2));
            var compensation = 24.0f - fftGain + -120.0f;

            Fourier.ForwardTransform(myCopy, myCopy.Length);
            

            if (iFft == null)
                iFft = new double[myCopy.Length];
            
            if (qFft == null)
                qFft = new double[myCopy.Length];

            Fourier.SpectrumPower(myCopy, ref qFft, myCopy.Length, compensation);
            spectrumAnalyzer1.Render(qFft);

            var scaledPower = new byte[rawPcm.Length];
            Fourier.ScaleFFT(qFft, ref scaledPower, scaledPower.Length, 0, 2000);

            var temp = new byte[scaledPower.Length];
            Fourier.SmoothCopy(scaledPower, ref temp, qFft.Length, scaledPower.Length, 1.0f, 0);

            for (var i = 0; i < qFft.Length; i++) {
                // -- ? Attack : Decay
                var ratio = qFft[i] < temp[i] ? 0.9 : 0.3;
                qFft[i] = Math.Round(qFft[i] * (1 - ratio) + temp[i] * ratio);
            }

            //for (var i = 0; i < rawPcm.Length; i++) {
            //    iFft[i] = rawPcm[i].Magnitude;
            //}

        }

        private void timer1_Tick(object sender, EventArgs e) {
            if (_rtlDevice == null || !_rtlDevice.IsStreaming)
                return;

            updateFFT();

            if (iPlot.plt.GetPlottables().Count == 0)
                initializePlot();

            iPlot.Render();
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            if (_rtlDevice != null && _rtlDevice.IsStreaming) {
                _rtlDevice.TunerGain = trackBar1.Value;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
        }

        private void Form1_Load(object sender, EventArgs e) {

        }
    }
}
