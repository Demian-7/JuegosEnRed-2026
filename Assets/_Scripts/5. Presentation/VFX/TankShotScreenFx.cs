using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    public enum ShotImpact
    {
        Shot,
        Hit,
        Kill
    }

    /// <summary>
    /// Drives two global volumes: a sharp Flash (instant peak, fast decay)
    /// and an Aftershock (quick attack, long ease-out).
    /// Place one in the gameplay scene and call TankShotScreenFX.Play(...) from presentation code.
    /// Uses unscaled time so hit-stop / slow-mo doesn't stretch the effect.
    /// </summary>
    [DisallowMultipleComponent]
    public class TankShotScreenFX : MonoBehaviour
    {
        [System.Serializable]
        private class Tier
        {
            [Range(0f, 1f)] public float flashWeight = 1f;
            [Range(0f, 1f)] public float aftershockWeight = 1f;
            [Min(0.01f)] public float flashDuration = 0.12f;
            [Min(0.05f)] public float aftershockDuration = 0.5f;
        }

        [Header("References")]
        [SerializeField] private Volume _flashVolume;
        [SerializeField] private Volume _aftershockVolume;

        [Header("Flash")]
        [Tooltip("Higher = sharper decay.")]
        [SerializeField, Min(1f)] private float _flashFalloffPower = 3f;

        [Header("Aftershock")]
        [Tooltip("Seconds (unscaled) to ramp up to the peak weight.")]
        [SerializeField, Min(0.001f)] private float _aftershockAttack = 0.03f;

        [Header("Intensity per impact type")]
        [SerializeField] private Tier _shot = new Tier
        {
            flashWeight = 0.5f, aftershockWeight = 0.3f, flashDuration = 0.10f, aftershockDuration = 0.25f
        };
        [SerializeField] private Tier _hit = new Tier
        {
            flashWeight = 0.8f, aftershockWeight = 0.6f, flashDuration = 0.12f, aftershockDuration = 0.4f
        };
        [SerializeField] private Tier _kill = new Tier
        {
            flashWeight = 1f, aftershockWeight = 1f, flashDuration = 0.14f, aftershockDuration = 0.5f
        };

        private static TankShotScreenFX _instance;

        private bool _flashActive;
        private float _flashT;
        private float _flashPeak;
        private float _flashDuration;
        private float _flashWeight;

        private bool _aftActive;
        private float _aftElapsed;
        private float _aftStart;
        private float _aftPeak;
        private float _aftDuration;
        private float _aftWeight;

        /// <summary>Safe to call from anywhere; does nothing if no FX is in the scene.</summary>
        public static void Play(ShotImpact impact)
        {
            if (_instance != null)
                _instance.Trigger(impact);
        }

        private void OnEnable()
        {
            _instance = this;
            ResetFX();
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
            ResetFX();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateFlash(dt);
            UpdateAftershock(dt);
        }

        private void Trigger(ShotImpact impact)
        {
            Tier tier = GetTier(impact);

            if (tier.flashWeight >= _flashWeight)
            {
                _flashActive = true;
                _flashT = 0f;
                _flashPeak = tier.flashWeight;
                _flashDuration = tier.flashDuration;
                _flashWeight = _flashPeak;
                ApplyWeight(_flashVolume, _flashWeight);
            }

            if (tier.aftershockWeight >= _aftWeight)
            {
                _aftActive = true;
                _aftElapsed = 0f;
                _aftStart = _aftWeight;
                _aftPeak = tier.aftershockWeight;
                _aftDuration = tier.aftershockDuration;
            }
        }

        private void UpdateFlash(float dt)
        {
            if (!_flashActive) return;

            _flashT += dt / _flashDuration;

            if (_flashT >= 1f)
            {
                _flashActive = false;
                _flashWeight = 0f;
            }
            else
            {
                _flashWeight = _flashPeak * Mathf.Pow(1f - _flashT, _flashFalloffPower);
            }

            ApplyWeight(_flashVolume, _flashWeight);
        }

        private void UpdateAftershock(float dt)
        {
            if (!_aftActive) return;

            _aftElapsed += dt;

            if (_aftElapsed < _aftershockAttack)
            {
                float a = _aftElapsed / _aftershockAttack;
                _aftWeight = Mathf.Lerp(_aftStart, _aftPeak, a);
            }
            else
            {
                float r = (_aftElapsed - _aftershockAttack) / _aftDuration;
                if (r >= 1f)
                {
                    _aftActive = false;
                    _aftWeight = 0f;
                }
                else
                {
                    _aftWeight = _aftPeak * Mathf.SmoothStep(1f, 0f, r);
                }
            }

            ApplyWeight(_aftershockVolume, _aftWeight);
        }

        private Tier GetTier(ShotImpact impact)
        {
            switch (impact)
            {
                case ShotImpact.Shot: return _shot;
                case ShotImpact.Hit: return _hit;
                default: return _kill;
            }
        }

        private static void ApplyWeight(Volume volume, float weight)
        {
            if (volume != null)
                volume.weight = weight;
        }

        private void ResetFX()
        {
            _flashActive = false;
            _aftActive = false;
            _flashWeight = 0f;
            _aftWeight = 0f;
            ApplyWeight(_flashVolume, 0f);
            ApplyWeight(_aftershockVolume, 0f);
        }
    }
}