using System;

namespace libRtlSdrSharp {
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

    public class RtlSdrIO : IDisposable {
        private uint _frequency = 1090000000;
        public ushort[] Magnitude = new ushort[RtlDevice.ReadLength + 119 * 4]; // -- MODES_DATA_LEN + (MODES_FULL_LEN-1)*4;
        private ushort[] MagnitudeLookup = new ushort[129*129];
        private DataReady _callback;

        ~RtlSdrIO() {
            Dispose();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        public void SelectDevice(uint index) {
            Close();
            Device = new RtlDevice(index);
            Device.RtlSdrDataAvailable += DeviceOnRtlSdrDataAvailable;
            Device.Frequency = _frequency;
        }

        private void DeviceOnRtlSdrDataAvailable() {
            byte[] myData;

            lock (Device.BufferLock) {
                long size = Device.Buffer.Length - Device.Buffer.Position; 
                myData = new byte[size];
                Device.Buffer.Read(myData, 0, (int)size);
            }

            if (myData == null || myData.Length == 0)
                return;

            ComputeMagnitudeVector(myData);
            
            _callback?.Invoke(myData);
        }

        private void ComputeMagnitudeVector(byte[] data) {
            for (var j = 0; j < data.Length; j += 2) {
                int i = data[j] - 127;
                int q = data[j + 1] - 127;

                if (i < 0) i = -i;
                if (q < 0) q = -q;
                Magnitude[i / 2] = MagnitudeLookup[i * 129 + q];
            }
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
            Device.RtlSdrDataAvailable -= DeviceOnRtlSdrDataAvailable;
            Device.Dispose();
            Device = null;
        }

        public void Start(DataReady callback) {
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
    }

    public delegate void DataReady(byte[] data);
}
