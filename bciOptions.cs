using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace bciData
{
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
        public string LogsFolderPath;
        public List<string> Tags;
        public ConcurrentQueue<int[]> EventQueue;
        public ProcessBciSampleDelegate ProcessBciSample;
        public BciOptions()
        {
            Verbosity = 0;
            IpAddress = string.Empty;
            IpPort = 0;
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
    }
}
