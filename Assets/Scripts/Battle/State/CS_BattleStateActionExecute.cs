public class CS_BattleStateActionExecute : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        while (context.actionQueue.Count > 0)
        {
            CS_BattleActionEntry entry = context.actionQueue.Dequeue();
            if (entry.actor.isDead) continue;

            CS_CharacterState target = entry.target;
            if (target.isDead)
            {
                target = context.PickRandomLivingTarget(context.GetOpposingParty(entry.actor));
                if (target == null) continue; // 相手が全滅していたらこの行動はキャンセル
            }

            entry.command.Execute(context, entry.actor, target);

            if (context.result != CSE_BattleResult.None) break;
        }

        machine.ChangeState(new CS_BattleStateJudgeResult());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}