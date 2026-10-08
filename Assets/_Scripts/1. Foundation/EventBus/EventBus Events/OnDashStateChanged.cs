using UnityEngine;

namespace Game.Foundation
{
    public readonly struct DashStateChangedEvent
    {
        public readonly bool canDash;

        public DashStateChangedEvent(bool can)
        {
            canDash = can;
        }
    }

}
