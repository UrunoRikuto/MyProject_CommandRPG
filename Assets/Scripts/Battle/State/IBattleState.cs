/// <summary>
/// 戦闘の状態パターン(Start→CommandInput→ActionOrder→ActionExecute→JudgeResult→…)の
/// 各ステートが実装するインターフェース
/// </summary>
public interface IBattleState
{
    void Enter(CS_BattleContext context, CS_BattleStateMachine machine);
    void Update(CS_BattleContext context, CS_BattleStateMachine machine);
    void Exit(CS_BattleContext context, CS_BattleStateMachine machine);
}