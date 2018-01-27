using System;

namespace EnsoMusicPlayer
{
    public delegate void MusicPlayerEventHandler(MusicPlayerEventArgs e);

    public class MusicPlayerEventArgs : EventArgs { }
}
