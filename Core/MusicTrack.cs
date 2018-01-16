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
        public virtual int sampleLoopStart { get; set; }
        public virtual int sampleLoopLength { get; set; }

        private int loadedLoopStart;
        private int loadedLoopLength;

        public int LoopStart
        {
            get
            {
                if (sampleLoopStart > 0)
                {
                    return sampleLoopStart;
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
        }

        public int LoopLength
        {
            get
            {
                if (sampleLoopLength > 0)
                {
                    return sampleLoopLength;
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