using Fusion;

namespace Game.Meta
{
    /// <summary>
    /// Acts as the single source of truth for the match configuration
    /// </summary>
    public class MatchSettings : NetworkBehaviour
    {
        public static MatchSettings Instance { get; private set; }
        
        [Networked] public ETeamSelectionMode TeamMode { get; set; }
        [Networked] public EMapSelectionMode MapMode { get; set; }
        [Networked] public EBestOfRounds Rounds { get; set; } = EBestOfRounds.Bo3;
        
        [Networked] public NetworkBool ModifiersEnabled { get; set; }
        [Networked] public NetworkBool PowerUpsEnabled { get; set; }
        
        //Stores the single map if MapMode == Fixed
        [Networked] public byte FixedMapId { get; set; } = MatchConstants.RANDOM_MAP_ID;
        
        //Stores the ordered sequence if MapMode == Carousel. Capacity is 7 (Max possible rounds).
        [Networked, Capacity(7)] public NetworkArray<byte> CaruoselMaps { get; }

        public void SetCarouselMap(int roundIndex, byte mapId)
        {
            if (!Object.HasStateAuthority)
                return;

            if (roundIndex >= 0 && roundIndex < 7)
            {
                CaruoselMaps.Set(roundIndex, mapId);
            }
        }

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                Runner.Despawn(Object);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }
    }
}