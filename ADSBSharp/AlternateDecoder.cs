using System;
using System.Numerics;
using BetterSDR.RTLSDR;
using libModeSharp;

namespace ADSBSharp {
    public class AlternateDecoder {
        private RtlDevice _rtlDevice;
        public ushort[] Magnitude = new ushort[Constants.ModesAsyncBufferSize + Constants.ModesPreableSize + Constants.ModesLongMessageBytes]; // -- MODES_DATA_LEN + (MODES_FULL_LEN-1)*4;
        private readonly ushort[] _magnitudeLookup = new ushort[2 * 256 * 256];
        private readonly byte[] _otherMagnitudeLut = new byte[129 * 129 * 2];
        public AlternateDecoder(RtlDevice deviceIo) {
            GenerateMagnitudeLookupTable();
            _rtlDevice = deviceIo;
            ModeSMessage.Init();
        }

        private void GenerateMagnitudeLookupTable() {
            for (var i = 0; i <= 255; i++) {
                for (var q = 0; q <= 255; q++) {
                    int magI = (i * 2) - 255;
                    int magQ = (q * 2) - 255;
                    var mag = (int)Math.Round((Math.Sqrt((magI * magI) + (magQ * magQ)) * 258.433254) - 365.4798);
                    _magnitudeLookup[(i * 256) + q] = (ushort)((mag < 65535) ? mag : 65535);
                }
            }

            for (var i = 0; i <= 128; i++) {
                for (var q = 0; q <= 128; q++) {
                    _otherMagnitudeLut[i * 129 + q] = (byte)Math.Round(Math.Sqrt(i * i + q * q) * 360);
                }
            }
        }

        public void DeviceOnRtlSdrDataAvailable(Complex[] samples) {
            ComputeMagnitudeVector(samples);
            DetectModeS(Magnitude, Constants.ModesAsyncBufferSamples);
        }


        private void ComputeMagnitudeVector(Complex[] data) {
            int m = Constants.ModesPreableSamples + Constants.ModesLongMessageSamples;
            var p = 0;
            Buffer.BlockCopy(Magnitude, Constants.ModesAsyncBufferSamples, Magnitude, 0, (Constants.ModesPreableSize) + (Constants.ModesLongMessageSize));
            for (var j = 0; j < Constants.ModesAsyncBufferSamples; j++) {
                Magnitude[m++] = _magnitudeLookup[(ushort)data[p++].Magnitude];
            }

            var mag2 = new ushort[Magnitude.Length];
            Array.Copy(Magnitude, mag2, Magnitude.Length);

            foreach (Complex item in data) {
                var i = (int)item.Imaginary;
                var q = (int)item.Real;

                if (i < 0) i = -i;
                if (q < 0) q = -q;

                mag2[i / 2] = _otherMagnitudeLut[i * 129 + q];
            }

            Magnitude = mag2;
        }
        public event FrameReceivedDelegate FrameReceived;
        private void DetectModeS(ushort[] data, int magLen) {
            var useCorrection = false;

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
                    data[j + 6] < data[j + 0] &&
                    data[j + 7] > data[j + 8] &&
                    data[j + 8] < data[j + 9] &&
                    data[j + 9] > data[j + 6]
                       )) {
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

                int sigStrength = (data[j + 0] - data[j + 1])
                                  + (data[j + 2] - data[j + 3])
                                  + (data[j + 7] - data[j + 6])
                                  + (data[j + 9] - data[j + 8]);

                int messageLength = Constants.ModesLongMessageBytes * 8;
                int scanLength = Constants.ModesLongMessageBytes * 8;
                // -- pPayload = data + j + modes_preable_samples

                var pMessage = new byte[Constants.ModesLongMessageBytes];

                var pMsgOffset = 0;
                int pPayloadOffset = j + Constants.ModesPreableSamples;
                byte theByte = 0;
                int errors = 0, errors56 = 0;
                int theErrs = 0, errorsTy = 0;

                for (var i = 0; i < scanLength; i++) {
                    ushort a = data[pPayloadOffset++];
                    ushort b = data[pPayloadOffset++];

                    if (a > b) {
                        theByte |= 1;
                        if (i < 56) 
                            sigStrength += (a - b);
                    }
                    else if (a < b) {
                        if (i < 56) 
                            sigStrength += (b - a);
                    }
                    else if (i >= (Constants.ModesShortMessageBytes * 8))
                        errors++;
                    else if (i >= 5) {
                        scanLength = Constants.ModesLongMessageBytes * 8;
                        errors56 = ++errors;
                    }
                    else if (i > 0) {
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
                    }
                    else if (i == 4) {
                        messageLength = ModeSMessage.GetMessageLength(theByte);
                        if (errors == 0)
                            scanLength = messageLength;
                    }

                    theByte <<= 1;

                    if (i < 7)
                        theErrs <<= 1;

                    // -- if acceptable error count has been passed, abondon ship.
                    if (errors <= Constants.ModesMessageEncoderErrors) 
                        continue;

                    if (i < Constants.ModesShortMessageBytes * 8)
                        messageLength = 0;
                    else if ((errorsTy == 1) && (theErrs == 0x80)) {
                        messageLength = Constants.ModesShortMessageBytes * 8;
                        pMessage[0] = (byte)(pMessage[0] ^ theErrs);
                        errorsTy = 0;
                        errors = errors56;
                    }
                    else if (i < Constants.ModesLongMessageBytes * 8) {
                        messageLength = Constants.ModesShortMessageBytes * 8;
                        errors = errors56;
                    }
                    else {
                        messageLength = Constants.ModesLongMessageBytes * 8;
                    }

                    break;
                }

                int derp = ModeSMessage.GetMessageLength(pMessage[0] >> 3);

                if (messageLength > derp)
                    messageLength = derp;
                else if (messageLength < derp)
                    messageLength = 0;

                if (messageLength > 0 && errorsTy == 1 && (theErrs & 0x78) > 0) {
                    int thisDf = ((theByte = pMessage[0]) >> 3) & 0x1f;
                    const uint validDFBits = 0x017F0831;
                    var thisDfBit = (uint)(1 << thisDf);

                    if (0 == (validDFBits & thisDfBit)) {
                        theByte = (byte)(theByte ^ theErrs);
                        thisDf = (theByte >> 3) & 0x1f;
                        thisDfBit = (uint)(1 << thisDf);

                        if ((validDFBits & thisDfBit) > 0) {
                            pMessage[0] = theByte;
                            errors--;
                        }
                    }
                }

                sigStrength = (sigStrength + 29) / 60;

                // When we reach this point, if error is small, and the signal strength is large enough
                // we may have a Mode S message on our hands. It may still be broken and the CRC may not 
                // be correct, but this can be handled by the next layer.
                if ((messageLength <= 0) || sigStrength <= 0x02FF ||
                    errors > Constants.ModesMessageEncoderErrors) 
                    continue;

                // -- snaps!
                ModeSMessage myModesMessage = ModeSMessage.DecodeMessage(pMessage);

                if (myModesMessage.IsCrcOk)
                    FrameReceived?.Invoke(myModesMessage.RawMessage, myModesMessage.RawMessage.Length);
            }
        }
    }
}
