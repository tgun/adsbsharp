using System;
using System.IO;

namespace BetterSDR.ADSB {
    class Program {
        private const int readInSize = 131072 * 8;
        private double[] sampleData;
        static void Main(string[] args) { // -- 131072
            Console.WriteLine("Hello World!");

            var bw = new BinaryReader(File.OpenRead("raw.raw"));
            var correctRelations = 0;

            for (var j = 0; j < 4; j++) {
                var samples = ReadChunk(bw);
                

                for (var i = 0; i < 131068; i++) {
                    if (!(samples[i + 0] > samples[i + 1] &&// -- 1
                          samples[i + 1] < samples[i + 2] && // -- 0
                          samples[i + 2] > samples[i + 3] && // -- 1
                          samples[i + 3] < samples[i + 0] && // -- 0
                          samples[i + 4] < samples[i + 0] && // -- 0
                          samples[i + 5] < samples[i + 0] && // -- 0
                        samples[i + 6] < samples[i + 0] && // -- 0
                        samples[i + 7] > samples[i + 8] && // -- 1
                        samples[i + 8] < samples[i + 9] && // -- 0
                        samples[i + 9] > samples[i + 6] // -- 1
                           )) {
                        continue; // -- this relation of samples is not correct. This relates to the preable bits.
                    }
                    int high = (int)(samples[i + 0] + samples[i + 2] + samples[i + 7] + samples[i + 9]) / 6;

                    if (samples[i + 4] >= high || samples[i + 5] >= high) // -- samples too high between 3 and 6.
                        continue;

                    if (samples[i + 11] >= high ||
                        samples[i + 12] >= high ||
                        samples[i + 13] >= high ||
                        samples[i + 14] >= high)
                        continue; // -- too high data between samples 10 and 15. (The last 6 bits of the preamble are low ('0'). If these are higher than our average high, weed it out.

                    correctRelations++;
                }
            }

            Console.WriteLine($"There are {correctRelations} good preamble matches in four samples of raw");
            CheckMag();
        }

        private static void CheckMag() {
            var bw = new BinaryReader(File.OpenRead("mag.raw"));
            var correctRelations = 0;

            for (var j = 0; j < 4; j++) {
                var samples = ReadChunkSmall(bw);


                for (var i = 0; i < 131054; i++) {
                    if (!(samples[i + 0] > samples[i + 1] &&// -- 1
                          samples[i + 1] < samples[i + 2] && // -- 0
                          samples[i + 2] > samples[i + 3] && // -- 1
                          samples[i + 3] < samples[i + 0] && // -- 0
                          samples[i + 4] < samples[i + 0] && // -- 0
                          samples[i + 5] < samples[i + 0] && // -- 0
                        samples[i + 6] < samples[i + 0] && // -- 0
                        samples[i + 7] > samples[i + 8] && // -- 1
                        samples[i + 8] < samples[i + 9] && // -- 0
                        samples[i + 9] > samples[i + 6] // -- 1
                           )) {
                        continue; // -- this relation of samples is not correct. This relates to the preable bits.
                    }

                    correctRelations++;
                }
            }

            Console.WriteLine($"There are {correctRelations} good preamble matches in four samples of mag");
        }
        private static double[] ReadChunk(BinaryReader reader) {
            var result = new double[131072]; // -- 14..131068 is max index we can hit.

            for (var i = 0; i < 131072; i++) {
                result[i] = reader.ReadDouble();
            }

            return result;
        }

        private static int[] ReadChunkSmall(BinaryReader reader) {
            var result = new int[131072]; // -- 14..131068 is max index we can hit.

            for (var i = 0; i < 131072; i++) {
                result[i] = reader.ReadInt32();
            }

            return result;
        }
    }
}
