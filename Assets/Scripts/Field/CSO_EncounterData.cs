using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_EncounterData")]
public class CSO_EncounterData : ScriptableObject
{
    [Header("エンカウントする敵のデータリスト")]
    [SerializeField]
    private List<CSO_CharacterData> _enemyDataList;
    public IReadOnlyList<CSO_CharacterData> enemyDataList => _enemyDataList;

    [Header("出現する敵のレベル範囲(最小)")]
    [SerializeField]
    private int _minLevel = 1;
    public int minLevel => _minLevel;

    [Header("出現する敵のレベル範囲(最大)")]
    [SerializeField]
    private int _maxLevel = 1;
    public int maxLevel => _maxLevel;

    private void OnValidate()
    {
        // レベル範囲を1〜MAX_LEVELに収め、最大値が最小値を下回らないようにする
        _minLevel = Mathf.Clamp(_minLevel, 1, CS_CharacterState.MAX_LEVEL);
        _maxLevel = Mathf.Clamp(_maxLevel, _minLevel, CS_CharacterState.MAX_LEVEL);
    }
}
