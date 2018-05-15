using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using EnsoMusicPlayer;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class Enso_MusicPlayerEventTests {

    MusicPlayer musicPlayer;
    Speaker module;
    Stopwatch watch = new Stopwatch();

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

        Assert.AreEqual(5, timesHandlerCalled);
    }

    [UnityTest]
    public IEnumerator Enso_TrackEndOrLoopCallbackCalledRightAtTheEnd()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackEndOrLoop += new MusicPlayerEventHandler(TestHandler);
        float lengthInSeconds = musicPlayer.GetTrackByName("QuickTest").LengthInSeconds;
        float loopLengthInSeconds = musicPlayer.GetTrackByName("QuickTest").LoopClip.length;

        musicPlayer.Play("QuickTest");
        watch.Start();

        yield return new WaitForSeconds(.2f);

        Assert.IsTrue(IsWithinMargin(lastWatchElapsedTime, lengthInSeconds, .1f));
    }

    [UnityTest]
    public IEnumerator Enso_TrackEndOrLoopCallbackConsistencyTest()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackEndOrLoop += new MusicPlayerEventHandler(ConsistencyTestHandler);
        float lengthInSeconds = musicPlayer.GetTrackByName("QuickTest").LengthInSeconds;
        float loopLengthInSeconds = musicPlayer.GetTrackByName("QuickTest").LoopClip.length;

        musicPlayer.Play("QuickTest");
        watch.Start();

        yield return new WaitForSeconds(4f);
    }

    private void ConsistencyTestHandler(MusicPlayerEventArgs e)
    {
        watch.Stop();
        timesHandlerCalled++;
        lastWatchElapsedTime = watch.ElapsedMilliseconds / 1000f;
        float lengthInSeconds = musicPlayer.GetTrackByName("QuickTest").LoopClip.length;

        Assert.IsTrue(IsWithinMargin(lastWatchElapsedTime, lengthInSeconds, .1f),
            string.Format("Elapsed time: {0}, length in seconds: {1}, iteration: {2}", lastWatchElapsedTime, lengthInSeconds, timesHandlerCalled));

        watch.Reset();
        watch.Start();
    }

    [UnityTest]
    public IEnumerator Enso_TrackEnd()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackEnd += new MusicPlayerEventHandler(TestHandler);

        musicPlayer.Play("QuickTest", 1);

        yield return new WaitForSeconds(.2f);

        Assert.AreEqual(1, timesHandlerCalled);

        musicPlayer.Play("QuickTest", 3);

        yield return new WaitForSeconds(.6f);

        Assert.AreEqual(2, timesHandlerCalled);
    }

    [UnityTest]
    public IEnumerator Enso_TrackLoop()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackLoop += new MusicPlayerEventHandler(TestHandler);

        musicPlayer.Play("QuickTest", 3);

        yield return new WaitForSeconds(.6f);

        Assert.AreEqual(2, timesHandlerCalled);
    }

    [UnityTest]
    public IEnumerator Enso_TrackLoopAndTrackEndOrLoopMustMatchInInfinitePlay()
    {
        SetUpMusicPlayer();

        yield return null;

        musicPlayer.TrackLoop += new MusicPlayerEventHandler(TestHandler);
        musicPlayer.TrackEndOrLoop += new MusicPlayerEventHandler(TestHandler2);

        musicPlayer.Play("QuickTest");

        yield return new WaitForSeconds(.6f);

        Assert.AreEqual(timesHandler2Called, timesHandlerCalled);
    }

    private bool testHandlerCalled = false;
    private int timesHandlerCalled = 0;
    private int timesHandler2Called = 0;
    private float lastWatchElapsedTime = 0f;
    private void TestHandler(MusicPlayerEventArgs e)
    {
        watch.Stop();
        testHandlerCalled = true;
        timesHandlerCalled++;
        lastWatchElapsedTime = watch.ElapsedMilliseconds / 1000f;
        watch.Reset();
    }

    private void TestHandler2(MusicPlayerEventArgs e)
    {
        timesHandler2Called++;
    }

    private bool IsWithinMargin(float input, float goal, float margin)
    {
        return input >= goal - margin && input <= goal + margin;
    }

    #region Setup
    private void SetUpModule()
    {
        GameObject player = new GameObject();
        player.AddComponent<AudioListener>();
        module = player.AddComponent<Speaker>();
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
                    Track = AudioClip.Create("MusicTest", 10000, 1, 1000, false),
                    loopPoints = new MusicTrack.LoopPoints
                    {
                        sampleLoopStart = 2000,
                        sampleLoopLength = 3000
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
        timesHandler2Called = 0;
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
