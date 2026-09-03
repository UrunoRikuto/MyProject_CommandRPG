using UnityEngine;

/// <summary>
/// プレイヤーのコマンド入力を待つ状態。
/// UI未実装のため、暫定でキー押下を選択として扱う(フェーズ5でUIに置き換え予定)。
/// </summary>
public class CS_BattleStateCommandInput : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        Debug.Log("コマンド入力フェーズ: 1=たたかう, 2=スキル");
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            context.playerCommand = new CS_AttackCommand();
            context.enemyCommand = new CS_AttackCommand();
            machine.ChangeState(new CS_BattleStateActionOrder());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            context.playerCommand = new CS_SkillCommand(0);
            context.enemyCommand = new CS_SkillCommand(0);
            machine.ChangeState(new CS_BattleStateActionOrder());
        }
    }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
