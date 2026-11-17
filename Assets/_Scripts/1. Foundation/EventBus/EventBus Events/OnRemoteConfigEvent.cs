
using UnityEngine;

namespace Game.Foundation
{
    public readonly struct RemoteConfigEvent
    {

        public readonly int LOBBY_REQUIRED_PLAYERS;

        public readonly float tank_move_speed;
        public readonly float tank_dash_force;
        public readonly float tank_dash_cooldown;

        public readonly float bullet_speed;
        public readonly float bullet_lifetime;

        public readonly float ability_reload_time;
        public readonly int ability_max_bullets;
        
        public RemoteConfigEvent(
                int lobbyReq,
                float tank_speed,
                float tank_dash_f,
                float tank_dash_cd,
                float bull_speed,
                float bull_lifetime,
                float ability_reload_t,
                int ability_max_bull)
        {
            LOBBY_REQUIRED_PLAYERS = lobbyReq;
            tank_move_speed = tank_speed;
            tank_dash_force = tank_dash_f;
            tank_dash_cooldown = tank_dash_cd;
            bullet_speed = bull_speed;
            bullet_lifetime = bull_lifetime;
            ability_reload_time = ability_reload_t;
            ability_max_bullets = ability_max_bull;
        }
    }
}
