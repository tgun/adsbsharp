using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libModeSharp {
    public enum ModeSMessageType {

    }

    public enum ModeSUnit {
        Feet = 0,
        Meters
    }

    /// <summary>
    /// Class representing a single ModeS Frame
    /// </summary>
    public class ModeSMessage {
        private const bool FixErrors = true;
        public const int ModesLongMessageBytes = 14; // -- 112 bits
        public const int ModesShortMessageBytes = 7; // -- 56 bits
        private const int ModesIcaoCacheLength = 1024; // -- Power of two required.
        private const int ICAOCacheTTL = 60;
        private static uint[] ICAOCache;
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

        public static void Init() {
            ICAOCache = new uint[ModesIcaoCacheLength];
        }
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

            // -- DF 11 & 17; Populate ICAO Whitelist. For DFs with an AP field, try to decode it.
            if (result.MessageType != 11 && result.MessageType != 17) {
                result.IsCrcOk = BruteForceAP(ref result);
            }
            else {
                // -- If this is DF11 or 17 and checksum is ok, we can add this to list of recently seen.
                if (result.IsCrcOk && result.ErrorBit == -1) {
                    var address = (uint)((result.Aa1 << 16) | (result.Aa2 << 8) | (result.Aa3));
                    AddRecentlySeenICAOAddress(address);
                }
            }

            // -- Decode 13 bit altitude for DF0,16,20
            if (result.MessageType == 0 || result.MessageType == 4 || result.MessageType == 16 ||
                result.MessageType == 20) {
                var altitudeUnit = 0;
                result.Altitude = DecodeAC13Field(result.RawMessage, ref altitudeUnit);
                result.Unit = altitudeUnit;
            }

            // -- Extended squitter specific stuff
            if (result.MessageType == 17) {

            }

            result.IsPhaseCorrected = false;
            return result;
        }

        private static bool WasICAORecentlySeen(uint address) {
            uint hashAddress = ICAOCacheHashAddress(address);
            uint addr = ICAOCache[hashAddress * 2];
            uint time = ICAOCache[(hashAddress * 2) + 1];

            double currentTime = GetUnixTime();

            return address == addr && (currentTime - time) <= ICAOCacheTTL;
        }

        private static bool BruteForceAP(ref ModeSMessage message) {
            var aux = new byte[ModesLongMessageBytes];
            int messageType = message.MessageType;
            int messageBits = message.RawMessage.Length * 8;

            if (messageType != 0 && messageType != 4 && messageType != 5 && messageType != 16 && messageType != 20 &&
                messageType != 21 && messageType != 24) 
                return false;

            int lastByte = (messageBits / 8) - 1;
            Array.Copy(message.RawMessage, aux, messageBits / 8);

            // -- Compute the CRC of the message and XOR it with the AP field to recover the address.
            // -- (ADDR xor CRC) xor CRC = ADDR

            uint crc = Checksum(aux, messageBits);

            aux[lastByte] ^= (byte)(crc & 0xff);
            aux[lastByte - 1] ^= (byte)((crc >> 8) & 0xff);
            aux[lastByte - 2] ^= (byte)((crc >> 16) & 0xff);

            var address = (uint)(aux[lastByte] | (aux[lastByte - 1] << 8) | (aux[lastByte - 2] << 16));

            if (!WasICAORecentlySeen(address)) 
                return false;

            message.Aa1 = aux[lastByte - 2];
            message.Aa2 = aux[lastByte - 1];
            message.Aa3 = aux[lastByte];

            return true;

        }
        /// <summary>
        /// Hash the ICAO Address to index our cache of ICAO_CACHE_LEN elements, that is assumed to be a power of two.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static uint ICAOCacheHashAddress(uint address) {
            // -- The following 3 rounds will make sure that every bit affects every output bit with ~ 50% probability.
            address = ((address >> 16) ^ address) * 0x45d9f3b;
            address = ((address >> 16) ^ address) * 0x45d9f3b;
            address = ((address >> 16) ^ address);
            return address & (ModesIcaoCacheLength - 1);
        }

        private static void AddRecentlySeenICAOAddress(uint address) {
            uint hashAddress = ICAOCacheHashAddress(address);
            ICAOCache[hashAddress * 2] = address;
            ICAOCache[hashAddress * 2 + 1] = (uint) GetUnixTime();
        }

        private static double GetUnixTime() {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private static int DecodeAC13Field(byte[] message, ref int unit) {
            int mBit = message[3] & (1 << 6);
            int qBit = message[3] & (1 << 4);

            if (!(mBit > 0)) {
                unit =(int) ModeSUnit.Feet;
                if (qBit > 0) {
                    // -- N is the 11 bit integer resulting from the removal of bit Q and M
                    int n = ((message[2] & 31) << 6) |
                            ((message[3] & 0x80) >> 2) |
                            ((message[3] & 0x20) >> 1) |
                            (message[3] & 15);
                    // -- Final altitude is due to the resutling number multiplied by 25, minus 1000.
                    return n * 25 - 1000;
                }
                // -- TODO: Altitude when Q=0 and M=0
            }
            else {
                unit = (int)ModeSUnit.Meters;
                // - -TODO: Altitude in meters.
            }

            return 0;
        }
    }
}
