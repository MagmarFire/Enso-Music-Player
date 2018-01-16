using System;
using UnityEngine;

namespace EnsoMusicPlayer
{
    /// <summary>
    /// This class handles all the math involved with volleying between different speakers while looping a song.
    /// Other than that, it serves as a wrapper for an audio source's methods. Fancy things that audio sources
    /// don't already support, like fading, should NOT be implemented here. Implement that in the module.
    /// </summary>
    public class Speaker : MonoBehaviour
    {

        public Speaker NextSpeaker;
        public SpeakerModule Module;
        public bool IsPrimary;

        AudioSource source;

        MusicTrack PlayingTrack
        {
            get
            {
                return Module.PlayingTrack;
            }
        }

        // The time relative to the music track when the beginning of the loop starts.
        // For instance, if the loop starts 5 seconds into the song, this field will return 5.
        double RelativeLoopStart
        {
            get
            {
                return (double)PlayingTrack.LoopStart / PlayingTrack.Track.frequency;
            }
        }

        // The length of the loop until the song needs to go back to the beginning loop point.
        // For instance, if the loop starts 5 seconds into the song and must loop back 20 seconds
        // after the song starts, this field will return 15.
        double LoopLength
        {
            get
            {
                return (double)PlayingTrack.LoopLength / PlayingTrack.Track.frequency;
            }
        }

        // The absolute time of the start point of the loop. For instance, if the loop start point is 5 seconds,
        // and this field was accessed after 20 seconds, it will return 25.
        double AbsoluteLoopStart
        {
            get
            {
                return AudioSettings.dspTime + RelativeLoopStart;
            }
        }

        // The absolute time of the end point of the loop. For instance, if the loop start point is 5 seconds,
        // the length of the loop is 20 seconds, and this field was accessed after 20 seconds, it will return 45.
        double AbsoluteLoopEnd
        {
            get
            {
                return AbsoluteLoopStart + LoopLength;
            }
        }

        // Use this for initialization
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Module.IsPlaying && !NextSpeaker.source.isPlaying)
            {
                // The next speaker ended its playback; we'll need to initialize it for when this speaker ends.

                double loopEnd = AbsoluteLoopEnd;

                NextSpeaker.source.timeSamples = PlayingTrack.LoopStart;
                NextSpeaker.source.PlayScheduled(loopEnd - source.time);
                NextSpeaker.source.SetScheduledEndTime(loopEnd + LoopLength - source.time);
            }
        }

        public void Play()
        {
            Stop();

            source.clip = PlayingTrack.Track;
            NextSpeaker.source.clip = PlayingTrack.Track;

            source.PlayScheduled(AudioSettings.dspTime);
            source.SetScheduledEndTime(AbsoluteLoopEnd);
        }

        internal void Pause()
        {
            source.Pause();
            NextSpeaker.source.Pause();
        }

        internal void UnPause()
        {
            source.UnPause();
            NextSpeaker.source.UnPause();
        }

        internal void Stop()
        {
            source.Stop();
            source.time = 0;
            NextSpeaker.source.Stop();
            NextSpeaker.source.time = 0;
        }

        internal void SetVolume(float volume)
        {
            source.volume = volume;
        }

        internal float GetVolume()
        {
            return source.volume;
        }
    }
}