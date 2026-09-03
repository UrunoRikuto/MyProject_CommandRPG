using UnityEngine;

public static class CS_BattleAI
{
    public static IBattleCommand DecideCommand(CS_CharacterState actor)
    {
        // MPが足りないスキルは選択肢から除外しておく(選んでも不発になるだけなので)
        float totalWeight = actor.attackWeight;
        for (int i = 0; i < actor.currentSkills.Count; i++)
        {
            if (actor.currentMP >= actor.currentSkills[i].cost)
            {
                totalWeight += actor.skillWeights[i];
            }
        }

        float roll = Random.Range(0f, totalWeight);

        if (roll < actor.attackWeight) return new CS_AttackCommand();
        roll -= actor.attackWeight;

        for (int i = 0; i < actor.currentSkills.Count; i++)
        {
            if (actor.currentMP < actor.currentSkills[i].cost) continue;

            if (roll < actor.skillWeights[i]) return new CS_SkillCommand(i);
            roll -= actor.skillWeights[i];
        }

        return new CS_AttackCommand(); // 保険(ここには基本到達しない)
    }
}