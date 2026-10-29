using Game.Foundation;
using Photon.Realtime;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Lives on the runner prefab, next to FusionVoiceClient and Recorder.
    /// Decides nothing: it applies the channel requested through VoiceChannelChangedEvent.
    /// </summary>
    [RequireComponent(typeof(FusionVoiceClient), typeof(Recorder))]
    public class VoiceChannelService : MonoBehaviour
    {
        // Photon delivers group 0 to everyone in the room, it can't be left.
        public const byte EVERYONE_GROUP = 0;

        private FusionVoiceClient _voiceClient;
        private Recorder _recorder;
        private byte _currentGroup = EVERYONE_GROUP;

        // Start instead of Awake/OnEnable: FusionVoiceClient creates its Client in its own Awake.
        private void Start()
        {
            _voiceClient = GetComponent<FusionVoiceClient>();
            _recorder = GetComponent<Recorder>();

            GameLauncher.Instance.EventBus.Subscribe<VoiceChannelChangedEvent>(OnVoiceChannelChanged);
            _voiceClient.Client.StateChanged += OnVoiceStateChanged;
        }

        private void OnDestroy()
        {
            if (GameLauncher.Instance != null)
                GameLauncher.Instance.EventBus.Unsubscribe<VoiceChannelChangedEvent>(OnVoiceChannelChanged);

            if (_voiceClient != null && _voiceClient.Client != null)
                _voiceClient.Client.StateChanged -= OnVoiceStateChanged;
        }

        private void OnVoiceChannelChanged(VoiceChannelChangedEvent evt)
        {
            _currentGroup = evt.Group;
            ApplyGroup();
        }

        private void OnVoiceStateChanged(ClientState fromState, ClientState toState)
        {
            // The voice room may be joined after the channel was requested, so apply it again.
            if (toState == ClientState.Joined)
                ApplyGroup();
        }

        private void ApplyGroup()
        {
            // Who we talk to.
            _recorder.InterestGroup = _currentGroup;

            // Who we hear. Only possible inside the voice room; OnVoiceStateChanged retries on join.
            if (!_voiceClient.Client.InRoom)
                return;

            // Leave every group (byte[0]), then join ours. Group 0 needs no join.
            byte[] groupsToAdd = _currentGroup == EVERYONE_GROUP ? null : new[] { _currentGroup };
            _voiceClient.Client.OpChangeGroups(new byte[0], groupsToAdd);

            Debug.Log($"[Voice] Channel set to group {_currentGroup}");
        }
    }
}
