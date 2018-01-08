using System;
using UnityEngine;

namespace EnsoMusicPlayer
{
    public class SpeakerModule : MonoBehaviour
    {

        private Speaker Primary;
        private Speaker Secondary;

        public MusicTrack PlayingTrack { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool IsFading
        {
            get
            {
                return VolumeStatus != VolumeStatuses.Static;
            }
        }

        public MusicPlayer Player;

        private enum VolumeStatuses { FadingIn, FadingOut, Static }
        private VolumeStatuses VolumeStatus = VolumeStatuses.Static;

        private float CrossFadeTimeLeft;
        private float MaxCrossFadeTime;

        private bool StopAfterFade { get; set; }

        // Use this for initialization
        void Awake()
        {
            Primary = gameObject.AddComponent<Speaker>();
            Secondary = gameObject.AddComponent<Speaker>();

            Primary.Module = this;
            Primary.NextSpeaker = Secondary;
            Primary.IsPrimary = true;

            Secondary.Module = this;
            Secondary.NextSpeaker = Primary;
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
                        SetVolume(Player.Volume);
                        VolumeStatus = VolumeStatuses.Static;
                    }
                    else
                    {
                        SetVolume(Player.Volume * (1.0f - CrossFadeTimeLeft / MaxCrossFadeTime));
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
                        SetVolume(Player.Volume * CrossFadeTimeLeft / MaxCrossFadeTime);
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
            IsPlaying = true;
            SetTrack(playingTrack);
            Primary.Play();
        }

        internal void Stop()
        {
            IsPlaying = false;
            Primary.Stop();
            Secondary.Stop();
        }

        internal void Pause()
        {
            IsPlaying = false;
            Primary.Pause();
            Secondary.Pause();
        }

        internal void UnPause()
        {
            IsPlaying = true;
            Primary.UnPause();
            Secondary.UnPause();
        }

        internal void SetVolume(float volume)
        {
            Primary.SetVolume(volume);
            Secondary.SetVolume(volume);
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