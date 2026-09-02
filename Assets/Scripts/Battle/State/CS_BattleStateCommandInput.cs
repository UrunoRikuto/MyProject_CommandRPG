using UnityEngine;

/// <summary>
/// プレイヤーのコマンド入力を待つ状態。
/// UI未実装のため、暫定でSpaceキー押下を「たたかう」の選択として扱う(フェーズ5でUIに置き換え予定)。
/// 敵のコマンドは簡易AIとして常に「たたかう」を選択する。
/// </summary>
public class CS_BattleStateCommandInput : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        Debug.Log("コマンドを選択してください [Space] たたかう");
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        context.playerCommand = new CS_AttackCommand();
        context.enemyCommand = new CS_AttackCommand();
        machine.ChangeState(new CS_BattleStateActionOrder());
    }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
