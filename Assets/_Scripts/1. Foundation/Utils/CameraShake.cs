using UnityEngine;

namespace Game.Presentation
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float _maxOffset = 0.25f;
        [SerializeField] private float _traumaDecay = 2.2f;
        [SerializeField] private float _noiseFrequency = 20f;
        
        public static CameraShake Instance { get; private set; }

        private float _trauma;
        private Vector3 _originLocalPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _originLocalPos = transform.localPosition;
        }

        /// <summary>
        /// Trauma is clamped 0-1. Shake intensity = trauma^2. Stack additive calls.
        /// </summary>
        public static void AddTrauma(float amount)
        {
            if (Instance == null) 
                return;
            
            Instance._trauma = Mathf.Clamp01(Instance._trauma + amount);
        }

        private void Update()
        {
            if (_trauma <= 0f) 
                return;

            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * _noiseFrequency;

            transform.localPosition = _originLocalPos + new Vector3(
                _maxOffset * (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * shake,
                _maxOffset * (Mathf.PerlinNoise(0f, t) * 2f - 1f) * shake,
                0f
            );

            _trauma -= _traumaDecay * Time.unscaledDeltaTime;

            if (_trauma <= 0f)
            {
                _trauma = 0f;
                transform.localPosition = _originLocalPos;
            }
        }
    }
}