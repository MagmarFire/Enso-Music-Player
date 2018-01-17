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