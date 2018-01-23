using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EnsoMusicPlayer
{
    public class Enso_MusicPlayerTests
    {
        MusicPlayer musicPlayer;
        Speaker module;
        Speaker module2;
        AudioSource speaker1;
        AudioSource speaker2;
        AudioSource speaker3;
        AudioSource speaker4;

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
        public IEnumerator Enso_FadeOutTrack()
        {
            SetUpModule();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            };
            track.CreateAndCacheClips();
            module.Play(track);

            float originalVolume = speaker1.volume;

            yield return null;

            module.FadeOut(2);

            Assert.AreEqual(Speaker.VolumeStatuses.FadingOut, module.VolumeStatus);

            yield return null;

            Assert.AreNotEqual(speaker1.volume, originalVolume, "The volume isn't changing when fading out.");

            yield return new WaitForSecondsRealtime(2);

            Assert.AreEqual(Speaker.VolumeStatuses.Static, module.VolumeStatus);
            Assert.AreEqual(speaker1.volume, 0f, "Speaker volume doesn't equal 0 when fading out is complete.");
        }

        [UnityTest]
        public IEnumerator Enso_PlayAfterFadeOut()
        {
            SetUpMusicPlayer();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            };
            track.CreateAndCacheClips();

            musicPlayer.Play(track);

            yield return null;

            musicPlayer.FadeOut();

            yield return new WaitForSeconds(2);

            Assert.IsTrue(speaker1.volume <= 0f, "Speaker should be muted after fadeout.");

            musicPlayer.Play("MusicTest");

            yield return null;

            Assert.IsTrue(speaker1.volume == musicPlayer.Volume, "Speaker1 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker2.volume == musicPlayer.Volume, "Speaker2 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker3.volume == musicPlayer.Volume, "Speaker3 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker4.volume == musicPlayer.Volume, "Speaker4 should be back at player volume after PlayTrack() is called.");
        }

        [UnityTest]
        public IEnumerator Enso_PlayWhileFadingOut()
        {
            SetUpMusicPlayer();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            };
            track.CreateAndCacheClips();

            musicPlayer.Play(track);

            yield return null;

            musicPlayer.FadeOut();

            musicPlayer.Play("MusicTest");

            yield return new WaitForSeconds(2);

            Assert.IsTrue(speaker1.volume == musicPlayer.Volume, "Speaker1 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker2.volume == musicPlayer.Volume, "Speaker2 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker3.volume == musicPlayer.Volume, "Speaker3 should be back at player volume after PlayTrack() is called.");
            Assert.IsTrue(speaker4.volume == musicPlayer.Volume, "Speaker4 should be back at player volume after PlayTrack() is called.");
        }

        [UnityTest]
        public IEnumerator Enso_FadeInTrack()
        {
            SetUpModule();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false)
            };
            track.CreateAndCacheClips();
            module.Play(track);

            yield return null;

            module.FadeIn(2);
            float originalVolume = speaker1.volume;

            Assert.AreEqual(Speaker.VolumeStatuses.FadingIn, module.VolumeStatus);

            yield return null;

            Assert.AreNotEqual(speaker1.volume, originalVolume, "The volume isn't changing when fading in.");

            yield return new WaitForSecondsRealtime(2);

            Assert.AreEqual(Speaker.VolumeStatuses.Static, module.VolumeStatus);
            Assert.AreEqual(speaker1.volume, originalVolume, "Speaker volume doesn't equal PlayerVolume when fading in is complete.");
        }

        [UnityTest]
        public IEnumerator Enso_DontChangeVolumeWhileFading()
        {
            SetUpMusicPlayer();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 4, 1, 1, false),
                LoopStart = 1,
                LoopLength = 1
            };
            track.CreateAndCacheClips();
            musicPlayer.Play(track);

            float playerVolume = musicPlayer.Volume;

            yield return null;

            musicPlayer.FadeIn();
            float originalVolume = speaker1.volume;

            musicPlayer.SetVolume(.5f);

            Assert.AreNotEqual(speaker1.volume, .5f, "Volume should not be changeable while fading in.");

            yield return new WaitForSeconds(2);

            musicPlayer.FadeOut();
            musicPlayer.SetVolume(.5f);

            Assert.AreNotEqual(speaker1.volume, .5f, "Volume should not be changeable while fading out.");
        }

        [UnityTest]
        public IEnumerator Enso_NullLoopPointsShouldNotThrowException()
        {
            SetUpMusicPlayer();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 10, 1, 1, false),
            };
            track.CreateAndCacheClips();
        }

        [UnityTest]
        public IEnumerator Enso_ScrubbingToEndsOfTrackShouldNotThrowError()
        {
            SetUpMusicPlayer();

            yield return null;

            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 13803264, 2, 44100, false),
                LoopStart = 11130,
                LoopLength = 6477145
            };
            track.CreateAndCacheClips();

            yield return null;

            musicPlayer.Play(track);

            yield return null;

            musicPlayer.Scrub(152.098f);

            yield return null;

            musicPlayer.ScrubAsPercentage(.97f);
        }

        [UnityTest]
        public IEnumerator Enso_PlayShouldSetCurrentTrack()
        {
            // Arrange
            SetUpMusicPlayer();

            // Play(string) test
            // Act
            musicPlayer.Play("MusicTest");

            yield return null;

            // Assert
            Assert.IsNotNull(musicPlayer.PlayingTrack);

            // Play(MusicTrack) test
            // Arrange
            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 1000, 1, 1, false)
            };
            track.CreateAndCacheClips();

            // Act
            musicPlayer.Stop();
            musicPlayer.Play(track);

            yield return null;

            // Assert
            Assert.IsNotNull(musicPlayer.PlayingTrack);
        }

        [UnityTest]
        public IEnumerator Enso_CrossfadeShouldSetCurrentTrack()
        {
            // Arrange
            SetUpMusicPlayer();
            MusicTrack track = new MusicTrack
            {
                Name = "test",
                Track = AudioClip.Create("test", 1000, 1, 1, false)
            };
            track.CreateAndCacheClips();

            musicPlayer.Tracks.Add(track);

            // Play(string) test
            // Act
            musicPlayer.Play("MusicTest");

            yield return null;

            musicPlayer.CrossFadeTo("test");

            yield return null;

            // Assert
            Assert.AreNotSame(musicPlayer.PlayingTrack, musicPlayer.Tracks.Where(x => x.Name == "MusicTest").First());
        }

        private void SetUpModule()
        {
            GameObject player = new GameObject();
            player.AddComponent<AudioListener>();
            module = player.AddComponent<Speaker>();

            module.SetPlayerVolume(1f);

            List<AudioSource> speakers = new List<AudioSource>(player.GetComponents<AudioSource>());

            speaker1 = speakers[0];
            speaker2 = speakers[1];
        }

        private void SetUpMusicPlayer()
        {
            GameObject player = new GameObject();
            player.AddComponent<AudioListener>();
            musicPlayer = player.AddComponent<MusicPlayer>();

            musicPlayer.Tracks = new List<MusicTrack>
            {
                new MusicTrack
                {
                    Name = "MusicTest",
                    Track = AudioClip.Create("MusicTest", 10, 1, 1, false),
                    loopPoints = new MusicTrack.LoopPoints
                    {
                        sampleLoopStart = 2,
                        sampleLoopLength = 3
                    }
                }
            };

            foreach (MusicTrack track in musicPlayer.Tracks)
            {
                track.CreateAndCacheClips();
            }

            Speaker[] modules = player.GetComponents<Speaker>();

            module = modules[0];
            module2 = modules[1];

            speaker1 = module.IntroSource;
            speaker2 = module.LoopSource;
            speaker3 = module2.IntroSource;
            speaker4 = module2.LoopSource;
        }

        [TearDown]
        public void CleanUp()
        {
            DestroyIfItExists(module);
            DestroyIfItExists(musicPlayer);
        }

        private MusicTrack CreateMockMusicTrack()
        {
            return new MusicTrack
            {
                Name = "MusicTest",
                Track = AudioClip.Create("MusicTest", 5, 1, 1, false),
                loopPoints = new MusicTrack.LoopPoints
                {
                    sampleLoopStart = 1,
                    sampleLoopLength = 3
                }
            };
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