public class CS_SkillCommand : IBattleCommand
{
    public string commandName => "スキル使用";

    // 使用するスキルのインデックス
    private int _skillIndex;

    public CS_SkillCommand(int skillIndex)
    {
        _skillIndex = skillIndex;
    }

    public void Execute(CS_CharacterState user, CS_CharacterState target)
    {
        // 使用するスキルのデータを取得
        CSO_SkillData useSkillData = user.currentSkills[_skillIndex];

        // スキルのコストを消費できるか確認
        if (!user.TryUseMP(useSkillData.cost)) return;

        // スキルのダメージを計算
        int damage = (int)(user.currentAttack * (int)useSkillData.damageRate);

        // ダメージを与える
        target.TakeDamage(damage);
    }
}