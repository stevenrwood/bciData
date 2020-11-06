using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using brainflow;

namespace bciData
{
    public delegate bool ProcessBciSampleDelegate(BciSample bciSample);
    public delegate void LoggerDelegate(string message);

    public class BciOptions
    {
        
        public int Verbosity;
        public bool Synthetic;
        public bool Daisy;
        public bool WiFi;
        public int CheckForRailedCount;
        public int Timeout;
        public string IpAddress;
        public int IpPort;
        public IpProtocolType IpProtocol;
        public string Port;
        public string LogsFolderPath;
        public List<string> Tags;
        public ConcurrentQueue<int[]> EventQueue;
        public ProcessBciSampleDelegate ProcessBciSample;
        public LoggerDelegate LogMessage;
        public BciOptions()
        {
            Verbosity = 0;
            IpAddress = string.Empty;
            IpPort = 0;
            IpProtocol = IpProtocolType.TCP;
            Port = string.Empty;
            LogsFolderPath = string.Empty;
            Tags = new List<string>();
            Daisy = false;
            WiFi = false;
            Timeout = 0;
            CheckForRailedCount = 5;
            EventQueue = null;
            ProcessBciSample = null;
        }

        public void DebugLog(bool error, string message)
        {
            BoardShim.log_message(Convert.ToInt32(LogLevels.LEVEL_INFO), message);
            LogMessage?.Invoke(message);
        }
    }
}
