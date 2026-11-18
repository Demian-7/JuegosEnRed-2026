using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Root of the tank explosion prefab. Tints each particle layer with the victim's color.
    /// </summary>
    public class TankExplosionVFX : MonoBehaviour
    {
        [System.Serializable]
        private struct Layer
        {
            public ParticleSystem system;
            [Tooltip("0 = pure victim color, 1 = pure white.")]
            [Range(0f, 1f)] public float whiteMix;
        }

        [SerializeField] private Layer[] _layers;
        [Tooltip("Seconds before the prefab is destroyed. Must be longer than the longest layer.")]
        [SerializeField, Min(0.1f)] private float _lifetime = 2f;

        public static TankExplosionVFX Spawn(TankExplosionVFX prefab, Vector3 position, Color tint)
        {
            if (prefab == null) return null;

            TankExplosionVFX instance = Instantiate(prefab, position, Quaternion.identity);
            instance.Play(tint);
            return instance;
        }

        public void Play(Color tint)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                ParticleSystem ps = _layers[i].system;
                if (ps == null) continue;

                Color c = Color.Lerp(tint, Color.white, _layers[i].whiteMix);
                c.a = 1f;

                ParticleSystem.MainModule main = ps.main;
                main.startColor = c;
                ps.Play(true);
            }

            Destroy(gameObject, _lifetime);
        }
    }
}