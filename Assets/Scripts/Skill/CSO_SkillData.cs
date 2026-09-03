using UnityEngine;

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_SkillData")]
public class CSO_SkillData : ScriptableObject
{
    [Header("名前")]
    [SerializeField] 
    private string _skillName;
    public string skillName => _skillName;

    [Header("使用コスト")]
    [SerializeField]
    private int _cost;
    public int cost => _cost;

    [Header("ダメージ倍率")]
    [SerializeField]
    private float _damageRate;
    public float damageRate => _damageRate;

}
