using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// 町シーン(TownScene)を1回だけ組み立てるワンショットのEditorツール。
/// 複雑なUI階層やPrefabInstanceを手書きYAMLで作るリスクを避け、Unity自身のAPI(Instantiate/AddComponent)で
/// 構築する。実行前に「新規の空シーンをアクティブにする」+「FieldScene.unityをAdditiveで開く」ことが前提
/// (ポーズ/装備メニューをFieldSceneから複製するため)
/// </summary>
public class CSED_TownSceneBuilder : EditorWindow
{
    private const int TOWN_W = 20;
    private const int TOWN_H = 16;
    private static readonly Vector3 TOWN_CENTER = new Vector3(8f, 6f, 0f);

    // 現在のプレイヤーパーティ5人(BattleScene/各CS_EquipmentMenuと同じ構成)。泉の回復機能で使う
    private static readonly string[] PLAYER_PARTY_DATA_PATHS =
    {
        "Assets/Data/Character/Paladin/DB_Char_Paladin.asset",
        "Assets/Data/Character/Warlock/DB_Char_Warlock.asset",
        "Assets/Data/Character/Ranger/DB_Char_Ranger.asset",
        "Assets/Data/Character/Cleric/DB_Char_Cleric.asset",
        "Assets/Data/Character/Engineer/DB_Char_Engineer.asset",
    };

    [MenuItem("Tools/Field/Build Town Scene")]
    public static void ShowWindow()
    {
        GetWindow<CSED_TownSceneBuilder>("町シーン構築");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "実行前の準備:\n" +
            "1. File > New Scene で新規の空シーンを作成してアクティブにする\n" +
            "2. FieldScene.unity をヒエラルキーへドラッグ&ドロップしてAdditiveで(重ねて)開く\n" +
            "   (ポーズ/装備メニューの複製元として使うため)\n" +
            "この状態でボタンを押してください。完了後、新規シーンを\n" +
            "Assets/Scenes/TownScene.unity として保存してください(FieldScene側は保存不要)。",
            MessageType.Info);

        if (GUILayout.Button("町シーンを構築", GUILayout.Height(28)))
        {
            Build();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "建物・装飾の配置:\n" +
            "保存済みのTownScene.unityを開いた状態(こちらはFieldSceneを開く必要はありません)で\n" +
            "押すと、地面を石畳に、家2棟(屋根・壁・扉が1枚に収まった素材)と噴水・墓・花壇を配置し、\n" +
            "町の出入口に看板テクスチャも付けます。何度押しても最新の内容に作り直されます。",
            MessageType.Info);
        if (GUILayout.Button("建物・装飾を配置", GUILayout.Height(28)))
        {
            AddTownBuildings();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "マップ選択UIの更新:\n" +
            "保存済みのTownScene.unityを開いた状態で押すと、マップ選択画面のボタンを\n" +
            "地域名+出現モンスター名の表示に作り直します。地域ごとのエンカウントデータを\n" +
            "変更した後などに、何度でも押し直して最新化できます。",
            MessageType.Info);
        if (GUILayout.Button("マップ選択UIを更新", GUILayout.Height(28)))
        {
            RefreshMapSelectUI();
        }
    }

    private void Build()
    {
        Scene townScene = EditorSceneManager.GetActiveScene();
        Scene fieldScene = FindLoadedScene("FieldScene");

        if (!fieldScene.IsValid())
        {
            Debug.LogError("FieldScene.unityがAdditiveで開かれていません。先に開いてから実行してください。");
            return;
        }
        if (townScene == fieldScene)
        {
            Debug.LogError("アクティブなシーンがFieldSceneのままです。新規の空シーンを作成してアクティブにしてから実行してください。");
            return;
        }

        BuildGround(townScene);
        InstantiatePlayer(townScene);
        CreateGameManager(townScene);
        EnsureEventSystem(townScene);

        CloneMenus(townScene, fieldScene);
        CS_MapSelectMenu mapSelectMenu = BuildMapSelectUI(townScene);
        CreateTownGate(townScene, mapSelectMenu);

        EditorSceneManager.MarkSceneDirty(townScene);
        Debug.Log("町シーンの構築が完了しました。新規シーンをAssets/Scenes/TownScene.unityとして保存してください。");
    }

    /// <summary>
    /// 保存済みのTownScene.unityを開いた状態で実行する追加ステップ。
    /// 地面はpipoya素材(mapchip2/base.png)の石畳、噴水・墓・花壇も同素材。
    /// 家はpipo-map001の一軒家スプライト(屋根・壁・扉が1枚に収まった完成品)をそのまま使う。
    /// 何度実行しても最新の内容に作り直される
    /// </summary>
    private void AddTownBuildings()
    {
        Scene townScene = EditorSceneManager.GetActiveScene();
        Grid grid = FindInScene<Grid>(townScene);
        if (grid == null)
        {
            Debug.LogError("シーン内にGridが見つかりません。TownScene.unityを開いてから実行してください。");
            return;
        }

        // 地面を石畳(Tile_TownPlaza)に塗り直す
        Tilemap ground = grid.transform.Find("Ground")?.GetComponent<Tilemap>();
        TileBase plazaTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Town/Tile_TownPlaza.asset");
        if (ground != null && plazaTile != null)
        {
            ground.ClearAllTiles();
            for (int y = 0; y < TOWN_H; y++)
            {
                for (int x = 0; x < TOWN_W; x++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), plazaTile);
                }
            }
        }

        // 建物の当たり判定はハウス素材のスプライト(後述のCreateRoof)自体のコライダーで賄うので、
        // 障害物Tilemapには町の外周を塞ぐ境界だけを塗る(プレイヤーがエリア外に出られないようにする)
        Tilemap obstacles = GetOrCreateTownObstacleTilemap(grid);
        obstacles.ClearAllTiles();
        TileBase borderTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Grassland/Tile_Rock.asset");
        PaintBorder(obstacles, borderTile);

        // 装飾(噴水・墓・花壇)をやり直せるよう、既存の物は一旦消してから作り直す
        GameObject existing = FindRootByName(townScene, "Buildings");
        if (existing != null)
        {
            DestroyImmediate(existing);
        }

        GameObject parent = new GameObject("Buildings");
        SceneManager.MoveGameObjectToScene(parent, townScene);

        Sprite fountain = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TownFountain.png");
        Sprite grave = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TownGrave.png");
        Sprite flowerPot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TownFlowerPot.png");
        Sprite houseWood = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Town/Tile_TownRoofWood.png");
        Sprite houseOrange = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Town/Tile_TownRoofOrange.png");

        // マス目の中心(整数座標+0.5)に置かないと、Tilemap上のマス目の見た目とズレて「浮いて」見えるため、
        // 装飾は全てセルの中心座標に配置する
        //
        // 家はpipo-map001の一軒家スプライト(屋根・壁・扉が1枚に収まった完成品)を建物の footprint
        // 全体(手前の行も含む)にそのまま引き伸ばして使う。手前だけ別素材の壁+扉を重ねると、
        // 素材のテイストが混ざって二重の壁に見えてしまうため、正面用の壁タイル・扉は使わない
        // レイアウト: 家2棟を上端寄りに並べ、その手前(下)に噴水+花壇の広場、
        // クエスト板(石碑)はオレンジの家の下・右寄りに配置する
        CreateRoof(parent.transform, "House_Wood", houseWood, 2, 6, 11, 14);
        CreateRoof(parent.transform, "House_Orange", houseOrange, 13, 17, 11, 14);

        CreateDecoration(parent.transform, "Fountain", fountain, new Vector3(10.5f, 8.5f, 0f), Vector2.one, true, new Vector2(1.4f, 1.4f));
        CreateDecoration(parent.transform, "Grave", grave, new Vector3(16.5f, 8.5f, 0f), Vector2.one, true, null);
        CreateDecoration(parent.transform, "FlowerPot_1", flowerPot, new Vector3(8.5f, 8.5f, 0f), Vector2.one, true, null);
        CreateDecoration(parent.transform, "FlowerPot_2", flowerPot, new Vector3(12.5f, 8.5f, 0f), Vector2.one, true, null);

        // 石碑(Grave)をクエスト板として使う。装飾用の当たり判定(物理ブロック)はそのまま残し、
        // 別途プレイヤーの近接検知用のトリガーコライダー+CS_QuestBoardを追加する
        GameObject graveObject = parent.transform.Find("Grave")?.gameObject;
        CS_QuestBoard questBoard = null;
        if (graveObject != null)
        {
            questBoard = graveObject.GetComponent<CS_QuestBoard>();
            if (questBoard == null)
            {
                BoxCollider2D proximityCollider = graveObject.AddComponent<BoxCollider2D>();
                proximityCollider.isTrigger = true;
                proximityCollider.size = new Vector2(2.5f, 2.5f);
                questBoard = graveObject.AddComponent<CS_QuestBoard>();
            }
        }
        else
        {
            Debug.LogWarning("Graveオブジェクトが見つからず、クエスト板を設定できませんでした。");
        }

        // 泉(Fountain)に近づいてFキーを押すとパーティを全回復するCS_TownFountainを追加する。
        // 装飾用の当たり判定(物理ブロック)はそのまま残し、Grave/QuestBoardと同様に
        // 別途プレイヤーの近接検知用のトリガーコライダーを追加する
        GameObject fountainObject = parent.transform.Find("Fountain")?.gameObject;
        if (fountainObject != null)
        {
            CS_TownFountain townFountain = fountainObject.GetComponent<CS_TownFountain>();
            if (townFountain == null)
            {
                BoxCollider2D fountainProximityCollider = fountainObject.AddComponent<BoxCollider2D>();
                fountainProximityCollider.isTrigger = true;
                fountainProximityCollider.size = new Vector2(2.2f, 2.2f);
                townFountain = fountainObject.AddComponent<CS_TownFountain>();
            }

            SerializedObject fountainSo = new SerializedObject(townFountain);
            SerializedProperty partyProp = fountainSo.FindProperty("_playerPartyData");
            string[] partyPaths = PLAYER_PARTY_DATA_PATHS;
            partyProp.arraySize = partyPaths.Length;
            for (int i = 0; i < partyPaths.Length; i++)
            {
                partyProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<CSO_CharacterData>(partyPaths[i]);
            }
            fountainSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("Fountainオブジェクトが見つからず、泉の回復機能を設定できませんでした。");
        }

        // 既存のTownGate(以前のバージョンで見た目なしのまま作られたもの)に看板テクスチャを補完する
        GameObject townGate = FindRootByName(townScene, "TownGate");
        if (townGate != null)
        {
            SpriteRenderer gateRenderer = townGate.GetComponent<SpriteRenderer>();
            if (gateRenderer == null)
            {
                gateRenderer = townGate.AddComponent<SpriteRenderer>();
            }
            gateRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TownSignpost.png");
        }

        // 既にFieldSceneの非アクティブなタイミングで複製されてしまった
        // PauseCanvas/EquipmentCanvasとその親Controllerを、念のため毎回強制的に有効化しておく
        string[] menuObjectNames = { "PauseMenuController", "PauseCanvas", "EquipmentMenuController", "EquipmentCanvas" };
        foreach (string menuObjectName in menuObjectNames)
        {
            GameObject menuObject = FindRootByName(townScene, menuObjectName);
            if (menuObject != null)
            {
                menuObject.SetActive(true);
            }
        }

        EnsureInteractionPrompt(townScene);

        if (questBoard != null)
        {
            BuildQuestBoardUI(townScene, questBoard);
        }

        EditorSceneManager.MarkSceneDirty(townScene);
        Debug.Log("町に建物・装飾を配置しました。シーンを保存してください。");
    }

    /// <summary>
    /// 家の完成品スプライト(屋根・壁・扉が1枚に収まっている)を、指定したマス目の範囲
    /// (x0〜x1, y0〜y1)にぴったり収まるように引き伸ばして配置する。
    /// 当たり判定はスプライトの実寸(sprite.bounds)を元に、この引き伸ばし後の大きさへ自動で合わせる
    /// </summary>
    private void CreateRoof(Transform parent, string name, Sprite sprite, int x0, int x1, int y0, int y1)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"{name}用のスプライトが見つかりませんでした。");
            return;
        }

        float width = x1 - x0 + 1;
        float height = y1 - y0 + 1;
        Vector2 nativeSize = sprite.bounds.size;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x0 + width / 2f, y0 + height / 2f, 0f);
        go.transform.localScale = new Vector3(width / nativeSize.x, height / nativeSize.y, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = nativeSize;
    }

    /// <summary>
    /// マップの一番外側1マスを障害物で塗り、プレイヤーがエリア外に出られないようにする
    /// </summary>
    private void PaintBorder(Tilemap obstacleTilemap, TileBase borderTile)
    {
        if (borderTile == null) return;

        for (int x = 0; x < TOWN_W; x++)
        {
            obstacleTilemap.SetTile(new Vector3Int(x, 0, 0), borderTile);
            obstacleTilemap.SetTile(new Vector3Int(x, TOWN_H - 1, 0), borderTile);
        }
        for (int y = 0; y < TOWN_H; y++)
        {
            obstacleTilemap.SetTile(new Vector3Int(0, y, 0), borderTile);
            obstacleTilemap.SetTile(new Vector3Int(TOWN_W - 1, y, 0), borderTile);
        }
    }

    private Tilemap GetOrCreateTownObstacleTilemap(Grid grid)
    {
        Transform existing = grid.transform.Find("TownObstacles");
        if (existing != null) return existing.GetComponent<Tilemap>();

        GameObject go = new GameObject("TownObstacles");
        go.transform.SetParent(grid.transform, false);
        Tilemap tilemap = go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -5;
        go.AddComponent<TilemapCollider2D>();
        return tilemap;
    }

    /// <summary>
    /// 見た目(SpriteRenderer)+必要なら当たり判定(BoxCollider2D、通行不可)を持つ装飾オブジェクトを1つ作る。
    /// 当たり判定の大きさはスプライト自体の実寸(sprite.bounds)から自動算出できるが、
    /// 噴水のように見た目より小さい判定にしたい場合はcolliderSizeOverrideで上書きできる
    /// </summary>
    private void CreateDecoration(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 scale, bool addCollider, Vector2? colliderSizeOverride)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"{name}用のスプライトが見つかりませんでした。");
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        if (addCollider)
        {
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = colliderSizeOverride ?? sprite.bounds.size;
        }
    }

    // ------------------------------------------------------------ helpers --

    private Scene FindLoadedScene(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name == name) return s;
        }
        return default;
    }

    private GameObject FindRootByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }

    private T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    // -------------------------------------------------------------- steps --

    private void BuildGround(Scene townScene)
    {
        GameObject gridGo = new GameObject("Grid");
        SceneManager.MoveGameObjectToScene(gridGo, townScene);
        gridGo.AddComponent<Grid>();

        GameObject groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridGo.transform, false);
        Tilemap tilemap = groundGo.AddComponent<Tilemap>();
        TilemapRenderer renderer = groundGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -10;

        TileBase groundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Town/Tile_TownPlaza.asset");
        if (groundTile == null)
        {
            Debug.LogError("地面タイル(Assets/Tiles/Town/Tile_TownPlaza.asset)が見つかりません。");
            return;
        }

        for (int y = 0; y < TOWN_H; y++)
        {
            for (int x = 0; x < TOWN_W; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }
        }
    }

    private void InstantiatePlayer(Scene townScene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Field/Player.prefab");
        if (prefab == null)
        {
            Debug.LogError("Assets/Prefabs/Field/Player.prefabが見つかりません。");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, townScene);
        instance.transform.position = TOWN_CENTER;
    }

    private void CreateGameManager(Scene townScene)
    {
        if (FindInScene<CS_GameManager>(townScene) != null) return;

        GameObject go = new GameObject("GameManager");
        SceneManager.MoveGameObjectToScene(go, townScene);
        go.AddComponent<CS_GameManager>();
    }

    private void EnsureEventSystem(Scene townScene)
    {
        if (FindInScene<EventSystem>(townScene) != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(go, townScene);
    }

    /// <summary>
    /// FieldSceneのポーズ/装備メニューを町シーンへ複製する。
    /// Controller(ロジック)とCanvas(見た目)は別々のルートオブジェクトだが互いに参照し合っているため
    /// (ControllerがCanvas内のパネルを参照、Canvas内のボタンがControllerのメソッドを参照)、
    /// 個別にInstantiateすると参照が複製先ではなく複製元(FieldScene)を向いたままになってしまう。
    /// 一時的な共通の親でまとめてから1回のInstantiateで複製することで、相互参照を正しく複製先へ付け替える
    /// </summary>
    private void CloneMenus(Scene townScene, Scene fieldScene)
    {
        GameObject pauseController = FindRootByName(fieldScene, "PauseMenuController");
        GameObject pauseCanvas = FindRootByName(fieldScene, "PauseCanvas");
        if (pauseController != null && pauseCanvas != null)
        {
            ActivateAll(CloneGroupIntoScene(townScene, pauseController, pauseCanvas));
        }
        else
        {
            Debug.LogWarning("FieldScene内にPauseMenuController/PauseCanvasが見つからず、ポーズメニューを複製できませんでした。");
        }

        GameObject equipController = FindRootByName(fieldScene, "EquipmentMenuController");
        GameObject equipCanvas = FindRootByName(fieldScene, "EquipmentCanvas");
        if (equipController != null && equipCanvas != null)
        {
            ActivateAll(CloneGroupIntoScene(townScene, equipController, equipCanvas));
        }
        else
        {
            Debug.LogWarning("FieldScene内にEquipmentMenuController/EquipmentCanvasが見つからず、装備メニューを複製できませんでした。");
        }
    }

    /// <summary>
    /// 複製元(FieldScene)側が戦闘中などでSetActive(false)されたタイミングのまま保存されていると、
    /// 複製先でも非アクティブなコピーができてしまう。町では戦闘中扱いは無いので、必ず有効化しておく
    /// </summary>
    private void ActivateAll(List<GameObject> objects)
    {
        foreach (GameObject go in objects)
        {
            go.SetActive(true);
        }
    }

    private List<GameObject> CloneGroupIntoScene(Scene targetScene, params GameObject[] sourceObjects)
    {
        GameObject tempParent = new GameObject("__CloneTempParent");
        var originalParents = new Transform[sourceObjects.Length];
        var originalSiblingIndices = new int[sourceObjects.Length];

        for (int i = 0; i < sourceObjects.Length; i++)
        {
            originalParents[i] = sourceObjects[i].transform.parent;
            originalSiblingIndices[i] = sourceObjects[i].transform.GetSiblingIndex();
            sourceObjects[i].transform.SetParent(tempParent.transform, true);
        }

        GameObject clonedTempParent = Instantiate(tempParent);

        // 複製元(FieldScene)の階層を元通りに戻す
        for (int i = 0; i < sourceObjects.Length; i++)
        {
            sourceObjects[i].transform.SetParent(originalParents[i], true);
            sourceObjects[i].transform.SetSiblingIndex(originalSiblingIndices[i]);
        }
        DestroyImmediate(tempParent);

        var clonedChildren = new List<GameObject>();
        foreach (Transform child in clonedTempParent.transform)
        {
            clonedChildren.Add(child.gameObject);
        }
        foreach (GameObject child in clonedChildren)
        {
            child.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(child, targetScene);
        }
        DestroyImmediate(clonedTempParent);

        return clonedChildren;
    }

    // 地域ボタンに添える代表モンスター名。CSED_FieldMapGeneratorのGetEncounterDataPaths()で
    // 実際に組んだ地域別エンカウントデータ(Common/Mid/Elite)と対応させている
    private static readonly (string theme, string label, string monsters)[] REGIONS =
    {
        ("Grassland", "草原", "マッシュルーム/ナイト/オーク"),
        ("Desert", "砂漠", "スパイダー/ガーゴイル/サイクロプス"),
        ("Wetlands", "湿地", "コウモリ/スパイダー/ゴースト"),
        ("Snow", "雪原", "コウモリ/ゴースト/ドラゴン"),
    };

    private const float REGION_BUTTON_HEIGHT = 64f;
    private const float REGION_BUTTON_SPACING = 76f;

    /// <summary>
    /// 保存済みのTownScene.unityに対して、マップ選択UI(MapSelectCanvas/MapSelectMenuController)だけを
    /// 破棄してから作り直す。地域のエンカウントデータや表示内容を変更した際に、Build()全体
    /// (FieldSceneのAdditive読み込みが必要)をやり直さずに済むようにするための再実行用入口
    /// </summary>
    private void RefreshMapSelectUI()
    {
        Scene townScene = EditorSceneManager.GetActiveScene();
        Grid grid = FindInScene<Grid>(townScene);
        if (grid == null)
        {
            Debug.LogError("シーン内にGridが見つかりません。TownScene.unityを開いてから実行してください。");
            return;
        }

        GameObject existingCanvas = FindRootByName(townScene, "MapSelectCanvas");
        if (existingCanvas != null) DestroyImmediate(existingCanvas);
        GameObject existingController = FindRootByName(townScene, "MapSelectMenuController");
        if (existingController != null) DestroyImmediate(existingController);

        CS_MapSelectMenu mapSelectMenu = BuildMapSelectUI(townScene);

        GameObject townGate = FindRootByName(townScene, "TownGate");
        CS_TownGate gate = townGate != null ? townGate.GetComponent<CS_TownGate>() : null;
        if (gate != null)
        {
            SerializedObject so = new SerializedObject(gate);
            so.FindProperty("_mapSelectMenu").objectReferenceValue = mapSelectMenu;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("TownGateが見つからず、マップ選択メニューの参照を張り直せませんでした。");
        }

        EditorSceneManager.MarkSceneDirty(townScene);
        Debug.Log("マップ選択UIを更新しました。シーンを保存してください。");
    }

    /// <summary>
    /// マップ選択UI(4地域+キャンセル)をコードで組み立てる。各地域ボタンには代表モンスター名も添える
    /// </summary>
    private CS_MapSelectMenu BuildMapSelectUI(Scene townScene)
    {
        GameObject canvasGo = new GameObject("MapSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, townScene);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject panelGo = new GameObject("MapSelectPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(360, 450);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Window.png");
        panelImage.type = Image.Type.Sliced;

        GameObject controllerGo = new GameObject("MapSelectMenuController");
        SceneManager.MoveGameObjectToScene(controllerGo, townScene);
        CS_MapSelectMenu menu = controllerGo.AddComponent<CS_MapSelectMenu>();

        UnityEngine.Events.UnityAction[] onClicks =
        {
            (UnityEngine.Events.UnityAction)menu.OnSelectGrasslandClicked,
            (UnityEngine.Events.UnityAction)menu.OnSelectDesertClicked,
            (UnityEngine.Events.UnityAction)menu.OnSelectWetlandsClicked,
            (UnityEngine.Events.UnityAction)menu.OnSelectSnowClicked,
        };

        float startY = ((REGIONS.Length - 1) * REGION_BUTTON_SPACING) / 2f;
        for (int i = 0; i < REGIONS.Length; i++)
        {
            float y = startY - i * REGION_BUTTON_SPACING;
            CreateRegionButton(panelGo.transform, REGIONS[i].label, REGIONS[i].monsters, new Vector2(0, y), onClicks[i]);
        }

        CreateButton(panelGo.transform, "キャンセル", new Vector2(0, -startY - REGION_BUTTON_SPACING), (UnityEngine.Events.UnityAction)menu.OnCancelClicked);

        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("_menuPanel").objectReferenceValue = panelGo;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);

        return menu;
    }

    private void CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 50);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Button.png");
        image.type = Image.Type.Sliced;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 28;
    }

    /// <summary>
    /// マップ選択の地域ボタン用。地域名(上段)+出現モンスター名(下段、小さめグレー)の2段表示にする
    /// </summary>
    private void CreateRegionButton(Transform parent, string label, string monsterNames, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, REGION_BUTTON_HEIGHT);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Button.png");
        image.type = Image.Type.Sliced;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(buttonGo.transform, false);
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.text = label;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontSize = 26;

        GameObject monsterGo = new GameObject("MonsterLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        monsterGo.transform.SetParent(buttonGo.transform, false);
        RectTransform monsterRect = monsterGo.GetComponent<RectTransform>();
        monsterRect.anchorMin = new Vector2(0f, 0f);
        monsterRect.anchorMax = new Vector2(1f, 0.5f);
        monsterRect.sizeDelta = Vector2.zero;
        monsterRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI monsterText = monsterGo.GetComponent<TextMeshProUGUI>();
        monsterText.text = monsterNames;
        monsterText.alignment = TextAlignmentOptions.Center;
        monsterText.color = new Color(0.8f, 0.8f, 0.85f);
        monsterText.fontSize = 16;
    }

    private const string INTERACTION_PROMPT_CANVAS_NAME = "InteractionPromptCanvas";

    /// <summary>
    /// 「Fキーで〜」プロンプトの画面下部UIを用意する。泉・クエスト板・町の出入口が
    /// 実行時にFindAnyObjectByTypeで見つけて使う。既存の物があれば何もしない
    /// </summary>
    private void EnsureInteractionPrompt(Scene townScene)
    {
        GameObject existing = FindRootByName(townScene, INTERACTION_PROMPT_CANVAS_NAME);
        if (existing != null) return;

        GameObject canvasGo = new GameObject(INTERACTION_PROMPT_CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, townScene);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject panelGo = new GameObject("PromptPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(360f, 50f);
        panelRect.anchoredPosition = new Vector2(0f, 70f);
        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Window.png");
        panelImage.type = Image.Type.Sliced;

        GameObject textGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 22;

        CS_InteractionPrompt prompt = canvasGo.AddComponent<CS_InteractionPrompt>();
        SerializedObject so = new SerializedObject(prompt);
        so.FindProperty("_promptPanel").objectReferenceValue = panelGo;
        so.FindProperty("_promptText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);
    }

    private const string QUEST_BOARD_CANVAS_NAME = "QuestBoardCanvas";
    private const string QUEST_BOARD_CONTROLLER_NAME = "QuestBoardMenuController";
    private const float QUEST_ROW_HEIGHT = 70f;

    /// <summary>
    /// クエスト板(石碑)のウィンドウをコードで組み立てる。受注可能なクエスト一覧(左、受注ボタン付き)と
    /// 受注中のクエスト一覧(右、進捗表示のみ)を、それぞれScrollRectの動的リストとして持つ。
    /// 何度実行しても既存のCanvas/Controllerを破棄してから作り直す(AddTownBuildings全体の再実行に合わせる)
    /// </summary>
    private void BuildQuestBoardUI(Scene townScene, CS_QuestBoard questBoard)
    {
        GameObject existingCanvas = FindRootByName(townScene, QUEST_BOARD_CANVAS_NAME);
        if (existingCanvas != null) DestroyImmediate(existingCanvas);
        GameObject existingController = FindRootByName(townScene, QUEST_BOARD_CONTROLLER_NAME);
        if (existingController != null) DestroyImmediate(existingController);

        GameObject canvasGo = new GameObject(QUEST_BOARD_CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, townScene);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject panelGo = new GameObject("QuestBoardPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760, 560);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Window.png");
        panelImage.type = Image.Type.Sliced;

        GameObject controllerGo = new GameObject(QUEST_BOARD_CONTROLLER_NAME);
        SceneManager.MoveGameObjectToScene(controllerGo, townScene);
        CS_QuestBoardMenu menu = controllerGo.AddComponent<CS_QuestBoardMenu>();

        CreateSectionLabel(panelGo.transform, "受注可能なクエスト", new Vector2(-190, 245));
        CreateSectionLabel(panelGo.transform, "進行中のクエスト", new Vector2(190, 245));

        Transform boardContent = CreateScrollList(panelGo.transform, new Vector2(-190, -20), new Vector2(340, 380), out ScrollRect boardScrollRect);
        Transform activeContent = CreateScrollList(panelGo.transform, new Vector2(190, -20), new Vector2(340, 380), out ScrollRect activeScrollRect);

        // 行テンプレートはリストのContentではなくPanel直下に置く。Content配下に置いてしまうと、
        // RefreshLists()のClearChildren()がリスト再構築のたびにテンプレート自身も道連れで
        // 非表示化→Destroyしてしまい、2回目以降Instantiateできなくなる不具合があったため
        GameObject boardRowTemplate = CreateQuestRow(panelGo.transform, "BoardRowTemplate", true);
        GameObject activeRowTemplate = CreateQuestRow(panelGo.transform, "ActiveRowTemplate", false);

        CreateButton(panelGo.transform, "閉じる", new Vector2(0, -250), (UnityEngine.Events.UnityAction)menu.OnCloseButtonClicked);

        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("_menuPanel").objectReferenceValue = panelGo;
        so.FindProperty("_boardRowTemplate").objectReferenceValue = boardRowTemplate;
        so.FindProperty("_boardListParent").objectReferenceValue = boardContent;
        so.FindProperty("_boardScrollRect").objectReferenceValue = boardScrollRect;
        so.FindProperty("_activeRowTemplate").objectReferenceValue = activeRowTemplate;
        so.FindProperty("_activeListParent").objectReferenceValue = activeContent;
        so.FindProperty("_activeScrollRect").objectReferenceValue = activeScrollRect;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelGo.SetActive(false);

        SerializedObject boardSo = new SerializedObject(questBoard);
        boardSo.FindProperty("_questBoardMenu").objectReferenceValue = menu;
        boardSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private void CreateSectionLabel(Transform parent, string label, Vector2 anchoredPosition)
    {
        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(340, 30);
        rect.anchoredPosition = anchoredPosition;
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 22;
    }

    /// <summary>
    /// スクロール可能なリスト領域を1つ作る。ScrollRect+Mask+背景Imageを同一オブジェクトに持たせ、
    /// その子にContentを配置する。行の並べ方はVerticalLayoutGroupに頼らず、
    /// CS_QuestBoardMenu側でCS_EquipmentMenu.BuildMemberButtonsと同じ「anchoredPositionを
    /// 直接計算する」方式にする(LayoutGroupのChildControlWidth/Height未設定だと
    /// 子の幅が0のまま反映されない問題があり、確実に動く手動計算方式に統一した)
    /// </summary>
    private Transform CreateScrollList(Transform parent, Vector2 anchoredPosition, Vector2 size, out ScrollRect scrollRect)
    {
        GameObject viewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(parent, false);
        RectTransform viewRect = viewGo.GetComponent<RectTransform>();
        viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.sizeDelta = size;
        viewRect.anchoredPosition = anchoredPosition;

        Image viewImage = viewGo.GetComponent<Image>();
        viewImage.color = new Color(0f, 0f, 0f, 0.15f);
        Mask mask = viewGo.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewGo.transform, false);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchoredPosition = Vector2.zero;

        scrollRect = viewGo.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return contentGo.transform;
    }

    /// <summary>
    /// クエスト1件分の行テンプレート。Title/Rewardの2段テキストを持つ。受注可能リスト用のみButtonを持たせる。
    /// 幅は上下ストレッチアンカー(anchorMin.x=0, anchorMax.x=1)でContentの幅に自動追従させ、
    /// 高さ・縦位置はCS_QuestBoardMenu側でインスタンス化するたびに直接計算する
    /// </summary>
    private GameObject CreateQuestRow(Transform parent, string name, bool withAcceptButton)
    {
        GameObject rowGo = withAcceptButton
            ? new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button))
            : new GameObject(name, typeof(RectTransform), typeof(Image));
        rowGo.transform.SetParent(parent, false);
        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, QUEST_ROW_HEIGHT);
        rowRect.anchoredPosition = Vector2.zero;

        Image image = rowGo.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UI_Button.png");
        image.type = Image.Type.Sliced;

        if (withAcceptButton)
        {
            Button button = rowGo.GetComponent<Button>();
            button.targetGraphic = image;
        }

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(rowGo.transform, false);
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.5f);
        titleRect.anchorMax = new Vector2(0.95f, 1f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = Color.white;
        titleText.fontSize = 20;

        GameObject rewardGo = new GameObject("Reward", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewardGo.transform.SetParent(rowGo.transform, false);
        RectTransform rewardRect = rewardGo.GetComponent<RectTransform>();
        rewardRect.anchorMin = new Vector2(0.05f, 0f);
        rewardRect.anchorMax = new Vector2(0.95f, 0.5f);
        rewardRect.sizeDelta = Vector2.zero;
        rewardRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI rewardText = rewardGo.GetComponent<TextMeshProUGUI>();
        rewardText.alignment = TextAlignmentOptions.MidlineLeft;
        rewardText.color = new Color(0.8f, 0.8f, 0.85f);
        rewardText.fontSize = 16;

        return rowGo;
    }

    private void CreateTownGate(Scene townScene, CS_MapSelectMenu mapSelectMenu)
    {
        GameObject go = new GameObject("TownGate");
        SceneManager.MoveGameObjectToScene(go, townScene);
        go.transform.position = new Vector3(TOWN_CENTER.x, 1f, 0f);

        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TownSignpost.png");

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 1f);

        CS_TownGate gate = go.AddComponent<CS_TownGate>();
        SerializedObject so = new SerializedObject(gate);
        so.FindProperty("_mapSelectMenu").objectReferenceValue = mapSelectMenu;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
