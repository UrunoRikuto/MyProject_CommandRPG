using System;
using System.Collections.Generic;
using UnityEngine;

// ステータスに関するクラス
public class Status : MonoBehaviour
{
    //体力
    private int nHp; // 現在の体力
    private int nMaxHp; // 最大体力

    // マナ(スキルコスト)
    private int nMp; // 現在のマナ
    private int nMaxMp; // 最大マナ

    // 攻撃力
    private int nPhysicalAttack; //物理攻撃力
    private int nMagicAttack; // 魔法攻撃力

    // 防御力(ダメージ軽減率)
    private int nPhysicalDefense;//物理防御力
    private int nMagicDefense; // 魔法防御力

    // 素早さ(行動順)
    private int nSpeed; // 素早さ

    // 属性
    private Element eElement; // 属性(装備品やスキルによって変化する可能性がある)

    // 所持スキル
    [SerializeField]
    private List<Skill> skills; // キャラクターが習得しているスキルのリスト

    // ステータス効果（ランタイム状態）
    [SerializeField]
    private List<StatusEffectInstance> statusEffects; // バフやデバフなどのステータス効果のリスト

    private bool IsCalcedEffectiveStatus; // バフデバフ適応後のステータスが計算されているかどうか(必要な時に再計算)

    //--- ステータス効果適応済み変数 ---//
    /* バフデバフ適応後の最大体力 */
    [NonSerialized] public int nEffectiveMaxHp;
    /* バフデバフ適応後の最大マナ */
    [NonSerialized] public int nEffectiveMaxMp;
    /* バフデバフ適応後の物理攻撃力 */
    [NonSerialized] public int nEffectivePhysicalAttack;
    /* バフデバフ適応後の魔法攻撃力 */
    [NonSerialized] public int nEffectiveMagicAttack;
    /* バフデバフ適応後の物理防御力 */
    [NonSerialized] public int nEffectivePhysicalDefense;
    /* バフデバフ適応後の魔法防御力 */
    [NonSerialized] public int nEffectiveMagicDefense;
    /* バフデバフ適応後の素早さ */
    [NonSerialized] public int nEffectiveSpeed;

    // ======================
    // StatusEffect付与/更新
    // ======================

    // ステータス効果を追加する処理（定義からインスタンス化して保持する）
    public void AddStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;
        if (statusEffects == null) statusEffects = new List<StatusEffectInstance>();

        // 同名（同定義）の重複処理
        var existing = statusEffects.Find(x => x != null && x.def == effect);
        if (existing != null)
        {
            switch (effect.eStackPolicy)
            {
                case StatusEffect.StackPolicy.RefreshDuration:
                    existing.remainingTurns = effect.nBaseDuration;
                    break;
                case StatusEffect.StackPolicy.Replace:
                    existing.remainingTurns = effect.nBaseDuration;
                    existing.stacks = 1;
                    break;
                case StatusEffect.StackPolicy.AddStack:
                    existing.stacks = Mathf.Clamp(existing.stacks + 1, 1, Mathf.Max(1, effect.nMaxStacks));
                    existing.remainingTurns = effect.nBaseDuration;
                    break;
            }
        }
        else
        {
            statusEffects.Add(new StatusEffectInstance(effect));
        }

        IsCalcedEffectiveStatus = false;
        RecalculateEffectiveStatus();
    }

    // 指定ステータスごとにバフデバフ倍率を計算
    private Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> BuffDebuffMultiplier()
    {
        Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> multipliers = new Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>>();
        foreach (StatusEffect.TargetType targetType in Enum.GetValues(typeof(StatusEffect.TargetType)))
        {
            multipliers[targetType] = new Dictionary<StatusEffect.EffectType, float>()
            {
                { StatusEffect.EffectType.Flat,0.0f },
                { StatusEffect.EffectType.Percent,0.0f }
            };
        }

        if (statusEffects == null) return multipliers;

        // StatusModifier のみ集計
        foreach (var inst in statusEffects)
        {
            if (inst?.def == null) continue;
            if (inst.def.eKind != StatusEffect.Kind.StatusModifier) continue;

            float value = inst.def.fEffectValue;
            int stacks = Mathf.Max(1, inst.stacks);

            switch (inst.def.eEffectType)
            {
                case StatusEffect.EffectType.Flat:
                    multipliers[inst.def.eTargetType][StatusEffect.EffectType.Flat] += value * stacks;
                    break;
                case StatusEffect.EffectType.Percent:
                    multipliers[inst.def.eTargetType][StatusEffect.EffectType.Percent] += value * stacks;
                    break;
            }
        }

        // パーセント値の合計値を最終倍率に変換（p = -1.0〜1.0 → 倍率 =0〜2）
        foreach (var targetType in multipliers.Keys)
        {
            float p = multipliers[targetType][StatusEffect.EffectType.Percent];
            multipliers[targetType][StatusEffect.EffectType.Percent] = 1.0f + p;
        }

        return multipliers;
    }

    // 指定ステータスタイプごとに、フラット値とパーセント値の倍率を適用して有効なステータスを計算
    private int CalcEffecticeStatus(int baseValue, StatusEffect.TargetType targetType, Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> multipliers)
    {
        float flat = multipliers[targetType][StatusEffect.EffectType.Flat];
        float percent = multipliers[targetType][StatusEffect.EffectType.Percent];
        return Mathf.RoundToInt((baseValue + flat) * percent);
    }

    // バフデバフ適用後ステータスを再計算（副作用：計算のみ）
    public void RecalculateEffectiveStatus()
    {
        if (IsCalcedEffectiveStatus) return;

        var multipliers = BuffDebuffMultiplier();
        nEffectiveMaxHp = CalcEffecticeStatus(nMaxHp, StatusEffect.TargetType.Hp, multipliers);
        nEffectiveMaxMp = CalcEffecticeStatus(nMaxMp, StatusEffect.TargetType.Mp, multipliers);
        nEffectivePhysicalAttack = CalcEffecticeStatus(nPhysicalAttack, StatusEffect.TargetType.PhysicalAttack, multipliers);
        nEffectiveMagicAttack = CalcEffecticeStatus(nMagicAttack, StatusEffect.TargetType.MagicAttack, multipliers);
        nEffectivePhysicalDefense = CalcEffecticeStatus(nPhysicalDefense, StatusEffect.TargetType.PhysicalDefense, multipliers);
        nEffectiveMagicDefense = CalcEffecticeStatus(nMagicDefense, StatusEffect.TargetType.MagicDefense, multipliers);
        nEffectiveSpeed = CalcEffecticeStatus(nSpeed, StatusEffect.TargetType.Speed, multipliers);

        IsCalcedEffectiveStatus = true;
    }

    // ======================
    // ターン経過処理（毒など）
    // ======================

    public void OnTurnStart()
    {
        TickStatusEffects(StatusEffect.TickTiming.OnTurnStart);
    }

    public void OnTurnEnd()
    {
        TickStatusEffects(StatusEffect.TickTiming.OnTurnEnd);

        // 継続ターン減算/解除
        DecrementAndRemoveExpiredEffects();

        // ステータス再計算
        IsCalcedEffectiveStatus = false;
        RecalculateEffectiveStatus();
    }

    private void TickStatusEffects(StatusEffect.TickTiming timing)
    {
        if (statusEffects == null || statusEffects.Count == 0) return;

        foreach (var inst in statusEffects)
        {
            if (inst?.def == null) continue;
            if (inst.def.eKind != StatusEffect.Kind.DamageOverTime) continue;
            if (inst.def.eTickTiming != timing) continue;

            int ticks = Mathf.Max(1, inst.stacks);
            int dmg = Mathf.Max(0, inst.def.nTickDamage) * ticks;
            if (dmg > 0) ApplyDamageDirect(dmg);
        }
    }

    private void DecrementAndRemoveExpiredEffects()
    {
        if (statusEffects == null) return;

        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            var inst = statusEffects[i];
            if (inst?.def == null)
            {
                statusEffects.RemoveAt(i);
                continue;
            }

            inst.remainingTurns--;
            if (inst.remainingTurns <= 0)
            {
                statusEffects.RemoveAt(i);
            }
        }
    }

    // ======================
    // ダメージ処理
    // ======================

    // ダメージ軽減率計算
    // ダメージ軽減率 = (防御力) / (防御力 +100)
    private float CalcDamageReduction(Damage.Type damageType)
    {
        // effective を使う（バフデバフ反映済み）
        RecalculateEffectiveStatus();

        switch (damageType)
        {
            case Damage.Type.Physical:
                return (float)nEffectivePhysicalDefense / (nEffectivePhysicalDefense + 100);
            case Damage.Type.Magical:
                return (float)nEffectiveMagicDefense / (nEffectiveMagicDefense + 100);
        }

        return 0.0f;
    }

    //直接HPを減らす（DoT等で利用）
    private void ApplyDamageDirect(int amount)
    {
        if (amount < 0) amount = 0;
        nHp -= amount;
        if (nHp < 0) nHp = 0;
    }

    // スキルによる被ダメ処理（このStatusが「受け手」）
    public void TakeDamage(Skill skill)
    {
        if (skill == null || skill.damage == null) return;

        float damageReduction = CalcDamageReduction(skill.damage.eDamageType);

        float elementMultiplier = 1.0f;
        if (skill.eMainElement != null)
        {
            elementMultiplier *= skill.eMainElement.CalcElementModfier(eElement.eElementType);
        }
        if (skill.eSubElement != null && skill.eSubElement.eElementType != Element.Type.None)
        {
            elementMultiplier *= skill.eSubElement.CalcElementModfier(eElement.eElementType);
        }

        int actualDamage = Mathf.RoundToInt(skill.damage.nDamageAmount * (1.0f - damageReduction) * elementMultiplier);
        if (actualDamage < 0) actualDamage = 0;

        ApplyDamageDirect(actualDamage);
    }

    //使う側（攻撃側）から呼ぶ簡易実行：ダメージ + 状態異常付与
    public void ExecuteSkillToTarget(Skill skill, Status target)
    {
        if (skill == null || target == null) return;

        // TODO: 消費MP等を入れるならここ

        // ダメージ
        target.TakeDamage(skill);

        //付与効果（毒/バフ/デバフ）
        if (skill.applyStatusEffects != null)
        {
            foreach (var e in skill.applyStatusEffects)
            {
                target.AddStatusEffect(e);
            }
        }
    }

    //※※※※※※※※※※※※
    // デバッグ用
    //※※※※※※※※※※※※

    [ContextMenu("Debug/体力とマナのリセット")]
    private void ResetHpMp()
    {
        nHp = nMaxHp;
        nMp = nMaxMp;
    }

    [ContextMenu("Debug/スキルの習得")]
    private void AddSkill()
    {
        // MasterySkillEditorを開いてスキルを選択し、選択したスキルをキャラクターに追加する
        MasterySkillEditor.Open(null, skill =>
        {
            if (skill != null)
            {
                if (skills == null) skills = new List<Skill>();
                skills.Add(skill);
                Debug.Log($"[Status] Added Skill: {skill.strName}");
            }
        });
    }

    [ContextMenu("Debug/ステータス効果の付与")]
    private void AddStatusEffectDebug()
    {
    }
}
