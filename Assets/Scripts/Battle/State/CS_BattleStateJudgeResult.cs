/// <summary>
/// 勝敗を判定する。まだ決着していなければコマンド入力に戻る。
/// </summary>
public class CS_BattleStateJudgeResult : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        if (context.enemyState.isDead)
        {
            context.result = CSE_BattleResult.Win;
        }
        else if (context.playerState.isDead)
        {
            context.result = CSE_BattleResult.Lose;
        }

        if (context.result == CSE_BattleResult.None)
        {
            machine.ChangeState(new CS_BattleStateCommandInput());
        }
        else
        {
            machine.ChangeState(new CS_BattleStateEnd());
        }
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
