using UnityEngine;

namespace Game.Foundation
{
    public readonly struct ReloadingStateChangedEvent
    {
        public readonly bool reloading;

        public ReloadingStateChangedEvent(bool reload)
        {
            reloading = reload;
        }
    }

}
