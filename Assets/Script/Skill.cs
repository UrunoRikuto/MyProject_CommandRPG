using System;
using UnityEngine;

// スキルに関するクラス
[Serializable]
public class Skill
{
    [Header("スキル名")]
    public string strName;

    [Header("スキルの説明")]
    [Multiline]
    public string strDescription;

    [Header("マナコスト")]
    [Min(0)]
    public int nManaCost;

    [Header("スキルのダメージ")]
    public Damage damage;

    [Header("スキルの属性")]
    public Element eMainElement;

    [Header("スキルのサブ属性")]
    public Element eSubElement;
}

[CreateAssetMenu(fileName = "New Skill", menuName = "ScriptableObjects/Skill")]
public class SkillSO : ScriptableObject
{
    public Skill[] skill;
}