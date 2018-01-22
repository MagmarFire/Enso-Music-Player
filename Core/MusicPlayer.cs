﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnsoMusicPlayer
{
    [AddComponentMenu("Audio/Ensō Music Player")]
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float Volume = 1f;
        // Remove this as soon as Unity supports C# 6; then we can simply use an auto-field initializer.
        private float PreviousVolume;
        public float CrossFadeTime = 2f;

        [Header("TrackSettings")]
        public List<MusicTrack> Tracks;

        public MusicTrack PlayingTrack { get; private set; }

        private Speaker PrimarySpeaker;
        private Speaker SecondarySpeaker;
        private Speaker CurrentSpeaker;

        // Use this for initialization
        void Awake()
        {
            PrimarySpeaker = gameObject.AddComponent<Speaker>();
            SecondarySpeaker = gameObject.AddComponent<Speaker>();
            PrimarySpeaker.Player = this;
            SecondarySpeaker.Player = this;
            CurrentSpeaker = PrimarySpeaker;

            PrimarySpeaker.SetPlayerVolume(Volume);
            SecondarySpeaker.SetPlayerVolume(Volume);

            // Cache all the clips before we play them for maximum performance when starting playback.
            if (Tracks != null)
            {
                foreach (MusicTrack track in Tracks)
                {
                    track.CreateAndCacheClips();
                }
            }

            RefreshSpeakerVolume(); // Initialize both modules' volume.
        }

        // Update is called once per frame
        void Update()
        {
            // We need this because this is C# 4, not 6...
            if (Volume != PreviousVolume)
            {
                RefreshSpeakerVolume();
                PreviousVolume = Volume;
            }
        }

        #region PublicAPI
        /// <summary>
        /// Play a music track.
        /// </summary>
        /// <param name="name">The name of the track</param>
        public void Play(string name)
        {
            Play(GetTrackByName(name));
        }

        /// <summary>
        /// Play a music track.
        /// </summary>
        /// <param name="track">The track to play</param>
        public void Play(MusicTrack track)
        {
            PlayingTrack = track;
            PlayAtPoint(track, 0f);
        }

        /// <summary>
        /// Scrubs the currently-playing track to a specific point in its timeline.
        /// </summary>
        /// <param name="time">How far along to scrub the track, in seconds</param>
        public void Scrub(float time)
        {
            if (PlayingTrack != null)
            {
                time = Mathf.Min(time, PlayingTrack.LengthInSeconds);
                PlayAtPoint(PlayingTrack, time);
            }
        }

        /// <summary>
        /// Scrubs the currently-playing track to a specific point in its timeline.
        /// </summary>
        /// <param name="percentage">How far along to scrub the track, as a percentage of the track's length</param>
        public void ScrubAsPercentage(float percentage)
        {
            if (PlayingTrack != null)
            {
                Scrub(percentage * PlayingTrack.LengthInSeconds);
            }
        }

        /// <summary>
        /// Plays the track starting at the given point on its timeline.
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="time">The time to play the song at, in seconds</param>
        public void PlayAtPoint(MusicTrack track, float time)
        {
            CurrentSpeaker.SetVolume(Volume);
            CurrentSpeaker.PlayAtPoint(track, time);
        }

        /// <summary>
        /// Crossfades to a music track.
        /// </summary>
        /// <param name="name">The name of the track</param>
        public void CrossFadeTo(string name)
        {
            CurrentSpeaker.FadeOut(CrossFadeTime, true);
            SwitchSpeakers();
            CurrentSpeaker.Play(GetTrackByName(name));
            CurrentSpeaker.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Pauses the current track.
        /// </summary>
        public void Pause()
        {
            CurrentSpeaker.Pause();
        }

        /// <summary>
        /// Unpauses the current track.
        /// </summary>
        public void Unpause()
        {
            CurrentSpeaker.UnPause();
        }

        /// <summary>
        /// Fades the currently-playing track in.
        /// </summary>
        public void FadeIn()
        {
            CurrentSpeaker.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Fades the currently-playing track out.
        /// </summary>
        public void FadeOut()
        {
            CurrentSpeaker.FadeOut(CrossFadeTime);
        }

        /// <summary>
        /// Sets the volume of the music player. Will do nothing if the player is in the middle of fading.
        /// </summary>
        /// <param name="volume">The volume level, from 0.0 to 1.0.</param>
        public void SetVolume(float volume)
        {
            if (!PrimarySpeaker.IsFading && !SecondarySpeaker.IsFading)
            {
                Volume = volume;
                PrimarySpeaker.SetPlayerVolume(volume);
                SecondarySpeaker.SetPlayerVolume(volume);
            }
        }

        /// <summary>
        /// Stops the player.
        /// </summary>
        public void Stop()
        {
            PrimarySpeaker.Stop();
            SecondarySpeaker.Stop();
            PlayingTrack = null;
        }

        #endregion

        private MusicTrack GetTrackByName(string name)
        {
            MusicTrack track
                = (from t in Tracks
                   where t.Name == name
                   select t).FirstOrDefault();

            if (track == null)
            {
                throw new KeyNotFoundException(string.Format(@"A song with the name ""{0}"" could not be found.", name));
            }

            return track;
        }

        /// <summary>
        /// Switches the primary speaker to the secondary one and vice versa.. Useful while crossfading.
        /// </summary>
        private void SwitchSpeakers()
        {
            if (CurrentSpeaker == PrimarySpeaker)
            {
                CurrentSpeaker = SecondarySpeaker;
            }
            else
            {
                CurrentSpeaker = PrimarySpeaker;
            }
        }

        /// <summary>
        /// Updates the modules' volume to match the music player's.
        /// </summary>
        private void RefreshSpeakerVolume()
        {
            PrimarySpeaker.SetVolume(Volume);
            SecondarySpeaker.SetVolume(Volume);
        }
    }
}