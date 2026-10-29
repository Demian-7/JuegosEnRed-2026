namespace Game.Foundation
{
    /// <summary>
    /// Asks the local voice chat to switch channel. A channel is a Photon Voice interest group:
    /// the local player only talks to, and only hears, the players on the same group.
    /// </summary>
    public readonly struct VoiceChannelChangedEvent
    {
        public readonly byte Group;

        public VoiceChannelChangedEvent(byte group)
        {
            Group = group;
        }
    }
}
