using UnityEngine;
using UnityEditor;
using TMPro;

public class SceneFontReplacer : EditorWindow
{
    private TMP_FontAsset newFont;

    [MenuItem("Tools/Replace Scene TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<SceneFontReplacer>("Scene TMP Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("1. シーン内の全TMPフォント一括変更", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("新しいフォントアセット", newFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("現在のシーン全体のフォントを置換", GUILayout.Height(30)))
        {
            ReplaceAllFonts();
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 区切り線
        EditorGUILayout.Space(10);

        GUILayout.Label("2. シーン内のTMP Bold(太字)一括解除", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("現在のシーンのBoldをすべて解除", GUILayout.Height(30)))
        {
            RemoveAllBoldStyles();
        }
    }

    // --- フォント一括置換処理 ---
    private void ReplaceAllFonts()
    {
        if (newFont == null)
        {
            EditorUtility.DisplayDialog("エラー", "新しいフォントアセットをセットしてください。", "OK");
            return;
        }

        // 非表示のオブジェクトも含めてシーン内の全TMPコンポーネントを取得
        TextMeshProUGUI[] uiTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        TextMeshPro[] worldTexts = Resources.FindObjectsOfTypeAll<TextMeshPro>();

        int count = 0;

        // Undo（元に戻す機能）に対応させるため登録
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var text in uiTexts)
        {
            // プレハブアセットではなく、現在のシーンに存在するオブジェクトのみを対象にする
            if (EditorUtility.IsPersistent(text.transform.root.gameObject)) continue;

            Undo.RecordObject(text, "Replace TMP Font");
            text.font = newFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        foreach (var text in worldTexts)
        {
            if (EditorUtility.IsPersistent(text.transform.root.gameObject)) continue;

            Undo.RecordObject(text, "Replace TMP Font");
            text.font = newFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("完了", $"シーン内の {count} 個のテキストフォントを変更しました。", "OK");
    }

    // --- Boldスタイル一括解除処理 ---
    private void RemoveAllBoldStyles()
    {
        TextMeshProUGUI[] uiTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        TextMeshPro[] worldTexts = Resources.FindObjectsOfTypeAll<TextMeshPro>();

        int count = 0;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // UI用テキストの処理
        foreach (var text in uiTexts)
        {
            if (EditorUtility.IsPersistent(text.transform.root.gameObject)) continue;

            // Boldフラグが立っているか判定
            if ((text.fontStyle & FontStyles.Bold) != 0)
            {
                Undo.RecordObject(text, "Remove Bold Style");
                // ビット演算でBoldフラグのみを解除（ItalicやUnderline等は維持）
                text.fontStyle &= ~FontStyles.Bold;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        // 3Dテキストの処理
        foreach (var text in worldTexts)
        {
            if (EditorUtility.IsPersistent(text.transform.root.gameObject)) continue;

            if ((text.fontStyle & FontStyles.Bold) != 0)
            {
                Undo.RecordObject(text, "Remove Bold Style");
                text.fontStyle &= ~FontStyles.Bold;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("完了", $"{count} 個のテキストから Bold スタイルを解除しました。", "OK");
    }
}