using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CS_EncounterSymbol : MonoBehaviour
{
    [Header("エンカウントする確率")]
    [SerializeField]
    [Range(0f, 1f)]
    private float _encounterRate = 0.1f;

    [Header("エンカウントデータリスト")]
    [SerializeField]
    private List<CSO_EncounterData> _encounterData;

    [Header("エンカウントクールタイム")]
    [SerializeField]
    private float _encounterCoolTime = 5f;

    private float _currentCoolTime = 0f;// 現在のクールタイム

    private void Start()
    {
        // コライダーをトリガーに設定
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;

        CS_ValueObserver.Instance.Register(gameObject, this, name + "のクールタイム", () => _currentCoolTime);
    }

    private void Update()
    {
        // クールタイムを減少させる
        if (_currentCoolTime > 0f)
        {
            _currentCoolTime -= Time.deltaTime;
            return;
        }


    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイヤーが接触した場合
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            // クールタイム中はエンカウントしない
            if (_currentCoolTime > 0f)
                return;
            // エンカウント確率に基づいてエンカウント判定
            if (Random.value < _encounterRate)
            {
                // エンカウントデータをランダムに選択
                CSO_EncounterData encounterData = _encounterData[Random.Range(0, _encounterData.Count)];

                //---- エンカウント処理を実行（例: 戦闘シーンに遷移）----//
                Debug.Log($"エンカウント！：");
                for (int i = 0; i < encounterData.enemyDataList.Count; i++)
                {
                    Debug.Log($"敵{i + 1}：{encounterData.enemyDataList[i].characterName}");
                }
                // ----------------------------------------------------- //

                // クールタイムをリセット
                _currentCoolTime = _encounterCoolTime;
            }
        }
    }
}
