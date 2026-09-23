using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _actualBullets;
    [SerializeField] private TMP_Text _reloadingState;
    [SerializeField] private TMP_Text _dashState;
    [SerializeField] private TMP_Text _matchState;
    [SerializeField] private TMP_Text _x1;
    [SerializeField] private TMP_Text _x2;


    public void UpdateBulletsCount(int actualBullets, int maxBullets)
    {
        _actualBullets.text = $"{actualBullets} / {maxBullets}";
    }

    public void UpdateReloadingState(bool reloading) 
    {
        if (reloading) 
        {
            _reloadingState.text = "Reloading";
        }
        else
        {
            _reloadingState.text = "";

        }

    }


    public void UpdateDashState(bool canDash) 
    {
        if (canDash)
        {
            _dashState.text = "Ready";
        }
        else
        {
            _dashState.text = "Cooldown";

        }
    }
    public void UpdateMatchState(int scoreTeam0, int scoreTeam1) 
    {
        _matchState.text = $"{scoreTeam0} - {scoreTeam1}";
    }

}
