using UnityEngine;

public class CS_BattleResultHandler : MonoBehaviour
{
    [SerializeField]
    private CS_BattleStateMachine _battleStateMachine;

    private void OnEnable() => _battleStateMachine.onBattleEnd += HandleBattleEnd;
    private void OnDisable() => _battleStateMachine.onBattleEnd -= HandleBattleEnd;

    private void HandleBattleEnd(CSE_BattleResult result)
    {
        if (result == CSE_BattleResult.Win)
        {
            GrantExpToSurvivors();
            GrantDropsFromDefeatedEnemies();
        }

        if (result == CSE_BattleResult.Lose)
        {
            // 敗北時は体力・MPを全回復させてから町に戻す
            foreach (var ally in _battleStateMachine.context.allyParty)
            {
                ally.FullHeal();
            }
        }

        CS_GameManager.Instance.UpdatePartyState(_battleStateMachine.context.allyParty);

        switch (result)
        {
            case CSE_BattleResult.Win:
                CS_GameManager.Instance.ReturnToField();
                break;
            case CSE_BattleResult.Lose:
                CS_GameManager.Instance.ReturnTown();
                break;
            case CSE_BattleResult.Escape:
                CS_GameManager.Instance.ReturnToField();
                break;
        }
    }

    // 敵の方がレベルが高いほど経験値が増えるボーナス。1レベル差につきこの割合だけ加算する
    private const float LEVEL_DIFF_BONUS_PER_LEVEL = 0.1f;

    /// <summary>
    /// 倒した敵の経験値を、生存している味方全員に(敵はレベルアップしないため味方限定で)与える。
    /// 自分より高レベルな敵ほどボーナス倍率が乗るため、必要な経験値は味方ごとに個別に計算する
    /// </summary>
    private void GrantExpToSurvivors()
    {
        var enemyParty = _battleStateMachine.context.enemyParty;

        foreach (var ally in _battleStateMachine.context.allyParty)
        {
            if (ally.isDead) continue;

            int totalExp = 0;
            foreach (var enemy in enemyParty)
            {
                float levelDiffBonus = 1f + Mathf.Max(0, enemy.level - ally.level) * LEVEL_DIFF_BONUS_PER_LEVEL;
                totalExp += Mathf.RoundToInt(enemy.expReward * levelDiffBonus);
            }

            if (totalExp > 0)
            {
                ally.GainExp(totalExp);
            }
        }
    }

    /// <summary>
    /// 倒した敵ごとに設定されたドロップ表を確率抽選し、当選したアイテム/装備を所持品に加える
    /// </summary>
    private void GrantDropsFromDefeatedEnemies()
    {
        foreach (var enemy in _battleStateMachine.context.enemyParty)
        {
            if (!enemy.isDead) continue;

            foreach (var drop in enemy.itemDrops)
            {
                if (Random.value <= drop.dropChance)
                {
                    CS_GameManager.Instance.AddItem(drop.itemId, 1);
                    Debug.Log($"{enemy.characterName}から{drop.itemId}をドロップした！");
                }
            }

            foreach (var drop in enemy.equipmentDrops)
            {
                if (Random.value <= drop.dropChance)
                {
                    CS_GameManager.Instance.AddEquipment(drop.itemId, 1);
                    Debug.Log($"{enemy.characterName}から{drop.itemId}をドロップした！");
                }
            }
        }
    }
}
