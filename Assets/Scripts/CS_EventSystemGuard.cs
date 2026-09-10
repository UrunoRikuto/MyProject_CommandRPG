using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// BattleSceneをFieldSceneにAdditiveで重ねてロードする都合上、EventSystemが2つ同時に存在してしまう。
/// 最初にAwakeが実行された方だけが生き残るようにし、後から読み込まれた方は自分自身を破棄する。
/// (FindObjectsByTypeは検出順序が不定なため使わず、静的フィールドで先着を判定する)
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class CS_EventSystemGuard : MonoBehaviour
{
    private static EventSystem _activeInstance;

    private void Awake()
    {
        EventSystem self = GetComponent<EventSystem>();

        if (_activeInstance != null && _activeInstance != self)
        {
            Debug.Log("[CS_EventSystemGuard] 重複したEventSystemを破棄しました。");
            Destroy(gameObject);
            return;
        }

        _activeInstance = self;
    }

    private void OnDestroy()
    {
        if (_activeInstance == GetComponent<EventSystem>())
        {
            _activeInstance = null;
        }
    }
}
