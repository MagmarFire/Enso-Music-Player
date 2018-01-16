using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;

namespace EnsoMusicPlayer
{
    public class Enso_MusicPlayerTests
    {
        MusicPlayer musicPlayer;
        SpeakerModule module;
        Speaker speaker1;
        Speaker speaker2;

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator Enso_SpeakerHasAudioSource()
        {
            SetUpModule();

            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;

            Assert.IsNotNull(speaker1.GetComponent<AudioSource>());
            Assert.IsNotNull(speaker2.GetComponent<AudioSource>()); 
        }

        [UnityTest]
        public IEnumerator Enso_PlayFromStop()
        {
            SetUpModule();

            AudioClip clipMock = AudioClip.Create("test", 1, 1, 1, false);

            yield return null;

            module.Play(new MusicTrack
            {
                Track = clipMock,
                sampleLoopLength = 0,
                sampleLoopStart = 0
            });

            yield return null;

            List<AudioSource> sources = new List<AudioSource>(module.GetComponents<AudioSource>());
            Assert.AreEqual(2, (from s in sources where s.isPlaying select s).Count());
        }

        [UnityTest]
        public IEnumerator Enso_PlayWhileAlreadyPlayingAnotherClip()
        {
            SetUpModule();

            AudioClip clipMock = AudioClip.Create("test", 1, 1, 1, false);
            AudioClip clipMock2 = AudioClip.Create("test2", 1, 1, 1, false);

            yield return null;

            module.Play(new MusicTrack
            {
                Track = clipMock
            });

            yield return null;

            module.Play(new MusicTrack
            {
                Track = clipMock2
            });

            yield return null;

            List<AudioSource> sources = new List<AudioSource>(module.GetComponents<AudioSource>());
            Assert.AreSame(clipMock2, sources[0].clip);
            Assert.AreSame(clipMock2, sources[1].clip);
        }

        [UnityTest]
        public IEnumerator Enso_FadeOutTrack()
        {
            SetUpModule();

            yield return null;

            module.Play(new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            });

            float originalVolume = speaker1.GetVolume();

            yield return null;

            module.FadeOut(2);

            Assert.AreEqual(SpeakerModule.VolumeStatuses.FadingOut, module.VolumeStatus);

            yield return null;

            Assert.AreNotEqual(speaker1.GetVolume(), originalVolume, "The volume isn't changing when fading out.");

            yield return new WaitForSecondsRealtime(2);

            Assert.AreEqual(SpeakerModule.VolumeStatuses.Static, module.VolumeStatus);
            Assert.AreEqual(speaker1.GetVolume(), 0f, "Speaker volume doesn't equal 0 when fading out is complete.");
        }

        [UnityTest]
        public IEnumerator Enso_FadeInTrack()
        {
            SetUpModule();

            yield return null;

            module.Play(new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            });

            float playerVolume = module.PlayerVolume;

            yield return null;

            module.FadeIn(2);
            float originalVolume = speaker1.GetVolume();

            Assert.AreEqual(SpeakerModule.VolumeStatuses.FadingIn, module.VolumeStatus);

            yield return null;

            Assert.AreNotEqual(speaker1.GetVolume(), originalVolume, "The volume isn't changing when fading in.");

            yield return new WaitForSecondsRealtime(2);

            Assert.AreEqual(SpeakerModule.VolumeStatuses.Static, module.VolumeStatus);
            Assert.AreEqual(speaker1.GetVolume(), playerVolume, "Speaker volume doesn't equal PlayerVolume when fading in is complete.");
        }

        [UnityTest]
        public IEnumerator Enso_DontChangeVolumeWhileFading()
        {
            SetUpMusicPlayer();

            yield return null;

            musicPlayer.PlayTrack(new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            });

            float playerVolume = musicPlayer.Volume;

            yield return null;

            musicPlayer.FadeInTrack();
            float originalVolume = speaker1.GetVolume();

            musicPlayer.SetVolume(.5f);

            Assert.AreNotEqual(speaker1.GetVolume(), .5f, "Volume should not be changeable while fading in.");

            yield return new WaitForSeconds(2);

            musicPlayer.FadeOutTrack();
            musicPlayer.SetVolume(.5f);

            Assert.AreNotEqual(speaker1.GetVolume(), .5f, "Volume should not be changeable while fading out.");
        }

        private void SetUpModule()
        {
            GameObject player = new GameObject();
            module = player.AddComponent<SpeakerModule>();
            List<Speaker> speakers = new List<Speaker>(player.GetComponents<Speaker>());

            speaker1 = speakers[0];
            speaker2 = speakers[1];
        }

        private void SetUpMusicPlayer()
        {
            GameObject player = new GameObject();
            musicPlayer = player.AddComponent<MusicPlayer>();
            module = player.GetComponent<SpeakerModule>();

            speaker1 = module.Primary;
            speaker2 = module.Secondary;
        }

        [TearDown]
        public void CleanUp()
        {
            DestroyIfItExists(module);
            DestroyIfItExists(musicPlayer);
        }

        private void DestroyIfItExists(MonoBehaviour obj)
        {
            if (obj)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
    }
}