using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ADSBSharp {
    public class PyDecoder {
        private const int SamplesPerMicrosecond = 2;
        private double[] sampleBuffer;
        private byte[] Preamble = new byte[] { 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
        private bool StopFlag = false;
        private long noiseFloor = 1000000;
        private Complex[] buffer;
        private float SignalAmplitudeThreshold = 0.8f;

        private int CalculateNoiseFloor() {
            var window = SamplesPerMicrosecond * 100;
            var totalLength = sampleBuffer.Length;
            var subLength = totalLength / (window * window);

            double[] temp = new double[subLength];
            Array.Copy(sampleBuffer, temp, subLength);

            // signalBuffer[0, totalLength / (window * window)]
            return 0;
        }
        private void ProcessBuffer() {
            // -- Update noise floor
            noiseFloor = Math.Min(CalculateNoiseFloor(), noiseFloor);
            // -- Set minimum signal amplitude
            var minSigAmp = 3.162 * noiseFloor;
            libModeSharp.ModeSMessage[] messages;
            var bufferLength = buffer.Length;
            var i = 0;
            while (i < bufferLength) {
                if (buffer[i].Magnitude < minSigAmp) {
                    i++;
                    continue;
                }

            }

        }
        private bool CheckPreamble(double[] pulses) {
            if (pulses.Length != 16)
                return false;

            for (var i = 0; i < 16; i++) {
                if (Math.Abs(pulses[i] - Preamble[i]) > SignalAmplitudeThreshold)
                    return false;
            }

            return true;
        }
    }
}
