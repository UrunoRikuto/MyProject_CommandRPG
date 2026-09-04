using System.Collections.Generic;

public class CS_BattleStateCommandInput : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        DecideActionsForParty(context, context.allyPartyWithoutPlayer);
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
            if (target == null) continue; // ëäéËÇ™ëSñ≈ÇµÇƒÇ¢ÇΩÇÁçsìÆÇ»Çµ

            context.actionQueue.Enqueue(new CS_BattleActionEntry(actor, target, command));
        }
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}