#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RandomSkillGenerator
{
    private const string DefaultSkillDbAssetPath = "Assets/Data/SkillDB.asset";

    [MenuItem("Tools/Skill/ランダム自動生成")]
    private static void GenerateRandomSkillMenu()
    {
        var db = AssetDatabase.LoadAssetAtPath<SkillSO>(DefaultSkillDbAssetPath);
        if (db == null)
        {
            Debug.LogError($"[RandomSkillGenerator] SkillDB not found: {DefaultSkillDbAssetPath}");
            return;
        }

        AddRandomSkillTo(db);
    }

    public static void AddRandomSkillTo(SkillSO db)
    {
        if (db == null)
        {
            Debug.LogError("[RandomSkillGenerator] SkillSO is null");
            return;
        }

        var newSkill = CreateRandomSkill();

        var list = new List<Skill>(db.skill ?? Array.Empty<Skill>());
        list.Add(newSkill);
        db.skill = list.ToArray();

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[RandomSkillGenerator] Added Skill: {newSkill.strName} (MP:{newSkill.nManaCost}, Dmg:{newSkill.damage?.nDamageAmount})");

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
    }

    private static Skill CreateRandomSkill()
    {
        var mainElement = GetRandomElementType(includeNone: false);
        var subElement = UnityEngine.Random.value < 0.35f ? GetRandomElementType(includeNone: false) : Element.Type.None;

        var dmgType = (Damage.Type)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Damage.Type)).Length);
        var dmgAmount = UnityEngine.Random.Range(5, 51);
        var mp = Mathf.Clamp(dmgAmount + UnityEngine.Random.Range(0, 21), 1, 100);

        // =========================
        // スキル種別決定
        // =========================
        int kindRoll = UnityEngine.Random.Range(0, 100);
        bool wantsModifier = kindRoll < 25; //25% を補助系にする
        bool wantsDot = !wantsModifier && UnityEngine.Random.value < 0.30f;

        StatusEffect[] effects = null;

        if (wantsModifier)
        {
            bool isBuff = UnityEngine.Random.value < 0.55f;
            var target = (StatusEffect.TargetType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(StatusEffect.TargetType)).Length);
            if (!isBuff && UnityEngine.Random.value < 0.35f) target = StatusEffect.TargetType.Speed;

            effects = new[] { CreateModifierEffect(isBuff, target) };

            dmgAmount = 0;
            mp = UnityEngine.Random.Range(3, 16);

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

        //低確率で毒(DoT)を付与
        if (wantsDot)
        {
            effects = new[] { CreatePoisonEffect() };

            // 要望: DoTがある場合は、メインor副属性に必ず毒属性を含める
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
            damage = new Damage
            {
                eDamageType = dmgType,
                nDamageAmount = dmgAmount,
            },
            eMainElement = new Element { eElementType = mainElement },
            eSubElement = new Element { eElementType = subElement },
            applyStatusEffects = effects,
        };
    }

    private static StatusEffect CreateModifierEffect(bool isBuff, StatusEffect.TargetType target)
    {
        int duration = UnityEngine.Random.Range(2, 5);
        float v = UnityEngine.Random.Range(0.10f, 0.30f);
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
            nMaxStacks = 1,
        };
    }

    private static StatusEffect CreatePoisonEffect()
    {
        int duration = UnityEngine.Random.Range(2, 5);
        int tick = UnityEngine.Random.Range(3, 11);

        // StatusEffect は「定義」であり、DoT(毒)の場合は Kind=DamageOverTime を使う。
        // 実際のダメージ適用は `Status` 側が `nTickDamage` を参照して行う。
        return new StatusEffect
        {
            sName = "毒",
            eKind = StatusEffect.Kind.DamageOverTime,
            eTickTiming = StatusEffect.TickTiming.OnTurnEnd,
            nTickDamage = tick,
            nBaseDuration = duration,
            eStackPolicy = StatusEffect.StackPolicy.RefreshDuration,
            nMaxStacks = 1,
        };
    }

    private static Element.Type GetRandomElementType(bool includeNone)
    {
        var values = Enum.GetValues(typeof(Element.Type)).Cast<Element.Type>().ToArray();
        if (!includeNone)
        {
            values = values.Where(v => v != Element.Type.None).ToArray();
        }
        return values[UnityEngine.Random.Range(0, values.Length)];
    }

    private static string BuildDescription(Element.Type main, Element.Type sub, Damage.Type dmgType, int amount, StatusEffect[] effects)
    {
        // 名前がカタカナ寄りなので、説明も英語を混ぜない
        string line1 = $"{SkillNameGenerator.ElementToKatakana(main)}属性の{(dmgType == Damage.Type.Magical ? "魔法" : "攻撃")}。威力:{amount}";

        string line2 = string.Empty;
        if (sub != Element.Type.None)
        {
            line2 = $"副属性:{SkillNameGenerator.ElementToKatakana(sub)}";
        }

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
        int pct = Mathf.RoundToInt(Mathf.Abs(effect.fEffectValue) * 100);
        string line3 = $"{stat}{sign}:{pct}% / {effect.nBaseDuration}T";

        return string.Join("\n", new[] { line1, line2, line3 }.Where(s => !string.IsNullOrEmpty(s)));
    }

    //文字列候補からランダムに1つ選ぶユーティリティ
    private static string Pick(params string[] words)
    {
        if (words == null || words.Length == 0) return string.Empty;
        return words[UnityEngine.Random.Range(0, words.Length)];
    }

    // 属性ごとのカタカナ系コア（フォールバック時に使用）
    private static string PickCoreName(Element.Type main, Damage.Type dmgType)
    {
        switch (main)
        {
            case Element.Type.Fire: return Pick("バースト", "ランス", "ボルト", "ストーム", "ノヴァ");
            case Element.Type.Water: return Pick("スピア", "カッター", "バブル", "ストリーム", "テンペスト");
            case Element.Type.Grass: return Pick("ソーン", "ブレード", "ヴァイン", "リーフ", "スラッシュ");
            case Element.Type.Earth: return Pick("クエイク", "スパイク", "クラッシュ", "バスター", "グラウンド");
            case Element.Type.Lightning: return Pick("ボルト", "スパーク", "レイ", "サンダー", "バースト");
            case Element.Type.Wind: return Pick("ゲイル", "カッター", "トルネード", "ストーム", "スラッシュ");
            case Element.Type.Light: return Pick("レイ", "フレア", "ジャッジ", "プリズム", "セイバー");
            case Element.Type.Dark: return Pick("アビス", "シャドウ", "リッパー", "ナイト", "カース");
            case Element.Type.Poison: return Pick("ニードル", "ミスト", "ブレス", "スモーク", "バイト", "スパイク");
            case Element.Type.Curse: return Pick("ヘックス", "マリス", "バインド", "ドゥーム", "リチュアル");
            default: return Pick("ショット", "スラッシュ", "ブレイク", "バッシュ", "スマッシュ");
        }
    }
}
#endif
