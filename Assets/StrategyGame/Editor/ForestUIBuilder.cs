using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame.Editor
{
    public static class ForestUIBuilder
    {
        [MenuItem("Strategy Game/Refresh Forest UI")]
        public static void Apply()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach(string path in new[]{StrategyProjectBuilder.MenuPath,StrategyProjectBuilder.GamePath})
            {
                EditorSceneManager.OpenScene(path);
                var menu=Object.FindFirstObjectByType<StrategyMenu>();
                if(menu!=null) { if(menu.canvas!=null) Object.DestroyImmediate(menu.canvas.gameObject); menu.canvas=null; menu.BuildUI(); }
                var hud=Object.FindFirstObjectByType<StrategyHud>();
                if(hud!=null) { if(hud.canvas!=null) Object.DestroyImmediate(hud.canvas.gameObject); hud.canvas=null; hud.BuildUI(); }
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets(); Debug.Log("FOREST_UI_COMPLETE");
        }
    }
}
