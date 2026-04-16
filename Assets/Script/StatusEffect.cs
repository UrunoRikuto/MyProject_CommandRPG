using System;
using UnityEngine;

// バフ・デバフ・状態異常などの「効果定義」
// ScriptableObject や Skillから参照される想定のため、ここ自体には残りターン等のランタイム状態を持たせない。
[Serializable]
public class StatusEffect
{
    [SerializeField]
    [Header("効果名")]
    public string sName;

    public string Name => sName;

    public enum Kind
    {
        StatusModifier,     // ステータスに対する加算/割合
        DamageOverTime,     // 毒など：ターンごとにダメージ
    }

    [Header("効果の種類")]
    public Kind eKind = Kind.StatusModifier;

    // 効果の対象となるステータスの種類を表す列挙型
    public enum TargetType
    {
        Hp,             // 体力
        Mp,             // マナ
        PhysicalAttack, // 物理攻撃力
        MagicAttack,    // 魔法攻撃力
        PhysicalDefense,// 物理防御力
        MagicDefense,   // 魔法防御力
        Speed,          // 素早さ
    }

    [Header("(StatModifier) 対象ステータス")]
    public TargetType eTargetType;

    public enum EffectType
    {
        Flat,   // 固定値
        Percent // 割合（-1.0〜1.0 を想定)
    }

    [Header("(StatModifier)変化の種類")]
    public EffectType eEffectType;

    [Header("(StatModifier)変化量 (最大:1.0f(100%)、最小: -1.0f(-100%))")]
    [Range(-1.0f, 1.0f)]
    public float fEffectValue;

    public enum TickTiming// ダメージオーバータイムのダメージ発生タイミング
    {
        OnTurnStart,    // ターン開始時
        OnTurnEnd,      // ターン終了時
    }

    [Header("(DamageOverTime) 発動タイミング")]
    public TickTiming eTickTiming = TickTiming.OnTurnEnd;

    [Header("(DamageOverTime)1ターンごとの固定ダメージ")]
    [Min(0)]
    public int nTickDamage = 0;

    public enum StackPolicy
    {
        RefreshDuration, // 同名付与で残りターンを更新
        AddStack,        // スタック加算（ダメージ/効果を加算)
        Replace,         // 上書き
    }

    [Header("重複ルール")]
    public StackPolicy eStackPolicy = StackPolicy.RefreshDuration;

    [Header("最大スタック(0や1なら実質スタックなし)")]
    [Min(1)]
    public int nMaxStacks = 1;

    [Header("基本継続ターン数")]
    [Min(1)]
    public int nBaseDuration = 1;
}

// ランタイム用：キャラクターに付与された効果の状態（残りターン、スタック）
[Serializable]
public class StatusEffectInstance
{
    public StatusEffect def;

    [Min(0)]
    public int remainingTurns;

    [Min(1)]
    public int stacks = 1;

    public StatusEffectInstance(StatusEffect def)
    {
        this.def = def;
        remainingTurns = def != null ? def.nBaseDuration : 0;
        stacks = 1;
    }
}

[CreateAssetMenu(fileName = "New StatusEffect", menuName = "ScriptableObjects/StatusEffect")]
public class StatusEffectSO : ScriptableObject
{
    public StatusEffect[] statusEffects;
}