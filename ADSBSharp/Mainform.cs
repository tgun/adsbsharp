using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Numerics;
using BetterSDR.RTLSDR;
using System.IO;

namespace ADSBSharp {
    public partial class MainForm : Form {
        public static int SSDetected, ESDetected, OtherDetected;
        public static int SSDecoded, ESDecoded, OtherDecoded;

        private IFrameSink _frameSink;
        private int _selectedDeviceIndex;
        private readonly AdsbBitDecoder _decoder = new AdsbBitDecoder();
        private AlternateDecoder _alternateDecoder;
        private RtlDevice _rtlDevice;
        private MessageDisplay _displayWindow;
        private bool _isDecoding;
        private bool _initialized;
        private int _frameCount;
        private int _newFrameCount;
        private float _avgFps;
        private float _newAvg;
        private Dictionary<int, string> _rtlDevices;
        private BinaryWriter _magWriter;
        private BinaryWriter _rawWriter;
        private List<Aircraft> _currentAircraft = new List<Aircraft>();

        public MainForm() {
            InitializeComponent();
            Text = "ADSB# v" + Assembly.GetExecutingAssembly().GetName().Version;

            _decoder.FrameReceived += delegate (byte[] frame, int length) {
                Interlocked.Increment(ref _frameCount);
                _frameSink.FrameReady(frame, length);
                ProcessAircraft(frame);
            };

            portNumericUpDown_ValueChanged(null, null);
            confidenceNumericUpDown_ValueChanged(null, null);
            timeoutNumericUpDown_ValueChanged(null, null);

            try {
                _rtlDevices = RtlDevice.GetAvailableDevices();
                deviceComboBox.Items.Clear();
                deviceComboBox.Items.AddRange(_rtlDevices.Values.ToArray());

                deviceComboBox.SelectedIndex = 0;
                deviceComboBox_SelectedIndexChanged(null, null);
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        #region GUI Controls

        private void startBtn_Click(object sender, EventArgs e) {
            if (!_isDecoding) {
                StartDecoding();
            }
            else {
                StopDecoding();
            }

            startBtn.Text = _isDecoding ? "Stop" : "Start";
            deviceComboBox.Enabled = !_rtlDevice.IsStreaming;
            portNumericUpDown.Enabled = !_rtlDevice.IsStreaming;
            shareCb.Enabled = !_rtlDevice.IsStreaming;
            hostnameTb.Enabled = !_rtlDevice.IsStreaming && shareCb.Checked;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (_isDecoding) 
                StopDecoding();
        }

        private int GetCurrentlySelectedItemIndex() {
            KeyValuePair<int, string> item = _rtlDevices.FirstOrDefault(a => a.Value == (string)deviceComboBox.SelectedItem);
            return item.Value == null ? -1 : item.Key;
        }

        private void deviceComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            var deviceDisplay = (string)deviceComboBox.SelectedItem;
            
            if (deviceDisplay == null) 
                return;

            try {
                _selectedDeviceIndex = GetCurrentlySelectedItemIndex();

                if (_selectedDeviceIndex == -1)
                    return;

                if (_initialized) {
                    _rtlDevice.Stop();
                    _rtlDevice.Dispose();
                }

                _rtlDevice = new RtlDevice(_selectedDeviceIndex);
                _rtlDevice.DataAvailable += _rtlDevice_DataAvailable;
                _rtlDevice.Frequency = 1090000000;
                _rtlDevice.SampleRate = 2000000;
                
                _initialized = true;
            }
            catch (Exception ex) {
                deviceComboBox.SelectedIndex = -1;
                _initialized = false;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ConfigureDevice();
            ConfigureGUI();
        }

        private void _rtlDevice_DataAvailable() {
            var readLength = (int)(_rtlDevice.SampleRate / 2);

            if (readLength > _rtlDevice.Buffer.Length)
                readLength = _rtlDevice.Buffer.Length;

            Complex[] samples = _rtlDevice.Buffer.Read(readLength);
            _alternateDecoder.DeviceOnRtlSdrDataAvailable(samples);

            foreach (Complex sample in samples) {
                double imaginary = sample.Imaginary * 10 - 1275;
                double real = sample.Real * 10 - 1275;
                var mag = (int)(real * real + imaginary * imaginary);
                var magBytes = BitConverter.GetBytes(mag);
                var complexBYtes = BitConverter.GetBytes(sample.Magnitude);
                
                _magWriter?.Write(magBytes);
                _rawWriter?.Write(complexBYtes);

                _decoder.ProcessSample(mag);
            }

            
        }

        private void tunerGainTrackBar_Scroll(object sender, EventArgs e) {
            if (!_initialized) {
                return;
            }

            int gain = _rtlDevice.SupportedGains[tunerGainTrackBar.Value];
            _rtlDevice.TunerGain = gain;
            gainLabel.Text = gain / 10.0 + " dB";
        }

        private void tunerAgcCheckBox_CheckedChanged(object sender, EventArgs e) {
            if (!_initialized)
                return;

            tunerGainTrackBar.Enabled = tunerAgcCheckBox.Enabled && !tunerAgcCheckBox.Checked;
            _rtlDevice.UseTunerAGC = tunerAgcCheckBox.Checked;
            gainLabel.Visible = tunerAgcCheckBox.Enabled && !tunerAgcCheckBox.Checked;
            
            if (!tunerAgcCheckBox.Checked) 
                tunerGainTrackBar_Scroll(null, null);
        }

        private void frequencyCorrectionNumericUpDown_ValueChanged(object sender, EventArgs e) {
            if (!_initialized)
                return;

            _rtlDevice.FrequencyCorrection = (int)frequencyCorrectionNumericUpDown.Value;
        }

        private void rtlAgcCheckBox_CheckedChanged(object sender, EventArgs e) {
            if (!_initialized)
                return;

            _rtlDevice.UseRtlAGC = rtlAgcCheckBox.Checked;
        }

        #endregion

        #region Private Methods

        private void ConfigureGUI() {
            startBtn.Enabled = _initialized;

            if (!_initialized) {
                return;
            }

            tunerTypeLabel.Text = _rtlDevice.TunerType.ToString();
            tunerGainTrackBar.Maximum = _rtlDevice.SupportedGains.Length - 1;
            tunerGainTrackBar.Value = tunerGainTrackBar.Maximum;

            for (var i = 0; i < deviceComboBox.Items.Count; i++) {
                int deviceIndex = _rtlDevices.FirstOrDefault((a) => a.Value == (string)deviceComboBox.Items[i]).Key;
                
                if (deviceIndex != _rtlDevice.Index) 
                    continue;

                deviceComboBox.SelectedIndex = i;
                break;
            }
        }

        private void ConfigureDevice() {
            frequencyCorrectionNumericUpDown_ValueChanged(null, null);
            rtlAgcCheckBox_CheckedChanged(null, null);
            tunerAgcCheckBox_CheckedChanged(null, null);

            if (!tunerAgcCheckBox.Checked) tunerGainTrackBar_Scroll(null, null);
        }

        private void StartDecoding() {
            try {
                if (shareCb.Checked)
                    _frameSink = new AdsbHubClient();
                else
                    _frameSink = new SimpleTcpServer();

                _frameSink.Start(hostnameTb.Text, (int)portNumericUpDown.Value);
            }
            catch (Exception e) {
                StopDecoding();
                MessageBox.Show("Unable to start networking\n" + e.Message);
                return;
            }
            
            _alternateDecoder = new AlternateDecoder();

            _alternateDecoder.FrameReceived += delegate (byte[] frame, int length) {
                Interlocked.Increment(ref _newFrameCount);
                _frameSink.FrameReady(frame, length);
                ProcessAircraft(frame);
            };

            try {
                _rtlDevice.Start();
            }
            catch (Exception e) {
                StopDecoding();
                MessageBox.Show("Unable to start RTL device\n" + e.Message);
                return;
            }

            _isDecoding = true;
        }

        private void StopDecoding() {
            _rtlDevice.Stop();
            _frameSink.Stop();
            _frameSink = null;
            _isDecoding = false;
            _avgFps = 0f;
            _frameCount = 0;
        }

        #endregion
        private void UpdateGuiStats() {
            txtDecodedES.Text = ESDecoded.ToString();
            txtDecodedOther.Text = OtherDecoded.ToString();
            txtDecodedSS.Text = SSDecoded.ToString();
            txtDetectedES.Text = ESDetected.ToString();
            txtDetectedSS.Text = SSDetected.ToString();
            txtDetectedOther.Text = OtherDetected.ToString();
        }

        private void ProcessAircraft(byte[] frame) {
            var msg = libModeSharp.ModeSMessage.DecodeMessage(frame);
            
            if (!msg.IsCrcOk)
                return;

            Aircraft currentAC;
            if (!_currentAircraft.Any(a => a.ICAO == msg.ICAO)) {
                var ac = new Aircraft() {
                    ICAO = msg.ICAO
                };
                lock (_currentAircraft) {
                    _currentAircraft.Add(ac);
                }
                currentAC = ac;
            } else {
                currentAC = _currentAircraft.FirstOrDefault(b => b.ICAO == msg.ICAO);
            }

            if (msg.Velocity != 0)
            currentAC.CurrentSpeed = msg.Velocity;
            
            if (!string.IsNullOrEmpty(msg.Flight))
                currentAC.FlightID = msg.Flight;
            if (msg.Altitude != 0)
                currentAC.CurrentAltitude = msg.Altitude;
            currentAC.idk = msg.Identity.ToString();
            currentAC.LastMessage = DateTime.Now;
        }

        private void UpdateAircraftDisplay() {
            listView1.Items.Clear();
            lock (_currentAircraft) {
                foreach (var ac in _currentAircraft) {
                    var meh = new ListViewItem(new string[] { ac.ICAO.ToString("x"), ac.FlightID != null ? ac.FlightID.ToString() : "", ac.CurrentAltitude.ToString(), ac.CurrentSpeed.ToString(), ac.idk != null ? ac.idk : ""});
                    listView1.Items.Add(meh);
                }

                _currentAircraft.RemoveAll(a => (DateTime.Now - a.LastMessage).TotalSeconds > 120);
            }
        }

        private void fpsTimer_Tick(object sender, EventArgs e) {
            float fps = (_frameCount) * 1000.0f / fpsTimer.Interval;
            float nfps = (_newFrameCount) * 1000.0f / fpsTimer.Interval;

            _frameCount = 0;
            _newFrameCount = 0;

            _avgFps = 0.9f * _avgFps + 0.1f * fps;
            _newAvg = 0.9f * _newAvg + 0.1f * nfps;

            fpsLabel.Text = ((int)_avgFps).ToString();
            label8.Text = ((int) _newAvg).ToString();
            UpdateGuiStats();
            UpdateAircraftDisplay();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {
            WindowState = FormWindowState.Normal;
        }

        private void portNumericUpDown_ValueChanged(object sender, EventArgs e) {
            notifyIcon.Text = Text + " on port " + portNumericUpDown.Value;
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        private void confidenceNumericUpDown_ValueChanged(object sender, EventArgs e) {
            _decoder.ConfidenceLevel = (int)confidenceNumericUpDown.Value;
        }

        private void timeoutNumericUpDown_ValueChanged(object sender, EventArgs e) {
            _decoder.Timeout = (int)timeoutNumericUpDown.Value;
        }

        private void shareCb_CheckedChanged(object sender, EventArgs e) {
            hostnameTb.Enabled = shareCb.Checked;
        }

        private void biasTeeCheckbox_CheckedChanged(object sender, EventArgs e) {
            if (!_initialized)
                return;

            _rtlDevice.BiasTee = biasTeeCheckbox.Checked;
        }

        private void btnDebug_Click(object sender, EventArgs e) {
            _magWriter = new BinaryWriter(File.Open("mag.raw", FileMode.OpenOrCreate));
            _rawWriter = new BinaryWriter(File.Open("raw.raw", FileMode.OpenOrCreate));

            StartDecoding();
            //if (_displayWindow != null && _displayWindow.IsDisposed == false && _displayWindow.Visible == true)
            //    return;

            //_displayWindow = new MessageDisplay();
            //_decoder.FrameReceived += _displayWindow.ReceiveOldFrame;

        }

        private void MainForm_Load(object sender, EventArgs e) {

        }

        private void button1_Click(object sender, EventArgs e) {
            _magWriter.Close();
            _rawWriter.Close();
            _magWriter.Dispose();
            _rawWriter.Dispose();
            StopDecoding();
        }
    }
}
