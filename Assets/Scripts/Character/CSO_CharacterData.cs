using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DB_CharacterData", menuName = "Scriptable Objects/DB_CharacterData")]
public class CSO_CharacterData : ScriptableObject
{
    [Header("–¼‘O")]
    [SerializeField]
    private string _characterName;
    public string characterName => _characterName;

    [Header("Šî‘b‘Ì—Í")]
    [SerializeField]
    private int _baseHealth;
    public int baseHealth => _baseHealth;

    [Header("Šî‘bMP")]
    [SerializeField]
    private int _baseMP;
    public int baseMP => _baseMP;

    [Header("Šî‘bUŒ‚—Í")]
    [SerializeField]
    private int _baseAttack;
    public int baseAttack => _baseAttack;

    [Header("Šî‘b–hŒä—Í")]
    [SerializeField]
    private int _baseDefense;
    public int baseDefense => _baseDefense;

    [Header("Šî‘b‘¬“x")]
    [SerializeField]
    private int _baseSpeed;
    public int baseSpeed => _baseSpeed;

    [Header("‰Šú‚©‚çŠ‚µ‚Ä‚¢‚éƒXƒLƒ‹ƒŠƒXƒg")]
    [SerializeField]
    private List<CSO_SkillData> _initialSkills;
    public IReadOnlyList<CSO_SkillData> initialSkills => _initialSkills;
}
