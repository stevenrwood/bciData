using System;
using System.IO;
using System.Text;
using System.Threading;
using brainflow;

namespace bciData
{
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
        private int _brainflowSampleCount;
        private const double ValidNegativeThreshold = -180000.0;
        private const double ValidPositiveThreshold =  180000.0;
        private readonly string _brainflowLogFilePath;
        private readonly string _logFilePath;
        private readonly StreamWriter _logFile;

        public OpenBCI(BciOptions options, string[] customEventNames)
        {
            _options = options;
            _customEventNames = customEventNames;
            var baseBoardId = _options.Daisy ? BoardIds.CYTON_DAISY_BOARD : BoardIds.CYTON_BOARD;
            _boardId = _options.Daisy
                ? (_options.WiFi ? BoardIds.CYTON_DAISY_WIFI_BOARD : BoardIds.CYTON_DAISY_BOARD)
                : (_options.WiFi ? BoardIds.CYTON_WIFI_BOARD : BoardIds.CYTON_BOARD);
            AreCollecting = false;
            _packetIdRow = BoardShim.get_package_num_channel(Convert.ToInt32(_boardId));
            _timeStampRow = BoardShim.get_timestamp_channel(Convert.ToInt32(_boardId));
            _eegRows = BoardShim.get_eeg_channels(Convert.ToInt32(_boardId));
            _eegNames = BoardShim.get_eeg_names(Convert.ToInt32(baseBoardId));
            _accelRows = BoardShim.get_accel_channels(Convert.ToInt32(_boardId));
            _checkForRailedCount = options.CheckForRailedCount;
            var logFileName = $"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.csv";
            _logFilePath = Path.Combine(_options.LogsFolderPath, logFileName);
            _brainflowLogFilePath = _logFilePath.Replace(".csv", "_raw.csv");

            var input_params = new BrainFlowInputParams();
            if (_options.WiFi)
            {
                if (!BciWiFi.ConnectToOpenBCIWifi(out var ssid))
                    throw new ApplicationException($"Unable to connected to OpenBCI headset Wifi: {ssid}");
                input_params.ip_address = string.IsNullOrEmpty(_options.IpAddress) ? "192.168.4.1" : _options.IpAddress;
                input_params.ip_port = _options.IpPort;
                input_params.timeout = _options.Timeout;
            }
            else
                input_params.serial_port = _options.Port;

            BoardShim.enable_dev_board_logger();
            _boardShim = new BoardShim(Convert.ToInt32(_boardId), input_params);
            _boardShim.prepare_session();
            if (!_boardShim.is_prepared())
                throw new ApplicationException("prepare_session succeeded but is_prepared still false");
            BoardShim.disable_board_logger();

            _logFile = new StreamWriter(_logFilePath);
            _logFile.WriteLine($"Timestamp,{string.Join(",", _eegNames)},AX,AY,AZ,{string.Join(",", _customEventNames)}");
            _logFile.Flush();
        }

        public int Verbosity => _options.Verbosity;
        public bool AreCollecting { get; set; }

        public bool StartStream()
        {
            AreCollecting = true;
            _dataThread = new Thread(CollectionThread);
            _dataThread.Start();
            return true;
        }

        private void CollectionThread()
        {
            _boardShim.start_stream(45000, $"file://{_brainflowLogFilePath}:w");
            var _railedCountdown = -1;
            while (AreCollecting)
            {
                double[,] data = _boardShim.get_board_data();
                int railedElectrodes = 0;
                for (var sampleIndex = 0; sampleIndex < data.GetLength(1); sampleIndex++)
                {
                    _brainflowSampleCount += 1;
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
                        channelData[eegIndex] = data[_eegRows[eegIndex], sampleIndex] *
                                                ((4500000) / 24 / (Math.Pow(2, 23) - 1));
                        if (channelData[eegIndex] < ValidNegativeThreshold ||
                            channelData[eegIndex] > ValidPositiveThreshold)
                            railedElectrodes |= 1 << eegIndex;
                    }

                    if (railedElectrodes != 0)
                    {
                        if (_railedCountdown == -1)
                            _railedCountdown = _checkForRailedCount;

                        _railedCountdown -= 1;
                        continue;
                    }

                    // No railed electrodes this sample, so reset railed count down counter.
                    _railedCountdown = -1;

                    var auxData = new double[_accelRows.Length];
                    for (var auxIndex = 0; auxIndex < _accelRows.Length; auxIndex++)
                        auxData[auxIndex] = data[_accelRows[auxIndex], sampleIndex] * (.002 / 16);
                    int[] eventData;
                    if (_options.EventQueue == null || !_options.EventQueue.TryDequeue(out eventData))
                        eventData = null;
                    var bciSample = new BciSample(_brainflowSampleCount, channelData, auxData, 
                        (ulong) data[_timeStampRow, sampleIndex], railedElectrodes, eventData);
                    if (!_options.ProcessBciSample(bciSample))
                        continue;

                    // Experiment wants to save this sample, so append to output .csv file
                    var sb = new StringBuilder();
                    sb.Append($"{bciSample.TimeStamp}");
                    for (var eegIndex = 0; eegIndex < _eegRows.Length; eegIndex++)
                        sb.AppendFormat(",{0,12:F4}", channelData[eegIndex]);

                    foreach (var d in bciSample.AuxData)
                        sb.AppendFormat(",{0,12:F4}", d);

                    if (bciSample.EventData != null)
                        foreach (var d in bciSample.EventData)
                            sb.AppendFormat(",{0,12:D}", (ulong) d);

                    _logFile.WriteLine($"{sb}");
                    _logFile.Flush();
                }
            }
        }

        public bool StopStream()
        {
            try
            {
                AreCollecting = false;
                _boardShim.stop_stream();
                _boardShim.release_session();
                _logFile.Close();
            }
            catch (BrainFlowException e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
                return false;
            }

            return true;
        }
    }

    public class BciSample
    {
        public BciSample(int packetId, double[] channelData, double[] auxData, ulong timeStamp, int railedElectrodes, int[] eventData)
        {
            Id = packetId;
            ChannelData = channelData;
            AuxData = auxData;
            TimeStamp = timeStamp;
            RailedElectrodes = railedElectrodes;
            EventData = eventData;
        }

        public int Id { get; }
        public double[] ChannelData { get; }
        public double[] AuxData { get; }
        public ulong TimeStamp { get; }
        public int[] EventData { get; set; }
        public int RailedElectrodes { get; }
    }
}
