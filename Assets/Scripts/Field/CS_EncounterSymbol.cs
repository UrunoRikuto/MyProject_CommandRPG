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

        if (_encounterData == null || _encounterData.Count == 0)
        {
            Debug.LogWarning($"{name}のエンカウントデータが設定されていません。");
        }

        CS_ValueObserver.Instance.Register(gameObject, this, name + "のクールタイム", () => _currentCoolTime);
    }

    /// <summary>
    /// エディタ編集中のSceneビューにだけ、エンカウントエリアを半透明な赤色で表示する
    /// (ゲーム画面には映らないUnity標準のギズモ機能。実行中の見た目には影響しない)
    /// </summary>
    private void OnDrawGizmos()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        Vector2 size = boxCollider != null ? boxCollider.size : Vector2.one;
        Vector2 offset = boxCollider != null ? boxCollider.offset : Vector2.zero;

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Vector3 center = transform.TransformPoint(offset);
        Vector3 size3D = new Vector3(size.x * transform.lossyScale.x, size.y * transform.lossyScale.y, 0.1f);
        Gizmos.DrawCube(center, size3D);
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

            if (_encounterData == null || _encounterData.Count == 0)
            {
                Debug.LogWarning($"{name}のエンカウントデータが設定されていません。");
                return;
            }

            // エンカウント確率に基づいてエンカウント判定
            if (Random.value < _encounterRate)
            {
                // エンカウントデータをランダムに選択
                CSO_EncounterData encounterData = _encounterData[Random.Range(0, _encounterData.Count)];

                // バトルをリクエスト
                CS_GameManager.Instance.RequestBattle(encounterData);

                // クールタイムをリセット
                _currentCoolTime = _encounterCoolTime;
            }
        }
    }
}
