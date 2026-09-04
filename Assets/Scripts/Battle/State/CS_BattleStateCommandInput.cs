using System.Collections.Generic;

public class CS_BattleStateCommandInput : IBattleState
{
    private CS_BattleContext _context;
    private CS_BattleStateMachine _machine;

    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        DecideActionsForParty(context, context.allyPartyWithoutPlayer);
        DecideActionsForParty(context, context.enemyParty);

        CS_CharacterState controlled = context.playerState;
        if (controlled == null || controlled.isDead)
        {
            machine.ChangeState(new CS_BattleStateActionOrder());
            return;
        }

        _context = context;
        _machine = machine;

        machine.commandButtonInput.SetAvailableSkills(controlled.currentSkills);
        machine.commandButtonInput.SetAvailableTargets(context.enemyParty);
        machine.commandButtonInput.onCommandDecided += HandleCommandDecided;
        machine.commandButtonInput.Show();
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

    private void HandleCommandDecided(IBattleCommand command, CS_CharacterState target)
    {
        // Ç…Ç∞ÇÈÇÕtarget==nullÇ≈ìÕÇ≠ÇÃÇ≈ÅAëfëÅÇ≥î‰ärÇÃäÓèÄÇ∆ÇµÇƒê∂ë∂íÜÇÃìGÇ©ÇÁÉâÉìÉ_ÉÄÇ…1ëÃï‚Ç§
        CS_CharacterState resolvedTarget = target ?? _context.PickRandomLivingTarget(_context.enemyParty);
        if (resolvedTarget != null)
        {
            _context.actionQueue.Enqueue(new CS_BattleActionEntry(_context.playerState, resolvedTarget, command));
        }

        _machine.ChangeState(new CS_BattleStateActionOrder());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.commandButtonInput.onCommandDecided -= HandleCommandDecided;
        machine.commandButtonInput.Hide();
    }
}