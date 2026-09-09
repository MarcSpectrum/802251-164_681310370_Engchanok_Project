using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace Engchanok.StrategyGame.Editor
{
    public static class StrategyProjectBuilder
    {
        const string Root = "Assets/StrategyGame";
        public const string MenuPath = Root + "/Scenes/MainMenu.unity";
        public const string GamePath = Root + "/Scenes/Survival.unity";
        [InitializeOnLoadMethod]
        static void Startup()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.delayCall += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
                if (menu != null) EditorSceneManager.playModeStartScene = menu;
            };
        }
        [MenuItem("Strategy Game/Rebuild Prototype")]
        public static void Rebuild()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Scenes", "Prefabs", "Data", "Materials" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<StrategySettings>(Root + "/Data/DefaultStrategy.asset");
            if (settings == null) { settings = ScriptableObject.CreateInstance<StrategySettings>(); AssetDatabase.CreateAsset(settings, Root + "/Data/DefaultStrategy.asset"); }
            var cyan = Material("Allies", new Color(.12f, .65f, .85f));
            var worker = Material("Workers", new Color(1, .68f, .15f));
            var red = Material("Hostiles", new Color(.95f, .18f, .23f));
            var metal = Material("Structures", new Color(.19f, .32f, .42f));
            var ground = Material("Ground", new Color(.055f, .105f, .14f));
            var mineral = Material("Minerals", new Color(.15f, .95f, .8f));
            var lane = Material("Lanes", new Color(.15f, .22f, .27f));
            var beam = Material("Beam", new Color(.8f, 1, 1), true);
            StrategyEntity[] prefabs = new StrategyEntity[System.Enum.GetValues(typeof(EntityKind)).Length];
            foreach (EntityKind kind in System.Enum.GetValues(typeof(EntityKind)))
            {
                var obj = new GameObject(kind.ToString());
                var entity = obj.AddComponent<StrategyEntity>(); entity.kind = kind;
                float radius = settings.Radius(kind);
                bool unit = entity.IsUnit;
                var collider = obj.AddComponent<CapsuleCollider>(); collider.radius = radius; collider.height = unit ? 1.8f : 3; collider.center = Vector3.up * collider.height / 2;
                PrimitiveType shape = kind == EntityKind.Runner ? PrimitiveType.Sphere : kind == EntityKind.Brute ? PrimitiveType.Cube : unit ? PrimitiveType.Capsule : kind == EntityKind.Turret ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                Visual(obj.transform, "Body", shape, new Vector3(0, unit ? .9f : 1.2f, 0), unit ? new Vector3(.9f, .9f, .9f) : new Vector3(radius * 1.55f, 2.4f, radius * 1.55f), kind == EntityKind.Worker ? worker : entity.IsEnemy ? red : unit ? cyan : metal);
                if (kind == EntityKind.Runner) obj.transform.Find("Body").localScale = new Vector3(.65f,.55f,1.3f);
                if (kind == EntityKind.Brute) obj.transform.Find("Body").localScale = new Vector3(1.4f,1.7f,1.2f);
                if (unit)
                {
                    Visual(obj.transform, "Visor", PrimitiveType.Cube, new Vector3(0,1.45f,.43f), new Vector3(.65f,.15f,.18f), beam);
                    if (kind == EntityKind.Worker) Visual(obj.transform,"Cargo pack",PrimitiveType.Cube,new Vector3(0,.9f,-.45f),new Vector3(.7f,.8f,.4f),metal);
                    if (kind == EntityKind.Soldier) Visual(obj.transform,"Rifle",PrimitiveType.Cube,new Vector3(.5f,1,.35f),new Vector3(.2f,.2f,1),metal);
                }
                else
                {
                    Visual(obj.transform,"Foundation",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(radius*1.8f,.12f,radius*1.8f),metal);
                    if (kind == EntityKind.Headquarters)
                    {
                        Visual(obj.transform,"Comms mast",PrimitiveType.Cylinder,new Vector3(0,3.2f,0),new Vector3(.18f,1.2f,.18f),cyan);
                        Visual(obj.transform,"Dish",PrimitiveType.Sphere,new Vector3(0,4.2f,0),new Vector3(1.5f,.3f,1.5f),metal);
                    }
                    if (kind == EntityKind.Barracks) for(int i=-1;i<=1;i++) Visual(obj.transform,"Roof rib",PrimitiveType.Cube,new Vector3(i*.9f,2.5f,0),new Vector3(.25f,.4f,3),cyan);
                }
                if (!unit) Visual(obj.transform, "Beacon", PrimitiveType.Cube, new Vector3(0, 2.6f, 0), new Vector3(radius, .25f, radius), cyan);
                if (kind == EntityKind.Turret) Visual(obj.transform, "Barrel", PrimitiveType.Cube, new Vector3(0, 2.5f, 1), new Vector3(.4f, .4f, 2), cyan);
                if (unit)
                {
                    var agent = obj.AddComponent<NavMeshAgent>(); agent.radius = .48f; agent.height = 1.8f; agent.angularSpeed = 720; agent.acceleration = 35; agent.avoidancePriority = entity.IsEnemy ? 60 : 40;
                }
                else
                {
                    var obstacle = obj.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Capsule; obstacle.center = Vector3.up * 1.5f; obstacle.radius = radius; obstacle.height = 3; obstacle.carving = true; obstacle.carveOnlyStationary = false;
                }
                var saved = PrefabUtility.SaveAsPrefabAsset(obj, Root + "/Prefabs/" + kind + ".prefab");
                prefabs[(int)kind] = saved.GetComponent<StrategyEntity>(); Object.DestroyImmediate(obj);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CameraObject(new Vector3(0, 40, -31), new Color(.025f, .05f, .09f));
            LightObject();
            var perimeter = new GameObject("Headquarters build perimeter").AddComponent<LineRenderer>();
            perimeter.sharedMaterial = beam; perimeter.startWidth = .08f; perimeter.endWidth = .08f;
            perimeter.loop = true; perimeter.positionCount = 96;
            for (int i = 0; i < 96; i++)
            {
                float angle = i * Mathf.PI * 2 / 96;
                perimeter.SetPosition(i, StrategyMatch.HomePosition + new Vector3(Mathf.Cos(angle) * settings.buildRadius, .04f, Mathf.Sin(angle) * settings.buildRadius));
            }
            var terrainRoot = new GameObject("Navigation");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Ground"; floor.transform.SetParent(terrainRoot.transform);
            floor.transform.position = new Vector3(0, -.3f, 0); floor.transform.localScale = new Vector3(settings.mapHalfSize * 2, .6f, settings.mapHalfSize * 2); floor.GetComponent<Renderer>().sharedMaterial = ground;
            foreach (var start in StrategyMatch.SpawnPoints)
            {
                Vector3 delta = StrategyMatch.HomePosition - start;
                var stripe = Visual(null, "Protected approach lane", PrimitiveType.Cube, (start + StrategyMatch.HomePosition) / 2 + Vector3.up * .015f, new Vector3(5, .025f, delta.magnitude), lane);
                stripe.transform.rotation = Quaternion.LookRotation(delta);
            }
            foreach (var position in new[] { new Vector3(-11, 0, -20), new Vector3(12, 0, -20), new Vector3(-18, 0, -4), new Vector3(18, 0, -4) })
            {
                var deposit = new GameObject("Mineral deposit"); deposit.transform.position = position; deposit.AddComponent<MineralDeposit>();
                var col = deposit.AddComponent<SphereCollider>(); col.radius = 1.5f; col.center = Vector3.up;
                for (int i = 0; i < 3; i++) { var crystal = Visual(deposit.transform, "Crystal", PrimitiveType.Cube, new Vector3((i - 1) * .7f, 1 + i * .2f, 0), new Vector3(.75f, 2 + i * .3f, .75f), mineral); crystal.transform.localRotation = Quaternion.Euler(0, 30 * i, 12 * (i - 1)); }
            }
            // Only the static floor contributes to the bake. Buildings carve their footprints at runtime.
            var surface = terrainRoot.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string navPath = Root + "/Data/SurvivalNavigation.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            else { EditorUtility.CopySerialized(surface.navMeshData, existing); surface.RemoveData(); surface.navMeshData = existing; surface.AddData(); EditorUtility.SetDirty(existing); }
            var controller = new GameObject("Strategy systems");
            var match = controller.AddComponent<StrategyMatch>(); match.settings = settings; match.prefabs = prefabs; match.beamMaterial = beam;
            var commander = controller.AddComponent<StrategyCommander>(); commander.match = match; commander.view = camera;
            var hud = controller.AddComponent<StrategyHud>(); hud.match = match; hud.commander = commander; hud.BuildUI();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), GamePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CameraObject(new Vector3(0, 30, -20), new Color(.025f, .05f, .09f)); LightObject();
            new GameObject("Main menu").AddComponent<StrategyMenu>().BuildUI();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MenuPath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(GamePath, true) };
            PlayerSettings.productName = "Outpost Strategy Survival";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets(); Startup();
            Debug.Log("STRATEGY_REBUILD_COMPLETE");
        }
        static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); material.color = color; AssetDatabase.CreateAsset(material, path); }
            return material;
        }
        static GameObject Visual(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        static Camera CameraObject(Vector3 position, Color background)
        {
            var obj = new GameObject("Main Camera"); obj.tag = "MainCamera"; obj.transform.position = position; obj.transform.rotation = Quaternion.Euler(57, 0, 0);
            var camera = obj.AddComponent<Camera>(); camera.backgroundColor = background; camera.clearFlags = CameraClearFlags.SolidColor; camera.farClipPlane = 250; obj.AddComponent<AudioListener>(); return camera;
        }
        static void LightObject()
        {
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(.45f, .5f, .6f);
        }
        [MenuItem("Strategy Game/Build Windows Development")]
        public static void BuildWindows()
        {
            if (!File.Exists(MenuPath) || !File.Exists(GamePath)) throw new System.InvalidOperationException("Run Rebuild Prototype before building.");
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { MenuPath, GamePath }, locationPathName = "Builds/Windows/OutpostStrategy.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("STRATEGY_WINDOWS_BUILD_COMPLETE");
        }
        // Explicit validation entry point; ordinary building never regenerates scenes.
        public static void ValidateUpgrade()
        {
            string settingsPath = Root + "/Data/DefaultStrategy.asset";
            string tuning = EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<StrategySettings>(settingsPath));
            string identity = AssetDatabase.AssetPathToGUID(settingsPath);
            for (int pass = 0; pass < 2; pass++)
            {
                Rebuild();
                if (tuning != EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<StrategySettings>(settingsPath)) || identity != AssetDatabase.AssetPathToGUID(settingsPath))
                    throw new System.Exception("Regeneration changed existing tuning or settings identity.");
                foreach (EntityKind kind in System.Enum.GetValues(typeof(EntityKind)))
                    if (AssetDatabase.LoadAssetAtPath<StrategyEntity>(Root + "/Prefabs/" + kind + ".prefab") == null)
                        throw new System.Exception("Missing prefab: " + kind);
            }
            Debug.Log("STRATEGY_REPEAT_GENERATION_PASSED");
            BuildWindows();
        }
    }
}
