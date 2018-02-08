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
        public class LoopPoints
        {
            [Tooltip("When the loop will start for this track, in samples. Set to 0 to use the track's defaults.")]
            public int sampleLoopStart;

            [Tooltip("How long a loop will last before going back to the start point, in samples. Set to 0 to use the track's defaults.")]
            public int sampleLoopLength;

            [Tooltip("Leave this checked to permit Ensō to automatically adjust the set loop points for any change in frequency that may have happened when importing the audio clip. Uncheck it to use the values as written.")]
            public bool compensateForFrequency = true;
        }
        public LoopPoints loopPoints = new LoopPoints();

        private int loadedLoopStart;
        private int loadedLoopLength;

        private bool attemptedLoopStartRead;
        private bool attemptedLoopLengthRead;

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
                    if (!attemptedLoopStartRead)
                    {
                        string loopstartTagValue = ReadTrackMetadata(EnsoConstants.LoopStartTag);
                        attemptedLoopStartRead = true;

                        if (string.IsNullOrEmpty(loopstartTagValue))
                        {
                            loadedLoopStart = 0;
                        }
                        else
                        {
                            loadedLoopStart = int.Parse(loopstartTagValue);
                        }
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
                    if (!attemptedLoopLengthRead)
                    {
                        string looplengthTagValue = ReadTrackMetadata(EnsoConstants.LoopLengthTag);
                        attemptedLoopLengthRead = true;

                        if (string.IsNullOrEmpty(looplengthTagValue))
                        {
                            loadedLoopLength = Track.samples - Math.Min(Track.samples, LoopStart);
                        }
                        else
                        {
                            loadedLoopLength = int.Parse(looplengthTagValue);
                        }
                    }

                    return loadedLoopLength;
                }
            }

            set
            {
                loopPoints.sampleLoopLength = value;
            }
        }

        public bool CompensateForFrequency
        {
            get
            {
                return loopPoints.compensateForFrequency;
            }
            set
            {
                loopPoints.compensateForFrequency = value;
            }
        }

        /// <summary>
        /// The new frequency of the track. This may be different than the original frequency.
        /// </summary>
        public virtual int Frequency
        {
            get
            {
                if (Track != null)
                {
                    return Track.frequency;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// The ratio between the new, Unity-assigned frequency and the track's original frequency.
        /// </summary>
        public float FrequencyRatio
        {
            get
            {
                // Sometimes, if the original sound file has a frequency Unity doesn't like,
                // it'll change it to one it does like. This invalidates the original loops points,
                // so we want to disguise that little detail from the designer if possible.
                int originalFrequency = OriginalFrequency;
                float frequencyRatio = 1f;

                if (originalFrequency > 0)
                {
                    frequencyRatio = (float)Frequency / originalFrequency;
                }

                return frequencyRatio;
            }
        }

        /// <summary>
        /// The number of audio channels the track has.
        /// </summary>
        public virtual int Channels
        {
            get
            {
                if (Track != null)
                {
                    return Track.channels;
                }
                else
                {
                    return 0;
                }
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

        private Track trackAsset;
        public Track TrackAsset
        {
            get
            {
                if (trackAsset == null)
                {
                    string trackPath = AssetDatabase.GetAssetPath(Track);

                    if (string.IsNullOrEmpty(trackPath)) return null;

                    trackAsset = new Track(trackPath);
                }

                return trackAsset;
            }
        }

        public virtual int OriginalFrequency
        {
            get
            {
                if (TrackAsset == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(TrackAsset.SampleRate);
                }
            }
        }

        /// <summary>
        /// Splits the audio track into two separate tracks (the intro and the loop) and caches them.
        /// This MUST be called before the overall track can be played.
        /// </summary>
        public void CreateAndCacheClips()
        {
            try
            {
                // Sometimes the designer might want to adjust the loop points themselves. That's fine, too.
                // If they uncheck the Compensate for Frequency box, we'll just do everything as normal.
                float frequencyRatio = CompensateForFrequency ? FrequencyRatio : 1f;

                float[] clipData = new float[Track.samples * Channels];
                int loopStartSampleCount = Convert.ToInt32(Math.Max(0, LoopStart) * Channels * frequencyRatio);
                int loopLengthSampleCount = Convert.ToInt32(
                    Math.Min(Track.samples - 1, Math.Max(1, LoopLength)) * frequencyRatio
                    );
                int introLength = Convert.ToInt32(Math.Max(1, LoopStart) * frequencyRatio);

                Track.GetData(clipData, 0);

                IntroClip = AudioClip.Create(
                    Name + " intro",
                    introLength,
                    Channels,
                    Frequency,
                    false);

                IntroClip.SetData(EnsoHelpers.Slice(clipData, 0, introLength * Channels), 0);

                LoopClip = AudioClip.Create(
                    Name + " loop",
                    loopLengthSampleCount,
                    Channels,
                    Frequency,
                    false);

                int maxSafeLoopLength = clipData.Length - loopStartSampleCount;
                LoopClip.SetData(EnsoHelpers.Slice(clipData, loopStartSampleCount,
                    Math.Min(loopLengthSampleCount * Channels, maxSafeLoopLength)), 0);
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
            return Mathf.Min(LengthInSeconds, (float)samples / Frequency);
        }

        /// <summary>
        /// Converts the inputted time to its equivalent time in samples.
        /// </summary>
        /// <param name="time">The time in seconds</param>
        /// <returns>The time in samples</returns>
        public int SecondsToSamples(float time)
        {
            return Math.Min(LengthInSamples, Convert.ToInt32(time * Frequency));
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
            if (TrackAsset == null)
            {
                return string.Empty;
            }

            try
            {
                return TrackAsset.AdditionalFields[name];
            }
            catch
            {
                Debug.LogWarning(string.Format(@"Field ""{0}"" does not exist for track ""{1}"".", name, Track.name));
                return string.Empty;
            }
        }
    }
}