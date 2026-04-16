// バフやデバフなどのステータス効果を表すクラス
public class StatusEffect
{
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
    public TargetType eTargetType; // 効果の対象となるステータス

    public enum EffectType
    {
        Flat,   // 固定値で増減
        Percent // パーセンテージで増減
    }
    public EffectType eEffectType; // 効果の増減の種類

    // 最大値：1.0f(100%)、最小値：-1.0f(-100%)
    public float fEffectValue; // 効果の値

    // 継続ターン数
    public int nDuration; // 効果の継続ターン数
}