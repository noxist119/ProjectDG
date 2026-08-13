using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DefenseGame
{
    /// <summary>
    /// Owns deterministic run-content streams. Combat and presentation must not use this service.
    /// Each channel starts from the run seed plus a fixed numeric salt so one channel's draw count
    /// cannot perturb any other content channel.
    /// </summary>
    public enum RunContentRandomChannel
    {
        Summon = 0,
        Augment = 1,
        Mission = 2,
        Shop = 3,
        Board = 4,
        Lucky = 5,
        Fate = 6,
        Merge = 7
    }

    public sealed class RunContentRandomService
    {
        private const int TraceLimitPerChannel = 64;
        private const uint SummonSalt = 0x13579BDFu;
        private const uint AugmentSalt = 0x2468ACE1u;
        private const uint MissionSalt = 0x0F1E2D3Cu;
        private const uint ShopSalt = 0x55AA7711u;
        private const uint BoardSalt = 0x39A6C4E7u;
        private const uint LuckySalt = 0x7F4A7C15u;
        private const uint FateSalt = 0xA3B19535u;
        private const uint MergeSalt = 0xC12E5A77u;

        private readonly Dictionary<RunContentRandomChannel, StreamState> streams = new Dictionary<RunContentRandomChannel, StreamState>();

        public int RunSeed { get; private set; }

        public void Reset(int runSeed)
        {
            RunSeed = runSeed;
            streams.Clear();
            foreach (RunContentRandomChannel channel in Enum.GetValues(typeof(RunContentRandomChannel)))
            {
                streams[channel] = new StreamState(MixSeed(unchecked((uint)runSeed), GetSalt(channel)));
            }
        }

        public int Range(RunContentRandomChannel channel, int minInclusive, int maxExclusive, string eventType = null)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            StreamState stream = GetStream(channel);
            uint range = (uint)(maxExclusive - minInclusive);
            int result = minInclusive + (int)(stream.NextUInt() % range);
            string request = (eventType ?? "range") + "[" +
                minInclusive.ToString(CultureInfo.InvariantCulture) + "," +
                maxExclusive.ToString(CultureInfo.InvariantCulture) + ")";
            stream.Record(request, result.ToString(CultureInfo.InvariantCulture));
            return result;
        }

        public float Value(RunContentRandomChannel channel, string eventType = null)
        {
            StreamState stream = GetStream(channel);
            float result = (stream.NextUInt() >> 8) * (1f / 16777216f);
            stream.Record(eventType, result.ToString("R", CultureInfo.InvariantCulture));
            return result;
        }

        public void RecordOutcome(RunContentRandomChannel channel, string eventType, string selectedResultId)
        {
            GetStream(channel).Record(eventType, selectedResultId);
        }

        public uint GetChannelSeed(RunContentRandomChannel channel)
        {
            return GetStream(channel).InitialSeed;
        }

        public int GetDrawCount(RunContentRandomChannel channel)
        {
            return GetStream(channel).DrawCount;
        }

        public string GetOutcomeHash(RunContentRandomChannel channel)
        {
            return GetStream(channel).GetHash();
        }

        public IReadOnlyList<string> GetTracePrefix(RunContentRandomChannel channel)
        {
            return GetStream(channel).Trace;
        }

        private StreamState GetStream(RunContentRandomChannel channel)
        {
            if (!streams.TryGetValue(channel, out StreamState stream))
            {
                stream = new StreamState(MixSeed(unchecked((uint)RunSeed), GetSalt(channel)));
                streams[channel] = stream;
            }

            return stream;
        }

        private static uint GetSalt(RunContentRandomChannel channel)
        {
            switch (channel)
            {
                case RunContentRandomChannel.Summon: return SummonSalt;
                case RunContentRandomChannel.Augment: return AugmentSalt;
                case RunContentRandomChannel.Mission: return MissionSalt;
                case RunContentRandomChannel.Shop: return ShopSalt;
                case RunContentRandomChannel.Board: return BoardSalt;
                case RunContentRandomChannel.Lucky: return LuckySalt;
                case RunContentRandomChannel.Fate: return FateSalt;
                case RunContentRandomChannel.Merge: return MergeSalt;
                default: return 0xC0FFEE11u;
            }
        }

        private static uint MixSeed(uint seed, uint salt)
        {
            uint value = seed ^ salt;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value == 0u ? 0x6D2B79F5u : value;
        }

        private sealed class StreamState
        {
            private const ulong FnvOffsetBasis = 14695981039346656037UL;
            private const ulong FnvPrime = 1099511628211UL;
            private uint state;
            public uint InitialSeed { get; }
            private ulong hash = FnvOffsetBasis;
            private readonly List<string> trace = new List<string>();

            public StreamState(uint initialState)
            {
                InitialSeed = initialState == 0u ? 0x6D2B79F5u : initialState;
                state = InitialSeed;
            }

            public int DrawCount { get; private set; }
            public IReadOnlyList<string> Trace => trace;

            public uint NextUInt()
            {
                uint value = state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                state = value == 0u ? 0x6D2B79F5u : value;
                DrawCount++;
                return state;
            }

            public void Record(string eventType, string outcome)
            {
                string entry = DrawCount.ToString(CultureInfo.InvariantCulture) + ":" + (eventType ?? "draw") + ":" + (outcome ?? string.Empty);
                UpdateHash(entry);
                if (trace.Count < TraceLimitPerChannel)
                {
                    trace.Add(entry);
                }
            }

            public string GetHash()
            {
                return hash.ToString("X16", CultureInfo.InvariantCulture);
            }

            private void UpdateHash(string value)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= FnvPrime;
                }

                hash ^= 10;
                hash *= FnvPrime;
            }
        }
    }
}
