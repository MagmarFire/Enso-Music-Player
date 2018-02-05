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
                    string loopstartTagValue = ReadTrackMetadata(EnsoConstants.LoopStartTag);

                    if (string.IsNullOrEmpty(loopstartTagValue))
                    {
                        loadedLoopStart = 0;
                    }
                    else
                    {
                        loadedLoopStart = int.Parse(loopstartTagValue);
                    }

                    return loadedLoopStart;
                }
            }

            set
            {
                loopPoints.sampleLoopStart = value;
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
                    string looplengthTagValue = ReadTrackMetadata(EnsoConstants.LoopLengthTag);

                    if (string.IsNullOrEmpty(looplengthTagValue))
                    {
                        loadedLoopLength = Track.samples - Math.Min(Track.samples, LoopStart);
                    }
                    else
                    {
                        loadedLoopLength = int.Parse(looplengthTagValue);
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
        /// The sum of the lengths of the intro clip and loop clip in samples.
        /// </summary>
        public int LengthInSamples
        {
            get
            {
                if (Track != null)
                {
                    return IntroClip.samples + LoopClip.samples;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// The sum of the lengths of the intro clip and loop clip in seconds.
        /// </summary>
        public float LengthInSeconds
        {
            get
            {
                if (Track != null)
                {
                    return IntroClip.length + LoopClip.length;
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
                int loopStartSampleCount = Math.Max(0, LoopStart);
                int loopLengthSampleCount = Math.Min(Track.samples - 1, Math.Max(1, LoopLength));
                int introLength = Math.Max(1, LoopStart);

                Track.GetData(clipData, 0);

                IntroClip = AudioClip.Create(
                    Name + " intro",
                    introLength,
                    channels,
                    Track.frequency,
                    false);

                IntroClip.SetData(EnsoHelpers.Slice(clipData, 0, introLength * channels), 0);

                LoopClip = AudioClip.Create(
                    Name + " loop",
                    loopLengthSampleCount,
                    channels,
                    Track.frequency,
                    false);

                LoopClip.SetData(EnsoHelpers.Slice(clipData, loopStartSampleCount * channels, loopLengthSampleCount * channels), 0);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(@"[Enso] An exception occurred when creating and caching the ""{0}"" music clip: {1}", Track.name, e.Message));
                throw e;
            }
        }

        /// <summary>
        /// Converts the inputted time in samples to its equivalent in seconds.
        /// </summary>
        /// <param name="samples">The time in samples</param>
        /// <returns>The time in seconds</returns>
        public float SamplesToSeconds(int samples)
        {
            return Mathf.Min(LengthInSeconds, (float)samples / Track.frequency);
        }

        /// <summary>
        /// Converts the inputted time to its equivalent time in samples.
        /// </summary>
        /// <param name="time">The time in seconds</param>
        /// <returns>The time in samples</returns>
        public int SecondsToSamples(float time)
        {
            return Math.Min(LengthInSamples, Convert.ToInt32(time * Track.frequency));
        }

        /// <summary>
        /// Converts the inputted time to its equivalent time in samples.
        /// </summary>
        /// <param name="time">The time in seconds</param>
        /// <returns>The time in samples</returns>
        [Obsolete("This is a deprecated alias for SecondsToSamples. Use that instead.")]
        public int TimeToSamples(float time)
        {
            return SecondsToSamples(time);
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