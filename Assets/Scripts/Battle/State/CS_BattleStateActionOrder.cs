using System.Collections.Generic;
using System.Linq;

public class CS_BattleStateActionOrder : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        List<CS_BattleActionEntry> sorted = context.actionQueue
            .OrderByDescending(e => e.actor.currentSpeed)
            .ThenByDescending(e => context.playerParty.Contains(e.actor)) // ìØë¨ÇÕÉvÉåÉCÉÑÅ[ë§óDêÊ
            .ToList();

        context.actionQueue.Clear();
        foreach (var entry in sorted) context.actionQueue.Enqueue(entry);

        machine.ChangeState(new CS_BattleStateActionExecute());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}