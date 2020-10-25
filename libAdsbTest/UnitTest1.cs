using System;
using System.Diagnostics;
using libModeSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libAdsbTest {
    [TestClass]
    public class ModeSMessageTests {
        private byte[] testFrame1 = new byte[] {0x8D, 0x40, 0x62, 0x1D, 0x58, 0xC3, 0x82, 0xD6, 0x90, 0xC8, 0xAC, 0x28, 0x63, 0xA7 };


        private byte[] testFrame2 = new byte[] { 0x8D, 0x40, 0x62, 0x1D, 0x58, 0xC3, 0x86, 0x43, 0x5C, 0xC4, 0x12, 0x69, 0x2A, 0xD6 };


        private byte[] testFrame3 = new byte[] { 0x8D, 0x48, 0x40, 0xD6, 0x20, 0x2C, 0xC3, 0x71, 0xC3, 0x2C, 0xE0, 0x57, 0x60, 0x98 };

        [TestInitialize]
        public void Setup() {
            ModeSMessage.Init();
        }

        [TestMethod]
        public void DecodeTest() {
            var result1 = ModeSMessage.DecodeMessage(testFrame1);
            var result2 = ModeSMessage.DecodeMessage(testFrame2);
            var result3 = ModeSMessage.DecodeMessage(testFrame3);
            
            uint expectedICAO = 0x4840D6;
            var expectedAltitude = 38000;
            var expectedSquawk = 2207;
            var expectedLatitude = 74158;
            var expectedLongitude = 50194;

            Assert.AreEqual(expectedAltitude, result1.Altitude);
            Assert.AreEqual(expectedSquawk, result1.Identity);
            
            Assert.AreEqual(expectedLatitude, result2.RawLatitude);
            Assert.AreEqual(expectedLongitude, result2.RawLongitude);

            Assert.AreEqual(expectedICAO, result3.ICAO);
        }

        [TestMethod]
        public void TestCrc1() {
            var givenModeSMessage = new byte[]
                {0x8D, 0x40, 0x6B, 0x90, 0x20, 0x15, 0xA6, 0x78, 0xD4, 0xD2, 0x20, 0xAA, 0x4B, 0xDA};

            var expectedCrc = 11160538u;

            uint actualCrc = ModeSMessage.Checksum(givenModeSMessage, 14 * 8);

            Assert.AreEqual(expectedCrc, actualCrc);
        }

        [TestMethod]
        public void TestCrc2() {
            var givenModeSMessage = new byte[]
                {0x8D, 0x40, 0x6B, 0x90, 0x20, 0x15, 0xA6, 0x78, 0xD4, 0xD2, 0x20, 0xAA, 0x4B, 0xDA};

            var expectedCrc = 11160538u;

            uint actualCrc = ModeSMessage.Checksum1(givenModeSMessage, 14 * 8);

            Assert.AreEqual(expectedCrc, actualCrc);
        }

        [TestMethod]
        public void FixSingleBitErrorsTest() {
            var givenModeSMessage = new byte[]
                {0x8D, 0x40, 0x6B, 0x90, 0x21, 0x15, 0xA6, 0x78, 0xD4, 0xD2, 0x20, 0xAA, 0x4B, 0xDA};

            var expectedModeSMessage = new byte[]
                {0x8D, 0x40, 0x6B, 0x90, 0x20, 0x15, 0xA6, 0x78, 0xD4, 0xD2, 0x20, 0xAA, 0x4B, 0xDA};

            var expectedFixedBitPosition = 39;

            int actual = ModeSMessage.FixSingleBitErrors(ref givenModeSMessage, 14 * 8);

            Assert.AreEqual(expectedFixedBitPosition, actual);
            Assert.AreEqual(expectedModeSMessage[4], givenModeSMessage[4]);
        }

        [TestMethod]
        public void CallSignTest() {
            var givenModeSMessage = new byte[]
                {0x8D, 0x40, 0x6B, 0x90, 0x20, 0x15, 0xA6, 0x78, 0xD4, 0xD2, 0x20, 0xAA, 0x4B, 0xDA};

            var actualmessage = ModeSMessage.DecodeMessage(givenModeSMessage);
            var expectedCallSign = "EZY85MH_";

            Assert.AreEqual(expectedCallSign, actualmessage.Flight);
        }
    }
}
