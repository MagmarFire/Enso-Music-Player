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

        public void SetTrack(MusicTrack musicTrack)
        {
            PlayingTrack = musicTrack;
        }

        public void Play(MusicTrack track)
        {
            PlayAtPoint(track, 0f);
        }

        public void PlayAtPoint(MusicTrack track, float time)
        {
            pausePosition = 0;
            Stop();
            SetTrack(track);

            InitializeVolume();

            IntroSource.clip = PlayingTrack.IntroClip;
            LoopSource.clip = PlayingTrack.LoopClip;

            PlayAtPosition(PlayingTrack.TimeToSamples(time));
        }

        private void PlayAtPosition(int samplePosition)
        {
            if (samplePosition <= IntroSource.clip.samples)
            {
                IntroSource.timeSamples = samplePosition;
                LoopSource.timeSamples = 0;
                IntroSource.Play();
                LoopSource.PlayDelayed((float)(IntroSource.clip.samples - samplePosition) / IntroSource.clip.frequency);
            }
            else
            {
                LoopSource.timeSamples = samplePosition - IntroSource.clip.samples;
                LoopSource.Play();
            }
        }

        internal void Stop()
        {
            IntroSource.Stop();
            LoopSource.Stop();
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

        internal void UnPause()
        {
            if (IsPaused)
            {
                PlayAtPosition(pausePosition);
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