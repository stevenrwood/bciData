using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace bciData
{
    public class BciWiFi
    {
        public static bool ConnectToOpenBCIWifi()
        {
            var reSSId = new Regex(@"^SSID\s+\d+\s+\:\s+(?<SSID>OPENBci-.+?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var output = RunNetSh("wlan show networks");
            var openBCISSIDName = string.Empty;
            var m = reSSId.Match(output);
            while (m.Success)
            {
                var ssid = m.Groups["SSID"].Value;
                if (ssid.StartsWith("OpenBCI-", StringComparison.OrdinalIgnoreCase))
                {
                    openBCISSIDName = ssid.Trim();
                    break;
                }
                m = m.NextMatch();
            }
            if (string.IsNullOrEmpty(openBCISSIDName))
                throw new ApplicationException("Wifi specified and no active OpenBCI-XXXX network visible to connect to.\nMake sure Cyton WiFi board turned on and booted.");

            output = RunNetSh($"wlan connect {openBCISSIDName} {openBCISSIDName} Wi-Fi").Trim();
            return output.StartsWith("Connection request was completed successfully.", StringComparison.OrdinalIgnoreCase);
        }

        public static string RunNetSh(string arguments)
        {
            var p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            try
            {
                p.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to run NetSh.exe - {e.Message}");
                Environment.Exit(1);
            }
            AsyncConsoleReader console = new AsyncConsoleReader(p);
            p.WaitForExit();

            return console.ToString();
        }
    }

    internal class AsyncConsoleReader
    {
        private readonly StringBuilder _output;

        internal AsyncConsoleReader(Process process)
        {
            _output = new StringBuilder();
            process.ErrorDataReceived += OnDataReceived;
            process.OutputDataReceived += OnDataReceived;
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        public override string ToString()
        {
            lock (_output)
                return _output.ToString();
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                lock (_output)
                {
                    _output.AppendLine(args.Data);
                }
            }
        }
    }
}


