using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using EnsoMusicPlayer;
using System.Collections.Generic;
using System;

public class Enso_MusicPlayerEventTests {

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
    public IEnumerator Enso_FadeOutComplete() {

        SetUpMusicPlayer();
		
		yield return null;

        musicPlayer.FadeOutComplete += new MusicPlayerEventHandler(TestHandler);

        musicPlayer.Play("MusicTest");

        yield return null;

        musicPlayer.FadeOut();

        yield return new WaitForSeconds(2);

        Assert.IsTrue(testHandlerCalled);
	}

    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityTest]
    public IEnumerator Enso_FadeInComplete()
    {

        SetUpMusicPlayer();

        yield return null;

        musicPlayer.FadeInComplete += new MusicPlayerEventHandler(TestHandler);

        musicPlayer.Play("MusicTest");

        yield return null;

        musicPlayer.FadeIn();

        yield return new WaitForSeconds(2);

        Assert.IsTrue(testHandlerCalled);
    }

    private bool testHandlerCalled = false;
    private void TestHandler(MusicPlayerEventArgs e)
    {
        testHandlerCalled = true;
    }

    #region Setup
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
        testHandlerCalled = false;
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

    #endregion
}
