using System;
using System.IO;
using System.Text;
using System.Threading;
using brainflow;

namespace bciData
{
    public static class BoardCytonConstants
    {
        // Ohms. There is a series resistor on the 32 bit board
        public static double series_resistor_ohms = 2200;                                                                                                                           
        //reference voltage for ADC in ADS1299.  set by its hardware    
        public static double ADS1299_Vref = 4.5;                                                                                                                          
        //assumed gain setting for ADS1299.  set by its Arduino code
        public static double ADS1299_gain = 24.0;                                                                                                                              
        //ADS1299 datasheet Table 7, confirmed through experiment
        public static double scale_factor_uVolts_per_count = ADS1299_Vref / (Math.Pow(2, 23) - 1) / ADS1299_gain * 1000000.0;                                                      
        //6 nA, set by its Arduino code
        public static double leadOffDrive_amps = 6.0e-9;                                                                                                                                                    
        public static double accelScale = 0.002 / (Math.Pow(2, 4));
        public const double _threshold_near_railed = 75.0;
        public const double _threshold_railed = 90.0;
    }



    public class OpenBCI
    {
        private readonly BciOptions _options;
        private readonly BoardIds _boardId;
        private readonly BoardShim _boardShim;
        private Thread _dataThread;
        private readonly int _checkForRailedCount;
        private readonly int _packetIdRow;
        private readonly int _timeStampRow;
        private readonly int[] _eegRows;
        private readonly string[] _eegNames;
        private readonly string[] _customEventNames;
        private readonly int[] _accelRows;

        private readonly StreamWriter _logFile;

        public OpenBCI(BciOptions options, string[] customEventNames)
        {
            _options = options;
            _customEventNames = customEventNames;
            var baseBoardId = _options.Daisy ? BoardIds.CYTON_DAISY_BOARD : BoardIds.CYTON_BOARD;
            _boardId = _options.Synthetic
                ? BoardIds.SYNTHETIC_BOARD
                : _options.Daisy
                    ? (_options.WiFi ? BoardIds.CYTON_DAISY_WIFI_BOARD : BoardIds.CYTON_DAISY_BOARD)
                    : (_options.WiFi ? BoardIds.CYTON_WIFI_BOARD : BoardIds.CYTON_BOARD);
            AreCollecting = false;
            _packetIdRow = BoardShim.get_package_num_channel(Convert.ToInt32(_boardId));
            _timeStampRow = BoardShim.get_timestamp_channel(Convert.ToInt32(_boardId));
            _eegRows = BoardShim.get_eeg_channels(Convert.ToInt32(_boardId));
            _eegNames = BoardShim.get_eeg_names(Convert.ToInt32(baseBoardId));
            _accelRows = BoardShim.get_accel_channels(Convert.ToInt32(_boardId));
            _checkForRailedCount = options.CheckForRailedCount;
            var logFileName = $"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}{string.Join("_", _options.Tags)}{(_options.Synthetic ? "_synthetic" : "")}.csv";
            LogFilePath = Path.Combine(_options.LogsFolderPath, logFileName);
            RawLogFilePath = LogFilePath.Replace(".csv", "_raw.csv");
            DebugLogFilePath = LogFilePath.Replace(".csv", "_debug.log");
            BrainflowLogFilePath = LogFilePath.Replace(".csv", "_brainflow.log");
            _options.DebugLog(false, $"Setting brainflow raw capture csv {RawLogFilePath}");
            _options.DebugLog(false, $"Setting bcigame normalized capture csv file path to: {LogFilePath}");
            _options.DebugLog(false, $"Setting brainflow log file path to: {BrainflowLogFilePath}");
            _options.DebugLog(false, $"Enabling brainflow logger");
            BoardShim.enable_dev_board_logger();
            BoardShim.set_log_file(BrainflowLogFilePath);
            LogModuleInfo("brainflow");
            LogModuleInfo("boardcontroller");
            File.WriteAllText(RawLogFilePath, $"Packet,{string.Join(",", _eegNames)},Other1,Other2,Other3,Other4,Other5,Other6,AX,AY,AZ,Other7,Other8,Other9,Timestamp\n");
            var input_params = new BrainFlowInputParams();
            if (!options.Synthetic)
            {
                if (_options.WiFi)
                {
                    if (!BciWiFi.ConnectToOpenBCIWifi(out var ssid))
                        _options.DebugLog(true, $"Unable to connected to OpenBCI headset Wifi: {ssid}");
                    input_params.ip_address =
                        string.IsNullOrEmpty(_options.IpAddress) ? "192.168.4.1" : _options.IpAddress;
                    input_params.ip_port = _options.IpPort == 0 ? 6677 : _options.IpPort;
                    input_params.ip_protocol = Convert.ToInt32(_options.IpProtocol);
                    input_params.timeout = _options.Timeout;
                }
                else
                {
                    input_params.serial_port = _options.Port;
                }
            }

            BoardShim.log_message(Convert.ToInt32(LogLevels.LEVEL_INFO), $"board_id: {_boardId} ({Convert.ToInt32(_boardId)})");
            BoardShim.log_message(Convert.ToInt32(LogLevels.LEVEL_INFO), input_params.to_json());
            try
            {
                _boardShim = new BoardShim(Convert.ToInt32(_boardId), input_params);
                _boardShim.prepare_session();
                if (!_boardShim.is_prepared())
                    throw new ApplicationException("prepare_session succeeded but is_prepared still false");

                IsConnected = true;
                _logFile = new StreamWriter(LogFilePath);
                _logFile.WriteLine($"Timestamp,{string.Join(",", _eegNames)},AX,AY,AZ,{string.Join(",", _customEventNames)}");
                _logFile.Flush();
            }
            catch (Exception e)
            {
                IsConnected = false;
                _options.DebugLog(true, $"Failed to connected to OpenBCI headset: {e.Message}");
            }
        }

        public int Verbosity => _options.Verbosity;
        public bool IsConnected { get; set; }
        public bool AreCollecting { get; set; }
        public int SamplesCollected { get; set; }
        public string LogFilePath { get; }
        public string RawLogFilePath { get; }
        public string DebugLogFilePath { get; }
        public string BrainflowLogFilePath { get; }

        private void LogModuleInfo(string name)
        {
            var hModule = NativeMethods.GetModuleHandle(name);
            var sb = new StringBuilder(256);
            if (NativeMethods.GetModuleFileName(hModule, sb, (uint)sb.Capacity) != 0)
                BoardShim.log_message(Convert.ToInt32(LogLevels.LEVEL_INFO), $"IsLoaded: {sb} {File.GetLastWriteTime(sb.ToString()):O}");
        }

        public bool StartStream()
        {
            if (!IsConnected)
            {
                _options.DebugLog(true, $"Attempt to start data stream failed as not connected.");
                return false;
            }

            AreCollecting = IsConnected;
            SamplesCollected = 0;

            _dataThread = new Thread(CollectionThread);
            _dataThread.Priority = ThreadPriority.BelowNormal;
            _options.DebugLog(false, $"Starting data collection thread.");
            _dataThread.Start();
            return true;
        }

        private void CollectionThread()
        {
            if (!IsConnected) return;

            _options.DebugLog(false, $"Entering data collection thread.  Starting data stream.");
            _boardShim.start_stream(45000, $"file://{RawLogFilePath}:a");
            var _railedCountdown = -1;
            while (AreCollecting)
            {
                if (_boardShim.get_board_data_count() == 0)
                    continue;
                
                var data = _boardShim.get_board_data();
                _options.DebugLog(false, $"get_data returned {data.GetLength(1)} rows");

                int railedElectrodes = 0;
                try
                {
                    railedElectrodes = CheckRailed(data);
                }
                catch (Exception e)
                {
                    _options.DebugLog(true, $"CheckRailed exception - {e.Message}\n{e.StackTrace}");
                }
                for (var sampleIndex = 0; sampleIndex < data.GetLength(1); sampleIndex++)
                {
                    SamplesCollected += 1;
                    if (SamplesCollected % 1000 == 0)
                        _options.DebugLog(false, $"Collection Thread heartbeat: {data[_timeStampRow, sampleIndex]}: {SamplesCollected}");

                    //
                    // Because the channels_data and aux_data is the raw data in counts read by the board,
                    // we need to multiply the data by a scale factor. There is a specific scale factor for each board:
                    //
                    // #### For the Cyton and Cyton + Daisy boards:
                    //
                    // Multiply uVolts_per_count to convert the channels_data to uVolts.
                    //   uVolts_per_count = (4500000)/24/(2**23-1) #uV/count
                    //
                    var channelData = new double[_eegRows.Length];
                    for (var eegIndex = 0; eegIndex < _eegRows.Length; eegIndex++)
                    {
                        channelData[eegIndex] = data[_eegRows[eegIndex], sampleIndex] * BoardCytonConstants.scale_factor_uVolts_per_count;
                    }

                    if (railedElectrodes != 0)
                    {
                        if (_railedCountdown == -1)
                            _railedCountdown = _checkForRailedCount;

                        _railedCountdown -= 1;
                    }
                    else
                    {
                        // No railed electrodes this sample, so reset railed count down counter.
                        _railedCountdown = -1;
                    }

                    var auxData = new double[_accelRows.Length];
                    for (var auxIndex = 0; auxIndex < _accelRows.Length; auxIndex++)
                        auxData[auxIndex] = data[_accelRows[auxIndex], sampleIndex] * BoardCytonConstants.accelScale;
                    if (_options.EventQueue == null || !_options.EventQueue.TryDequeue(out var eventData))
                        eventData = null;
                    var bciSample = new BciSample(SamplesCollected, channelData, auxData,
                        data[_timeStampRow, sampleIndex], railedElectrodes, eventData);
                    if (!_options.ProcessBciSample(bciSample))
                        continue;

                    // Experiment wants to save this sample, so append to output .csv file
                    var sb = new StringBuilder();
                    sb.Append($"{bciSample.TimeStamp:0.0#####}");
                    for (var eegIndex = 0; eegIndex < _eegRows.Length; eegIndex++)
                        sb.Append($",{channelData[eegIndex]:0.0#####}");

                    foreach (var d in bciSample.AuxData)
                        sb.Append($",{d:0.0#####}");

                    if (bciSample.EventData != null)
                        foreach (var d in bciSample.EventData)
                            sb.Append($",{d:00}");

                    _logFile.WriteLine($"{sb}");
                    _logFile.Flush();
                }
            }
        }

        public enum RailedStatus
        {
            NotRailed,
            NearRailed,
            Railed
        }

        public int CheckRailed(double[,] data)
        {
            var numRows = data.GetLength(1);
            if (numRows < 3 * 3)
            {
                return 0;
            }

            var railedStatus = 0;
            var maxVal = BoardCytonConstants.scale_factor_uVolts_per_count * Math.Pow(2, 23);
            var numSeconds = 3;
            var nPoints = numSeconds * BoardShim.get_sampling_rate(Convert.ToInt32(_boardId));
            var endPos = numRows;
            _options.DebugLog(false, $"CheckRailed - maxVal: {maxVal}  nPoints: {nPoints}  endPos: {endPos}  #cols: {data.GetLength(0)}");
            for (var channel = 0; channel < _eegNames.Length; channel++)
            {
                var startPos = Math.Max(0, endPos - nPoints);
                var max = double.MinValue;
                var row = channel < _eegRows.Length ? _eegRows[channel] : -1;
                for (var i = startPos; row != -1 && i < endPos; i++)
                {
                    try
                    {
                        max = Math.Max(max, Math.Abs(data[row, i]));
                    }
                    catch (Exception e)
                    {
                        _options.DebugLog(true, $"CheckRailed: data[{_eegRows[channel]}, {i}] - {e.Message}");
                    }
                }
                var percentage= (max / maxVal) * 100.0;
                var channelStatus = percentage > BoardCytonConstants._threshold_railed ? RailedStatus.Railed :
                    percentage > BoardCytonConstants._threshold_near_railed ? RailedStatus.NearRailed :
                    RailedStatus.NotRailed;
                railedStatus |= (int) channelStatus << channel * 2;
            }
            _options.DebugLog(false, $"CheckRailed returns: {railedStatus:X}");
            return railedStatus;
        }

        public bool StopStream()
        {
            try
            {
                _options.DebugLog(false, $"Stopping data stream.");
                AreCollecting = false;
                _boardShim.stop_stream();
            }
            catch (BrainFlowException e)
            {
                _options.DebugLog(true, $"{e.Message}\n{e.StackTrace}");
                return false;
            }

            return true;
        }

        public bool Close()
        {
            try
            {
                _options.DebugLog(false, $"Releasing BCI connection and closing logfile.  IsPrepared: {_boardShim.is_prepared()}");
                if (_boardShim.is_prepared())
                    _boardShim.release_session();
                BoardShim.disable_board_logger();
                _logFile.Close();
            }
            catch (BrainFlowException e)
            {
                _options.DebugLog(true, $"{e.Message}\n{e.StackTrace}");
                return false;
            }

            return true;
        }

    }

    public class BciSample
    {
        private static double _baseTimestamp = 0.0;
        public BciSample(int packetId, double[] channelData, double[] auxData, double timeStamp, int railedElectrodes, int[] eventData)
        {
            Id = packetId;
            ChannelData = channelData;
            AuxData = auxData;
            TimeStamp = timeStamp;
            if (_baseTimestamp == 0)
                _baseTimestamp = timeStamp;
            else
                TimeStamp -= _baseTimestamp;
            RailedElectrodes = railedElectrodes;
            EventData = eventData;
        }

        public int Id { get; }
        public double[] ChannelData { get; }
        public double[] AuxData { get; }
        public double TimeStamp { get; }
        public int[] EventData { get; set; }
        public int RailedElectrodes { get; }
    }
}
