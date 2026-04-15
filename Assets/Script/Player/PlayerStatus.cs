using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class Element
{
    public enum Type
    {
        None,       // ‚È‚µ
        Normal,     // –³
        Fire,       // ‰Î
        Water,      // …
        Grass,      // ‘
        Earth,      // “y
        Lightning,  // —‹
        Wind,       // •—
        Light,      // Œõ
        Dark,       // ˆÅ
        Poison,     // “Å
        Curse,      // ô
    }
    public Type eElementType = Type.None; // ‘®«‚Ìí—Ş


    // ‘®«‘Š«‚Ì”{—¦•\(UŒ‚->–hŒä)
    // ‰Î|[‘:2.0”{][•—:2.0”{][…:0.5”{][“y:0.5”{]
    // …|[‰Î:2.0”{][“y:2.0”{][‘:0.5”{][—‹:0.5”{]
    // ‘|[…:2.0”{][—‹:2.0”{][‰Î:0.5”{][•—:0.5”{]
    // “y|[—‹:2.0”{][•—:2.0”{][…:0.5”{][‘:0.5”{]
    // —‹|[…:2.0”{][•—:2.0”{][“y:0.5”{][‘:0.5”{]
    // •—|[‰Î:2.0”{][‘:2.0”{][—‹:0.5”{][“y:0.5”{]
    // Œõ|[ˆÅ:2.0”{]
    // ˆÅ|[Œõ:2.0”{]
    // “Å|”{—¦•â³‚È‚µ
    // ô|”{—¦•â³‚È‚µ
    static float WeakModfier = 2.0f; // ã“_”{—¦
    static float NomalModfier = 1.0f; // ’Êí”{—¦
    static float ResistModfier = 0.5f; // ‘Ï«”{—¦
    private static readonly Dictionary<(Type atk, Type def), float> elementTable = new Dictionary<(Type atk, Type def), float>
    {
        // ‰Î
        {(Type.Fire, Type.Grass), WeakModfier},
        {(Type.Fire, Type.Wind), WeakModfier},
        {(Type.Fire, Type.Water), ResistModfier},
        {(Type.Fire, Type.Earth), ResistModfier},

        // …
        {(Type.Water, Type.Fire), WeakModfier},
        {(Type.Water, Type.Earth), WeakModfier},
        {(Type.Water, Type.Grass), ResistModfier},
        {(Type.Water, Type.Lightning), ResistModfier},

        // ‘
        {(Type.Grass, Type.Water), WeakModfier},
        {(Type.Grass, Type.Lightning), WeakModfier},
        {(Type.Grass, Type.Fire), ResistModfier},
        {(Type.Grass, Type.Wind), ResistModfier},

        // “y
        {(Type.Earth, Type.Lightning), WeakModfier},
        {(Type.Earth, Type.Wind), WeakModfier},
        {(Type.Earth, Type.Water), ResistModfier},
        {(Type.Earth, Type.Grass), ResistModfier},

        // —‹
        {(Type.Lightning, Type.Water), WeakModfier},
        {(Type.Lightning, Type.Wind), WeakModfier},
        {(Type.Lightning, Type.Earth), ResistModfier},
        {(Type.Lightning, Type.Grass), ResistModfier},

        // •—
        {(Type.Wind, Type.Fire), WeakModfier},
        {(Type.Wind, Type.Grass), WeakModfier},
        {(Type.Wind, Type.Lightning), ResistModfier},
        {(Type.Wind, Type.Earth), ResistModfier},

        // ŒõEˆÅ
        {(Type.Light, Type.Dark), WeakModfier},
        {(Type.Dark, Type.Light), WeakModfier},
    };

    // ‘®«‘Š«‚É‚æ‚éƒ_ƒ[ƒW•â³
    // –ß‚è’lF2.0fA1.0fA0.5f
    public float CalcElementModfier(Element.Type targetType)
    {
        if (elementTable.TryGetValue((this.eElementType, targetType), out float value))
        {
            return value; // •â³’l‚ğ•Ô‚·
        }

        return NomalModfier; // •â³‚È‚µ
    }
}

struct Damage
{
    enum Type
    {
        Physical,   // •¨—ƒ_ƒ[ƒW
        Magical     // –‚–@ƒ_ƒ[ƒW
    }
    Type eDamageType; // ƒ_ƒ[ƒW‚Ìí—Ş

    int nDamageAmount; // ƒ_ƒ[ƒW—Ê
}

/// <summary>
/// ƒvƒŒƒCƒ„[‚ÌƒXƒe[ƒ^ƒX‚ğŠÇ—‚·‚éƒNƒ‰ƒX
/// </summary>
public class PlayerStatus
{
    // ‘Ì—Í
    public int nHp;             // Œ»İ‚Ì‘Ì—Í
    public int nMaxHp;          // Å‘å‘Ì—Í

    // ƒ}ƒi(ƒXƒLƒ‹ƒRƒXƒg)
    public int nMp;             // Œ»İ‚Ìƒ}ƒi
    public int nMaxMp;          // Å‘åƒ}ƒi

    // UŒ‚—Í
    public int nPhysicalAttack; // •¨—UŒ‚—Í
    public int nMagicAttack;    // –‚–@UŒ‚—Í

    // –hŒä—Í(ƒ_ƒ[ƒWŒyŒ¸—¦)
    // ƒ_ƒ[ƒWŒyŒ¸—¦ = (–hŒä—Í) / (–hŒä—Í + 100)
    public int nPhysicalDefense;// •¨—–hŒä—Í
    public int nMagicDefense;   // –‚–@–hŒä—Í

    // ‘f‘‚³(s“®‡)
    public int nSpeed;          // ‘f‘‚³

    // ƒ_ƒ[ƒWŒyŒ¸—¦ŒvZ
    // ƒ_ƒ[ƒWŒyŒ¸—¦ = (–hŒä—Í) / (–hŒä—Í + 100)
    // Å‘å’l1.0fAÅ¬’l0.0f
    private float CalcDamageReduction()
    {


        return 0.0f;// ŒyŒ¸—¦0%
    }
}
