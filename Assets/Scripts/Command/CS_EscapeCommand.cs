using UnityEngine;

public class CS_EscapeCommand : IBattleCommand
{
    public string commandName => "にげる";

    // 逃げる確率の固定値
    private const float _escapeSuccessRate = 0.5f; // 50%の確率で逃げることができる

    public void Execute(CS_BattleContext context, CS_CharacterState user, CS_CharacterState target)
    {
        // 確率を速度比較して調整
        float speedRatio = (float)user.currentSpeed / target.currentSpeed;
        float adjustedEscapeRate = Mathf.Min(_escapeSuccessRate * speedRatio, 1.0f); // 最大で100%に制限

        // 逃げる判定
        if (Random.value >= adjustedEscapeRate) return;

        context.result = CSE_BattleResult.Escape; // 逃げることに成功した場合の結果を設定
    }
}