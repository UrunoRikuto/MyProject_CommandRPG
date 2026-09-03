using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DB_CharacterData", menuName = "Scriptable Objects/DB_CharacterData")]
public class CSO_CharacterData : ScriptableObject
{
    [Header("名前")]
    [SerializeField]
    private string _characterName;
    public string characterName => _characterName;

    [Header("基礎体力")]
    [SerializeField]
    private int _baseHealth;
    public int baseHealth => _baseHealth;

    [Header("基礎MP")]
    [SerializeField]
    private int _baseMP;
    public int baseMP => _baseMP;

    [Header("基礎攻撃力")]
    [SerializeField]
    private int _baseAttack;
    public int baseAttack => _baseAttack;

    [Header("基礎防御力")]
    [SerializeField]
    private int _baseDefense;
    public int baseDefense => _baseDefense;

    [Header("基礎速度")]
    [SerializeField]
    private int _baseSpeed;
    public int baseSpeed => _baseSpeed;

    [Header("初期から所持しているスキルリスト")]
    [SerializeField]
    private List<CSO_SkillData> _initialSkills;
    public IReadOnlyList<CSO_SkillData> initialSkills => _initialSkills;

    [Header("AIが「たたかう」を選ぶ重み")]
    [SerializeField] private float _attackWeight = 1f;
    public float attackWeight => _attackWeight;
    [Header("AIが各スキルを選ぶ重み")]
    [SerializeField] private List<float> _skillWeights;
    public IReadOnlyList<float> skillWeights => _skillWeights;

    void OnValidate()
    {
        // スキルの数と重みの数が一致するように調整
        if (_skillWeights.Count != _initialSkills.Count)
        {
            int diff = _initialSkills.Count - _skillWeights.Count;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    _skillWeights.Add(0.0f); // デフォルトの重みを追加
                }
            }
            else
            {
                _skillWeights.RemoveRange(_skillWeights.Count + diff, -diff); // 余分な重みを削除
            }
        }
    }
}
