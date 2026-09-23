
using UnityEngine;

namespace Game.Foundation
{
    public readonly struct CurrentBulletsUpdateEvent
    {
        public readonly int currentBullets;
        public CurrentBulletsUpdateEvent(int current)
        {
            currentBullets = current;
        }
    }
}
