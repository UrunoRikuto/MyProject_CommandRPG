using UnityEngine;

public class CS_AttackCommand : IBattleCommand
{
    public string commandName => "たたかう";

    public void Execute(CS_BattleContext context, CS_CharacterState user, CS_CharacterState target)
    {
        // 攻撃力を取得
        int damage = user.currentAttack;

        Debug.Log($"{user.characterName}は{target.characterName}に攻撃した！");

        // ダメージを与える
        target.TakeDamage(damage);
    }
}