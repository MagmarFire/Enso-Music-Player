using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnsoMusicPlayer
{
    [AddComponentMenu("Audio/Ensō Music Player")]
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float Volume = 1f; // Should be considered readonly outside the player's scope.
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
        /// The event handler that is called when FadeOut completes.
        /// </summary>
        public event MusicPlayerEventHandler FadeOutComplete;

        /// <summary>
        /// The event handler that is called when FadeIn completes.
        /// </summary>
        public event MusicPlayerEventHandler FadeInComplete;

        /// <summary>
        /// The time of the current track.
        /// </summary>
        public float CurrentTime
        {
            get
            {
                return CurrentSpeaker.CurrentTime;
            }
        }

        /// <summary>
        /// The length of the current track.
        /// </summary>
        public float CurrentLength
        {
            get
            {
                return CurrentSpeaker.CurrentLength;
            }
        }

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
                CurrentSpeaker.SetPosition(PlayingTrack.TimeToSamples(time));
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
        /// <param name="time">The time to play the track at, in seconds</param>
        public void PlayAtPoint(MusicTrack track, float time)
        {
            CurrentSpeaker.SetVolume(Volume);
            CurrentSpeaker.PlayAtPoint(track, time);
        }

        /// <summary>
        /// Fades in the track with the given name starting at the given point on its timeline.
        /// </summary>
        /// <param name="name">The name of the track</param>
        /// <param name="time">The time to fade the track in at, in seconds</param>
        public void FadeInAtPoint(string name, float time)
        {
            FadeInAtPoint(GetTrackByName(name), time);
        }

        /// <summary>
        /// Fades in the given track starting at the given point on its timeline.
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="time">The playback time for the next track, in seconds</param>
        public void FadeInAtPoint(MusicTrack track, float time)
        {
            PlayingTrack = track;
            Scrub(time);
            CurrentSpeaker.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Crossfades to the track with the given name at the given point on its timeline.
        /// </summary>
        /// <param name="name">The name of the track</param>
        /// <param name="time">The playback time for the next track, in seconds</param>
        public void CrossFadeAtPoint(string name, float time)
        {
            CrossFadeAtPoint(GetTrackByName(name), time);
        }

        /// <summary>
        /// Crossfades to the given track at the given point on its timeline.
        /// </summary>
        /// <param name="track">The track to crossfade to</param>
        /// <param name="time">The playback time for the next track, in seconds</param>
        public void CrossFadeAtPoint(MusicTrack track, float time)
        {
            CrossFadeTo(track);
            Scrub(time);
            CurrentSpeaker.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Crossfades to a track.
        /// </summary>
        /// <param name="name">The name of the track to play</param>
        public void CrossFadeTo(string name)
        {
            CrossFadeTo(GetTrackByName(name));
        }

        /// <summary>
        /// Crossfades to a track.
        /// </summary>
        /// <param name="track">The track to play</param>
        public void CrossFadeTo(MusicTrack track)
        {
            CurrentSpeaker.FadeOut(CrossFadeTime, true);
            SwitchSpeakers();
            PlayingTrack = track;
            CurrentSpeaker.Play(PlayingTrack);
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
        [Obsolete("Unpause is deprecated. Simply use Play instead.")]
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

        /// <summary>
        /// Gets a track by its playlist name.
        /// </summary>
        /// <param name="name">The name of the track</param>
        /// <returns>The track with the given playlist name</returns>
        public MusicTrack GetTrackByName(string name)
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

        #endregion

        /// <summary>
        /// Called by a speaker that completes a fadeout.
        /// </summary>
        public void OnFadeOutComplete()
        {
            OnFadeOutComplete(new MusicPlayerEventArgs());
        }

        public void OnFadeInComplete()
        {
            OnFadeInComplete(new MusicPlayerEventArgs());
        }

        /// <summary>
        /// Called when a speaker completes a fadeout.
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void OnFadeOutComplete(MusicPlayerEventArgs e)
        {
            if (FadeOutComplete != null)
            {
                FadeOutComplete(e);
            }
        }

        /// <summary>
        /// Called when a speaker completes a fade in.
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void OnFadeInComplete(MusicPlayerEventArgs e)
        {
            if (FadeInComplete != null)
            {
                FadeInComplete(e);
            }
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