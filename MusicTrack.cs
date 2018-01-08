using System;
using UnityEngine;

namespace EnsoMusicPlayer
{
    [Serializable]
    public class MusicTrack
    {
        public string Name;
        public AudioClip Track;
        public int sampleLoopStart;
        public int sampleLoopLength;
    }
}