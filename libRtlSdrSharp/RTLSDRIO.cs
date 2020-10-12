using System;

namespace libRtlSdrSharp {
    public unsafe delegate void SamplesReadyDelegate(object sender, Complex* data, int length);
    public class DeviceDisplay {
        public uint Index { get; private set; }
        public string Name { get; set; }

        public static DeviceDisplay[] GetActiveDevices() {
            uint count = LibraryWrapper.rtlsdr_get_device_count();
            var result = new DeviceDisplay[count];

            for (var i = 0u; i < count; i++) {
                string name = LibraryWrapper.rtlsdr_get_device_name(i);
                result[i] = new DeviceDisplay { Index = i, Name = name };
            }

            return result;
        }

        public override string ToString() {
            return Name;
        }
    }

    public unsafe class RtlSdrIO : IDisposable {
        private uint _frequency = 1090000000;
        private SamplesReadyDelegate _callback;

        ~RtlSdrIO() {
            Dispose();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        public void SelectDevice(uint index) {
            Close();
            Device = new RtlDevice(index);
            Device.SamplesAvailable += rtlDevice_SamplesAvailable;
            Device.Frequency = _frequency;
        }

        public RtlDevice Device { get; private set; }

        public void Open() {
            DeviceDisplay[] devices = DeviceDisplay.GetActiveDevices();
            foreach (DeviceDisplay device in devices) {
                try {
                    SelectDevice(device.Index);
                    return;
                }
                catch (ApplicationException) {
                    // Just ignore it
                }
            }
            if (devices.Length > 0) {
                throw new ApplicationException(devices.Length + " compatible devices have been found but are all busy");
            }
            throw new ApplicationException("No compatible devices found");
        }

        public void Close() {
            if (Device == null) 
                return;

            Device.Stop();
            Device.SamplesAvailable -= rtlDevice_SamplesAvailable;
            Device.Dispose();
            Device = null;
        }

        public void Start(SamplesReadyDelegate callback) {
            if (Device == null) {
                throw new ApplicationException("No device selected");
            }
            _callback = callback;
            try {
                Device.Start();
            }
            catch {
                Open();
                Device.Start();
            }
        }

        public void Stop() {
            Device.Stop();
        }

        public double Samplerate => Device?.SampleRate ?? 0.0;

        public long Frequency {
            get => _frequency;
            set {
                _frequency = (uint)value;
                if (Device != null) {
                    Device.Frequency = _frequency;
                }
            }
        }

        private void rtlDevice_SamplesAvailable(object sender, SamplesAvailableEventArgs e) {
            _callback(this, e.Buffer, e.Length);
        }
    }
}
