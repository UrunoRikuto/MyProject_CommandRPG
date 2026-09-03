/// <summary>
/// 行動キューを順番に実行する。
/// 既に倒れているキャラクターの行動はスキップする。
/// </summary>
public class CS_BattleStateActionExecute : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        while (context.actionQueue.Count > 0)
        {
            CS_BattleActionEntry entry = context.actionQueue.Dequeue();
            if (entry.actor.isDead) continue;

            entry.command.Execute(context, entry.actor, entry.target);

            if (context.result != CSE_BattleResult.None) break;
        }

        machine.ChangeState(new CS_BattleStateJudgeResult());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
