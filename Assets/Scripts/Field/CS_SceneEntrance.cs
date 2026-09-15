using UnityEngine;

/// <summary>
/// 触れると指定したシーンへ遷移する汎用の出入口。フィールド上の「町へ戻る」出入口などに使う
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_SceneEntrance : MonoBehaviour
{
    [Header("遷移先のシーン名")]
    [SerializeField]
    private string _targetSceneName;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() == null) return;

        CS_GameManager.Instance.SaveGame();
        CS_SceneManager.Instance.LoadScene(_targetSceneName);
    }
}
