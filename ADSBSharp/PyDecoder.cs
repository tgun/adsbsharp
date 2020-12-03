using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADSBSharp {
    public class PyDecoder {
        private const int SamplesPerMicrosecond = 2;
        private double[] sampleBuffer;

        private int CalculateNoiseFloor() {
            var window = SamplesPerMicrosecond * 100;
            var totalLength = sampleBuffer.Length;
            var subLength = totalLength / (window * window);

            double[] temp = new double[subLength];
            Array.Copy(sampleBuffer, temp, subLength);

            // signalBuffer[0, totalLength / (window * window)]
            return 0;
        }
    }
}
