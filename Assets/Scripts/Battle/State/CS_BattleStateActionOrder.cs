using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 行動順を決めるステート。入力順とは別に、素早さ降順(同速はプレイヤー側優先)で
/// actionQueueを並べ替えてからACTION_EXECUTEへ進む
/// </summary>
public class CS_BattleStateActionOrder : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        List<CS_BattleActionEntry> sorted = context.actionQueue
            .OrderByDescending(e => e.actor.currentSpeed)
            .ThenByDescending(e => context.allyParty.Contains(e.actor)) // 同速はプレイヤー側優先
            .ToList();

        context.actionQueue.Clear();
        foreach (var entry in sorted) context.actionQueue.Enqueue(entry);

        machine.ChangeState(new CS_BattleStateActionExecute());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}