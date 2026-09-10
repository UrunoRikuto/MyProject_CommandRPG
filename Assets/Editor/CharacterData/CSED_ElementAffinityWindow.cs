using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 全キャラクター×全属性の耐性倍率をまとめて確認・編集できる相性表ウィンドウ。
/// CSED_CharacterDataWindowの「属性相性表」ボタンから開く
/// </summary>
public class CSED_ElementAffinityWindow : EditorWindow
{
    private const string PREF_PREFIX = "CSED_ElementAffinityWindow.";
    private const float NAME_COLUMN_WIDTH = 130f;
    private const float ELEMENT_COLUMN_WIDTH = 55f;

    // Noneは耐性の影響を受けない(たたかうは常に等倍)ため表には出さない
    private static readonly CSE_ElementType[] ELEMENTS =
        System.Enum.GetValues(typeof(CSE_ElementType))
            .Cast<CSE_ElementType>()
            .Where(e => e != CSE_ElementType.None)
            .ToArray();

    private static readonly Dictionary<CSE_ElementType, string> ELEMENT_LABELS = new Dictionary<CSE_ElementType, string>
    {
        { CSE_ElementType.Fire, "炎" },
        { CSE_ElementType.Water, "水" },
        { CSE_ElementType.Grass, "草" },
        { CSE_ElementType.Ice, "氷" },
        { CSE_ElementType.Thunder, "雷" },
        { CSE_ElementType.Earth, "土" },
        { CSE_ElementType.Light, "光" },
        { CSE_ElementType.Dark, "闇" },
    };

    private Vector2 _scrollPosition;
    private List<CSO_CharacterData> _characters = new List<CSO_CharacterData>();
    private string _filterText = "";

    [MenuItem("Tools/Element Affinity Table (属性相性表)")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(CSED_ElementAffinityWindow));
        window.titleContent = new GUIContent("属性相性表");
        window.minSize = new Vector2(NAME_COLUMN_WIDTH + ELEMENT_COLUMN_WIDTH * ELEMENTS.Length + 40, 200);
    }

    private void OnEnable()
    {
        RefreshCharacterList();
    }

    private void RefreshCharacterList()
    {
        _characters.Clear();

        string[] guids = AssetDatabase.FindAssets("t:CSO_CharacterData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CSO_CharacterData data = AssetDatabase.LoadAssetAtPath<CSO_CharacterData>(path);
            if (data != null)
            {
                _characters.Add(data);
            }
        }

        _characters = _characters.OrderBy(c => c.characterName).ToList();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawHeaderRow();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (_characters.Count == 0)
        {
            EditorGUILayout.HelpBox("CSO_CharacterDataアセットが見つかりません。", MessageType.Info);
        }

        foreach (CSO_CharacterData character in _characters)
        {
            if (character == null || !MatchesFilter(character)) continue;
            DrawCharacterRow(character);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox("倍率: 1=等倍、1より大きい=弱点、1より小さい=耐性、0=無効。「たたかう」は常に無属性のためこの表の影響を受けません。", MessageType.None);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            RefreshCharacterList();
        }

        GUILayout.Label("フィルター:", GUILayout.Width(60));
        _filterText = EditorGUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{_characters.Count}体", GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();
    }

    private bool MatchesFilter(CSO_CharacterData character)
    {
        if (string.IsNullOrEmpty(_filterText)) return true;
        return character.characterName.ToLower().Contains(_filterText.ToLower());
    }

    private void DrawHeaderRow()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("キャラクター", EditorStyles.boldLabel, GUILayout.Width(NAME_COLUMN_WIDTH));

        foreach (var element in ELEMENTS)
        {
            GUILayout.Label(ELEMENT_LABELS[element], EditorStyles.boldLabel, GUILayout.Width(ELEMENT_COLUMN_WIDTH));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCharacterRow(CSO_CharacterData character)
    {
        var serializedObject = new SerializedObject(character);
        serializedObject.Update();
        var resistancesProp = serializedObject.FindProperty("_elementResistances");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(character.characterName, EditorStyles.linkLabel, GUILayout.Width(NAME_COLUMN_WIDTH)))
        {
            EditorGUIUtility.PingObject(character);
            Selection.activeObject = character;
        }

        foreach (var element in ELEMENTS)
        {
            float currentValue = character.GetElementMultiplier(element);
            float newValue = EditorGUILayout.FloatField(currentValue, GUILayout.Width(ELEMENT_COLUMN_WIDTH));
            if (!Mathf.Approximately(newValue, currentValue))
            {
                SetElementMultiplier(resistancesProp, element, newValue);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(character);
        }
    }

    /// <summary>
    /// 指定した属性の倍率を書き換える。既存エントリがあれば更新、無ければ追加する。
    /// 等倍(1)に戻した場合はリストを簡潔に保つためエントリ自体を削除する
    /// </summary>
    private void SetElementMultiplier(SerializedProperty resistancesProp, CSE_ElementType element, float value)
    {
        for (int i = 0; i < resistancesProp.arraySize; i++)
        {
            var entry = resistancesProp.GetArrayElementAtIndex(i);
            var elementProp = entry.FindPropertyRelative("element");
            if ((CSE_ElementType)elementProp.enumValueIndex == element)
            {
                if (Mathf.Approximately(value, 1f))
                {
                    resistancesProp.DeleteArrayElementAtIndex(i);
                }
                else
                {
                    entry.FindPropertyRelative("multiplier").floatValue = value;
                }
                return;
            }
        }

        if (Mathf.Approximately(value, 1f)) return; // 等倍のままなら追加不要

        int newIndex = resistancesProp.arraySize;
        resistancesProp.InsertArrayElementAtIndex(newIndex);
        var newEntry = resistancesProp.GetArrayElementAtIndex(newIndex);
        newEntry.FindPropertyRelative("element").enumValueIndex = (int)element;
        newEntry.FindPropertyRelative("multiplier").floatValue = value;
    }
}
