using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using EnsoMusicPlayer;
using System.Collections.Generic;

public class Enso_MusicPlayerEventTests {

    MusicPlayer musicPlayer;
    Speaker module;

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

    [UnityTest]
    public IEnumerator Enso_TrackEndOrLoop()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackEndOrLoop += new MusicPlayerEventHandler(TestHandler);

        musicPlayer.Play("QuickTest");

        yield return new WaitForSeconds(1);

        Assert.IsTrue(timesHandlerCalled >= 4);
    }

    private bool testHandlerCalled = false;
    private int timesHandlerCalled = 0;
    private void TestHandler(MusicPlayerEventArgs e)
    {
        testHandlerCalled = true;
        timesHandlerCalled++;
    }

    #region Setup
    private void SetUpModule()
    {
        GameObject player = new GameObject();
        player.AddComponent<AudioListener>();
        module = player.AddComponent<Speaker>();

        module.SetPlayerVolume(1f);
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
                },
                new MusicTrack
                {
                    Name = "QuickTest",
                    Track = AudioClip.Create("QuickTest", 10000, 1, 50000, false), // 1/5 of a second long total
                    loopPoints = new MusicTrack.LoopPoints
                    {
                        sampleLoopStart = 200
                    }
                }
            };

        foreach (MusicTrack track in musicPlayer.Tracks)
        {
            track.CreateAndCacheClips();
        }

        Speaker[] modules = player.GetComponents<Speaker>();

        module = modules[0];
    }

    [TearDown]
    public void CleanUp()
    {
        DestroyIfItExists(module);
        DestroyIfItExists(musicPlayer);
        testHandlerCalled = false;
        timesHandlerCalled = 0;
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
