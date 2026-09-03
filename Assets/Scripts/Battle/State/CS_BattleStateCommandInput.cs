using System.Collections.Generic;

public class CS_BattleStateCommandInput : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        // 6-3で操作キャラクター(playerParty[0])の入力に置き換えるまでは全員AI
        DecideActionsForParty(context, context.playerParty);
        DecideActionsForParty(context, context.enemyParty);

        machine.ChangeState(new CS_BattleStateActionOrder());
    }

    private void DecideActionsForParty(CS_BattleContext context, IReadOnlyList<CS_CharacterState> party)
    {
        foreach (var actor in party)
        {
            if (actor.isDead) continue;

            IBattleCommand command = CS_BattleAI.DecideCommand(actor);
            CS_CharacterState target = context.PickRandomLivingTarget(context.GetOpposingParty(actor));
            if (target == null) continue; // 相手が全滅していたら行動なし

            context.actionQueue.Enqueue(new CS_BattleActionEntry(actor, target, command));
        }
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}