using UnityEngine;

/// <summary>
/// 触れて(近づいてFキーで)指定したシーンへ遷移する汎用の出入口。フィールド上の「町へ戻る」
/// 出入口などに使う。CS_TownGate/CS_QuestBoardと同じ「近づいてFキー」方式
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_SceneEntrance : MonoBehaviour
{
    [Header("遷移先のシーン名")]
    [SerializeField]
    private string _targetSceneName;

    private bool _playerNearby;
    private CS_InteractionPrompt _prompt;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        _prompt = GameObject.FindAnyObjectByType<CS_InteractionPrompt>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            _playerNearby = true;
            if (_prompt != null) _prompt.Show("Fキーで街に戻る");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            _playerNearby = false;
            if (_prompt != null) _prompt.Hide();
        }
    }

    private void Update()
    {
        if (_playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            CS_GameManager.Instance.SaveGame();
            CS_SceneManager.Instance.LoadScene(_targetSceneName);
        }
    }
}
