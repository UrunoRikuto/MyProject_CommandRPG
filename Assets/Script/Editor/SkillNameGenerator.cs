#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// スキル名生成ロジックを1か所にまとめたユーティリティ。
/// `SkillSOEditor` と `RandomSkillGenerator` の重複実装を避けるために用意。
/// 
/// 命名ルール（現状の要望反映）:
/// - ランク表記は付けない
/// - 基本は「属性漢字 +物/現象（熟語）」形式（例: 火柱 / 水刃）
/// - 毒は "漢字系" と "カタカナ系" を分ける
/// - 漢字系: 毒針 / 毒霧 / 毒牙 ...（毒という文字を含む熟語）
/// - カタカナ系: ポイズン/ヴェノム + ニードル/ミスト ...（毒文字なし）
/// - 「毒」の文字は poison属性がメイン or サブに含まれる場合のみ使用
/// </summary>
public static class SkillNameGenerator
{
 // -----------------
 // Public API
 // -----------------

 /// <summary>
 /// 説明文などで使う「正式な属性名(カタカナ)」を返す。
 /// </summary>
 public static string ElementToKatakana(Element.Type type)
 {
 switch (type)
 {
 case Element.Type.Normal: return "ノーマル";
 case Element.Type.Fire: return "ファイア";
 case Element.Type.Water: return "ウォーター";
 case Element.Type.Grass: return "グラス";
 case Element.Type.Earth: return "アース";
 case Element.Type.Lightning: return "ライトニング";
 case Element.Type.Wind: return "ウィンド";
 case Element.Type.Light: return "ライト";
 case Element.Type.Dark: return "ダーク";
 case Element.Type.Poison: return "ポイズン";
 case Element.Type.Curse: return "カース";
 default: return string.Empty;
 }
 }

 /// <summary>
 /// メイン/サブ属性と、DoT有無（＝毒効果の有無）からスキル名を生成する。
 /// </summary>
 public static string BuildName(Element.Type main, Element.Type sub, bool hasDot)
 {
 bool hasPoisonElement = main == Element.Type.Poison || sub == Element.Type.Poison;

 // 毒属性が絡まない場合は、毒関連の語彙を絶対に使わない
 if (!hasPoisonElement)
 {
 return BuildElementKanjiNameOrFallback(main);
 }

 // 毒属性が絡む場合：
 // - DoTがあるなら「毒っぽい」ので漢字系を優先
 // - DoTが無いならカタカナ系（ポイズン/ヴェノム）を優先
 if (hasDot)
 {
 return BuildPoisonKanjiName();
 }

 return BuildPoisonKatakanaName();
 }

 /// <summary>
 /// バフ/デバフ（StatusModifier）用のスキル名を生成する。
 /// 
 ///ルール:
 /// - 漢字は「鈍化の呪い」など “語句” として使用（単体漢字ランク等は無し）
 /// - 毒文字は poison 属性が絡む場合のみ（ここでは poison が絡む想定は低いが、将来拡張用に引数を残す）
 /// </summary>
 public static string BuildModifierSkillName(
 Element.Type main,
 Element.Type sub,
 StatusEffect.TargetType target,
 bool isBuff
 )
 {
 // 属性由来の接頭辞（非毒）
 string elemPrefix = BuildElementKanjiPrefixForModifier(main);

 // 対象ステータスに応じた語彙
 string statWord = TargetTypeToKanjiWord(target);

 if (isBuff)
 {
 //例: 火の鼓舞・攻 / 水の加護・防 /風の加速・速
 string buffWord = Pick("鼓舞", "加護", "祝福", "活性", "加速", "奮起");
 return string.IsNullOrEmpty(elemPrefix)
 ? $"{buffWord}{statWord}"
 : $"{elemPrefix}{buffWord}{statWord}";
 }
 else
 {
 //例: 闇の衰弱・攻 / 土の崩し・防 / 鈍化の呪い（速度低下）
 if (target == StatusEffect.TargetType.Speed)
 {
 // ユーザー指定例に寄せる
 return "鈍化の呪い";
 }

 string debuffWord = Pick("衰弱", "呪縛", "弱体", "封殺", "崩し", "腐食");
 return string.IsNullOrEmpty(elemPrefix)
 ? $"{debuffWord}{statWord}"
 : $"{elemPrefix}{debuffWord}{statWord}";
 }
 }

 // -----------------
 // Poison naming
 // -----------------

 private static string BuildPoisonKanjiName()
 {
 // 毒という文字は熟語としてのみ使用（単体で使わない）
 string poisonKanji = Pick("毒針", "毒霧", "毒牙", "毒沼", "毒鎖", "毒刃");
 string core = Pick("針", "霧", "牙", "沼", "鎖", "刃");

 //（例: 毒針針などの重複を避ける）
 if (poisonKanji.EndsWith(core))
 {
 core = Pick("槍", "弾", "花", "棘");
 }

 return $"{poisonKanji}{core}";
 }

 private static string BuildPoisonKatakanaName()
 {
 // 毒文字を使わないカタカナ系
 string prefix = Pick("ポイズン", "ヴェノム");
 string core = Pick("ニードル", "ミスト", "ブレス", "スモーク", "バイト", "スパイク");
 return $"{prefix}{core}";
 }

 // -----------------
 // Non-poison naming
 // -----------------

 private static string BuildElementKanjiNameOrFallback(Element.Type main)
 {
 string kanjiPrefix = ElementToKanjiPrefix(main);
 string kanjiNoun = PickKanjiNoun(main);

 if (!string.IsNullOrEmpty(kanjiPrefix) && !string.IsNullOrEmpty(kanjiNoun))
 {
 return $"{kanjiPrefix}{kanjiNoun}";
 }

 // フォールバック（漢字が無い属性など）
 return $"{ElementToShortKatakana(main)}{PickCoreName(main)}";
 }

 private static string ElementToKanjiPrefix(Element.Type type)
 {
 switch (type)
 {
 case Element.Type.Fire: return "火";
 case Element.Type.Water: return "水";
 case Element.Type.Grass: return "草";
 case Element.Type.Earth: return "土";
 case Element.Type.Lightning: return "雷";
 case Element.Type.Wind: return "風";
 case Element.Type.Light: return "光";
 case Element.Type.Dark: return "闇";
 default: return string.Empty;
 }
 }

 private static string BuildElementKanjiPrefixForModifier(Element.Type type)
 {
 // 「火柱」等とは違い、補助スキルは「火の～」の方が自然なので “の” を付ける
 switch (type)
 {
 case Element.Type.Fire: return "火の";
 case Element.Type.Water: return "水の";
 case Element.Type.Grass: return "草の";
 case Element.Type.Earth: return "土の";
 case Element.Type.Lightning: return "雷の";
 case Element.Type.Wind: return "風の";
 case Element.Type.Light: return "光の";
 case Element.Type.Dark: return "闇の";
 default: return string.Empty;
 }
 }

 private static string TargetTypeToKanjiWord(StatusEffect.TargetType target)
 {
 switch (target)
 {
 case StatusEffect.TargetType.Hp: return "体";
 case StatusEffect.TargetType.Mp: return "魔";
 case StatusEffect.TargetType.PhysicalAttack: return "攻";
 case StatusEffect.TargetType.MagicAttack: return "術";
 case StatusEffect.TargetType.PhysicalDefense: return "防";
 case StatusEffect.TargetType.MagicDefense: return "護";
 case StatusEffect.TargetType.Speed: return "速";
 default: return string.Empty;
 }
 }

 private static string PickKanjiNoun(Element.Type type)
 {
 switch (type)
 {
 case Element.Type.Fire:
 return Pick("柱", "刃", "槍", "弾", "輪", "壁", "雨");
 case Element.Type.Water:
 return Pick("刃", "槍", "泡", "渦", "波", "壁", "雨");
 case Element.Type.Grass:
 return Pick("蔦", "刃", "棘", "槍", "花", "壁");
 case Element.Type.Earth:
 return Pick("刃", "槍", "柱", "壁", "礫", "鎧");
 case Element.Type.Lightning:
 return Pick("槍", "刃", "弾", "鎖", "柱");
 case Element.Type.Wind:
 return Pick("刃", "槍", "輪", "壁", "嵐");
 case Element.Type.Light:
 return Pick("輪", "槍", "刃", "柱", "壁", "矢");
 case Element.Type.Dark:
 return Pick("鎌", "槍", "刃", "鎖", "影", "穴");
 default:
 return string.Empty;
 }
 }

 // -----------------
 // Fallback katakana
 // -----------------

 private static string ElementToShortKatakana(Element.Type type)
 {
 switch (type)
 {
 case Element.Type.Fire: return "フレイム";
 case Element.Type.Water: return "アクア";
 case Element.Type.Grass: return "リーフ";
 case Element.Type.Earth: return "ガイア";
 case Element.Type.Lightning: return "サンダー";
 case Element.Type.Wind: return "ウィンド";
 case Element.Type.Light: return "ルミナ";
 case Element.Type.Dark: return "シャドウ";
 case Element.Type.Poison: return "ヴェノム";
 case Element.Type.Curse: return "ヘックス";
 case Element.Type.Normal: return "ノーマル";
 default: return string.Empty;
 }
 }

 private static string PickCoreName(Element.Type main)
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
 case Element.Type.Curse: return Pick("ヘックス", "マリス", "バインド", "ドゥーム", "リチュアル");
 default: return Pick("ショット", "スラッシュ", "ブレイク", "バッシュ", "スマッシュ");
 }
 }

 private static string Pick(params string[] words)
 {
 if (words == null || words.Length ==0) return string.Empty;
 return words[UnityEngine.Random.Range(0, words.Length)];
 }
}
#endif
