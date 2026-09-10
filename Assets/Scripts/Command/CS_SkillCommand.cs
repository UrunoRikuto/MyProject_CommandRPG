using UnityEngine;

public class CS_SkillCommand : IBattleCommand
{
    public string commandName => "スキル使用";

    // 使用するスキルのインデックス
    private int _skillIndex;
    public int skillIndex => _skillIndex;

    public CS_SkillCommand(int skillIndex)
    {
        _skillIndex = skillIndex;
    }

    public void Execute(CS_BattleContext context, CS_CharacterState user, CS_CharacterState target)
    {
        // スキルのインデックスが有効か確認
        if (_skillIndex < 0 || _skillIndex >= user.currentSkills.Count) return;

        // 使用するスキルのデータを取得
        CSO_SkillData useSkillData = user.currentSkills[_skillIndex];

        // スキルのコストを消費できるか確認
        if (!user.TryUseMP(useSkillData.cost)) return;

        // スキルのダメージを計算
        int damage = (int)(user.currentAttack * useSkillData.damageRate);

        Debug.Log($"{user.characterName}は{useSkillData.skillName}を使用！ {target.characterName}に{damage}のダメージを与えた！");

        // ダメージを与える(スキルの属性を反映)
        target.TakeDamage(damage, useSkillData.element);
        LogElementEffectiveness(target, useSkillData.element);
    }

    /// <summary>
    /// 属性の効果を簡易的にログ出力する(Noneのスキルには表示しない)
    /// </summary>
    private void LogElementEffectiveness(CS_CharacterState target, CSE_ElementType element)
    {
        if (element == CSE_ElementType.None) return;

        // TakeDamage内で使われる倍率と同じものを参照するため、CS_CharacterStateには公開しないまま
        // ここではダメージ結果からの逆算はせず、単純にターゲットの耐性データを直接見て判定する
        float multiplier = target.GetElementMultiplier(element);
        if (multiplier <= 0f)
        {
            Debug.Log("効果がないようだ...");
        }
        else if (multiplier > 1f)
        {
            Debug.Log("効果は抜群だ！");
        }
        else if (multiplier < 1f)
        {
            Debug.Log("効果はいまひとつのようだ...");
        }
    }
}
