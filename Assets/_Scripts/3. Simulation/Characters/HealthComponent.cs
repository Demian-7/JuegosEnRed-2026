using Fusion;
using Game.Core;
using Game.Foundation;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    private NetworkObject _parent;
    private NetworkRunner _runner;

    public void Init(NetworkObject parent, NetworkRunner runner)
    {
        _parent = parent;
        _runner = runner;
    }

    public void DamageableHit()
    {
        _runner.GetComponent<GameLauncher>().EventBus.Publish(new PlayerDiedEvent(_parent));
        _runner.Despawn(_parent);
    }
}