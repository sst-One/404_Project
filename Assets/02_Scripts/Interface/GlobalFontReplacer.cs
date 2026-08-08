using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class GlobalFontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/404 Project/일괄 폰트 교체")]
    public static void ShowWindow()
    {
        GetWindow<GlobalFontReplacer>("폰트 일괄 교체");
    }

    private void OnGUI()
    {
        GUILayout.Label("TextMeshPro 폰트 일괄 교체 도구");
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("새 폰트 에셋", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("현재 열린 씬의 모든 폰트 변경"))
        {
            ReplaceFontsInCurrentScene();
        }

        if (GUILayout.Button("모든 프리팹의 폰트 변경"))
        {
            ReplaceFontsInPrefabs();
        }
    }

    private void ReplaceFontsInCurrentScene()
    {
        if (targetFont == null) return;

        TextMeshProUGUI[] textComponents = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        int count = 0;

        foreach (var text in textComponents)
        {
            Undo.RecordObject(text, "Replace Font");
            text.font = targetFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"현재 씬에서 {count}개의 폰트가 교체되었습니다.");
    }

    private void ReplaceFontsInPrefabs()
    {
        if (targetFont == null) return;

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

            if (texts.Length > 0)
            {
                foreach (var text in texts)
                {
                    text.font = targetFont;
                    count++;
                }
                EditorUtility.SetDirty(prefab);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"모든 프리팹에서 {count}개의 폰트가 교체되었습니다.");
    }
}