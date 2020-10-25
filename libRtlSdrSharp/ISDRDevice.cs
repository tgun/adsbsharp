﻿using System;
using System.IO;

namespace libRtlSdrSharp {
    public interface ISDRDevice : IDisposable {
        // -- Features
        int[] SupportedGains { get; }
        RtlSdrTunerType TunerType { get; }
        // -- Status
        uint Index { get; }
        uint SampleRate { get; set; }
        uint Frequency { get; set; }
        string Name { get; }
        bool UseRtlAGC { get; set; }
        bool UseTunerAGC { get; set; }
        int FrequencyCorrection { get; set; }
        bool BiasTee { get; set; }
        int TunerGain { get; set; }
        bool IsStreaming { get; }
        object BufferLock { get; set; }
        MemoryStream Buffer { get; set; }

        void Start();
        void Stop();
        event EmptyEventDelegate DataAvailable;
    }
}
