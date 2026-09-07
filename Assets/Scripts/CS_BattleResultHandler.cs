using UnityEngine;

public class CS_BattleResultHandler : MonoBehaviour
{
    [SerializeField]
    private CS_BattleStateMachine _battleStateMachine;

    private void OnEnable() => _battleStateMachine.onBattleEnd += HandleBattleEnd;
    private void OnDisable() => _battleStateMachine.onBattleEnd -= HandleBattleEnd;

    private void HandleBattleEnd(CSE_BattleResult result)
    {
        switch (result)
        {
            case CSE_BattleResult.Win:
                CS_GameManager.Instance.ReturnToField();
                break;
            case CSE_BattleResult.Lose:
                CS_GameManager.Instance.ReturnTown();
                break;
            case CSE_BattleResult.Escape:
                CS_GameManager.Instance.ReturnToField();
                break;
        }
    }

}
