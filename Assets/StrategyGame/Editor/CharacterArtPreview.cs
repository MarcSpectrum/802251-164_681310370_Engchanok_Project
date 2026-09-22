using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Engchanok.StrategyGame.Editor
{
    public static class CharacterArtPreview
    {
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var kinds=new[]{EntityKind.Worker,EntityKind.Soldier,EntityKind.Defender,EntityKind.Ranger,EntityKind.Medic,EntityKind.Engineer,EntityKind.Enemy,EntityKind.Runner,EntityKind.Brute,EntityKind.Lancer,EntityKind.Breaker,EntityKind.Warden,EntityKind.Juggernaut};
            for(int i=0;i<kinds.Length;i++)
            {
                var root=new GameObject(kinds[i].ToString());
                root.transform.position=new Vector3((i<6?i-2.5f:i-9)*3.5f,0,i<6?5:0);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StrategyGame/Prefabs/"+kinds[i]+".prefab");
                foreach(Transform child in prefab.transform) Object.Instantiate(child.gameObject,root.transform,false);
                var view=root.GetComponentInChildren<StrategyCharacterView>();
                view.idle.SampleAnimation(view.animator.gameObject,.4f);
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale=Vector3.one*8;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color=new Color(.28f,.4f,.24f); floor.GetComponent<Renderer>().sharedMaterial=material;
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.6f; sun.transform.rotation=Quaternion.Euler(50,-30,0);
            RenderSettings.ambientLight=new Color(.65f,.68f,.7f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var camera=new GameObject("Preview camera").AddComponent<Camera>(); camera.transform.position=new Vector3(0,12,22); camera.transform.LookAt(new Vector3(0,1.3f,2.8f));
            camera.orthographic=true; camera.orthographicSize=8; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.28f,.4f,.24f);
            var target=new RenderTexture(1800,1000,24); camera.targetTexture=target; camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=target;
            var texture=new Texture2D(1800,1000,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1800,1000),0,0); texture.Apply();
            Directory.CreateDirectory("Builds/AssetIntegration"); File.WriteAllBytes("Builds/AssetIntegration/roster.png",texture.EncodeToPNG());
            RenderTexture.active=previous; camera.targetTexture=null; Object.DestroyImmediate(target); Object.DestroyImmediate(texture); Object.DestroyImmediate(material);
            Debug.Log("CHARACTER_PREVIEW_COMPLETE");
        }
        public static void ApplyAndCapture() { OutpostArtBuilder.Apply(); Capture(); }
    }
}
