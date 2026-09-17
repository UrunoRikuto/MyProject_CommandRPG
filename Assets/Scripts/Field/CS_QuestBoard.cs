using UnityEngine;

/// <summary>
/// クエスト板(町の石碑に付与する)。プレイヤーが近くにいる間にFキーを押すとクエストボードを開く
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_QuestBoard : MonoBehaviour
{
    [SerializeField]
    private CS_QuestBoardMenu _questBoardMenu;

    private bool _playerNearby;
    private CS_InteractionPrompt _prompt;

    private void Start()
    {
        _prompt = GameObject.FindAnyObjectByType<CS_InteractionPrompt>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            _playerNearby = true;
            if (_prompt != null) _prompt.Show("Fキーでクエストボードを開く");
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
            _questBoardMenu.Open();
        }
    }
}
