using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 日本語TMP Font Asset(Assets/Fonts/NotoSansJP SDF.asset)をプロジェクト標準フォントに設定し、
/// 既存のプレハブ・シーン内のTextMeshProUGUIを一括で差し替えるツール。
/// フォントアセット自体の作成はUnityの標準機能(Font Asset Creator)を使う前提(下記ヘルプ参照)。
/// スクリプトからのCreateFontAsset呼び出しはTMPバージョンによって挙動が変わりやすく、
/// ここでは安定しているAPI(SerializedObjectでの参照差し替え・コンポーネント走査)だけを使う
/// </summary>
public class CSED_JapaneseFontSetup : EditorWindow
{
    private const string SOURCE_FONT_PATH = "Assets/Fonts/NotoSansCJKjp-Regular.otf";
    private const string FONT_ASSET_PATH = "Assets/Fonts/NotoSansJP SDF.asset";

    [MenuItem("Tools/Font/Setup Japanese Font")]
    public static void ShowWindow()
    {
        GetWindow<CSED_JapaneseFontSetup>("日本語フォント設定");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "事前準備(初回のみ、GUIでの操作):\n" +
            $"1. Window > TextMeshPro > Font Asset Creator を開く\n" +
            $"2. Source Font File に {SOURCE_FONT_PATH} を指定\n" +
            "   (可変フォント(NotoSansJP[wght].ttf)はDynamic生成時に新規グリフを追加できないため、\n" +
            "    ウェイト固定のNotoSansCJKjp-Regular.otfに変更済み)\n" +
            "3. Atlas Population Mode を「Dynamic」にする(文字を実行時に必要な分だけ生成する方式。\n" +
            "   日本語は文字数が非常に多いため、あらかじめ全部焼き込むStaticではなくこちらを使う)\n" +
            "4. Atlas Resolution は 1024x1024 程度で十分\n" +
            "5. Generate Font Atlas → Save\n" +
            $"   保存先は必ず {FONT_ASSET_PATH} にする(既存のファイルを上書き)\n\n" +
            "その後、下のボタンを押すと:\n" +
            "・作成したフォントをプロジェクトの標準フォントに設定\n" +
            "・既存の全プレハブ・全シーンのTextMeshProUGUIを新フォントへ一括差し替え\n" +
            "・作業中のシーンは開き直すため、事前に保存しておいてください",
            MessageType.Info);

        if (GUILayout.Button("日本語フォントを適用", GUILayout.Height(28)))
        {
            Setup();
        }
    }

    private void Setup()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (fontAsset == null)
        {
            Debug.LogError($"{FONT_ASSET_PATH} が見つかりません。上のヘルプの手順でFont Asset Creatorから先に作成してください。" +
                            $"(元フォント: {SOURCE_FONT_PATH})");
            return;
        }

        SetAsDefaultFont(fontAsset);
        int prefabCount = UpdatePrefabs(fontAsset);
        int sceneCount = UpdateScenes(fontAsset);

        AssetDatabase.SaveAssets();
        Debug.Log($"日本語フォントの適用が完了しました。プレハブ{prefabCount}件・シーン{sceneCount}件のテキストを更新しました。");
    }

    private void SetAsDefaultFont(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("TMP Settingsが見つかりませんでした。標準フォントの設定はスキップします。");
            return;
        }

        SerializedObject so = new SerializedObject(settings);
        SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
    }

    /// <summary>
    /// プロジェクト内の全プレハブを走査し、TextMeshProUGUIのフォントを差し替える
    /// </summary>
    private int UpdatePrefabs(TMP_FontAsset fontAsset)
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool changed = false;
            foreach (TextMeshProUGUI text in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.font == fontAsset) continue;
                text.font = fontAsset;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Assets/Scenes配下の全シーンを開き、TextMeshProUGUIのフォントを差し替えて保存する
    /// </summary>
    private int UpdateScenes(TMP_FontAsset fontAsset)
    {
        int count = 0;
        List<string> scenePaths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Scenes/")) scenePaths.Add(path);
        }

        foreach (string path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text.font == fontAsset) continue;
                    text.font = fontAsset;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }
        }
        return count;
    }
}
