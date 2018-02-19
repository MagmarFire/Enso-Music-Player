using System;
using UnityEngine;

namespace EnsoMusicPlayer
{
    public class Speaker : MonoBehaviour
    {

        public AudioSource IntroSource { get; private set; }
        public AudioSource LoopSource { get; private set; }

        public MusicTrack PlayingTrack { get; private set; }

        public bool IsPlaying
        {
            get
            {
                return IntroSource.isPlaying || LoopSource.isPlaying;
            }
        }

        public bool IsFading
        {
            get
            {
                return VolumeStatus != VolumeStatuses.Static;
            }
        }

        public MusicPlayer Player;
        // Holds the volume of the music player. This is held separately instead of referenced directly
        // in order to decouple the module from the player.
        public float PlayerVolume { get; private set; }

        public enum VolumeStatuses { FadingIn, FadingOut, Static }
        public VolumeStatuses VolumeStatus { get; private set; }

        private float FadeTimeLeft;
        private float MaxFadeTime;

        private bool StopAfterFade { get; set; }

        private int pausePosition;
        private int numberOfLoopsLeft;
        public bool IsPaused
        {
            get
            {
                return pausePosition > 0;
            }
        }

        public float CurrentTime
        {
            get
            {
                if (PlayingTrack != null)
                {
                    if (IntroSource.isPlaying)
                    {
                        return IntroSource.time;
                    }
                    else if (LoopSource.isPlaying)
                    {
                        return IntroSource.clip.length + LoopSource.time;
                    }
                }
                return 0f;
            }
        }

        public float CurrentLength
        {
            get
            {
                if (PlayingTrack != null)
                {
                    return PlayingTrack.LengthInSeconds;
                }
                return 0f;
            }
        }

        public int CurrentLengthInSamples
        {
            get
            {
                if (PlayingTrack != null)
                {
                    return PlayingTrack.LengthInSamples;
                }
                return 0;
            }
        }

        // Use this for initialization
        void Awake()
        {
            IntroSource = gameObject.AddComponent<AudioSource>();
            LoopSource = gameObject.AddComponent<AudioSource>();

            LoopSource.loop = true;

            InitializeVolume();
        }

        // Update is called once per frame
        void Update()
        {
            if (VolumeStatus != VolumeStatuses.Static)
            {
                if (FadeTimeLeft > 0)
                {
                    FadeTimeLeft -= Time.deltaTime;
                }
            }

            switch (VolumeStatus)
            {
                case VolumeStatuses.FadingIn:
                    if (FadeTimeLeft <= 0)
                    {
                        OnFadeInComplete();
                    }
                    else
                    {
                        float t = FadeTimeLeft / MaxFadeTime;
                        SetVolume(PlayerVolume * EnsoHelpers.CalculateEqualPowerCrossfade(t, true));
                    }
                    break;

                case VolumeStatuses.FadingOut:
                    if (FadeTimeLeft <= 0)
                    {
                        OnFadeOutComplete();
                    }
                    else
                    {
                        float t = FadeTimeLeft / MaxFadeTime;
                        SetVolume(PlayerVolume * EnsoHelpers.CalculateEqualPowerCrossfade(t, false));
                    }
                    break;
            }
        }

        #region Event Callback
        private void OnFadeInComplete()
        {
            SetVolume(PlayerVolume);
            VolumeStatus = VolumeStatuses.Static;
            Player.OnFadeInComplete();
        }

        private void OnFadeOutComplete()
        {
            SetVolume(0);
            VolumeStatus = VolumeStatuses.Static;

            if (StopAfterFade)
            {
                Stop();
            }

            Player.OnFadeOutComplete();
        }

        private void OnTrackEndOrLoop()
        {
            bool trackEnded = numberOfLoopsLeft == 1;
            if (numberOfLoopsLeft > 0)
            {
                numberOfLoopsLeft--;
            }

            if (trackEnded)
            {
                Stop();
                Player.OnTrackEnd();
            }
            else
            {
                Player.OnTrackLoop();
            }

            Player.OnTrackEndOrLoop();
        }
        #endregion

        public void SetTrack(MusicTrack musicTrack)
        {
            PlayingTrack = musicTrack;
        }

        /// <summary>
        /// Plays the given track.
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="numberOfLoops">The number of times to loop the track. Set to 0 for endless play.</param>
        public void Play(MusicTrack track, int numberOfLoops)
        {
            SetTrack(track);
            PlayAtPoint(track, track.SamplesToSeconds(pausePosition), numberOfLoops);
            pausePosition = 0;
        }

        /// <summary>
        /// Plays the current track starting at the given play position.
        /// </summary>
        /// <param name="time">The time to play the track at, in seconds</param>
        /// <param name="numberOfLoops">The number of times to loop the track. Set to 0 for endless play.</param>
        public void PlayAtPoint(float time, int numberOfLoops)
        {
            if (PlayingTrack != null)
            {
                PlayAtPoint(PlayingTrack, time, numberOfLoops);
            }
        }

        /// <summary>
        /// Plays the given track starting at the given play position.
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="time">The time to play the track at, in seconds</param>
        /// <param name="numberOfLoops">The number of times to loop the track. Set to 0 for endless play.</param>
        public void PlayAtPoint(MusicTrack track, float time, int numberOfLoops)
        {
            pausePosition = 0;
            Stop();
            SetTrack(track);

            InitializeVolume();

            IntroSource.clip = PlayingTrack.IntroClip;
            LoopSource.clip = PlayingTrack.LoopClip;

            PlayAtPosition(PlayingTrack.SecondsToSamples(time), numberOfLoops);
        }

        /// <summary>
        /// Plays the current track starting at the given play position.
        /// </summary>
        /// <param name="samplePosition">The play position, in samples</param>
        /// <param name="numberOfLoops">The number of times to loop the track. Set to 0 for endless play.</param>
        private void PlayAtPosition(int samplePosition, int numberOfLoops)
        {
            if (samplePosition <= IntroSource.clip.samples)
            {
                IntroSource.timeSamples = samplePosition;
                LoopSource.timeSamples = 0;
                IntroSource.Play();
                LoopSource.PlayDelayed(PlayingTrack.SamplesToSeconds(IntroSource.clip.samples - samplePosition));
            }
            else
            {
                LoopSource.timeSamples = samplePosition - IntroSource.clip.samples;
                LoopSource.Play();
            }

            numberOfLoopsLeft = numberOfLoops;
            InvokeRepeating("OnTrackEndOrLoop",
                PlayingTrack.SamplesToSeconds(PlayingTrack.LengthInSamples - samplePosition),
                PlayingTrack.SamplesToSeconds(PlayingTrack.LoopLength));
        }

        internal void Stop()
        {
            if (!IsPaused)
            {
                pausePosition = 0;
            }
            
            IntroSource.Stop();
            LoopSource.Stop();

            CancelInvoke();
        }

        private int GetPositionInSamples()
        {
            if (PlayingTrack != null)
            {
                if (IntroSource.isPlaying)
                {
                    return IntroSource.timeSamples;
                }
                else
                {
                    return IntroSource.clip.samples + LoopSource.timeSamples;
                }
            }
            else
            {
                return 0;
            }
        }

        internal void Pause()
        {
            if (!IsPaused)
            {
                pausePosition = GetPositionInSamples();
                Stop();
            }
        }

        internal void SetPosition(MusicTrack track, int position)
        {
            SetTrack(track);
            if (IsPaused)
            {
                pausePosition = Math.Min(position, CurrentLengthInSamples);
            }
            else
            {
                PlayAtPoint(PlayingTrack.SamplesToSeconds(position), numberOfLoopsLeft);
            }
        }

        internal void UnPause()
        {
            if (IsPaused)
            {
                PlayAtPosition(pausePosition, numberOfLoopsLeft);
                pausePosition = 0;
            }
        }

        internal void SetVolume(float volume)
        {
            IntroSource.volume = volume;
            LoopSource.volume = volume;
        }

        internal void SetPlayerVolume(float playerVolume)
        {
            PlayerVolume = playerVolume;
        }

        private void InitializeVolume()
        {
            VolumeStatus = VolumeStatuses.Static;
            SetVolume(PlayerVolume);
        }

        internal void FadeOut(float fadeTime, bool stopAfterFade = false)
        {
            MaxFadeTime = fadeTime;
            FadeTimeLeft = fadeTime;
            VolumeStatus = VolumeStatuses.FadingOut;
            StopAfterFade = stopAfterFade;
        }

        internal void FadeIn(float fadeTime)
        {
            MaxFadeTime = fadeTime;
            FadeTimeLeft = fadeTime;
            VolumeStatus = VolumeStatuses.FadingIn;
        }
    }
}