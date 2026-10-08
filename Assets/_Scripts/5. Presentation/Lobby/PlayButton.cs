using Fusion;
using UnityEngine;

//Should be MonoBehaviour instead of NetworkBehaviour, since it acts as a "Ready", and when all 4/4 players are ready, the game auto-starts.
//TODO: Ready logic. Hardcoded for now.
public class PlayButton : MonoBehaviour
{
    [SerializeField] private NetworkRunner _runner;
}