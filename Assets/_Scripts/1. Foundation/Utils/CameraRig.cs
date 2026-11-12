using DG.Tweening;
using Game.Meta;
using Game.Simulation;
using UnityEngine;

namespace Game.Presentation
{
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float _maxOffset = 0.25f;
        [SerializeField] private float _traumaDecay = 2.2f;
        [SerializeField] private float _noiseFrequency = 20f;
        
        [Header("Pre-Round Zoom")]
        [SerializeField] private float _startZoomSize = 1.5f;
        [SerializeField] private float _defaultZoomSize = 6.5f;
        [SerializeField] private Ease _tweenEase = Ease.InOutSine;
        
        public static CameraRig Instance { get; private set; }

        private Camera _cam;
        private float _trauma;
        private Vector3 _originLocalPos;
        private Vector3 _basePosition; 
        
        private bool _hasTriggeredPreRound = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _cam = GetComponent<Camera>();
            _cam.enabled = false;
            
            _originLocalPos = transform.localPosition;
            _basePosition = _originLocalPos;
        }

        public static void AddTrauma(float amount)
        {
            if (Instance == null) return;
            Instance._trauma = Mathf.Clamp01(Instance._trauma + amount);
        }

        private void Update()
        {
            if (MatchManager.Instance != null)
            {
                if (MatchManager.Instance.Phase == MatchPhase.PreRound && !_hasTriggeredPreRound)
                {
                    if (TankController.LocalTank != null)
                    {
                        TriggerPreRoundPan();
                        _hasTriggeredPreRound = true;
                    }
                }
                else if (MatchManager.Instance.Phase == MatchPhase.Playing && !_cam.enabled)
                {
                    _cam.enabled = true;
                    _basePosition = _originLocalPos;
                    _cam.orthographicSize = _defaultZoomSize;
                }
                else if (MatchManager.Instance.Phase != MatchPhase.PreRound)
                {
                    _hasTriggeredPreRound = false; 
                }
            }

            float shakeX = 0f;
            float shakeY = 0f;

            if (_trauma > 0f)
            {
                float shake = _trauma * _trauma;
                float time = Time.unscaledTime * _noiseFrequency;

                shakeX = _maxOffset * (Mathf.PerlinNoise(time, 0f) * 2f - 1f) * shake;
                shakeY = _maxOffset * (Mathf.PerlinNoise(0f, time) * 2f - 1f) * shake;

                _trauma -= _traumaDecay * Time.unscaledDeltaTime;
                if (_trauma < 0f) _trauma = 0f;
            }

            transform.localPosition = _basePosition + new Vector3(shakeX, shakeY, 0f);
        }

        private void TriggerPreRoundPan()
        {
            DOTween.Kill(this);

            Vector3 tankPos = TankController.LocalTank.transform.position;
            tankPos.z = _originLocalPos.z;

            _basePosition = tankPos;
            _cam.orthographicSize = _startZoomSize;
            _cam.enabled = true;

            float? remaining = MatchManager.Instance.Timer.RemainingTime(MatchManager.Instance.Runner);
            float tweenDuration = remaining.HasValue ? remaining.Value : 3f;
            if (tweenDuration <= 0) tweenDuration = 0.1f;

            DOTween.To(() => _basePosition, x => _basePosition = x, _originLocalPos, tweenDuration)
                .SetEase(_tweenEase)
                .SetTarget(this);

            _cam.DOOrthoSize(_defaultZoomSize, tweenDuration)
                .SetEase(_tweenEase)
                .SetTarget(this);
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}