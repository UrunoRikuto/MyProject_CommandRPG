using UnityEngine;

public class CS_AttackCommand : IBattleCommand
{
    public string commandName => "‚½‚½‚©‚¤";

    public void Execute(CS_CharacterState user, CS_CharacterState target)
    {
        // UŒ‚—Í‚ğæ“¾
        int damage = user.currentAttack;

        // ƒ_ƒ[ƒW‚ğ—^‚¦‚é
        target.TakeDamage(damage);
    }
}