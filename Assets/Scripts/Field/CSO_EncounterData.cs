using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_EncounterData")]
public class CSO_EncounterData : ScriptableObject
{
    [Header("エンカウントする敵のデータリスト")]
    [SerializeField]
    private List<CSO_CharacterData> _enemyDataList;
    public IReadOnlyList<CSO_CharacterData> enemyDataList => _enemyDataList;
}
