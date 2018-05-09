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
                Track = AudioClip.Create("test", 2000, 1, 1000, false),
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

            track.ReadTrackMetadata(Arg.Is(EnsoConstants.LoopStartTag)).Returns("100");
            track.ReadTrackMetadata(Arg.Is(EnsoConstants.LoopLengthTag)).Returns("40000");
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

            track.ReadTrackMetadata(Arg.Is(EnsoConstants.LoopStartTag)).Returns("0");
            track.ReadTrackMetadata(Arg.Is(EnsoConstants.LoopLengthTag)).Returns("0");
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
        public void Enso_LengthInSamples()
        {
            // Arrange
            var clip = AudioClip.Create("test", 20000, 2, 1000, false);

            MusicTrack track = new MusicTrack
            {
                Track = clip,
                LoopStart = 3,
                LoopLength = 10
            };
            track.Track = clip;

            // Act
            track.CreateAndCacheClips();

            // Assert
            Assert.AreEqual(13, track.LengthInSamples);
        }

        [Test]
        public void Enso_NoLoopSettings()
        {
            // Arrange
            var clip = AudioClip.Create("test", 20000, 2, 1000, false);

            MusicTrack track = new MusicTrack
            {
                Track = clip,
                LoopStart = 0,
                LoopLength = 0
            };
            track.Track = clip;

            // Act
            track.CreateAndCacheClips();
        }

        [Test]
        public void Enso_FrequencyRatio()
        {
            // Arrange
            var track = Substitute.For<MusicTrack>();

            track.CompensateForFrequency = true;
            track.OriginalFrequency.Returns(1000);
            track.Frequency.Returns(2000);

            // Act
            float ratio = track.FrequencyRatio;

            // Assert
            Assert.AreEqual(2, ratio);
        }

        [Test]
        public void Enso_OutOfBoundsFailsafe()
        {
            // Arrange
            var clip = AudioClip.Create("test", 20000, 2, 1000, false);

            MusicTrack track = new MusicTrack
            {
                Track = clip,
                LoopStart = 10,
                LoopLength = 50
            };
            track.Track = clip;

            // Act
            track.CreateAndCacheClips();
        }
    }
}