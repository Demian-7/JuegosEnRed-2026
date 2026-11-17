using Game.Core;
using Game.Foundation;
using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class LiveOpsManager : MonoBehaviour
{

    private IEventBus _eventBus;

    [Header("Lobby Settings")]
    public int LOBBY_REQUIRED_PLAYERS = 2;
    [Header("Tank Properties")]
    public float tank_move_speed = 150f;
    public float tank_dash_force = 100f;
    public float tank_dash_cooldown = 3f;
    [Header("Bullet Properties")]
    public float bullet_speed = 10f;
    public float bullet_lifetime = 10f;
    [Header("Ability Properties")]
    public float ability_reload_time = 2f;
    public int ability_max_bullets = 1;


    public struct UserAtributes { }
    public struct AppAtributes { }

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            await LoadConfigAsync();
        }
        catch (Exception ex) { Debug.LogError(ex); }
    }

    private async Task LoadConfigAsync()
    {
        try
        {

            await RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAtributes(), new AppAtributes());

            var config = RemoteConfigService.Instance.appConfig;

            //Cach vars.
            LOBBY_REQUIRED_PLAYERS = config.GetInt("LOBBY_REQUIRED_PLAYERS", 2);
            tank_move_speed = config.GetFloat("tank_move_speed", 150f);
            tank_dash_force = config.GetFloat("tank_dash_force", 100f);
            tank_dash_cooldown = config.GetFloat("tank_dash_cooldown", 3f);
            bullet_speed = config.GetFloat("bullet_speed", 10f);
            bullet_lifetime = config.GetFloat("bullet_lifetime", 10f);
            ability_reload_time = config.GetFloat("ability_reload_time", 2f);
            ability_max_bullets = config.GetInt("ability_max_bullets", 1);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Remote Config no Disponible: {e.Message}");
        }
    }

    public void Init(IEventBus bus)
    {
        _eventBus = bus;
    }

    public void PublishRemoteConfigEvent()
    {
        if (_eventBus != null)
        {
            _eventBus.Publish(new RemoteConfigEvent(
                                    LOBBY_REQUIRED_PLAYERS,
                                    tank_move_speed,
                                    tank_dash_force,
                                    tank_dash_cooldown,
                                    bullet_speed,
                                    bullet_lifetime,
                                    ability_reload_time,
                                    ability_max_bullets));
        }
    }
}
