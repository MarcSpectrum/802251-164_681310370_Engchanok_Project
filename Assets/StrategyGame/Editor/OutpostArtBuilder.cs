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
    public static class OutpostArtBuilder
    {
        const string Root = "Assets/StrategyGame/Art/";
        const string Kay = Root + "KayKit/";
        static Material ConvertMaterial(Material source, bool toon)
        {
            string name = source.name.Replace(" (Instance)", "");
            string path = Root + "Materials/" + (toon ? "Toon_" : "Kay_") + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Directory.CreateDirectory(Root + "Materials"); AssetDatabase.Refresh();
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Smoothness", .12f);
            if (toon) material.color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.gray;
            else if (name == "Glow") { material.color = new Color(1,.35f,.15f); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(1,.15f,.04f)); }
            else
            {
                string pack = name == "skeleton" ? "Skeletons" : "Adventurers";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Kay + pack + "/Textures/" + name + "_texture.png");
                if (texture == null) throw new InvalidOperationException("Missing atlas: " + name);
                material.SetTexture("_BaseMap", texture); material.color = Color.white;
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject Model(string path, Transform parent, bool toon = false)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing approved asset: " + path);
            var model = Object.Instantiate(source, parent, false); model.name = source.name;
            foreach(var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(m=>ConvertMaterial(m, toon)).ToArray();
            foreach(var col in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            return model;
        }
        static AnimationClip Clip(string file, string name)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Kay+"Animations/Rig_Medium_"+file+".fbx").OfType<AnimationClip>().FirstOrDefault(c=>c.name==name);
            if(clip == null) throw new InvalidOperationException("Missing animation: "+name);
            return clip;
        }
        public static void ReplaceUnitVisuals(GameObject root, EntityKind kind)
        {
            if (!StrategyEntity.IsUnitKind(kind)) return;
            while(root.transform.childCount>0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            bool enemy = StrategyMatch.IsHostileKind(kind);
            string pack = enemy ? "Skeletons" : "Adventurers";
            string character, weapon, shield = null, actionFile = "CombatMelee", actionName = "Melee_1H_Attack_Chop";
            float scale = .9f;
            switch(kind)
            {
                case EntityKind.Worker: character="Rogue"; weapon="axe_1handed"; break;
                case EntityKind.Soldier: character="Knight"; weapon="crossbow_1handed"; actionFile="CombatRanged"; actionName="Ranged_1H_Shoot"; break;
                case EntityKind.Defender: character="Knight"; weapon="sword_1handed"; shield="shield_square"; scale=1.02f; break;
                case EntityKind.Ranger: character="Ranger"; weapon="bow_withString"; actionFile="CombatRanged"; actionName="Ranged_Bow_Release"; break;
                case EntityKind.Medic: character="Mage"; weapon="staff"; actionFile="CombatRanged"; actionName="Ranged_Magic_Spellcasting"; scale=.82f; break;
                case EntityKind.Engineer: character="Barbarian"; weapon="axe_1handed"; break;
                case EntityKind.Runner: character="Skeleton_Rogue"; weapon="Skeleton_Blade"; scale=.72f; break;
                case EntityKind.Brute: character="Skeleton_Warrior"; weapon="Skeleton_Axe"; shield="Skeleton_Shield_Small_A"; scale=1.12f; break;
                case EntityKind.Lancer: character="Skeleton_Rogue"; weapon="Skeleton_Crossbow"; actionFile="CombatRanged"; actionName="Ranged_1H_Shoot"; break;
                case EntityKind.Breaker: character="Skeleton_Warrior"; weapon="Skeleton_Axe"; scale=.94f; break;
                case EntityKind.Warden: character="Skeleton_Mage"; weapon="Skeleton_Staff"; actionFile="CombatRanged"; actionName="Ranged_Magic_Spellcasting"; break;
                case EntityKind.Juggernaut: character="Skeleton_Warrior"; weapon="Skeleton_Axe"; shield="Skeleton_Shield_Large_B"; scale=1.4f; break;
                default: character="Skeleton_Minion"; weapon="Skeleton_Blade"; scale=.83f; break;
            }
            var visual = new GameObject("Character visual"); visual.transform.SetParent(root.transform,false);
            var model = Model(Kay+pack+"/Characters/"+character+".fbx",visual.transform);
            model.transform.localScale *= scale;
            float height=model.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y)-root.transform.position.y;
            var collider=root.GetComponent<CapsuleCollider>();
            if(collider!=null) { collider.height=Mathf.Max(1.8f,height); collider.center=Vector3.up*collider.height*.5f; }
            var bones = model.GetComponentsInChildren<Transform>();
            string hand = kind==EntityKind.Ranger ? "handslot.l" : "handslot.r";
            // Adventurer equipment is shared between classes; its atlas follows its original material.
            Model(Kay+pack+"/Equipment/"+weapon+".fbx", bones.First(t=>t.name==hand));
            if(shield!=null) Model(Kay+pack+"/Equipment/"+shield+".fbx",bones.First(t=>t.name=="handslot.l"));
            var view = visual.AddComponent<StrategyCharacterView>(); view.markerHeight=height+.2f;
            view.animator = model.GetComponent<Animator>(); if(view.animator==null) view.animator=model.AddComponent<Animator>();
            view.idle = kind==EntityKind.Ranger ? Clip("CombatRanged","Ranged_Bow_Idle") : Clip("General","Idle_A");
            view.run = Clip("MovementBasic","Running_A");
            view.action = Clip(actionFile,actionName);
            view.work = kind==EntityKind.Worker ? Clip("Tools","Chopping") : kind==EntityKind.Engineer ? Clip("Tools","Hammering") : Clip("CombatRanged","Ranged_Magic_Spellcasting");
            view.animator.applyRootMotion=false;
        }
        public static void Dress(bool menu)
        {
            var old=GameObject.Find("Outpost supplies"); if(old!=null) Object.DestroyImmediate(old);
            var root=new GameObject("Outpost supplies").transform;
            Vector3[] camps = menu ? new[]{new Vector3(19,0,1), new Vector3(3,0,8)} : new[]{new Vector3(-29,0,-19),new Vector3(29,0,-18),new Vector3(-28,0,18),new Vector3(29,0,19)};
            for(int i=0;i<camps.Length;i++)
            {
                var camp = new GameObject("Supply camp "+(i+1)).transform; camp.SetParent(root,false); camp.position=camps[i]; camp.rotation=Quaternion.Euler(0,i%2==0?25:-35,0);
                Prop("Container_Small",camp,new Vector3(0,0,1),1.35f);
                Prop("Pallet",camp,new Vector3(-2,0,-1),1);
                Prop("Crate",camp,new Vector3(-2,.19f,-1),1);
                Prop("Crate",camp,new Vector3(-1.5f,.19f,-.8f),.7f);
                Prop("GasTank",camp,new Vector3(2,0,.5f),.85f);
                Prop("SackTrench_Small",camp,new Vector3(.5f,0,-1.9f),1.1f);
                Prop("StreetLight",camp,new Vector3(2.5f,0,2),.8f);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer=2;
        }
        static void Prop(string name, Transform parent, Vector3 position, float scale)
        {
            var prop=Model(Root+"ToonShooter/"+name+".fbx",parent,true);
            prop.transform.localPosition=position; prop.transform.localScale*=scale;
        }
        [MenuItem("Strategy Game/Refresh Approved Character Art")]
        public static void Apply()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach(EntityKind kind in Enum.GetValues(typeof(EntityKind)))
            {
                if(!StrategyEntity.IsUnitKind(kind)) continue;
                string path="Assets/StrategyGame/Prefabs/"+kind+".prefab";
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try { ReplaceUnitVisuals(prefab,kind); PrefabUtility.SaveAsPrefabAsset(prefab,path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            foreach(string path in new[]{StrategyProjectBuilder.MenuPath,StrategyProjectBuilder.GamePath})
            {
                EditorSceneManager.OpenScene(path);
                bool menu=path==StrategyProjectBuilder.MenuPath;
                if(menu)
                {
                    var root=GameObject.Find("Menu outpost diorama");
                    foreach(EntityKind kind in Enum.GetValues(typeof(EntityKind)))
                    {
                        if(!StrategyEntity.IsUnitKind(kind)) continue;
                        var display=root.transform.Find(kind+" display"); if(display==null) continue;
                        display.localRotation=Quaternion.Euler(0,180,0);
                        while(display.childCount>0) Object.DestroyImmediate(display.GetChild(0).gameObject);
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StrategyGame/Prefabs/"+kind+".prefab");
                        foreach(Transform child in prefab.transform) Object.Instantiate(child.gameObject,display,false);
                    }
                }
                Dress(menu); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets(); Debug.Log("APPROVED_ART_COMPLETE");
        }
    }
}
