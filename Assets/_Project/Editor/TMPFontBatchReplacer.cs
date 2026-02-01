#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public static class TMPFontBatchReplacer
{
    [MenuItem("Tools/VSL/Replace TMP Font In Open Scenes")]
    public static void ReplaceInOpenScenes()
    {
        var font = Selection.activeObject as TMP_FontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("TMP Font Replace",
                "Project 창에서 TMP_FontAsset(한글 폰트 에셋)을 선택한 다음 다시 실행해줘.",
                "OK");
            return;
        }

        int count = 0;
        foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Replace TMP Font");
            t.font = font;
            EditorUtility.SetDirty(t);
            count++;
        }

        EditorUtility.DisplayDialog("TMP Font Replace", $"완료! 변경된 TMP 텍스트: {count}개", "OK");
    }

    [MenuItem("Tools/VSL/Replace TMP Font In Selected Prefabs")]
    public static void ReplaceInSelectedPrefabs()
    {
        var font = Selection.activeObject as TMP_FontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("TMP Font Replace",
                "Project 창에서 TMP_FontAsset(한글 폰트 에셋)을 먼저 선택해줘.",
                "OK");
            return;
        }

        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("TMP Font Replace",
                "Project 창에서 프리팹들을 함께 선택한 다음 실행해줘.\n(또는 씬 교체 메뉴를 사용해도 돼)",
                "OK");
            return;
        }

        int total = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // 프리팹 내용 수정
            var root = PrefabUtility.LoadPrefabContents(path);
            var texts = root.GetComponentsInChildren<TMP_Text>(true);

            foreach (var t in texts)
            {
                t.font = font;
                total++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("TMP Font Replace", $"완료! 변경된 TMP 텍스트: {total}개", "OK");
    }
}
#endif
