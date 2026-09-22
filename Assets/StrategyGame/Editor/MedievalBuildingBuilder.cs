using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace Engchanok.StrategyGame.Editor
{
    public static class MedievalBuildingBuilder
    {
        const string Art = "Assets/StrategyGame/Art/KayKit/MedievalHexagon/";
        public static readonly EntityKind[] Kinds = { EntityKind.Headquarters, EntityKind.Barracks, EntityKind.RangerPost, EntityKind.SupportBay, EntityKind.SupplyRelay, EntityKind.Turret };
        static string ModelName(EntityKind kind) => kind switch
        {
            EntityKind.Headquarters => "castle",
            EntityKind.Barracks => "barracks",
            EntityKind.RangerPost => "archeryrange",
            EntityKind.SupportBay => "church",
            EntityKind.SupplyRelay => "market",
            EntityKind.Turret => "tower_catapult",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        static Material AtlasMaterial()
        {
            const string path="Assets/StrategyGame/Art/Materials/Kay_MedievalHexagon.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/hexagons_medieval.png");
            if(texture==null) throw new InvalidOperationException("Missing Medieval Hexagon texture atlas.");
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap",texture); material.color=Color.white; material.SetFloat("_Smoothness",.12f);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        public static void ReplaceVisuals(GameObject root, EntityKind kind, float radius)
        {
            string path=Art+"Models/building_"+ModelName(kind)+"_blue.fbx";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null) throw new InvalidOperationException("Missing approved building: "+path);
            while(root.transform.childCount>0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            var visual=new GameObject("Medieval building visual").transform; visual.SetParent(root.transform,false);
            var model=Object.Instantiate(source,visual,false); model.name=source.name;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>AtlasMaterial()).ToArray();
            foreach(var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            // Preserve FBX import transforms and fit the complete model within the existing circular footprint.
            visual.localRotation=Quaternion.Euler(0,180,0);
            Bounds bounds=StrategyCameraController.VisualBounds(visual);
            float footprint=new Vector2(bounds.extents.x,bounds.extents.z).magnitude;
            visual.localScale=Vector3.one*(radius*.98f/Mathf.Max(.01f,footprint));
            bounds=StrategyCameraController.VisualBounds(visual);
            visual.position+=new Vector3(root.transform.position.x-bounds.center.x,root.transform.position.y-bounds.min.y,root.transform.position.z-bounds.center.z);
            bounds=StrategyCameraController.VisualBounds(visual);
            float height=bounds.max.y-root.transform.position.y;
            root.GetComponent<StrategyEntity>().buildingMarkerHeight=height+.3f;
            var selection=root.GetComponent<CapsuleCollider>();
            if(selection!=null) { selection.height=Mathf.Max(3,height); selection.center=Vector3.up*selection.height*.5f; }
        }
        [MenuItem("Strategy Game/Refresh Medieval Buildings")]
        public static void Apply()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<StrategySettings>("Assets/StrategyGame/Data/DefaultStrategy.asset");
            string tuning=EditorJsonUtility.ToJson(settings);
            foreach(var kind in Kinds)
            {
                string path="Assets/StrategyGame/Prefabs/"+kind+".prefab";
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try { ReplaceVisuals(prefab,kind,settings.Radius(kind)); PrefabUtility.SaveAsPrefabAsset(prefab,path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            foreach(string path in new[]{StrategyProjectBuilder.MenuPath,StrategyProjectBuilder.GamePath})
            {
                EditorSceneManager.OpenScene(path);
                if(path==StrategyProjectBuilder.MenuPath)
                {
                    var diorama=GameObject.Find("Menu outpost diorama").transform;
                    foreach(var kind in Kinds)
                    {
                        var display=diorama.Find(kind+" display");
                        if(display==null) throw new InvalidOperationException("Missing menu display: "+kind);
                        while(display.childCount>0) Object.DestroyImmediate(display.GetChild(0).gameObject);
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StrategyGame/Prefabs/"+kind+".prefab");
                        foreach(Transform child in prefab.transform) Object.Instantiate(child.gameObject,display,false);
                    }
                }
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            if(tuning!=EditorJsonUtility.ToJson(settings)) throw new InvalidOperationException("Building refresh changed tuning.");
            AssetDatabase.SaveAssets(); Debug.Log("MEDIEVAL_BUILDINGS_COMPLETE");
        }
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            for(int i=0;i<Kinds.Length;i++)
            {
                var root=new GameObject(Kinds[i].ToString()).transform;
                root.position=new Vector3((i%3-1)*7,0,i<3?6:0);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StrategyGame/Prefabs/"+Kinds[i]+".prefab");
                foreach(Transform child in prefab.transform) Object.Instantiate(child.gameObject,root,false);
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale=Vector3.one*8;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color=new Color(.32f,.44f,.26f); floor.GetComponent<Renderer>().sharedMaterial=material;
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.5f; sun.transform.rotation=Quaternion.Euler(50,-30,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.6f,.65f,.7f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var camera=new GameObject("Preview camera").AddComponent<Camera>(); camera.transform.position=new Vector3(11,15,-23); camera.transform.LookAt(new Vector3(0,1,3));
            camera.orthographic=true; camera.orthographicSize=9;
            var target=new RenderTexture(1800,1000,24); camera.targetTexture=target; camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=target;
            var texture=new Texture2D(1800,1000,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1800,1000),0,0); texture.Apply();
            Directory.CreateDirectory("Builds/BuildingIntegration"); File.WriteAllBytes("Builds/BuildingIntegration/buildings.png",texture.EncodeToPNG());
            RenderTexture.active=previous; camera.targetTexture=null; Object.DestroyImmediate(target); Object.DestroyImmediate(texture); Object.DestroyImmediate(material);
            Debug.Log("BUILDING_PREVIEW_COMPLETE");
        }
        public static void ApplyAndCapture() { Apply(); Capture(); }
    }
}
