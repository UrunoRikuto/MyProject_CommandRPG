using UnityEngine;

public class CS_GameManager : MonoBehaviour
{
    public static CS_GameManager Instance { get; private set; }

    private CSO_EncounterData _currentEncounterData;

    private GameObject _player;
    private GameObject _fieldEnvironment;

    void Awake()
    {
        // シングルトンのインスタンスを作成する
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンが切り替わっても破棄されないようにする
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスが存在する場合は破棄する
        }
    }

    public void RequestBattle(CSO_EncounterData encounterData)
    {
        _currentEncounterData = encounterData;

        if (_player == null)
            _player = GameObject.FindAnyObjectByType<CS_PlayerMove>().gameObject;
        if (_fieldEnvironment == null)
            _fieldEnvironment = GameObject.Find("FieldEnvironment");

        CS_SceneManager.Instance.LoadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがロードされた後の処理
            Debug.Log("BattleSceneがロードされました。");

            // プレイヤーとカメラを非表示にする
            if (_player != null)
                _player.SetActive(false); // プレイヤーの移動を無効化する
            // フィールド環境を非表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(false);
        });
    }

    public CSO_EncounterData ConsumePendingEncounter()
    {
        var encounterData = _currentEncounterData;
        _currentEncounterData = null; // 消費したらnullにする
        return encounterData;
    }

    public void ReturnToField()
    {
        CS_SceneManager.Instance.UnloadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがアンロードされた後の処理
            Debug.Log("BattleSceneがアンロードされました。");

            // プレイヤーとカメラを再表示する
            if (_player != null)
                _player.SetActive(true);
            // フィールド環境を非表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(true);
        });
    }

    public void ReturnTown()
    {
        CS_SceneManager.Instance.UnloadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがアンロードされた後の処理
            Debug.Log("BattleSceneがアンロードされました。");

            // プレイヤーとカメラを再表示する
            if (_player != null)
                _player.SetActive(true);
            // フィールド環境を非表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(true);

            // ここで町に移動させるする処理を追加する

        });
    }
}
