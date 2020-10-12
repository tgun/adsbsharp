using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace libRtlSdrSharp {
    public enum SamplingMode {
        Quadrature = 0,
        DirectSamplingI,
        DirectSamplingQ
    }

    public sealed unsafe class RtlDevice : IDisposable {
        private const uint DefaultFrequency = 1090000000;
        private const int DefaultSampleRate = 2000000;
        private IntPtr _dev;
        private bool _useTunerAgc = true;
        private bool _useRtlAgc;
        private int _tunerGain;
        private uint _centerFrequency = DefaultFrequency;
        private uint _sampleRate = DefaultSampleRate;
        private int _frequencyCorrection;
        private SamplingMode _samplingMode;
        private bool _useOffsetTuning;
        private GCHandle _gcHandle;
        private UnsafeBuffer _iqBuffer;
        private Complex* _iqPtr;
        private Thread _worker;
        private readonly SamplesAvailableEventArgs _eventArgs = new SamplesAvailableEventArgs();
        private static readonly RtlSdrReadAsyncDelegate RtlCallback = RtlSdrSamplesAvailable;
        private static readonly uint _readLength = 16 * 1024;

        public RtlDevice(uint index) {
            Index = index;
            int r = LibraryWrapper.rtlsdr_open(out _dev, Index);
            if (r != 0) {
                throw new ApplicationException("Cannot open RTL device. Is the device locked somewhere?");
            }
            int count = _dev == IntPtr.Zero ? 0 : LibraryWrapper.rtlsdr_get_tuner_gains(_dev, null);
            if (count < 0) {
                count = 0;
            }
            SupportsOffsetTuning = LibraryWrapper.rtlsdr_set_offset_tuning(_dev, 0) != -2;
            SupportedGains = new int[count];
            if (count >= 0) {
                LibraryWrapper.rtlsdr_get_tuner_gains(_dev, SupportedGains);
            }
            Name = LibraryWrapper.rtlsdr_get_device_name(Index);
            _gcHandle = GCHandle.Alloc(this);
        }

        ~RtlDevice() {
            Dispose();
        }

        public void Dispose() {
            Stop();
            LibraryWrapper.rtlsdr_close(_dev);
            if (_gcHandle.IsAllocated) {
                _gcHandle.Free();
            }
            _dev = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        public event SamplesAvailableDelegate SamplesAvailable;

        public void Start() {
            if (_worker != null) {
                throw new ApplicationException("Already running");
            }

            int r = LibraryWrapper.rtlsdr_set_center_freq(_dev, _centerFrequency);
            if (r != 0) {
                throw new ApplicationException("Cannot access RTL device");
            }

            r = LibraryWrapper.rtlsdr_set_tuner_gain_mode(_dev, _useTunerAgc ? 0 : 1);
            if (r != 0) {
                throw new ApplicationException("Cannot access RTL device");
            }

            if (!_useTunerAgc) {
                r = LibraryWrapper.rtlsdr_set_tuner_gain(_dev, _tunerGain);
                if (r != 0) {
                    throw new ApplicationException("Cannot access RTL device");
                }
            }

            r = LibraryWrapper.rtlsdr_reset_buffer(_dev);
            if (r != 0) {
                throw new ApplicationException("Cannot access RTL device");
            }

            _worker = new Thread(StreamProc) {Priority = ThreadPriority.Highest};
            _worker.Start();
        }

        public void Stop() {
            if (_worker == null) {
                return;
            }
            LibraryWrapper.rtlsdr_cancel_async(_dev);
            _worker.Join(100);
            _worker = null;
        }

        public uint Index { get; }

        public string Name { get; }

        public uint SampleRate {
            get => _sampleRate;
            set {
                _sampleRate = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_sample_rate(_dev, _sampleRate);
                }
            }
        }

        public uint Frequency {
            get => _centerFrequency;
            set {
                _centerFrequency = value;
                if (_dev == IntPtr.Zero) return;
                if (LibraryWrapper.rtlsdr_set_center_freq(_dev, _centerFrequency) != 0) {
                    throw new ArgumentException("The frequency cannot be set: " + value);
                }
            }
        }

        public bool UseRtlAGC {
            get => _useRtlAgc;
            set {
                _useRtlAgc = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_agc_mode(_dev, _useRtlAgc ? 1 : 0);
                }
            }
        }

        public bool UseTunerAGC {
            get => _useTunerAgc;
            set {
                _useTunerAgc = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_tuner_gain_mode(_dev, _useTunerAgc ? 0 : 1);
                }
            }
        }

        public SamplingMode SamplingMode {
            get => _samplingMode;
            set {
                _samplingMode = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_direct_sampling(_dev, (int)_samplingMode);
                }
            }
        }

        private bool _useBiasTee;
        public bool UseBiasTee {
            get => _useBiasTee;
            set {
                _useBiasTee = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_bias_tee(_dev, value ? 1 : 0);
                }
            }
        }

        public bool SupportsOffsetTuning { get; }

        public bool UseOffsetTuning {
            get => _useOffsetTuning;
            set {
                _useOffsetTuning = value;

                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_offset_tuning(_dev, _useOffsetTuning ? 1 : 0);
                }
            }
        }

        public int[] SupportedGains { get; }

        public int TunerGain {
            get => _tunerGain;
            set {
                _tunerGain = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_tuner_gain(_dev, _tunerGain);
                }
            }
        }

        public int FrequencyCorrection {
            get => _frequencyCorrection;
            set {
                _frequencyCorrection = value;
                if (_dev != IntPtr.Zero) {
                    LibraryWrapper.rtlsdr_set_freq_correction(_dev, _frequencyCorrection);
                }
            }
        }

        public RtlSdrTunerType TunerType => _dev == IntPtr.Zero ? RtlSdrTunerType.Unknown : LibraryWrapper.rtlsdr_get_tuner_type(_dev);

        public bool IsStreaming => _worker != null;

        #region Streaming methods

        private void StreamProc() {
            LibraryWrapper.rtlsdr_read_async(_dev, RtlCallback, (IntPtr)_gcHandle, 0, _readLength);
        }

        private void ComplexSamplesAvailable(Complex* buffer, int length) {
            if (SamplesAvailable == null) return;
            _eventArgs.Buffer = buffer;
            _eventArgs.Length = length;
            SamplesAvailable(this, _eventArgs);
        }

        private static void RtlSdrSamplesAvailable(byte* buf, uint len, IntPtr ctx) {
            GCHandle gcHandle = GCHandle.FromIntPtr(ctx);
            if (!gcHandle.IsAllocated) {
                return;
            }
            var instance = (RtlDevice)gcHandle.Target;

            int sampleCount = (int)len / 2;
            if (instance._iqBuffer == null || instance._iqBuffer.Length != sampleCount) {
                instance._iqBuffer = UnsafeBuffer.Create(sampleCount, sizeof(Complex));
                instance._iqPtr = (Complex*)instance._iqBuffer;
            }
            Complex* ptr = instance._iqPtr;
            for (var i = 0; i < sampleCount; i++) {
                ptr->Imag = *buf++ * 10 - 1275;
                ptr->Real = *buf++ * 10 - 1275;
                ptr++;
            }

            instance.ComplexSamplesAvailable(instance._iqPtr, instance._iqBuffer.Length);
        }

        #endregion
    }

    public delegate void SamplesAvailableDelegate(object sender, SamplesAvailableEventArgs e);

    public sealed unsafe class SamplesAvailableEventArgs : EventArgs {
        public int Length { get; set; }
        public Complex* Buffer { get; set; }
    }

    public struct Complex {
        public int Real;
        public int Imag;
    }
}
