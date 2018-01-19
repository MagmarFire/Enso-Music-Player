using UnityEngine;

namespace EnsoMusicPlayer
{
    public class Speaker : MonoBehaviour
    {

        public AudioSource Primary { get; private set; }
        public AudioSource Secondary { get; private set; }

        public MusicTrack PlayingTrack { get; private set; }

        public bool IsPlaying
        {
            get
            {
                return Primary && Primary.isPlaying || Secondary && Secondary.isPlaying;
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

        private float CrossFadeTimeLeft;
        private float MaxCrossFadeTime;

        private bool StopAfterFade { get; set; }

        private int pausePosition;
        public bool IsPaused
        {
            get
            {
                return pausePosition > 0;
            }
        }

        // Use this for initialization
        void Awake()
        {
            Primary = gameObject.AddComponent<AudioSource>();
            Secondary = gameObject.AddComponent<AudioSource>();

            Secondary.loop = true;

            InitializeVolume();
        }

        // Update is called once per frame
        void Update()
        {
            if (VolumeStatus != VolumeStatuses.Static)
            {
                if (CrossFadeTimeLeft > 0)
                {
                    CrossFadeTimeLeft -= Time.deltaTime;
                }
            }

            switch (VolumeStatus)
            {
                case VolumeStatuses.FadingIn:
                    if (CrossFadeTimeLeft <= 0)
                    {
                        SetVolume(PlayerVolume);
                        VolumeStatus = VolumeStatuses.Static;
                    }
                    else
                    {
                        float t = CrossFadeTimeLeft / MaxCrossFadeTime;
                        SetVolume(PlayerVolume * EnsoHelpers.CalculateEqualPowerCrossfade(t, true));
                    }
                    break;

                case VolumeStatuses.FadingOut:
                    if (CrossFadeTimeLeft <= 0)
                    {
                        SetVolume(0);
                        VolumeStatus = VolumeStatuses.Static;

                        if (StopAfterFade)
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        float t = CrossFadeTimeLeft / MaxCrossFadeTime;
                        SetVolume(PlayerVolume * EnsoHelpers.CalculateEqualPowerCrossfade(t, false));
                    }
                    break;
            }
        }

        internal void SetTrack(MusicTrack musicTrack)
        {
            PlayingTrack = musicTrack;
        }

        internal void Play(MusicTrack playingTrack)
        {
            pausePosition = 0;
            Stop();
            SetTrack(playingTrack);

            InitializeVolume();

            Primary.clip = PlayingTrack.IntroClip;
            Secondary.clip = PlayingTrack.LoopClip;

            PlayAtPosition(0);
        }

        private void PlayAtPosition(int samplePosition)
        {
            if (samplePosition <= Primary.clip.samples)
            {
                Primary.timeSamples = samplePosition;
                Secondary.timeSamples = 0;
                Primary.Play();
                Secondary.PlayDelayed((float)(Primary.clip.samples - samplePosition) / Primary.clip.frequency);
            }
            else
            {
                Secondary.timeSamples = samplePosition - Primary.clip.samples;
                Secondary.Play();
            }
        }

        internal void Stop()
        {
            Primary.Stop();
            Secondary.Stop();
        }

        private int GetPositionInSamples()
        {
            if (PlayingTrack != null)
            {
                if (Primary.isPlaying)
                {
                    return Primary.timeSamples;
                }
                else
                {
                    return Primary.clip.samples + Secondary.timeSamples;
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
            Primary.volume = volume;
            Secondary.volume = volume;
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

        internal void FadeOut(float crossFadeTime, bool stopAfterFade = false)
        {
            MaxCrossFadeTime = crossFadeTime;
            CrossFadeTimeLeft = crossFadeTime;
            VolumeStatus = VolumeStatuses.FadingOut;
            StopAfterFade = stopAfterFade;
        }

        internal void FadeIn(float crossFadeTime)
        {
            MaxCrossFadeTime = crossFadeTime;
            CrossFadeTimeLeft = crossFadeTime;
            VolumeStatus = VolumeStatuses.FadingIn;
        }
    }
}