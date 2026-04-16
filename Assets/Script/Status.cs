using System;
using System.Collections.Generic;
using UnityEngine;

// ステータスに関するクラス
public class Status : MonoBehaviour
{
    // 体力
    private int nHp;             // 現在の体力
    private int nMaxHp;          // 最大体力

    // マナ(スキルコスト)
    private int nMp;             // 現在のマナ
    private int nMaxMp;          // 最大マナ

    // 攻撃力
    private int nPhysicalAttack; // 物理攻撃力
    private int nMagicAttack;    // 魔法攻撃力

    // 防御力(ダメージ軽減率)
    private int nPhysicalDefense;// 物理防御力
    private int nMagicDefense;   // 魔法防御力

    // 素早さ(行動順)
    private int nSpeed;          // 素早さ

    // 属性
    private Element eElement;    // 属性(装備品やスキルによって変化する可能性がある)

    // 所持スキル
    [SerializeField]
    private List<Skill> skills; // キャラクターが習得しているスキルのリスト

    // ステータス効果
    private List<StatusEffect> statusEffects; // バフやデバフなどのステータス効果のリスト
    private bool IsCalcedEffectiveStatus;            // バフデバフ適応後のステータスが計算されているかどうか(ターンごとに計算する)
    //--- ステータス効果適応済み変数 ---//
    /* バフデバフ適応後の最大体力   */[NonSerialized] public int nEffectiveMaxHp;
    /* バフデバフ適応後の最大マナ   */[NonSerialized] public int nEffectiveMaxMp;
    /* バフデバフ適応後の物理攻撃力 */[NonSerialized] public int nEffectivePhysicalAttack;
    /* バフデバフ適応後の魔法攻撃力 */[NonSerialized] public int nEffectiveMagicAttack;
    /* バフデバフ適応後の物理防御力 */[NonSerialized] public int nEffectivePhysicalDefense;
    /* バフデバフ適応後の魔法防御力 */[NonSerialized] public int nEffectiveMagicDefense;
    /* バフデバフ適応後の素早さ     */[NonSerialized] public int nEffectiveSpeed;             


    // 指定ステータスごとにバフデバフ倍率を計算
    private Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType,float>> BuffDebuffMultiplier()
    {
        // 効果の種類ごとに倍率を格納する辞書を初期化
        Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> multipliers = new Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>>();
        foreach (StatusEffect.TargetType targetType in System.Enum.GetValues(typeof(StatusEffect.TargetType)))
        {
            multipliers[targetType] = new Dictionary<StatusEffect.EffectType, float>()
            {
                { StatusEffect.EffectType.Flat, 0.0f },
                { StatusEffect.EffectType.Percent, 0.0f }
            };
        }

        // ステータス効果のリストをループして、指定された効果タイプに一致するものを見つける
        foreach (var effect in statusEffects)
        {
            switch (effect.eEffectType) // 効果の種類に応じて倍率を計算
            {
                case StatusEffect.EffectType.Flat: // フラット値の場合はそのまま加算
                    multipliers[effect.eTargetType][StatusEffect.EffectType.Flat] += effect.fEffectValue;
                    break;
                case StatusEffect.EffectType.Percent: // パーセント値の場合は割合として加算
                    multipliers[effect.eTargetType][StatusEffect.EffectType.Percent] += effect.fEffectValue;
                    break;
            }
        }

        // パーセント値の合計値を最終倍率に変換（p = -1.0〜1.0 → 倍率 = 0〜2）
        foreach (var targetType in multipliers.Keys)
        {
            float p = multipliers[targetType][StatusEffect.EffectType.Percent];
            float percentMultiplier = 1.0f + p;
            multipliers[targetType][StatusEffect.EffectType.Percent] = percentMultiplier;
        }

        return multipliers; // 計算された倍率の辞書を返す
    }

    // 指定ステータスタイプごとに、フラット値とパーセント値の倍率を適用して有効なステータスを計算
    private int CalcEffecticeStatus(int baseValue, StatusEffect.TargetType targetType, Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> multipliers)
    {
        // フラット値
        float flatMultiplier = multipliers[targetType][StatusEffect.EffectType.Flat];
        // パーセント倍率
        float percentMultiplier = multipliers[targetType][StatusEffect.EffectType.Percent];

        // 基本値にフラット値を加算し、さらにパーセント倍率を掛けて最終的なステータスを計算
        return Mathf.RoundToInt((baseValue + flatMultiplier) * percentMultiplier);
    }

    // バフデバフによるステータスの変化を更新する処理
    public void UpdateStatusEffects()
    {
        // バフデバフが存在しない場合
        if (statusEffects == null || statusEffects.Count == 0) return; // 処理を終了

        // 計算済みかどうかをチェック
        if (IsCalcedEffectiveStatus) return; // すでに計算されている場合は処理を終了

        // ステータスごとにバフデバフ倍率を計算
        Dictionary<StatusEffect.TargetType, Dictionary<StatusEffect.EffectType, float>> multipliers =　BuffDebuffMultiplier();
        // 各ステータスに対して、フラット値とパーセント値の倍率を適用して有効なステータスを計算
        nEffectiveMaxHp = CalcEffecticeStatus(nMaxHp, StatusEffect.TargetType.Hp, multipliers);
        nEffectiveMaxMp = CalcEffecticeStatus(nMaxMp, StatusEffect.TargetType.Mp, multipliers);
        nEffectivePhysicalAttack = CalcEffecticeStatus(nPhysicalAttack, StatusEffect.TargetType.PhysicalAttack, multipliers);
        nEffectiveMagicAttack = CalcEffecticeStatus(nMagicAttack, StatusEffect.TargetType.MagicAttack, multipliers);
        nEffectivePhysicalDefense = CalcEffecticeStatus(nPhysicalDefense, StatusEffect.TargetType.PhysicalDefense, multipliers);
        nEffectiveMagicDefense = CalcEffecticeStatus(nMagicDefense, StatusEffect.TargetType.MagicDefense, multipliers);
        nEffectiveSpeed = CalcEffecticeStatus(nSpeed, StatusEffect.TargetType.Speed, multipliers);

        // ステータス効果のリストをループして、継続ターン数を減らす
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            statusEffects[i].nDuration--; // 継続ターン数を減らす
            if (statusEffects[i].nDuration <= 0) // 継続ターン数が0以下になった場合は効果を解除
            {
                statusEffects.RemoveAt(i); // 効果をリストから削除
            }
        }

        IsCalcedEffectiveStatus = true; // バフデバフ適応後のステータスが計算されたことを示すフラグを立てる
    }

    // ダメージ軽減率計算
    // ダメージ軽減率 = (防御力) / (防御力 + 100)
    // 最大値1.0f、最小値0.0f
    private float CalcDamageReduction(Damage.Type damageType)
    {
        switch(damageType)// ダメージの種類に応じて軽減率を計算
        {
            case Damage.Type.Physical:// 物理ダメージの軽減率を計算
                return (float)nPhysicalDefense / (nPhysicalDefense + 100);
            case Damage.Type.Magical: // 魔法ダメージの軽減率を計算
                return (float)nMagicDefense / (nMagicDefense + 100);
        }

        return 0.0f;// 軽減率0%
    }

    // ダメージを受ける処理
    // 引数: ダメージの情報
    public void TakeDamage(Skill skill)
    {
        // ダメージ軽減率を計算
        float damageReduction = CalcDamageReduction(skill.damage.eDamageType);
        // メイン属性によるダメージ倍率を計算
        float elementMultiplier = skill.eMainElement.CalcElementModfier(eElement.eElementType);
        // サブ属性がある場合はサブ属性によるダメージ倍率も計算
        if (skill.eSubElement.eElementType != Element.Type.None)
        {
            // サブ属性によるダメージ倍率を計算
            float subElementMultiplier = skill.eSubElement.CalcElementModfier(eElement.eElementType);

            // メイン属性とサブ属性のダメージ倍率を掛け合わせる
            elementMultiplier *= subElementMultiplier;
        }

        // 実際のダメージ量を計算
        int actualDamage = Mathf.RoundToInt(skill.damage.nDamageAmount * (1.0f - damageReduction) * elementMultiplier);

        // ダメージ量が0未満にならないようにする
        if (actualDamage < 0) actualDamage = 0;

        // 体力を減らす
        nHp -= actualDamage;
        // 体力が0以下になった場合は0にする
        if (nHp < 0) nHp = 0;
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
    private void AddStatusEffect()
    {

    }
}
