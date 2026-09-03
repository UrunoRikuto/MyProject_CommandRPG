public class CS_BattleStateCommandInput : IBattleState
{
    private CS_BattleContext _context;
    private CS_BattleStateMachine _machine;

    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        _context = context;
        _machine = machine;

        machine.commandButtonInput.SetAvailableSkills(context.playerState.currentSkills);
        machine.commandButtonInput.onCommandDecided += HandleCommandDecided;
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        // ボタン入力待ちなので、ここでは何もしなくてよい
        // (キー入力のInput.GetKeyDown判定はまるごと不要になる)
    }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.commandButtonInput.onCommandDecided -= HandleCommandDecided;
    }

    private void HandleCommandDecided(IBattleCommand playerCommand)
    {
        _context.playerCommand = playerCommand;
        _context.enemyCommand = new CS_AttackCommand(); // 簡易AI:常にたたかう
        _machine.ChangeState(new CS_BattleStateActionOrder());
    }
}