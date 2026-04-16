using System;
using UnityEngine;

// ダメージに関するクラス
[Serializable]
public class Damage
{
    public enum Type
    {
        Physical,   // 物理ダメージ
        Magical     // 魔法ダメージ
    }
    [Header("ダメージの種類")]
    public Type eDamageType;

    [Header("ダメージ量")]
    [Min(0)]
    public int nDamageAmount;
}
