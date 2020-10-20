using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using UnityEngine;

namespace bciData
{
    public struct GameColors
    {
        public GameColors(Color stimulus, Color correct, Color incorrect, Color text,
            Color erase)
        {
            StimulusChoice = stimulus;
            CorrectChoice = correct;
            IncorrectChoice = incorrect;
            Text = text;
            Erase = erase;
        }

        public Color StimulusChoice { get; }
        public Color CorrectChoice { get; }
        public Color IncorrectChoice { get; }
        public Color Text { get; }
        public Color Erase { get; }
    }

    public class BciSample
    {
        public BciSample(int packetId, double[] channelData, double[] auxData, ulong timeStamp, int railedElectrodes)
        {
            Id = packetId;
            ChannelData = channelData;
            AuxData = auxData;
            TimeStamp = timeStamp;
            RailedElectrodes = railedElectrodes;
            EventData = null;
        }

        public int Id { get; }
        public double[] ChannelData { get; }
        public double[] AuxData { get; }
        public ulong TimeStamp { get; }
        public int[] EventData { get; set; }
        public int RailedElectrodes { get; }
    }

    public interface IBCIDATA
    {
        int Verbosity { get; }
        bool AreCollecting { get; }
        bool StartStream();
        bool StopStream();
    }
}


















