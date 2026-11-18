namespace Game.Foundation
{
    public readonly struct SfxRequestedEvent
    {
        public readonly ESfx Sfx;

        public SfxRequestedEvent(ESfx sfx)
        {
            Sfx = sfx;
        }
    }
}
