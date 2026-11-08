
using UnityEngine;

namespace Game.Foundation
{
    public readonly struct MaxBulletsUpdateEvent
    {
        public readonly int maxBullets;
        public MaxBulletsUpdateEvent( int max)
        {
            maxBullets = max;
        }
    }
}
