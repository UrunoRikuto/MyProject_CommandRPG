#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillSO))]
public sealed class SkillSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("ランダム生成", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1追加", GUILayout.Height(26)))
                {
                    AddRandomSkillTo((SkillSO)target, 1);
                }
                if (GUILayout.Button("+5追加", GUILayout.Height(26)))
                {
                    AddRandomSkillTo((SkillSO)target, 5);
                }
            }

            EditorGUILayout.HelpBox("この SkillDB にランダムスキルを追加します。\n※Undoは未対応です。", MessageType.Info);
        }
    }

    private static void AddRandomSkillTo(SkillSO db, int count)
    {
        if (db == null) return;

        var list = new List<Skill>(db.skill ?? Array.Empty<Skill>());
        for (int i = 0; i < Mathf.Max(1, count); i++)
        {
            list.Add(CreateRandomSkill());
        }
        db.skill = list.ToArray();

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(db);
    }

    private static Skill CreateRandomSkill()
    {
        var mainElement = GetRandomElementType(includeNone: false);
        var subElement = UnityEngine.Random.value <0.35f ? GetRandomElementType(includeNone: false) : Element.Type.None;

        var dmgType = (Damage.Type)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Damage.Type)).Length);
        var dmgAmount = UnityEngine.Random.Range(5,51);
        var mp = Mathf.Clamp(dmgAmount + UnityEngine.Random.Range(0,21),1,100);

        // =========================
        // スキル種別決定
        // =========================
        //0:攻撃1:毒(DoT)2:バフ/デバフ
        int kindRoll = UnityEngine.Random.Range(0,100);
        bool wantsModifier = kindRoll <25; //25% を補助系にする
        bool wantsDot = !wantsModifier && UnityEngine.Random.value <0.30f;

        StatusEffect[] effects = null;

        if (wantsModifier)
        {
            // バフ/デバフ用のStatusEffectを生成
            bool isBuff = UnityEngine.Random.value <0.55f;
            var target = (StatusEffect.TargetType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(StatusEffect.TargetType)).Length);

            // Speedデバフは「鈍化の呪い」を優先したいので、デバフならSpeedを引きやすくする
            if (!isBuff && UnityEngine.Random.value <0.35f) target = StatusEffect.TargetType.Speed;

            effects = new[] { CreateModifierEffect(isBuff, target) };

            // 補助スキルはダメージをほぼ持たせない
            dmgAmount =0;
            mp = UnityEngine.Random.Range(3,16);

            // 名前は補助用ジェネレータ
            var name = SkillNameGenerator.BuildModifierSkillName(mainElement, subElement, target, isBuff);
            var desc = BuildModifierDescription(mainElement, subElement, isBuff, target, effects[0]);

            return new Skill
            {
                strName = name,
                strDescription = desc,
                nManaCost = mp,
                damage = new Damage { eDamageType = dmgType, nDamageAmount = dmgAmount },
                eMainElement = new Element { eElementType = mainElement },
                eSubElement = new Element { eElementType = subElement },
                applyStatusEffects = effects,
            };
        }

        if (wantsDot)
        {
            effects = new[] { CreatePoisonEffect() };

            // 要望: DoT がある場合は、メイン or 副属性に必ず毒属性を含める
            if (mainElement != Element.Type.Poison && subElement != Element.Type.Poison)
            {
                subElement = Element.Type.Poison;
            }
        }

        // 名前生成は共通ユーティリティに委譲
        bool hasDot = effects != null && effects.Any(e => e != null && e.eKind == StatusEffect.Kind.DamageOverTime);
        var nameAttack = SkillNameGenerator.BuildName(mainElement, subElement, hasDot);

        var descAttack = BuildDescription(mainElement, subElement, dmgType, dmgAmount, effects);

        return new Skill
        {
            strName = nameAttack,
            strDescription = descAttack,
            nManaCost = mp,
            damage = new Damage { eDamageType = dmgType, nDamageAmount = dmgAmount },
            eMainElement = new Element { eElementType = mainElement },
            eSubElement = new Element { eElementType = subElement },
            applyStatusEffects = effects,
        };
    }

    private static StatusEffect CreateModifierEffect(bool isBuff, StatusEffect.TargetType target)
    {
        // バフ/デバフの基本継続
        int duration = UnityEngine.Random.Range(2,5);

        // Percent のみ使う（-1.0?1.0）
        // バフ:+0.10?+0.30 / デバフ:-0.10?-0.30
        float v = UnityEngine.Random.Range(0.10f,0.30f);
        if (!isBuff) v = -v;

        return new StatusEffect
        {
            sName = isBuff ? "バフ" : "デバフ",
            eKind = StatusEffect.Kind.StatusModifier,
            eTargetType = target,
            eEffectType = StatusEffect.EffectType.Percent,
            fEffectValue = v,
            nBaseDuration = duration,
            eStackPolicy = StatusEffect.StackPolicy.RefreshDuration,
            nMaxStacks =1,
        };
    }

    private static StatusEffect CreatePoisonEffect()
    {
        int duration = UnityEngine.Random.Range(2,5);
        int tick = UnityEngine.Random.Range(3,11);
        return new StatusEffect
        {
            sName = "毒",
            eKind = StatusEffect.Kind.DamageOverTime,
            eTickTiming = StatusEffect.TickTiming.OnTurnEnd,
            nTickDamage = tick,
            nBaseDuration = duration,
            eStackPolicy = StatusEffect.StackPolicy.RefreshDuration,
            nMaxStacks =1,
        };
    }

    private static Element.Type GetRandomElementType(bool includeNone)
    {
        var values = Enum.GetValues(typeof(Element.Type)).Cast<Element.Type>().ToArray();
        if (!includeNone) values = values.Where(v => v != Element.Type.None).ToArray();
        return values[UnityEngine.Random.Range(0, values.Length)];
    }

    private static string BuildDescription(Element.Type main, Element.Type sub, Damage.Type dmgType, int amount, StatusEffect[] effects)
    {
        string line1 = $"{SkillNameGenerator.ElementToKatakana(main)}属性の{(dmgType == Damage.Type.Magical ? "魔法" : "攻撃")}。威力:{amount}";
        string line2 = sub != Element.Type.None ? $"副属性:{SkillNameGenerator.ElementToKatakana(sub)}" : string.Empty;

        string line3 = string.Empty;
        var dot = effects?.FirstOrDefault(e => e != null && e.eKind == StatusEffect.Kind.DamageOverTime);
        if (dot != null)
        {
            var when = dot.eTickTiming == StatusEffect.TickTiming.OnTurnStart ? "開始" : "終了";
            line3 = $"毒付与:{dot.nBaseDuration}T / {dot.nTickDamage}ダメージ（ターン{when}時）";
        }

        return string.Join("\n", new[] { line1, line2, line3 }.Where(s => !string.IsNullOrEmpty(s)));
    }

    private static string BuildModifierDescription(Element.Type main, Element.Type sub, bool isBuff, StatusEffect.TargetType target, StatusEffect effect)
    {
        string line1 = $"{SkillNameGenerator.ElementToKatakana(main)}属性の補助。";
        string line2 = sub != Element.Type.None ? $"副属性:{SkillNameGenerator.ElementToKatakana(sub)}" : string.Empty;

        string stat = target.ToString();
        string sign = isBuff ? "上昇" : "低下";
        int pct = Mathf.RoundToInt(Mathf.Abs(effect.fEffectValue) *100);
        string line3 = $"{stat}{sign}:{pct}% / {effect.nBaseDuration}T";

        return string.Join("\n", new[] { line1, line2, line3 }.Where(s => !string.IsNullOrEmpty(s)));
    }
}
#endif
