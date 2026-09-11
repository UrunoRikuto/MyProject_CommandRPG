using UnityEngine;

/// <summary>
/// フィールドに置く宝箱。プレイヤーが触れると1回だけ報酬を渡す(開封済みはセーブデータに記録)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_TreasureChest : MonoBehaviour
{
    [Header("セーブデータでの開封済み判定に使う一意なID")]
    [SerializeField] private string _chestId;

    [Header("報酬(どうぐ)。未設定なら渡さない")]
    [SerializeField] private string _rewardItemId;
    [SerializeField] private int _rewardItemCount = 1;

    [Header("報酬(装備)。未設定なら渡さない")]
    [SerializeField] private string _rewardEquipmentId;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (CS_GameManager.Instance.IsChestOpened(_chestId))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() == null) return;

        Open();
    }

    private void Open()
    {
        if (!string.IsNullOrEmpty(_rewardItemId))
        {
            CS_GameManager.Instance.AddItem(_rewardItemId, _rewardItemCount);
        }
        if (!string.IsNullOrEmpty(_rewardEquipmentId))
        {
            CS_GameManager.Instance.AddEquipment(_rewardEquipmentId, 1);
        }

        CS_GameManager.Instance.MarkChestOpened(_chestId);
        Debug.Log($"宝箱({_chestId})を開けた！");

        gameObject.SetActive(false);
    }
}
