namespace ADSBSharp {
    public interface IFrameSink {
        void Start(string hostName, int port);
        void Stop();
        void FrameReady(byte[] frame, int actualLength);
    }
}
