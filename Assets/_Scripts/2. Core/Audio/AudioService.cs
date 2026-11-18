using System;
using DG.Tweening;
using Game.Foundation;
using UnityEngine;

namespace Game.Core
{
    public class AudioService : MonoBehaviour
    {
        [Serializable]
        public class SfxEntry
        {
            public ESfx Sfx;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        [Header("SFX")]
        [SerializeField] private SfxEntry[] _sfx;

        [Header("Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField, Range(0f, 1f)] private float _menuMusicVolume = 0.5f;
        [SerializeField] private AudioClip _gameplayMusic;
        [SerializeField, Range(0f, 1f)] private float _gameplayMusicVolume = 0.5f;
        [SerializeField] private float _musicFadeTime = 0.5f;

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        private SfxEntry[] _sfxById;
        private EMusicTrack _currentTrack = EMusicTrack.None;
        private bool _subscribed;

        private void Start()
        {
            // skip the duplicate GameLauncher that gets destroyed when Menu reloads
            if (GameLauncher.Instance == null || GameLauncher.Instance.gameObject != gameObject)
                return;

            CreateSources();
            BuildSfxLookup();

            GameLauncher.Instance.EventBus.Subscribe<SfxRequestedEvent>(OnSfxRequested);
            GameLauncher.Instance.EventBus.Subscribe<MusicRequestedEvent>(OnMusicRequested);
            _subscribed = true;

            OnMusicRequested(new MusicRequestedEvent(EMusicTrack.Menu));
        }

        private void OnDestroy()
        {
            if (!_subscribed || GameLauncher.Instance == null)
                return;

            GameLauncher.Instance.EventBus.Unsubscribe<SfxRequestedEvent>(OnSfxRequested);
            GameLauncher.Instance.EventBus.Unsubscribe<MusicRequestedEvent>(OnMusicRequested);
        }

        private void CreateSources()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = 0f;
        }

        private void BuildSfxLookup()
        {
            _sfxById = new SfxEntry[Enum.GetValues(typeof(ESfx)).Length];

            foreach (SfxEntry entry in _sfx)
            {
                _sfxById[(int)entry.Sfx] = entry;
            }
        }

        private void OnSfxRequested(SfxRequestedEvent evt)
        {
            SfxEntry entry = _sfxById[(int)evt.Sfx];
            if (entry == null || entry.Clip == null)
                return;

            _sfxSource.PlayOneShot(entry.Clip, entry.Volume);
        }

        private void OnMusicRequested(MusicRequestedEvent evt)
        {
            if (evt.Track == _currentTrack)
                return;

            _currentTrack = evt.Track;
            AudioClip nextClip = GetMusicClip(evt.Track);
            float nextVolume = GetMusicVolume(evt.Track);

            _musicSource.DOKill();
            _musicSource.DOFade(0f, _musicFadeTime).OnComplete(() => StartMusic(nextClip, nextVolume));
        }

        private void StartMusic(AudioClip clip, float volume)
        {
            _musicSource.Stop();

            if (clip == null)
                return;

            _musicSource.clip = clip;
            _musicSource.Play();
            _musicSource.DOFade(volume, _musicFadeTime);
        }

        private float GetMusicVolume(EMusicTrack track)
        {
            if (track == EMusicTrack.Menu)
                return _menuMusicVolume;

            if (track == EMusicTrack.Gameplay)
                return _gameplayMusicVolume;

            return 0f;
        }

        private AudioClip GetMusicClip(EMusicTrack track)
        {
            if (track == EMusicTrack.Menu)
                return _menuMusic;

            if (track == EMusicTrack.Gameplay)
                return _gameplayMusic;

            return null;
        }
    }
}
