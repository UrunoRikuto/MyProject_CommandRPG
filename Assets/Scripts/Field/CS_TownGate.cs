using UnityEngine;

/// <summary>
/// 町の出入口。触れるとマップ選択メニューを開く
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_TownGate : MonoBehaviour
{
    [SerializeField]
    private CS_MapSelectMenu _mapSelectMenu;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() == null) return;

        _mapSelectMenu.Open();
    }
}
