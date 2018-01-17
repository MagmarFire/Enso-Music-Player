# Ensō Music Player
A simple, free music looping system for Unity 3D.

To use Ensō after downloading it from the Unity Asset Store or downloading it from GitHub, follow these instructions:

1. Create a GameObject that you'd like to be the source of audio in your worldspace. An empty GameObject will work fine.
2. With the new GameObject highlighted in the scene, go to Component > Audio > Ensō Music Player. Ensō will then be attached to the GameObject you just created.
3. Scroll down to the Track Settings section of the Inspector and change the size of the Tracks array by however many songs you want to play in your game.
4. Give the new track elements a name, a corresponding music file (only OGGs are supported thus far), and the loop start and length sample values for the song. Note: If you've imported loopable music files from RPG Maker projects, Ensō will read the LOOPSTART and LOOPLENGTH metadata and use those values automatically on playback. You can override these values if you want, without modifying the base files.
5. In a script, reference Ensō like you would any other component in your scene (GameObjectYouCreated.GetComponent<EnsoMusicPlayer.MusicPlayer>()). And then call the PlayTrack() function while passing in the name of the song you want to play. Use the name you gave the track element, not the name of the song file!

And woot! The song you specified will play at the very beginning and loop back to the point you specified after reaching the end of the loop.
