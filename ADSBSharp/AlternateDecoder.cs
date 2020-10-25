using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libModeSharp;
using libRtlSdrSharp;

namespace ADSBSharp {
    public class AlternateDecoder {
        private ISDRDevice _rtlDevice;
        public ushort[] Magnitude = new ushort[RtlDevice.ReadLength + 119 * 4]; // -- MODES_DATA_LEN + (MODES_FULL_LEN-1)*4;
        private ushort[] MagnitudeLookup = new ushort[129 * 129];
      //  private DataReady _callback;

        public AlternateDecoder(ISDRDevice _deviceIo) {
            _rtlDevice = _deviceIo;
            _rtlDevice.DataAvailable += DeviceOnRtlSdrDataAvailable;
        }

        public void DeviceOnRtlSdrDataAvailable() {
            byte[] myData;

            lock (_rtlDevice.BufferLock) {
                long size = _rtlDevice.Buffer.Length - _rtlDevice.Buffer.Position;
                myData = new byte[size];
                _rtlDevice.Buffer.Read(myData, 0, (int)size);
            }

            if (myData == null || myData.Length == 0)
                return;

            ComputeMagnitudeVector(myData);

       //     _callback?.Invoke(myData);
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

        public void ReceiveRtlData(byte[] data) {

        }

        private void DetectModeS(byte[] data, int magLen) {
            bool useCorrection = false;
            for (var j = 0; j < magLen - Constants.ModesFullLength * 2; j++) {
                if (useCorrection)
                    break; // -- GOTO statement

                /* First check of relations between the first 10 samples
                 * representing a valid preamble. We don't even investigate further
                 * if this simple test is not passed. */
                // -- TODO: Note this won't work because the original in dump1090 assumes an unsigned char, not an array of ushort.
            }
        }
    }
}
