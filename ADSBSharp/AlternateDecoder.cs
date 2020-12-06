using System;
using System.Collections.Generic;
using System.Numerics;
using BetterSDR.RTLSDR;
using libModeSharp;

namespace ADSBSharp {
    public class AlternateDecoder {
        public List<long> SeenIcaos { get; set; }
        public ushort[] Magnitude = new ushort[Constants.ModesAsyncBufferSize + Constants.ModesPreableSize + Constants.ModesLongMessageBytes]; // -- MODES_DATA_LEN + (MODES_FULL_LEN-1)*4;
        private readonly ushort[] _magnitudeLookup = new ushort[2 * 256 * 256];
        public AlternateDecoder( ) {
            SeenIcaos = new List<long>();
            GenerateMagnitudeLookupTable();
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
        }

        public void DeviceOnRtlSdrDataAvailable(Complex[] samples) {
            ushort[] combined = new ushort[samples.Length];
            for (var i = 0; i < samples.Length; i++) {
                byte real = (byte)samples[i].Real;
                byte imag = (byte)samples[i].Imaginary;
                ushort meh = (ushort)((real << 8) | imag);
                combined[i] = meh;
            }

            ComputeMagnitudeVector(combined);
            DetectModeS(Magnitude, Constants.ModesAsyncBufferSamples);
        }


        private void ComputeMagnitudeVector(ushort[] data) {
            int m = Constants.ModesPreableSamples + Constants.ModesLongMessageSamples;
            var p = 0;
            Buffer.BlockCopy(Magnitude, Constants.ModesAsyncBufferSamples, Magnitude, 0, (Constants.ModesPreableSize) + (Constants.ModesLongMessageSize));
            for (var j = 0; j < Constants.ModesAsyncBufferSamples; j++) {
                Magnitude[m++] = _magnitudeLookup[data[p++]];
            }
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
              //  { 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
                if (!(data[j + 0] > data[j + 1] &&// -- 1
                      data[j + 1] < data[j + 2] && // -- 0
                      data[j + 2] > data[j + 3] && // -- 1
                      data[j + 3] < data[j + 0] && // -- 0
                      data[j + 4] < data[j + 0] && // -- 0
                      data[j + 5] < data[j + 0] && // -- 0
                    data[j + 6] < data[j + 0] && // -- 0
                    data[j + 7] > data[j + 8] && // -- 1
                    data[j + 8] < data[j + 9] && // -- 0
                    data[j + 9] > data[j + 6] // -- 1
                       )) {
                    continue; // -- this relation of samples is not correct. This relates to the preable bits.
                }

                // The samples between the two spikes must be < than the average
                // of the high spikes level. We don't test bits too near to
                // the high levels as signals can be out of phase so part of the
                // energy can be in the near samples
                // -- This is an average of the power level of all of the '1' bits.
                int high = (data[j + 0] + data[j + 2] + data[j + 7] + data[j + 9]) / 6;

                if (data[j + 4] >= high || data[j + 5] >= high) // -- samples too high between 3 and 6.
                    continue;

                if (data[j + 11] >= high ||
                    data[j + 12] >= high ||
                    data[j + 13] >= high ||
                    data[j + 14] >= high)
                    continue; // -- too high data between samples 10 and 15. (The last 6 bits of the preamble are low ('0'). If these are higher than our average high, weed it out.

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

                // -- Demodulation loop
                for (var i = 0; i < scanLength; i++) {
                    ushort a = data[pPayloadOffset++];
                    ushort b = data[pPayloadOffset++];
                    // -- Decode/demodulate into ones and zeros...
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

                    if ((i & 7) == 7) {// -- If we have decoded 8 bits, assign the byte and move on.
                        pMessage[pMsgOffset++] = theByte;
                    }
                    else if (i == 4) { // -- Attempt to determine the message length after a certain amount of messages decoded.
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

                if (myModesMessage.IsCrcOk) {
                    FrameReceived?.Invoke(myModesMessage.RawMessage, myModesMessage.RawMessage.Length);
                    if (!SeenIcaos.Contains(myModesMessage.ICAO))
                        SeenIcaos.Add(myModesMessage.ICAO);
                }
            }
        }
    }
}
