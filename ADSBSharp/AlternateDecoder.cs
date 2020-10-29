using System;
using libModeSharp;
using libRtlSdrSharp;

namespace ADSBSharp {
    public class AlternateDecoder {
        private ISDRDevice _rtlDevice;
        public ushort[] Magnitude = new ushort[Constants.ModesAsyncBufferSize + Constants.ModesPreableSize + Constants.ModesLongMessageBytes]; // -- MODES_DATA_LEN + (MODES_FULL_LEN-1)*4;
        private ushort[] MagnitudeLookup = new ushort[2 * 256 * 256];

        public AlternateDecoder(ISDRDevice _deviceIo) {
            GenerateMagnitudeLookupTable();
            _rtlDevice = _deviceIo;
            _rtlDevice.DataAvailable += DeviceOnRtlSdrDataAvailable;
            ModeSMessage.Init();
        }

        private void GenerateMagnitudeLookupTable() {
            for (var i = 0; i <= 255; i++) {
                for (var q = 0; q <= 255; q++) {
                    int mag, mag_i, mag_q;
                    mag_i = (i * 2) - 255;
                    mag_q = (q * 2) - 255;
                    mag = (int) Math.Round((Math.Sqrt((mag_i * mag_i) + (mag_q * mag_q)) * 258.433254) - 365.4798);
                    MagnitudeLookup[(i * 256) + q] = (ushort) ((mag < 65535) ? mag : 65535);
                }
            }
        }

        public void DeviceOnRtlSdrDataAvailable() {
            lock (_rtlDevice.BufferLock) {
                if (_rtlDevice.SampleBufferDataReady > 0) {
                    _rtlDevice.SampleBufferDataOut &= (15);
                    ComputeMagnitudeVector(_rtlDevice.SampleBuffer[_rtlDevice.SampleBufferDataOut]);
                    _rtlDevice.SampleBufferDataOut =
                        (RtlDevice.ModesAsyncBufNumber - 1) & (_rtlDevice.SampleBufferDataOut + 1);
                    _rtlDevice.SampleBufferDataReady = (RtlDevice.ModesAsyncBufNumber - 1) &
                                                       (_rtlDevice.SampleBufferDataIn - _rtlDevice.SampleBufferDataOut);
                }
            }

            DetectModeS(Magnitude, Constants.ModesAsyncBufferSamples);
        }

        private byte[] ConvertShortToByteArray(short[] input) {
            var result = new byte[input.Length * 2];
            var i = 0;
            foreach(var ip in input) {
                Array.Copy(BitConverter.GetBytes(ip), 0, result, i * 2, 2);
            }
            return result;
        }

        private void ComputeMagnitudeVector(short[] data) {
            int m = Constants.ModesPreableSamples + Constants.ModesLongMessageSamples;
            int p = 0;
            Buffer.BlockCopy(Magnitude, Constants.ModesAsyncBufferSamples, Magnitude,  0, (Constants.ModesPreableSamples * 2) + (Constants.ModesLongMessageSamples * 2));
            for (var j = 0; j < Constants.ModesAsyncBufferSamples; j++) {
                Magnitude[m++] = MagnitudeLookup[(ushort)data[p++]];
            }

            var derp = ConvertShortToByteArray(data);

            var mag2 = new ushort[Magnitude.Length];
            Array.Copy(Magnitude, mag2, Magnitude.Length);

            for (var j = 0; j < derp.Length; j += 2) {
                int i = derp[j] - 127;
                int q = derp[j + 1] - 127;

                if (i < 0) i = -i;
                if (q < 0) q = -q;
                mag2[i / 2] = MagnitudeLookup[i * 129 + q];
            }

            Console.WriteLine("hi mom");
        }

        private void DetectModeS(ushort[] data, int magLen) {
            bool useCorrection = false;

            for (var j = 0; j < magLen; j++) {
                if (useCorrection)
                    break; // -- GOTO statement

                /* First check of relations between the first 10 samples
                 * representing a valid preamble. We don't even investigate further
                 * if this simple test is not passed. */
                if (!(data[j + 0] > data[j + 1] &&
                      data[j + 1] < data[j + 2] &&
                      data[j + 2] > data[j + 3] &&
                      data[j + 3] < data[j + 0] &&
                      data[j + 4] < data[j + 0] &&
                      data[j + 5] < data[j + 0] &&
                    data[j + 6] < data[j + 0]  &&
                    data[j + 7] > data[j + 8] &&
                    data[j + 8] < data[j + 9] &&
                    data[j + 9] > data[j + 6]
                       )) 
                    {
                    continue; // -- this relation of samples is not correct.
                }

                // The samples between the two spikes must be < than the average
                // of the high spikes level. We don't test bits too near to
                // the high levels as signals can be out of phase so part of the
                // energy can be in the near samples
                int high = (data[j + 0] + data[j + 2] + data[j + 7] + data[j + 9]) / 6;

                if (data[j + 4] >= high || data[j + 5] >= high) // -- samples too high between 3 and 6.
                    continue;

                if (data[j + 11] >= high ||
                    data[j + 12] >= high ||
                    data[j + 13] >= high ||
                    data[j + 14] >= high)
                    continue; // -- too high data between samples 10 and 15.

                Console.WriteLine("Hey I got some real data here!!");
                int sigStrength = (data[j + 0] - data[j + 1])
                                  + (data[j + 2] - data[j + 3])
                                  + (data[j + 7] - data[j + 6])
                                  + (data[j + 9] - data[j + 8]);
                
                int messageLength = Constants.ModesLongMessageBytes * 8;
                int scanLength = Constants.ModesLongMessageBytes * 8;
                // -- pPayload = data + j + modes_preable_samples

                byte[] pMessage = new byte[Constants.ModesLongMessageBytes];

                int pMsgOffset = 0;
                var pPayloadOffset = j + Constants.ModesPreableSamples;
                byte theByte = 0;
                int errors = 0, errors56 = 0;
                int theErrs = 0, errorsTy = 0;

                for (var i = 0; i < scanLength; i++) {
                    ushort a = data[pPayloadOffset++];
                    ushort b = data[pPayloadOffset++];

                    if (a > b) {
                        theByte |= 1;
                        if (i < 56) {
                            sigStrength += (a - b);
                        }
                    } else if (a < b) {
                        if (i < 56) {
                            sigStrength += (b - a);
                        }
                    } else if (i >= (Constants.ModesShortMessageBytes * 8))
                        errors++;
                    else if (i >= 5) {
                        scanLength = Constants.ModesLongMessageBytes * 8;
                        errors56 = ++errors;
                    } else if (i > 0) {
                        errors56 = ++errors;
                        errorsTy = errors56;
                        theErrs |= 1;
                    }
                    else {
                        errors56 = ++errors;
                        errorsTy = errors56;
                        theErrs |= 1;
                        theByte |= 1;
                    }

                    if ((i & 7) == 7) {
                        pMessage[pMsgOffset++] = theByte;
                    } else if (i == 4) {
                        messageLength = ModeSMessage.GetMessageLength(theByte);
                        if (errors == 0)
                            scanLength = messageLength;
                    }

                    theByte <<= 1;

                    if (i < 7)
                        theErrs <<= 1;

                    // -- if acceptable error count has been passed, abondon ship.
                    if (errors > Constants.ModesMessageEncoderErrors) {
                        if (i < Constants.ModesShortMessageBytes * 8)
                            messageLength = 0;
                        else if ((errorsTy == 1) && (theErrs == 0x80)) {
                            messageLength = Constants.ModesShortMessageBytes * 8;
                            pMessage[0] = (byte)(pMessage[0] ^ theErrs);
                            errorsTy = 0;
                            errors = errors56;
                        } else if (i < Constants.ModesLongMessageBytes * 8) {
                            messageLength = Constants.ModesShortMessageBytes * 8;
                            errors = errors56;
                        }
                        else {
                            messageLength = Constants.ModesLongMessageBytes * 8;
                        }

                        break;
                    }
                }

                int derp = ModeSMessage.GetMessageLength(pMessage[0] >> 3);
                
                if (messageLength > derp)
                    messageLength = derp;
                else if (messageLength < derp)
                    messageLength = 0;

                if (messageLength > 0 && errorsTy == 1 && (theErrs & 0x78) > 0) {
                    int thisDF = ((theByte = pMessage[0]) >> 3) & 0x1f;
                    uint validDFBits = 0x017F0831;
                    uint thisDFBit = (uint)(1 << thisDF);
                    if (0 == (validDFBits & thisDFBit)) {
                        theByte = (byte)(theByte ^ theErrs);
                        thisDF = (theByte >> 3) & 0x1f;
                        thisDFBit = (uint)(1 << thisDF);

                        if ((validDFBits & thisDFBit) > 0) {
                            pMessage[0] = theByte;
                            errors--;
                        }
                    }
                }

                sigStrength = (sigStrength + 29) / 60;

                // When we reach this point, if error is small, and the signal strength is large enough
                // we may have a Mode S message on our hands. It may still be broken and the CRC may not 
                // be correct, but this can be handled by the next layer.
                if ((messageLength > 0) && sigStrength > 0x02FF && errors <= Constants.ModesMessageEncoderErrors) {
                    // -- snaps!
                    var myModesMesage = ModeSMessage.DecodeMessage(pMessage);
                    if (myModesMesage.IsCrcOk) {
                        Console.WriteLine("I got a message mom!");
                    }
                    
                }
            }
        }
    }
}
