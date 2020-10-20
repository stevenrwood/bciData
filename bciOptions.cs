using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace bciData
{
    public delegate bool DebugLogDelegate(string message);
    public delegate bool ProcessBciSampleDelegate(BciSample bciSample);

    public class BciOptions
    {
        public int Verbosity;
        public bool Daisy;
        public bool WiFi;
        public int CheckForRailedCount;
        public int Timeout;
        public string IpAddress;
        public int IpPort;
        public string Port;
        public string LogFolderPath;
        public string[] CustomFieldNames;
        public int FixedStimulusDelay;
        public int RandomStimulusDelay;
        public int FeedbackDelay;
        public ProcessBciSampleDelegate ProcessBciSample;
        public DebugLogDelegate DebugLog;
        public BciOptions()
        {
            Verbosity = 0;
            IpAddress = string.Empty;
            IpPort = 0;
            Port = string.Empty;
            LogFolderPath = string.Empty;
            Daisy = false;
            WiFi = false;
            Timeout = 0;
            CheckForRailedCount = 5;
            CustomFieldNames = null;
            FixedStimulusDelay = 0;
            RandomStimulusDelay = 4000;
            FeedbackDelay = 1000;
            ProcessBciSample = null;
            DebugLog = null;
        }
    }
}
