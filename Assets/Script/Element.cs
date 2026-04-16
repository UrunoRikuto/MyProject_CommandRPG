using System.Collections.Generic;
using System;
using UnityEngine;

// ‘®«‚ÉŠÖ‚·‚éƒNƒ‰ƒX
[Serializable]
public class Element
{
    // ‘®«‚Ìí—Ş
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
    [Header("‘®«‚Ìí—Ş")]
    public Type eElementType = Type.None;


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

    // ‘®«‘Š«‚Ì”{—¦•\
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
    // ˆø”F–hŒä‘¤‚Ì‘®«A‘ÎÛ‚Ì‘®«
    // –ß‚è’lF2.0fA1.0fA0.5f
    public float CalcElementModfier(Element.Type targetType)
    {
        if (elementTable.TryGetValue((this.eElementType, targetType), out float value))
        {
            return value; // •â³’l‚ğ•Ô‚·
        }

        return NomalModfier; // •â³‚È‚µ
    }

    // ô‚¢‚â“Å‚È‚Ç‚ÌŒp‘±ƒ^[ƒ“”
    [Header("Œp‘±ƒ^[ƒ“”(¦ô‚¢‚â“Å‚Ég—p)")]
    public int nDuration = 0;
}
