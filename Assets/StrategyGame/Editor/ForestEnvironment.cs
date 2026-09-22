using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Engchanok.StrategyGame.Editor
{
    public static class ForestEnvironment
    {
        const string Root = "Assets/StrategyGame";
        static Material grass, lime, leaf, shade, bark, stone, path, blue, cream;
        static System.Random random;
        static Mesh roundedMesh;
        static float TileNoise(float x,float y,float scale)
        {
            float a=Mathf.PerlinNoise(x*scale+41,y*scale+41);
            float b=Mathf.PerlinNoise((x-1)*scale+41,y*scale+41);
            float c=Mathf.PerlinNoise(x*scale+41,(y-1)*scale+41);
            float d=Mathf.PerlinNoise((x-1)*scale+41,(y-1)*scale+41);
            return Mathf.Lerp(Mathf.Lerp(a,b,x),Mathf.Lerp(c,d,x),y);
        }
        static void MeadowTexture()
        {
            const string asset=Root+"/Materials/Forest meadow texture.png";
            if(!System.IO.File.Exists(asset))
            {
                var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
                for(int y=0;y<256;y++) for(int x=0;x<256;x++)
                {
                    float broad=TileNoise(x/256f,y/256f,5);
                    float fine=TileNoise(x/256f,y/256f,32);
                    float value=.84f+broad*.12f+fine*.04f;
                    texture.SetPixel(x,y,new Color(value,value,value));
                }
                texture.Apply(); System.IO.File.WriteAllBytes(asset,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(asset);
                var importer=(TextureImporter)AssetImporter.GetAtPath(asset);
                importer.wrapMode=TextureWrapMode.Repeat; importer.filterMode=FilterMode.Bilinear; importer.SaveAndReimport();
                grass.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
                grass.mainTextureScale=new Vector2(16,16); EditorUtility.SetDirty(grass);
            }
        }
        // Smooth normals on a modest mesh keep the forest light enough for the tactical camera.
        static Mesh RoundedMesh()
        {
            if(roundedMesh!=null) return roundedMesh;
            const int rings=5, sides=8;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var triangles=new List<int>();
            for(int y=0;y<=rings;y++) for(int x=0;x<=sides;x++)
            {
                float v=y*Mathf.PI/rings, u=x*Mathf.PI*2/sides;
                var normal=new Vector3(Mathf.Sin(v)*Mathf.Cos(u),Mathf.Cos(v),Mathf.Sin(v)*Mathf.Sin(u));
                vertices.Add(normal*.5f); normals.Add(normal);
            }
            for(int y=0;y<rings;y++) for(int x=0;x<sides;x++)
            {
                int a=y*(sides+1)+x, b=a+sides+1;
                triangles.Add(a); triangles.Add(a+1); triangles.Add(b);
                triangles.Add(a+1); triangles.Add(b+1); triangles.Add(b);
            }
            roundedMesh=new Mesh { name="Forest rounded source" };
            roundedMesh.SetVertices(vertices); roundedMesh.SetNormals(normals); roundedMesh.SetTriangles(triangles,0); roundedMesh.RecalculateBounds();
            return roundedMesh;
        }
        static float R(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        static Material Mat(string name, string hex)
        {
            string asset = Root + "/Materials/Forest " + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(asset);
            if (m != null) return m;
            ColorUtility.TryParseHtmlString(hex, out var color);
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            m.SetFloat("_Smoothness", .05f); m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, asset); return m;
        }
        static void Palette()
        {
            grass=Mat("Meadow", "#82AB42"); lime=Mat("Sunlit leaves", "#BDD94C");
            leaf=Mat("Leaves", "#659B38"); shade=Mat("Deep leaves", "#397751");
            bark=Mat("Bark", "#87704C"); stone=Mat("Moss stone", "#82927B");
            path=Mat("Earth", "#BFA16C"); blue=Mat("Blue flowers", "#70CBE4"); cream=Mat("Petals", "#FFF1B1");
            MeadowTexture();
        }
        static Transform Part(Transform parent, string name, PrimitiveType shape, Vector3 p, Vector3 scale, Material material)
        {
            var obj=GameObject.CreatePrimitive(shape); obj.name=name;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            if(shape==PrimitiveType.Sphere||shape==PrimitiveType.Capsule) obj.GetComponent<MeshFilter>().sharedMesh=RoundedMesh();
            obj.layer=2; obj.transform.SetParent(parent,false); obj.transform.localPosition=p; obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=material; return obj.transform;
        }
        static void Tree(Transform root, Vector3 p, float size)
        {
            Part(root,"Rounded trunk",PrimitiveType.Cylinder,p+Vector3.up*size*2.4f,new Vector3(size*.65f,size*2.4f,size*.65f),bark).localRotation=Quaternion.Euler(R(-7,7),0,R(-9,9));
            for(int i=0;i<5;i++)
            {
                float a=i*2.4f;
                Vector3 crown=p+new Vector3(Mathf.Cos(a)*size*1.35f,size*(4.7f+R(0,1.2f)),Mathf.Sin(a)*size*1.35f);
                Part(root,"Soft leaf crown",PrimitiveType.Sphere,crown,new Vector3(size*3.8f,size*2.9f,size*3.5f),i%3==0?lime:leaf);
            }
        }
        static void Garden(Transform root, Vector3 p, float size)
        {
            for(int i=0;i<3;i++)
                Part(root,"Leafy shrub",PrimitiveType.Sphere,p+new Vector3(R(-1,1)*size,.45f*size,R(-1,1)*size),new Vector3(1.7f,.95f,1.5f)*size,i==0?lime:leaf);
            for(int i=0;i<12;i++)
            {
                Vector3 q=p+new Vector3(R(-2,2)*size,0,R(-2,2)*size);
                var blade=Part(root,"Grass leaf",PrimitiveType.Capsule,q+Vector3.up*.35f,new Vector3(.09f,R(.25f,.55f),.14f),i%3==0?lime:leaf);
                blade.localRotation=Quaternion.Euler(R(-30,30),R(0,180),R(-35,35));
                if(i%3==0) Part(root,"Wildflower",PrimitiveType.Sphere,q+Vector3.up*.65f,new Vector3(.28f,.12f,.28f),i%2==0?blue:cream);
            }
        }
        public static void Dress(float extent, bool menu=false)
        {
            Palette(); random=new System.Random(menu?913:207);
            var root=new GameObject("Forest environment").transform;
            // All meshes are cosmetic; never bake them into navigation or intercept world clicks.
            Part(root,"Meadow surround",PrimitiveType.Cube,new Vector3(0,-.38f,0),new Vector3(160,.1f,160),grass);
            if(menu) Part(root,"Garden clearing",PrimitiveType.Sphere,new Vector3(9,-.14f,0),new Vector3(28,.2f,23),path);
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<12;i++)
                {
                    float z=-32+i*7;
                    Tree(root,new Vector3(side*(extent+R(2,8)),0,z),R(1.4f,2.1f));
                    Garden(root,new Vector3(side*(extent-R(1,4)),0,z),R(1,1.8f));
                    Part(root,"Mossy boulder",PrimitiveType.Sphere,new Vector3(side*(extent-R(0,2)),.5f,z+2),new Vector3(3,1.8f,2.5f),stone);
                }
            for(int i=0;i<13;i++) Tree(root,new Vector3(-48+i*8,0,extent+R(6,14)),R(1.8f,2.7f));
            if(menu)
            {
                var camera=Camera.main;
                if(camera!=null) { camera.transform.position=new Vector3(0,12,-25); camera.transform.rotation=Quaternion.Euler(27,0,0); }
                for(int i=0;i<7;i++) Tree(root,new Vector3(-17+i*6,0,14+R(0,5)),R(.9f,1.4f));
                for(int i=0;i<10;i++) Garden(root,new Vector3(-6+i*3,0,-10+R(-1,1)),R(.8f,1.3f));
                // Warm timber fence at the garden's edge.
                for(int i=0;i<8;i++)
                {
                    Part(root,"Fence post",PrimitiveType.Cylinder,new Vector3(-4+i*3,.7f,10),new Vector3(.23f,.7f,.23f),bark);
                    if(i<7) for(int rail=0;rail<2;rail++) Part(root,"Fence rail",PrimitiveType.Cube,new Vector3(-2.5f+i*3,.5f+rail*.55f,10),new Vector3(3,.14f,.16f),bark);
                }
            }
            else
            {
                // Sparse low plants outside the build perimeter and away from every reserved approach lane.
                for(int i=0;i<130;i++)
                {
                    Vector3 p=new Vector3(R(-35,35),0,R(-34,35));
                    if(Vector3.Distance(p,StrategyMatch.HomePosition)<27) continue;
                    bool lane=false;
                    foreach(var start in StrategyMatch.SpawnPoints)
                    {
                        var d=StrategyMatch.HomePosition-start;
                        var closest=start+d*Mathf.Clamp01(Vector3.Dot(p-start,d)/d.sqrMagnitude);
                        if(Vector3.Distance(p,closest)<5) lane=true;
                    }
                    if(!lane) Garden(root,p,R(.5f,1));
                }
            }
            Combine(root,menu?"Menu":"Battlefield");
        }
        static void Combine(Transform root,string label)
        {
            string folder=Root+"/Environment";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root,"Environment");
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var filters=root.GetComponentsInChildren<MeshFilter>();
            foreach(var f in filters)
            {
                var material=f.GetComponent<Renderer>().sharedMaterial;
                if(!groups.ContainsKey(material)) groups[material]=new List<CombineInstance>();
                groups[material].Add(new CombineInstance { mesh=f.sharedMesh, transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix });
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh { name=label+" "+group.Key.name, indexFormat=IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray()); mesh.RecalculateBounds();
                string asset=folder+"/"+mesh.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
                if(existing==null) AssetDatabase.CreateAsset(mesh,asset);
                else { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; EditorUtility.SetDirty(mesh); }
                var obj=new GameObject(group.Key.name); obj.layer=2; obj.transform.SetParent(root,false);
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                obj.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
            foreach(var f in filters) Object.DestroyImmediate(f.gameObject);
        }
        public static void Lighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.65f,.79f,.72f);
            RenderSettings.ambientEquatorColor=new Color(.46f,.59f,.38f);
            RenderSettings.ambientGroundColor=new Color(.29f,.35f,.22f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.56f,.76f,.65f); RenderSettings.fogStartDistance=65; RenderSettings.fogEndDistance=155;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            { if(light.type!=LightType.Directional) continue; light.color=new Color(1,.94f,.76f); light.intensity=1.15f; light.shadowStrength=.65f; light.shadows=LightShadows.Soft; }
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.backgroundColor=RenderSettings.fogColor;
        }
        [MenuItem("Strategy Game/Apply Forest Environment")]
        public static void Apply()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Palette();
            foreach(string scene in new[]{StrategyProjectBuilder.MenuPath,StrategyProjectBuilder.GamePath})
            {
                EditorSceneManager.OpenScene(scene);
                bool menu=scene==StrategyProjectBuilder.MenuPath;
                foreach(string name in new[]{"Cosmetic ground and perimeter","Forest environment"})
                { var old=GameObject.Find(name); if(old!=null) Object.DestroyImmediate(old); }
                foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if(renderer.name=="Ground"||renderer.name=="Display deck") renderer.sharedMaterial=grass;
                    if(renderer.name=="Protected approach lane") renderer.sharedMaterial=path;
                    if(renderer.name=="Deck beacon") Object.DestroyImmediate(renderer.gameObject);
                }
                Dress(AssetDatabase.LoadAssetAtPath<StrategySettings>(Root+"/Data/DefaultStrategy.asset").mapHalfSize,menu);
                Lighting(); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets(); Debug.Log("FOREST_ENVIRONMENT_COMPLETE");
        }
    }
}



