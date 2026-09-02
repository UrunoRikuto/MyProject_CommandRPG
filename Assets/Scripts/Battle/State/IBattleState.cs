public interface IBattleState
{
    void Enter(CS_BattleContext context, CS_BattleStateMachine machine);
    void Update(CS_BattleContext context, CS_BattleStateMachine machine);
    void Exit(CS_BattleContext context, CS_BattleStateMachine machine);
}