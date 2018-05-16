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

        public float Volume
        {
            get
            {
                // This should be the same as LoopSource.volume, so it doesn't matter which one we use
                return IntroSource.volume;
            }
        }

        public MusicPlayer Player;

        public enum VolumeStatuses { FadingIn, FadingOut, Static }
        public VolumeStatuses VolumeStatus { get; private set; }

        private float FadeTimeLeft;
        private float MaxFadeTime;
        private float DestFadeVolume;
        private float OrigFadeVolume;

        private bool StopAfterFade { get; set; }

        private int pausePosition;
        private int numberOfLoopsLeft;
        public bool IsPaused { get; private set; }

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
        }

        public void SetPlayerAndInitializeVolume(MusicPlayer player)
        {
            Player = player;
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
                        SetSpeakerVolume(OrigFadeVolume + (DestFadeVolume - OrigFadeVolume) * EnsoHelpers.CalculateEqualPowerCrossfade(t, true));
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
                        SetSpeakerVolume(OrigFadeVolume * EnsoHelpers.CalculateEqualPowerCrossfade(t, false));
                    }
                    break;
            }
        }

        #region Event Callback
        private void OnFadeInComplete()
        {
            VolumeStatus = VolumeStatuses.Static;
            SetSpeakerVolume(DestFadeVolume);
            Player.OnFadeInComplete();
        }

        private void OnFadeOutComplete()
        {
            VolumeStatus = VolumeStatuses.Static;
            SetSpeakerVolume(DestFadeVolume);

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
            RemovePauseState();
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
            RemovePauseState();
            Stop();
            SetTrack(track);

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
            if (PlayingTrack != null)
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
        }

        internal void Stop()
        {
            if (!IsPaused)
            {
                RemovePauseState();
            }
            
            IntroSource.Stop();
            LoopSource.Stop();

            CancelInvoke();
        }

        private int GetPositionInSamples()
        {
            if (PlayingTrack != null && IsPlaying)
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
                IsPaused = true;
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
            else if (IsPlaying)
            {
                PlayAtPoint(PlayingTrack.SamplesToSeconds(position), numberOfLoopsLeft);
            }
        }

        internal void UnPause()
        {
            if (IsPaused)
            {
                PlayAtPosition(pausePosition, numberOfLoopsLeft);
                RemovePauseState();
            }
        }

        internal void SetSpeakerVolume(float volume)
        {
            IntroSource.volume = volume;
            LoopSource.volume = volume;
        }

        private void InitializeVolume()
        {
            VolumeStatus = VolumeStatuses.Static;
            SetSpeakerVolume(Player.Volume);
        }

        private void RemovePauseState()
        {
            pausePosition = 0;
            IsPaused = false;
        }

        public void FadeTo(float volume, float fadeTime)
        {
            if (volume > Player.Volume)
            {
                FadeInTo(volume, fadeTime);
            }
            else if (volume < Player.Volume)
            {
                FadeOutTo(volume, fadeTime);
            }
        }

        public void FadeInTo(float destVolume, float fadeTime)
        {
            DestFadeVolume = destVolume;
            OrigFadeVolume = Volume;
            MaxFadeTime = fadeTime;
            FadeTimeLeft = fadeTime;
            VolumeStatus = VolumeStatuses.FadingIn;
        }

        public void FadeOutTo(float destVolume, float fadeTime)
        {
            DestFadeVolume = destVolume;
            OrigFadeVolume = Volume;
            MaxFadeTime = fadeTime;
            FadeTimeLeft = fadeTime;
            VolumeStatus = VolumeStatuses.FadingOut;
        }

        internal void FadeOut(float fadeTime, bool stopAfterFade = false)
        {
            FadeOutTo(0f, fadeTime);
            StopAfterFade = stopAfterFade;
        }

        internal void FadeIn(float fadeTime)
        {
            FadeInTo(Player.Volume, fadeTime);
        }

        internal void StopFading()
        {
            VolumeStatus = VolumeStatuses.Static;
            SetSpeakerVolume(Player.Volume);
        }
    }
}