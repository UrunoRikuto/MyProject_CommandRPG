using UnityEngine;

/// <summary>
/// 町の出入口。プレイヤーが近くにいる間にFキーを押すとマップ選択メニューを開く
/// (CS_QuestBoardと同じ「近づいてFキー」方式。誤って触れただけで即座に町を出てしまわないようにする)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_TownGate : MonoBehaviour
{
    [SerializeField]
    private CS_MapSelectMenu _mapSelectMenu;

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
            if (_prompt != null) _prompt.Show("Fキーで移動先を選ぶ");
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
            _mapSelectMenu.Open();
        }
    }
}
