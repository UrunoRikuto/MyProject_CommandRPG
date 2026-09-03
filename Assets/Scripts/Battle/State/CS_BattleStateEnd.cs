using UnityEngine;

/// <summary>戦闘終了。結果をログに出す(リザルト演出はフェーズ5で追加予定)。</summary>
public class CS_BattleStateEnd : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        string message = context.result switch
        {
            CSE_BattleResult.Win => "勝利!",
            CSE_BattleResult.Escape => "にげきった!",
            _ => "敗北..."
        };
        Debug.Log($"戦闘終了: {message}");
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
