using Fusion;
using Game.Core;
using Game.Foundation;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation
{
    public class Bullet : NetworkBehaviour
    {
        public static readonly HashSet<Bullet> ActiveBullets = new();

        [SerializeField] private LayerMask _wallMask;
        [SerializeField] private LayerMask _hitboxMask;
        [SerializeField] private GameObject _visual;
        [SerializeField] private GameObject _hitEffectPrefab;

        [Networked] private Vector2 Position { get; set; }
        [Networked, OnChangedRender(nameof(OnHitChanged))] private NetworkBool Hit { get; set; }
        [Networked] private Vector2 HitPoint { get; set; }

        private float _speed;
        private float _lifetime;
        private Vector2 _origin;
        private Vector2 _direction;
        private int _fireTick;
        private PlayerRef _shooter;
        private NetworkId _ignoredRoot;
        private TickTimer _despawnTimer;

        private static readonly List<LagCompensatedHit> _hits = new();
        private float _radius;

        private void Awake()
        {
            _radius = _visual.GetComponent<SpriteRenderer>().bounds.extents.x;
        }

        public void Init(Vector2 origin, Vector2 direction, PlayerRef shooter, NetworkId ignoredRoot, float speed, float lifetime)
        {
            _speed = speed;
            _lifetime = lifetime;
            _origin = origin;
            _direction = direction;
            _shooter = shooter;
            _ignoredRoot = ignoredRoot;
            _fireTick = Runner.Tick.Raw;
            Position = origin;
        }

        private Vector2 PositionAt(float seconds) => _origin + _direction * (_speed * seconds);

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (Hit)
            {
                if (_despawnTimer.Expired(Runner)) Runner.Despawn(Object);
                return;
            }

            float dt = Runner.DeltaTime;
            int age = Runner.Tick.Raw - _fireTick;

            if (age * dt >= _lifetime)
            {
                Runner.Despawn(Object);
                return;
            }

            Vector2 from = PositionAt(age * dt);
            Vector2 to = PositionAt((age + 1) * dt);
            Position = to;

            Vector2 delta = to - from;
            float dist = delta.magnitude;

            var wall = Runner.GetPhysicsScene2D().CircleCast(from, _radius, delta / dist, dist, _wallMask);
            if (wall.collider != null)
            {
                RegisterHit(wall.centroid);
                return;
            }

            _hits.Clear();
            int count = Runner.LagCompensation.OverlapSphere(
                to, _radius, _shooter, _hits, _hitboxMask, HitOptions.None);

            for (int i = 0; i < count; i++)
            {
                var go = _hits[i].GameObject;
                if (go == null) continue;

                var no = go.GetComponentInParent<NetworkObject>();
                if (no != null && no.Id == _ignoredRoot) continue;

                var dmg = go.GetComponentInParent<IDamageable>();
                if (dmg == null) continue;

                dmg.DamageableHit();
                RegisterHit(to);
                return;
            }
        }

        private void RegisterHit(Vector2 point)
        {
            Hit = true;
            HitPoint = point;
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
        }

        public override void Render()
        {
            transform.position = Hit ? HitPoint : Position;
        }

        private void OnHitChanged()
        {
            _visual.SetActive(!Hit);

            if (Hit && _hitEffectPrefab != null)
                Instantiate(_hitEffectPrefab, HitPoint, Quaternion.identity);

            if (Hit)
                GameLauncher.Instance.EventBus.Publish(new SfxRequestedEvent(ESfx.BulletImpact));
        }

        public override void Spawned()
        {
            _visual.SetActive(!Hit);

            if (!Hit)
                GameLauncher.Instance.EventBus.Publish(new SfxRequestedEvent(ESfx.Shoot));

            if (Object.HasStateAuthority)
                ActiveBullets.Add(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ActiveBullets.Remove(this);
        }
    }
}