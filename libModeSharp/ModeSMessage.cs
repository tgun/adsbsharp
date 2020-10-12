using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libModeSharp {
    public enum ModeSMessageType {

    }
    /// <summary>
    /// Class representing a single ModeS Frame
    /// </summary>
    public class ModeSMessage {
        private const bool FixErrors = false;
        public const int ModesLongMessageBytes = 14; // -- 112 bits
        public const int ModesShortMessageBytes = 7; // -- 56 bits
        public byte[] RawMessage;
        public int MessageType { get; set; }
        public bool IsCrcOk { get; set; }
        public uint Crc { get; set; }
        public int ErrorBit { get; set; }
        public int Aa1 { get; set; }
        public int Aa2 { get; set; }
        public int Aa3 { get; set; }
        public bool IsPhaseCorrected { get; set; }

        // -- DF 11
        public int Ca { get; set; } // -- Responder Capabilities

        // -- DF 17
        public int MessageTypeExtended { get; set; }                 /* Extended squitter message type. */
        public int MessageSubType { get; set; }                 /* Extended squitter message subtype. */
        public bool HeadingIsValid { get; set; }
        public int Heading { get; set; }
        public int AircraftType { get; set; }
        public int Fflag { get; set; }                 /* 1 = Odd, 0 = Even CPR message. */
        public int Tflag { get; set; }                 /* UTC synchronized? */
        public int RawLatitude { get; set; }           /* Non decoded latitude */
        public int RawLongitude { get; set; }          /* Non decoded longitude */
        public byte[] Flight { get; set; }             /* 8 chars flight number. size of 9 */
        public int EwDir { get; set; }                 /* 0 = East, 1 = West. */
        public int EwVelocity { get; set; }            /* E/W velocity. */
        public int NsDir { get; set; }                 /* 0 = North, 1 = South. */
        public int NsVelocity { get; set; }            /* N/S velocity. */
        public int VerticalRateSource { get; set; }       /* Vertical rate source. */
        public int VerticalRateSign { get; set; }         /* Vertical rate sign. */
        public int VerticalRate { get; set; }              /* Vertical rate. */
        public int Velocity { get; set; }              /* Computed from EW and NS velocity. */

        /* DF4, DF5, DF20, DF21 */
        public int FlightStatus { get; set; }                     /* Flight status for DF4,5,20,21 */
        public int DownlinkRequest { get; set; }                     /* Request extraction of downlink request. */
        public int Um { get; set; }                            /* Request extraction of downlink request. */
        public int Identity { get; set; }               /* 13 bits identity (Squawk). */

        /* Fields used by multiple message types. */
        public int Altitude { get; set; }
        public int Unit { get; set; }

        public static uint Checksum(byte[] message, int bits) {
            uint result = 0;
            int offset = (bits == 112) ? 0 : (112 - 56);

            for (var j = 0; j < bits; j++) {
                int i = j / 8;
                int bit = j % 8;
                int bitmask = 1 << (7 - bit);

                if ((message[i] & bitmask) > 0) // -- If Bit i set, xor with corresponding table entry
                    result ^= Constants.ChecksumTable[j + offset];
            }

            return result;
        }

        public static uint Checksum1(byte[] message, int bits) {
            int bytes = bits / 8;
            // -- Always the last 3 bytes
            return ((uint)message[bytes - 3] << 16) |
                   (uint)message[bytes - 2] << 8 |
                   (uint)message[bytes - 1];
        }

        public static int GetMessageLength(int type) {
            if (type == 16 || type == 17 || type == 19 || type == 20 || type == 21)
                return ModesLongMessageBytes;

            return ModesShortMessageBytes;
        }

        public static int FixSingleBitErrors(ref byte[] message, int bits) {
            byte[] aux = new byte[ModesLongMessageBytes];
            int bytes = bits / 8;

            for (int j = 0; j < bits; j++) {
                int i = j / 8;
                int bitmask = 1 << (7 - (j % 8));
                uint crc1, crc2;
                Array.Copy(message, aux, bytes);
                aux[i] ^= (byte) bitmask; // -- Flip j-th bit

                crc1 = Checksum1(aux, bytes * 8);

                crc2 = Checksum(aux, bits);
                if (crc1 == crc2) {
                    // -- Error has been fixed. Overwrite the original with the corrected sequence.
                    Array.Copy(aux, message, bytes);
                    return j;
                }
            }

            return -1;
        }

        public static ModeSMessage DecodeMessage(byte[] message) {
            var result = new ModeSMessage();
            
            // -- Get the message type first.
            result.MessageType = message[0] >> 3; /* Downlink Format */
            result.RawMessage = new byte[GetMessageLength(result.MessageType)];
            result.Crc = Checksum1(message, result.RawMessage.Length * 8);
            uint crc2 = Checksum(message, result.RawMessage.Length * 8);

            Array.Copy(message, result.RawMessage, result.RawMessage.Length);
            
            result.ErrorBit = -1;
            result.IsCrcOk = (result.Crc == crc2);

            // -- Error correction
            if (!result.IsCrcOk && FixErrors && (result.MessageType == 11 || result.MessageType == 17)) {
                result.ErrorBit = FixSingleBitErrors(ref result.RawMessage, result.RawMessage.Length * 8);
                if (result.ErrorBit != -1) {
                    result.Crc = Checksum(result.RawMessage, result.RawMessage.Length * 8);
                    result.IsCrcOk = true;
                }
            }

            result.Ca = message[0] & 7; // -- Capabilities
            result.Aa1 = message[1]; // -- ICAO address
            result.Aa2 = message[2];
            result.Aa3 = message[3];

            // -- DF-17 Type
            result.MessageTypeExtended = message[4] >> 3;
            result.MessageSubType = message[4] & 7;

            // -- Fields for DF4,5,20,21.
            result.FlightStatus = message[0] & 7;
            result.DownlinkRequest = message[1] >> 3 & 31;
            result.Um = ((message[1] & 7) << 3) | message[2] >> 5;

            { // -- squawk  decoding.
                int a, b, c, d;

                a = ((message[3] & 0x80) >> 5) |
                    ((message[2] & 0x02) >> 0) |
                    ((message[2] & 0x08) >> 3);
                b = ((message[3] & 0x02) << 1) |
                    ((message[3] & 0x08) >> 2) |
                    ((message[3] & 0x20) >> 5);
                c = ((message[2] & 0x01) << 2) |
                    ((message[2] & 0x04) >> 1) |
                    ((message[2] & 0x10) >> 4);
                d = ((message[3] & 0x01) << 2) |
                    ((message[3] & 0x04) >> 1) |
                    ((message[3] & 0x10) >> 4);

                result.Identity = a * 1000 + b * 100 + c * 10 + d;
            }
        }
    }
}
