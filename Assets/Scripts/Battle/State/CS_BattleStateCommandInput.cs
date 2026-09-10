using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 味方全員をボタン操作で1体ずつ(素早さ降順で)行動決定させる。敵はAIで即決定する。
/// </summary>
public class CS_BattleStateCommandInput : IBattleState
{
    private CS_BattleContext _context;
    private CS_BattleStateMachine _machine;
    private Queue<CS_CharacterState> _pendingAllies;
    private CS_CharacterState _currentActor;

    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        DecideActionsForParty(context, context.enemyParty);

        _context = context;
        _machine = machine;

        _pendingAllies = new Queue<CS_CharacterState>(
            context.allyParty.Where(a => !a.isDead).OrderByDescending(a => a.currentSpeed));

        machine.commandButtonInput.onCommandDecided += HandleCommandDecided;

        PromptNextAlly();
    }

    private void DecideActionsForParty(CS_BattleContext context, IReadOnlyList<CS_CharacterState> party)
    {
        foreach (var actor in party)
        {
            if (actor.isDead) continue;

            IBattleCommand command = CS_BattleAI.DecideCommand(actor);
            CS_CharacterState target = context.PickRandomLivingTarget(context.GetOpposingParty(actor));
            if (target == null) continue;

            context.actionQueue.Enqueue(new CS_BattleActionEntry(actor, target, command));
        }
    }

    /// <summary>
    /// 行動未決定の味方が残っていれば次の1体のUIを開く。いなければ行動順決定へ進む
    /// </summary>
    private void PromptNextAlly()
    {
        if (_pendingAllies.Count == 0)
        {
            _machine.characterUIWindow.SetActingCharacter(null);
            _machine.ChangeState(new CS_BattleStateActionOrder());
            return;
        }

        _currentActor = _pendingAllies.Dequeue();

        _machine.characterUIWindow.SetActingCharacter(_currentActor);
        _machine.commandButtonInput.SetAvailableSkills(_currentActor.currentSkills);
        _machine.commandButtonInput.SetAvailableTargets(_context.enemyParty);
        _machine.commandButtonInput.Show();
    }

    private void HandleCommandDecided(IBattleCommand command, CS_CharacterState target)
    {
        // にげるはtarget==nullで届くので、素早さ比較の基準として生存中の敵からランダムに1体補う
        CS_CharacterState resolvedTarget = target ?? _context.PickRandomLivingTarget(_context.enemyParty);
        if (resolvedTarget != null)
        {
            _context.actionQueue.Enqueue(new CS_BattleActionEntry(_currentActor, resolvedTarget, command));
        }

        _machine.commandButtonInput.Hide();
        PromptNextAlly();
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.commandButtonInput.onCommandDecided -= HandleCommandDecided;
        machine.commandButtonInput.Hide();
    }
}
