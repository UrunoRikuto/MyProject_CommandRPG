/// <summary>
/// 勝敗を判定する。まだ決着していなければコマンド入力に戻る。
/// </summary>
public class CS_BattleStateJudgeResult : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        bool allEnemysDead = true;
        foreach (var enemy in context.enemyParty)
        {
            if (!enemy.isDead)
            {
                allEnemysDead = false;
                break;
            }
        }

        if (allEnemysDead)
        {
            context.result = CSE_BattleResult.Win;
        }
        else
        {
            bool allPlayersDead = true;
            foreach (var player in context.playerParty)
            {
                if (!player.isDead)
                {
                    allPlayersDead = false;
                    break;
                }
            }

            if (allPlayersDead)
            {
                context.result = CSE_BattleResult.Lose;
            }
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
