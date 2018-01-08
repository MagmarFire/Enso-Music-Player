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
        public float Volume = 1f;
        // Remove this as soon as Unity supports C# 6; then we can simply use an auto-field initializer.
        private float PreviousVolume;
        public float CrossFadeTime = 2f;

        [Header("TrackSettings")]
        public List<MusicTrack> Tracks;

        private MusicTrack PlayingTrack;

        private SpeakerModule PrimaryModule;
        private SpeakerModule SecondaryModule;
        private SpeakerModule CurrentModule;

        // Use this for initialization
        void Start()
        {

            PrimaryModule = gameObject.AddComponent<SpeakerModule>();
            SecondaryModule = gameObject.AddComponent<SpeakerModule>();
            PrimaryModule.Player = this;
            SecondaryModule.Player = this;
            CurrentModule = PrimaryModule;

            RefreshModuleVolume(); // Initialize both modules' volume.
        }

        // Update is called once per frame
        void Update()
        {
            // We need this because this is C# 4, not 6...
            if (Volume != PreviousVolume)
            {
                RefreshModuleVolume();
                PreviousVolume = Volume;
            }
        }

        /// <summary>
        /// Play a music track.
        /// </summary>
        /// <param name="name">The name of the track</param>
        public void PlayTrack(string name)
        {
            PlayingTrack = GetTrackByName(name);

            CurrentModule.Play(PlayingTrack);
        }

        /// <summary>
        /// Crossfades to a music track.
        /// </summary>
        /// <param name="name">The name of the track</param>
        public void CrossFadeToTrack(string name)
        {
            CurrentModule.FadeOut(CrossFadeTime, true);
            SwitchModules();
            CurrentModule.Play(GetTrackByName(name));
            CurrentModule.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Pauses the current track.
        /// </summary>
        public void PauseTrack()
        {
            CurrentModule.Pause();
        }

        /// <summary>
        /// Unpauses the current track.
        /// </summary>
        public void UnPauseTrack()
        {
            CurrentModule.UnPause();
        }

        /// <summary>
        /// Fades the currently-playing track in.
        /// </summary>
        public void FadeInTrack()
        {
            CurrentModule.FadeIn(CrossFadeTime);
        }

        /// <summary>
        /// Fades the currently-playing track out.
        /// </summary>
        public void FadeOutTrack()
        {
            CurrentModule.FadeOut(CrossFadeTime);
        }

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
        /// Switches the active module. Useful while crossfading.
        /// </summary>
        private void SwitchModules()
        {
            if (CurrentModule == PrimaryModule)
            {
                CurrentModule = SecondaryModule;
            }
            else
            {
                CurrentModule = PrimaryModule;
            }
        }

        /// <summary>
        /// Sets the volume of the music player. Will do nothing if the player is in the middle of fading.
        /// </summary>
        /// <param name="volume">The volume level, from 0.0 to 1.0.</param>
        public void SetVolume(float volume)
        {
            if (!PrimaryModule.IsFading && !SecondaryModule.IsFading)
            {
                Volume = volume;
            }
        }

        /// <summary>
        /// Updates the modules' volume to match the music player's.
        /// </summary>
        private void RefreshModuleVolume()
        {
            PrimaryModule.SetVolume(Volume);
            SecondaryModule.SetVolume(Volume);
        }
    }
}