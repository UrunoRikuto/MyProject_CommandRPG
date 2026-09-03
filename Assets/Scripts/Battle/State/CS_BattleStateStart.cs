using UnityEngine;

/// <summary>
/// 戦闘開始演出(現状はログのみ)。すぐにコマンド入力へ進む。
/// </summary>
public class CS_BattleStateStart : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.ChangeState(new CS_BattleStateCommandInput());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
