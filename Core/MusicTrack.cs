using System;
using UnityEditor;
using UnityEngine;
using ATL;

namespace EnsoMusicPlayer
{
    [Serializable]
    public class MusicTrack
    {
        public string Name;
        public AudioClip Track;

        [Serializable]
        public struct LoopPoints
        {
            [Tooltip("When the loop will start for this track, in samples. Set to 0 to use the track's defaults.")]
            public int sampleLoopStart;

            [Tooltip("How long a loop will last before going back to the start point, in samples. Set to 0 to use the track's defaults.")]
            public int sampleLoopLength;
        }
        public LoopPoints loopPoints;

        private int loadedLoopStart;
        private int loadedLoopLength;

        public int LoopStart
        {
            get
            {
                if (loopPoints.sampleLoopStart > 0)
                {
                    return loopPoints.sampleLoopStart;
                }
                else if (loadedLoopStart > 0)
                {
                    return loadedLoopStart;
                }
                else
                {
                    string loopstartTag = ReadTrackMetadata("LOOPSTART");

                    if (string.IsNullOrEmpty(loopstartTag))
                    {
                        loadedLoopStart = 0;
                    }
                    else
                    {
                        loadedLoopStart = int.Parse(loopstartTag);
                    }

                    return loadedLoopStart;
                }
            }

            set
            {
                loopPoints.sampleLoopStart = value;
            }
        }

        public float LoopStartInSeconds
        {
            get
            {
                return (float)LoopStart / Track.frequency;
            }
        }

        public int LoopLength
        {
            get
            {
                if (loopPoints.sampleLoopLength > 0)
                {
                    return loopPoints.sampleLoopLength;
                }
                else if (loadedLoopLength > 0)
                {
                    return loadedLoopLength;
                }
                else
                {
                    string looplengthTag = ReadTrackMetadata("LOOPLENGTH");

                    if (string.IsNullOrEmpty(looplengthTag))
                    {
                        loadedLoopLength = 0;
                    }
                    else
                    {
                        loadedLoopLength = int.Parse(looplengthTag);
                    }

                    return loadedLoopLength;
                }
            }

            set
            {
                loopPoints.sampleLoopLength = value;
            }
        }

        /// <summary>
        /// Length of the track in samples. Useful for determining the length
        /// of a track without needing to take into account how many channels
        /// it has.
        /// </summary>
        public int LengthInSamples
        {
            get
            {
                if (Track != null)
                {
                    return Track.samples / Track.channels;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int LengthInSeconds
        {
            get
            {
                if (Track != null)
                {
                    return LengthInSamples / Track.frequency;
                }
                else
                {
                    return 0;
                }
            }
        }

        public AudioClip IntroClip{ get; private set; }
        public AudioClip LoopClip { get; private set; }

        /// <summary>
        /// Splits the audio track into two separate tracks (the intro and the loop) and caches them.
        /// This MUST be called before the overall track can be played.
        /// </summary>
        public void CreateAndCacheClips()
        {
            try
            {
                int channels = Track.channels;
                float[] clipData = new float[Track.samples * channels];
                int loopStartSampleCount = Math.Max(1, LoopStart * channels);
                int loopLengthSampleCount = Math.Max(1, LoopLength * channels);

                Track.GetData(clipData, 0);

                IntroClip = AudioClip.Create(
                    Name + " intro",
                    Math.Max(1, LoopStart),
                    channels,
                    Track.frequency,
                    false);

                IntroClip.SetData(EnsoHelpers.Slice(clipData, 0, loopStartSampleCount), 0);

                LoopClip = AudioClip.Create(
                    Name + " loop",
                    Math.Max(1, LoopLength),
                    channels,
                    Track.frequency,
                    false);

                LoopClip.SetData(EnsoHelpers.Slice(clipData, loopStartSampleCount, loopLengthSampleCount), 0);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(@"[Enso] An exception occurred when creating and caching the ""{0}"" music clip: {1}", Track.name, e.Message));
                throw e;
            }
        }

        /// <summary>
        /// Converts the inputted time to its equivalent time in samples.
        /// </summary>
        /// <param name="time">The time in seconds</param>
        /// <returns>The time in samples</returns>
        public int TimeToSamples(float time)
        {
            return Math.Min(
                Math.Max(0, LengthInSamples - 1), // Subtract 1 to avoid an edge case error.
                Convert.ToInt32(time * Track.frequency));
        }

        public virtual string ReadTrackMetadata(string name)
        {
            string trackPath = AssetDatabase.GetAssetPath(Track);

            if (string.IsNullOrEmpty(trackPath)) return string.Empty;

            Track trackAsset = new Track(trackPath);

            try
            {
                return trackAsset.AdditionalFields[name];
            }
            catch
            {
                Debug.LogWarning(string.Format(@"Field ""{0}"" does not exist for track ""{1}"".", name, Track.name));
                return string.Empty;
            }
        }
    }
}