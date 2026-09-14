using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// FieldSceneのマップ(60x40)を、地面(歩行可)と障害物(衝突あり)の2枚のTilemapへ
/// テーマ別に手続き生成するエディタツール。手書きでタイルデータを編集するリスクを避け、
/// Unity自身のTilemap APIで書き込む方式。テーマごとにSeedを変えて何度でも再生成できる
/// </summary>
public class CSED_FieldMapGenerator : EditorWindow
{
    private const int W = 60;
    private const int H = 40;
    private const int CENTER_X = W / 2;
    private const int CENTER_Y = H / 2;
    private const int SPAWN_CLEAR_RADIUS = 5;

    private enum Theme { Grassland, Desert, Wetlands, Snow }

    private Theme _theme = Theme.Grassland;
    private int _seed = 42;
    private int _encounterZoneCount = 8;
    private int _chestCount = 2;

    private System.Random _rand;

    [MenuItem("Tools/Field/Field Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<CSED_FieldMapGenerator>("フィールド生成");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "FieldSceneを開いた状態で実行してください。\n" +
            "Grid配下の「Ground」(地面)と「Tilemap」(障害物)を書き換えます。",
            MessageType.Info);

        EditorGUILayout.Space();
        _theme = (Theme)EditorGUILayout.EnumPopup("テーマ", _theme);
        _seed = EditorGUILayout.IntField("Seed", _seed);
        _encounterZoneCount = Mathf.Max(0, EditorGUILayout.IntField("エンカウントエリアの数", _encounterZoneCount));
        _chestCount = Mathf.Max(0, EditorGUILayout.IntField("宝箱の数", _chestCount));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("ランダムなSeedにする"))
        {
            _seed = UnityEngine.Random.Range(0, 1000000);
        }
        if (GUILayout.Button("生成", GUILayout.Height(28)))
        {
            Generate(_theme, _seed, _encounterZoneCount, _chestCount);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void Generate(Theme theme, int seed, int encounterZoneCount, int chestCount)
    {
        Grid grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError("シーン内にGridが見つかりません。FieldSceneを開いてから実行してください。");
            return;
        }

        Tilemap groundTilemap = GetOrCreateGroundTilemap(grid);
        Tilemap obstacleTilemap = grid.transform.Find("Tilemap")?.GetComponent<Tilemap>();
        if (obstacleTilemap == null)
        {
            Debug.LogError("Grid配下に障害物用の「Tilemap」が見つかりません。");
            return;
        }

        groundTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

        _rand = new System.Random(seed);

        switch (theme)
        {
            case Theme.Grassland:
                GenerateGrassland(groundTilemap, obstacleTilemap);
                break;
            case Theme.Desert:
                GenerateDesert(groundTilemap, obstacleTilemap);
                break;
            case Theme.Wetlands:
                GenerateWetlands(groundTilemap, obstacleTilemap);
                break;
            case Theme.Snow:
                GenerateSnow(groundTilemap, obstacleTilemap);
                break;
        }

        PlaceEncounterZonesAndChests(grid, obstacleTilemap, encounterZoneCount, chestCount);

        EditorUtility.SetDirty(groundTilemap);
        EditorUtility.SetDirty(obstacleTilemap);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);

        Debug.Log($"{theme}のフィールドマップを生成しました(Seed: {seed})。シーンの保存をお忘れなく。");
    }

    private const string ENCOUNTER_DATA_COMMON = "Assets/Data/EncounterData/DB_Encounter_Common.asset";
    private const string ENCOUNTER_DATA_MID = "Assets/Data/EncounterData/DB_Encounter_Mid.asset";
    private const string ENCOUNTER_DATA_ELITE = "Assets/Data/EncounterData/DB_Encounter_Elite.asset";
    private static readonly Vector3 CHEST_SCALE = new Vector3(1f, 1f, 1f); // マス目1つ分に収まる大きさ

    /// <summary>
    /// エンカウントシンボル・宝箱を指定数ぴったりに揃えた上で、障害物の無い位置へ配置する。
    /// 地形をテーマ別に再生成すると元の座標が岩・木・水などと重なる可能性があるため、
    /// 障害物Tilemapを直接参照して空いているマスだけを探す。オブジェクト同士も一定距離離す
    /// </summary>
    private void PlaceEncounterZonesAndChests(Grid grid, Tilemap obstacleTilemap, int encounterZoneCount, int chestCount)
    {
        Transform fieldEnvironment = grid.transform.parent != null ? grid.transform.parent : grid.transform;
        Transform encounterParent = FindOrCreateChild(fieldEnvironment, "EncountAreaParent");

        var symbols = EnsureEncounterSymbols(encounterParent, encounterZoneCount);
        var chests = EnsureTreasureChests(fieldEnvironment, chestCount);

        const int MARGIN = 3;
        const float MIN_DISTANCE = 6f;
        const int MAX_TRIES = 500;
        List<Vector2> placed = new List<Vector2>();

        // 障害物Tilemapのマス目(整数座標)そのものを基準に判定・配置する。
        // 連続値のまま四捨五入して判定すると、実際に置かれる座標(小数)とチェックしたマスが
        // 1マスずれることがあり、「チェック上は空きだが見た目は障害物に埋もれる」原因になっていた
        bool IsAreaClear(int ix, int iy, float radius)
        {
            int ri = Mathf.CeilToInt(radius);
            for (int dy = -ri; dy <= ri; dy++)
            {
                for (int dx = -ri; dx <= ri; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int nx = ix + dx;
                    int ny = iy + dy;
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) return false;
                    if (obstacleTilemap.GetTile(new Vector3Int(nx, ny, 0)) != null) return false;
                }
            }
            return true;
        }

        bool TryFindPosition(float clearRadius, out Vector2 result)
        {
            for (int attempt = 0; attempt < MAX_TRIES; attempt++)
            {
                int ix = RandInt(MARGIN, W - MARGIN - 1);
                int iy = RandInt(MARGIN, H - MARGIN - 1);
                // タイルの見た目上の中心(マスの中央)に合わせる
                Vector2 center = new Vector2(ix + 0.5f, iy + 0.5f);

                bool tooClose = false;
                foreach (var p in placed)
                {
                    if ((p - center).sqrMagnitude < MIN_DISTANCE * MIN_DISTANCE)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
                if (!IsAreaClear(ix, iy, clearRadius)) continue;

                result = center;
                return true;
            }

            result = Vector2.zero;
            return false;
        }

        foreach (var symbol in symbols)
        {
            float radius = Mathf.Max(symbol.transform.localScale.x, symbol.transform.localScale.y) / 2f + 0.5f;
            if (TryFindPosition(radius, out Vector2 pos))
            {
                Vector3 local = symbol.transform.localPosition;
                symbol.transform.localPosition = new Vector3(pos.x, pos.y, local.z);
                placed.Add(pos);
                EditorUtility.SetDirty(symbol);
            }
            else
            {
                Debug.LogWarning($"{symbol.name} の配置場所が見つかりませんでした。");
            }
        }

        foreach (var chest in chests)
        {
            float radius = Mathf.Max(chest.transform.localScale.x, chest.transform.localScale.y) / 2f + 0.5f;
            if (TryFindPosition(radius, out Vector2 pos))
            {
                Vector3 local = chest.transform.localPosition;
                chest.transform.localPosition = new Vector3(pos.x, pos.y, local.z);
                placed.Add(pos);
                EditorUtility.SetDirty(chest);
            }
            else
            {
                Debug.LogWarning($"{chest.name} の配置場所が見つかりませんでした。");
            }
        }
    }

    private Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>
    /// エンカウントシンボルの数を指定数ぴったりに揃える(不足分は新規作成、超過分は末尾から削除)。
    /// 新規作成分にだけ、共通/中堅/エリートの3段階全てを候補として割り当てる
    /// </summary>
    private CS_EncounterSymbol[] EnsureEncounterSymbols(Transform parent, int count)
    {
        var symbols = new List<CS_EncounterSymbol>(
            UnityEngine.Object.FindObjectsByType<CS_EncounterSymbol>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        while (symbols.Count > count)
        {
            CS_EncounterSymbol last = symbols[symbols.Count - 1];
            symbols.RemoveAt(symbols.Count - 1);
            Undo.DestroyObjectImmediate(last.gameObject);
        }

        CSO_EncounterData common = AssetDatabase.LoadAssetAtPath<CSO_EncounterData>(ENCOUNTER_DATA_COMMON);
        CSO_EncounterData mid = AssetDatabase.LoadAssetAtPath<CSO_EncounterData>(ENCOUNTER_DATA_MID);
        CSO_EncounterData elite = AssetDatabase.LoadAssetAtPath<CSO_EncounterData>(ENCOUNTER_DATA_ELITE);

        while (symbols.Count < count)
        {
            GameObject go = new GameObject($"EncounterZone_{symbols.Count + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create Encounter Zone");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(3f, 3f, 1f);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            CS_EncounterSymbol symbol = go.AddComponent<CS_EncounterSymbol>();

            SerializedObject so = new SerializedObject(symbol);
            so.FindProperty("_encounterRate").floatValue = 0.6f;
            so.FindProperty("_encounterCoolTime").floatValue = 5f;
            SerializedProperty dataProp = so.FindProperty("_encounterData");
            dataProp.arraySize = 3;
            dataProp.GetArrayElementAtIndex(0).objectReferenceValue = common;
            dataProp.GetArrayElementAtIndex(1).objectReferenceValue = mid;
            dataProp.GetArrayElementAtIndex(2).objectReferenceValue = elite;
            so.ApplyModifiedPropertiesWithoutUndo();

            symbols.Add(symbol);
        }

        return symbols.ToArray();
    }

    /// <summary>
    /// 宝箱の数を指定数ぴったりに揃える(不足分は新規作成、超過分は末尾から削除)。
    /// 新規作成分にだけ一意なIDと報酬装備を割り当てる
    /// </summary>
    private CS_TreasureChest[] EnsureTreasureChests(Transform parent, int count)
    {
        var chests = new List<CS_TreasureChest>(
            UnityEngine.Object.FindObjectsByType<CS_TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        while (chests.Count > count)
        {
            CS_TreasureChest last = chests[chests.Count - 1];
            chests.RemoveAt(chests.Count - 1);
            Undo.DestroyObjectImmediate(last.gameObject);
        }

        string[] rewardPool = { "IronPlate", "PoisonRing", "SpeedCharm", "IronSword", "LeatherArmor", "PoisonDagger" };

        while (chests.Count < count)
        {
            int index = chests.Count;
            GameObject go = new GameObject($"TreasureChest_{index + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Create Treasure Chest");
            go.transform.SetParent(parent, false);
            go.transform.localScale = CHEST_SCALE;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TreasureChest.png");
            spriteRenderer.color = Color.white;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            CS_TreasureChest chest = go.AddComponent<CS_TreasureChest>();

            SerializedObject so = new SerializedObject(chest);
            so.FindProperty("_chestId").stringValue = $"chest_field_{index + 1:D2}";
            so.FindProperty("_rewardItemId").stringValue = "";
            so.FindProperty("_rewardItemCount").intValue = 0;
            so.FindProperty("_rewardEquipmentId").stringValue = rewardPool[index % rewardPool.Length];
            so.ApplyModifiedPropertiesWithoutUndo();

            chests.Add(chest);
        }

        // 以前の生成で古い仮素材(岩テクスチャの色替え・過大なスケール)のまま残っている宝箱も、
        // 既存/新規を問わず全て現在の見た目(スプライト・大きさ)へ揃え直す
        Sprite chestSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Tiles/Objects/Tile_TreasureChest.png");
        foreach (var chest in chests)
        {
            SpriteRenderer spriteRenderer = chest.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = chest.gameObject.AddComponent<SpriteRenderer>();
            }
            spriteRenderer.sprite = chestSprite;
            spriteRenderer.color = Color.white;
            chest.transform.localScale = CHEST_SCALE;
            EditorUtility.SetDirty(spriteRenderer);
            EditorUtility.SetDirty(chest.transform);
        }

        return chests.ToArray();
    }

    private Tilemap GetOrCreateGroundTilemap(Grid grid)
    {
        Transform existing = grid.transform.Find("Ground");
        if (existing != null)
        {
            return existing.GetComponent<Tilemap>();
        }

        GameObject groundObject = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(groundObject, "Create Ground Tilemap");
        groundObject.transform.SetParent(grid.transform, false);
        groundObject.transform.SetAsFirstSibling(); // 描画順を障害物レイヤーより下にする

        Tilemap tilemap = groundObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = groundObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -10;

        return tilemap;
    }

    private static TileBase Load(string path)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (tile == null) Debug.LogError($"タイルアセットが見つかりません: {path}");
        return tile;
    }

    // ------------------------------------------------------------ helpers --

    private bool NearSpawn(int x, int y)
    {
        int dx = x - CENTER_X, dy = y - CENTER_Y;
        return dx * dx + dy * dy <= SPAWN_CLEAR_RADIUS * SPAWN_CLEAR_RADIUS;
    }

    private float RandRange(float a, float b) => a + (float)_rand.NextDouble() * (b - a);
    private int RandInt(int a, int b) => a + _rand.Next(b - a + 1);

    /// <summary>
    /// 円形のランダムウォークで有機的な塊(森・岩・池など)を[x,y]グリッドへ焼き込む
    /// </summary>
    private void StampBlob(bool[,] mask, float cx, float cy, int steps, float rMin, float rMax, bool avoidSpawn)
    {
        float x = cx, y = cy;
        for (int s = 0; s < steps; s++)
        {
            float r = RandRange(rMin, rMax);
            int ri = Mathf.CeilToInt(r) + 1;
            for (int dy = -ri; dy <= ri; dy++)
            {
                for (int dx = -ri; dx <= ri; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int nx = Mathf.RoundToInt(x + dx);
                    int ny = Mathf.RoundToInt(y + dy);
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    if (avoidSpawn && NearSpawn(nx, ny)) continue;
                    mask[nx, ny] = true;
                }
            }
            x = Mathf.Clamp(x + RandRange(-2.2f, 2.2f), 1, W - 2);
            y = Mathf.Clamp(y + RandRange(-2.2f, 2.2f), 1, H - 2);
        }
    }

    /// <summary>
    /// 曲がりくねった帯状の道を[x,y]グリッドへ焼き込む。skipMaskがtrueのセルは上書きしない(池を避ける等)
    /// </summary>
    private void CarvePath(bool[,] mask, bool[,] skipMask, float x, float y, float dx, float dy, int length, float width)
    {
        for (int i = 0; i < length; i++)
        {
            int wi = Mathf.CeilToInt(width) + 1;
            for (int wdy = -wi; wdy <= wi; wdy++)
            {
                for (int wdx = -wi; wdx <= wi; wdx++)
                {
                    if (wdx * wdx + wdy * wdy > width * width) continue;
                    int nx = Mathf.RoundToInt(x + wdx);
                    int ny = Mathf.RoundToInt(y + wdy);
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    if (skipMask != null && skipMask[nx, ny]) continue;
                    mask[nx, ny] = true;
                }
            }
            x = Mathf.Clamp(x + dx + RandRange(-0.5f, 0.5f), 1, W - 2);
            y = Mathf.Clamp(y + dy + RandRange(-0.5f, 0.5f), 1, H - 2);
        }
    }

    private void ClearNearSpawn(bool[,] mask)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (NearSpawn(x, y)) mask[x, y] = false;
    }

    /// <summary>
    /// sourceがtrueのセルの周囲radius以内を全てtrueにした新しいマスクを返す(縁取り・ハロー用)
    /// </summary>
    private bool[,] Dilate(bool[,] source, float radius)
    {
        bool[,] result = new bool[W, H];
        int ri = Mathf.CeilToInt(radius);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (!source[x, y]) continue;
                for (int dy = -ri; dy <= ri; dy++)
                {
                    for (int dx = -ri; dx <= ri; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                        result[nx, ny] = true;
                    }
                }
            }
        }
        return result;
    }

    private void Paint(Tilemap tilemap, bool[,] mask, TileBase tile)
    {
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (mask[x, y]) tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private void Fill(Tilemap tilemap, TileBase tile)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    // ---------------------------------------------------------- grassland --

    /// <summary>
    /// 草原: 外周寄りの森+まばらな岩+中央から伸びる土道
    /// </summary>
    private void GenerateGrassland(Tilemap groundTilemap, Tilemap obstacleTilemap)
    {
        TileBase grass = Load("Assets/Tiles/Grassland/Tile_Grass.asset");
        TileBase dirt = Load("Assets/Tiles/Grassland/Tile_Dirt.asset");
        TileBase rock = Load("Assets/Tiles/Grassland/Tile_Rock.asset");
        TileBase tree = Load("Assets/Tiles/Grassland/Tile_Tree.asset");

        Fill(groundTilemap, grass);

        bool[,] dirtMask = new bool[W, H];
        bool[,] rockMask = new bool[W, H];
        bool[,] treeMask = new bool[W, H];

        for (int i = 0; i < 10; i++)
        {
            float sx, sy;
            PickEdgePoint(out sx, out sy);
            StampBlob(treeMask, sx, sy, RandInt(5, 9), 1.4f, 2.6f, true);
        }
        for (int i = 0; i < 4; i++)
        {
            StampBlob(treeMask, RandRange(8, W - 8), RandRange(8, H - 8), RandInt(5, 9), 1.4f, 2.6f, true);
        }
        for (int i = 0; i < 7; i++)
        {
            StampBlob(rockMask, RandRange(5, W - 5), RandRange(5, H - 5), RandInt(3, 5), 1.0f, 2.0f, true);
        }

        CarvePath(dirtMask, null, 0, CENTER_Y + RandInt(-2, 2), 1, 0, W, 1.3f);
        CarvePath(dirtMask, null, CENTER_X + RandInt(-6, 6), CENTER_Y, 0, 1, H / 2 + 4, 1.1f);
        CarvePath(dirtMask, null, CENTER_X - RandInt(2, 8), CENTER_Y, 0.6f, -1, H / 2, 1.0f);

        ClearNearSpawn(rockMask);
        ClearNearSpawn(treeMask);

        Paint(groundTilemap, dirtMask, dirt);
        Paint(obstacleTilemap, rockMask, rock);
        Paint(obstacleTilemap, treeMask, tree);
    }

    // -------------------------------------------------------------- desert --

    /// <summary>
    /// 砂漠: 砂地に大きめの岩山を数箇所、まれにオアシス(池)。オアシスの水際だけ草地を縁取りする。
    /// 道は作らない(轍の無い砂丘)
    /// </summary>
    private void GenerateDesert(Tilemap groundTilemap, Tilemap obstacleTilemap)
    {
        TileBase sand = Load("Assets/Tiles/Desert/Tile_Sand.asset");
        TileBase desertRock = Load("Assets/Tiles/Desert/Tile_DesertRock.asset");
        TileBase water = Load("Assets/Tiles/Desert/Tile_Water.asset");
        TileBase oasisGrass = Load("Assets/Tiles/Grassland/Tile_Grass.asset");

        Fill(groundTilemap, sand);

        bool[,] rockMask = new bool[W, H];
        bool[,] oasisMask = new bool[W, H];

        for (int i = 0; i < 6; i++)
        {
            StampBlob(rockMask, RandRange(5, W - 5), RandRange(5, H - 5), RandInt(3, 5), 1.8f, 3.2f, true);
        }
        int oasisCount = RandInt(1, 2);
        for (int i = 0; i < oasisCount; i++)
        {
            StampBlob(oasisMask, RandRange(8, W - 8), RandRange(8, H - 8), RandInt(2, 3), 1.5f, 2.3f, true);
        }

        ClearNearSpawn(rockMask);
        ClearNearSpawn(oasisMask);

        // オアシスの水際だけ草の縁取りを作る(水セル自体は除く)
        bool[,] oasisHalo = Dilate(oasisMask, 2.2f);
        bool[,] grassRing = new bool[W, H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                grassRing[x, y] = oasisHalo[x, y] && !oasisMask[x, y];
            }
        }

        Paint(groundTilemap, grassRing, oasisGrass);
        Paint(obstacleTilemap, rockMask, desertRock);
        Paint(obstacleTilemap, oasisMask, water);
    }

    // ------------------------------------------------------------ wetlands --

    /// <summary>
    /// 湖畔湿地: 地面そのものが泥濘(土)で、水場が複数点在する沼地。岩は置かず、
    /// 水際にだけ草の茂みを縁取りし、葦(木)は水場周辺に密生させる。
    /// 草原と構図がかぶらないよう、ベース地面(草→泥)・道(あり→なし)・障害物種別(岩あり→なし)を
    /// あえて逆転させている
    /// </summary>
    private void GenerateWetlands(Tilemap groundTilemap, Tilemap obstacleTilemap)
    {
        TileBase mud = Load("Assets/Tiles/Grassland/Tile_Dirt.asset");
        TileBase reedGrass = Load("Assets/Tiles/Grassland/Tile_Grass.asset");
        TileBase tree = Load("Assets/Tiles/Grassland/Tile_Tree.asset");
        TileBase water = Load("Assets/Tiles/Wetlands/Tile_Water.asset");

        Fill(groundTilemap, mud);

        bool[,] waterMask = new bool[W, H];
        bool[,] treeMask = new bool[W, H];

        // 複数の水場を点在させ、沼地らしく水の面積を大きく取る
        int poolCount = RandInt(3, 4);
        for (int i = 0; i < poolCount; i++)
        {
            StampBlob(waterMask, RandRange(6, W - 6), RandRange(6, H - 6), RandInt(4, 6), 2.4f, 4.2f, true);
        }

        // 葦(木)は水場周辺に密生させる
        bool[,] waterHaloWide = Dilate(waterMask, 3.5f);
        for (int i = 0; i < 16; i++)
        {
            int tries = 0;
            float sx, sy;
            do
            {
                sx = RandRange(3, W - 3);
                sy = RandRange(3, H - 3);
                tries++;
            } while (tries < 20 && !waterHaloWide[Mathf.RoundToInt(sx), Mathf.RoundToInt(sy)]);
            StampBlob(treeMask, sx, sy, RandInt(2, 4), 1.0f, 1.8f, true);
        }

        ClearNearSpawn(waterMask);
        ClearNearSpawn(treeMask);

        // 水際だけ草の茂みを縁取る(水セル自体は除く)。木は縁取りより先に上書きされないよう後で描く
        bool[,] waterHalo = Dilate(waterMask, 1.8f);
        bool[,] grassFringe = new bool[W, H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                grassFringe[x, y] = waterHalo[x, y] && !waterMask[x, y];
            }
        }

        Paint(groundTilemap, grassFringe, reedGrass);
        Paint(obstacleTilemap, treeMask, tree);
        Paint(obstacleTilemap, waterMask, water);
    }

    // ---------------------------------------------------------------- snow --

    /// <summary>
    /// 雪原: 草原と同じ構図(外周寄りの林+まばらな岩+中央十字路)だが、
    /// 雪化粧した木・岩、雪原の地面、金色の踏み固められた道、まれに凍った池と、
    /// パレットと素材を丸ごと入れ替えて別の地形に見せている
    /// </summary>
    private void GenerateSnow(Tilemap groundTilemap, Tilemap obstacleTilemap)
    {
        TileBase snow = Load("Assets/Tiles/Snow/Tile_Snow.asset");
        TileBase snowPath = Load("Assets/Tiles/Snow/Tile_SnowPath.asset");
        TileBase ice = Load("Assets/Tiles/Snow/Tile_Ice.asset");
        TileBase snowTree = Load("Assets/Tiles/Snow/Tile_SnowTree.asset");
        TileBase snowRock = Load("Assets/Tiles/Snow/Tile_SnowRock.asset");

        Fill(groundTilemap, snow);

        bool[,] pathMask = new bool[W, H];
        bool[,] treeMask = new bool[W, H];
        bool[,] rockMask = new bool[W, H];
        bool[,] iceMask = new bool[W, H];

        for (int i = 0; i < 10; i++)
        {
            float sx, sy;
            PickEdgePoint(out sx, out sy);
            StampBlob(treeMask, sx, sy, RandInt(5, 9), 1.4f, 2.6f, true);
        }
        for (int i = 0; i < 4; i++)
        {
            StampBlob(treeMask, RandRange(8, W - 8), RandRange(8, H - 8), RandInt(5, 9), 1.4f, 2.6f, true);
        }
        for (int i = 0; i < 7; i++)
        {
            StampBlob(rockMask, RandRange(5, W - 5), RandRange(5, H - 5), RandInt(3, 5), 1.0f, 2.0f, true);
        }

        CarvePath(pathMask, null, 0, CENTER_Y + RandInt(-2, 2), 1, 0, W, 1.3f);
        CarvePath(pathMask, null, CENTER_X + RandInt(-6, 6), CENTER_Y, 0, 1, H / 2 + 4, 1.1f);
        CarvePath(pathMask, null, CENTER_X - RandInt(2, 8), CENTER_Y, 0.6f, -1, H / 2, 1.0f);

        // まれに凍った池を1つ
        if (RandInt(0, 1) == 1)
        {
            StampBlob(iceMask, RandRange(10, W - 10), RandRange(10, H - 10), RandInt(4, 6), 2.2f, 3.6f, true);
        }

        ClearNearSpawn(treeMask);
        ClearNearSpawn(rockMask);
        ClearNearSpawn(iceMask);

        Paint(groundTilemap, pathMask, snowPath);
        Paint(obstacleTilemap, rockMask, snowRock);
        Paint(obstacleTilemap, treeMask, snowTree);
        Paint(obstacleTilemap, iceMask, ice);
    }

    private void PickEdgePoint(out float sx, out float sy)
    {
        int edge = _rand.Next(4);
        switch (edge)
        {
            case 0: sx = RandRange(0, W); sy = RandRange(0, 4); break; // top
            case 1: sx = RandRange(0, W); sy = RandRange(H - 4, H); break; // bottom
            case 2: sx = RandRange(0, 4); sy = RandRange(0, H); break; // left
            default: sx = RandRange(W - 4, W); sy = RandRange(0, H); break; // right
        }
    }
}
