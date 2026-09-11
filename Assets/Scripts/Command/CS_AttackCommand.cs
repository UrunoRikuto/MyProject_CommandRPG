using UnityEngine;

public class CS_AttackCommand : IBattleCommand
{
    public string commandName => "たたかう";

    public void Execute(CS_BattleContext context, CS_CharacterState user, CS_CharacterState target)
    {
        // 攻撃力を取得
        int damage = user.currentAttack;

        Debug.Log($"{user.characterName}は{target.characterName}に攻撃した！");

        // ダメージを与える(武器に属性上書きが設定されていればその属性で。毒武器などに対応)
        target.TakeDamage(damage, user.attackElement);
    }
}