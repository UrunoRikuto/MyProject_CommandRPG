using UnityEngine;

[CreateAssetMenu(fileName = "DB_CharacterData", menuName = "Scriptable Objects/DB_CharacterData")]
public class CSO_CharacterData : ScriptableObject
{
    [Header("–¼‘O")]
    private string _characterName;
    public string characterName => _characterName;

    [Header("Šî‘b‘Ì—Í")]
    private int _baseHealth;
    public int baseHealth => _baseHealth;

    [Header("Šî‘bUŒ‚—Í")]
    private int _baseAttack;
    public int baseAttack => _baseAttack;

    [Header("Šî‘b‘¬“x")]
    private int _baseSpeed;
    public int baseSpeed => _baseSpeed;
}
