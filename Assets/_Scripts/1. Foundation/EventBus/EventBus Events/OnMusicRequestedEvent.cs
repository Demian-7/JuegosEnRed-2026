namespace Game.Foundation
{
    public readonly struct MusicRequestedEvent
    {
        public readonly EMusicTrack Track;

        public MusicRequestedEvent(EMusicTrack track)
        {
            Track = track;
        }
    }
}
