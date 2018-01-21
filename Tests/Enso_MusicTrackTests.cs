using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using NSubstitute;

namespace EnsoMusicPlayer
{
    public class Enso_MusicTrackTests
    {
        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [Test]
        public void Enso_OverriddenTagValue()
        {
            MusicTrack track = new MusicTrack
            {
                Track = AudioClip.Create("test", 2, 1, 1, false),
                LoopStart = 42,
                LoopLength = 79
            };

            Assert.AreEqual(42, track.LoopStart);
            Assert.AreEqual(79, track.LoopLength);
        }

        [Test]
        public void Enso_NoOverriddenTagValue()
        {
            var track = Substitute.For<MusicTrack>();

            track.ReadTrackMetadata(Arg.Is("LOOPSTART")).Returns("100");
            track.ReadTrackMetadata(Arg.Is("LOOPLENGTH")).Returns("40000");
            track.LoopStart = 0;
            track.LoopLength = 0;

            Assert.AreEqual(100, track.LoopStart);
            Assert.AreEqual(40000, track.LoopLength);
        }

        [Test]
        public void Enso_DoNotLookUpMetadataIfItWasDoneAlready()
        {
            // Arrange
            var track = Substitute.For<MusicTrack>();

            track.ReadTrackMetadata(Arg.Is("LOOPSTART")).Returns("100");
            track.ReadTrackMetadata(Arg.Is("LOOPLENGTH")).Returns("40000");
            track.LoopStart = 0;
            track.LoopLength = 0;

            // Act
            int start = track.LoopStart;
            int length = track.LoopLength;

            // Assert
            track.Received().ReadTrackMetadata(Arg.Any<string>());

            track.ClearReceivedCalls();

            start = track.LoopStart;
            length = track.LoopLength;

            track.DidNotReceive().ReadTrackMetadata(Arg.Any<string>());
        }

        [Test]
        {
            // Arrange
            var clip = AudioClip.Create("test", 20, 2, 1, false);

            MusicTrack track = new MusicTrack();
            track.Track = clip;

            Assert.AreEqual(10, track.LengthInSamples);
        }
    }
}