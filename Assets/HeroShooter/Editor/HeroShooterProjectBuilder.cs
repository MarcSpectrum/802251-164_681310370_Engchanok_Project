using System.IO;
using Engchanok.HeroShooter;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Engchanok.HeroShooter.Editor
{
    public static class HeroShooterProjectBuilder
    {
        private const string Root = "Assets/HeroShooter";
        private const string Scenes = Root + "/Scenes";
        private const string Prefabs = Root + "/Prefabs";
        private const string Data = Root + "/Data";
        private const string Materials = Root + "/Materials";
        private const string MenuScene = Scenes + "/MainMenu.unity";
        private const string SandboxScene = Scenes + "/HeroSandbox.unity";
        private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";

        private static Font _font;

        [InitializeOnLoadMethod]
        private static void ConfigureStartup()
        {
            SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene);
            if (menu != null) EditorSceneManager.playModeStartScene = menu;
        }

        [MenuItem("Hero Shooter/Rebuild Starter Project")]
        public static void BuildStarterProject()
        {
            EnsureFolders();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Material playerMaterial = GetOrCreateMaterial("Player", new Color(0.08f, 0.42f, 0.95f));
            Material weaponMaterial = GetOrCreateMaterial("Weapon", new Color(0.08f, 0.1f, 0.14f));
            Material targetMaterial = GetOrCreateMaterial("Target", new Color(0.92f, 0.12f, 0.12f));
            Material groundMaterial = GetOrCreateMaterial("Ground", new Color(0.14f, 0.17f, 0.21f));

            MovementSettings movement = GetOrCreateAsset<MovementSettings>(Data + "/DefaultMovement.asset");
            WeaponSettings weapon = GetOrCreateAsset<WeaponSettings>(Data + "/DefaultRifle.asset");
            GameObject hero = BuildHeroPrefab(movement, weapon, playerMaterial, weaponMaterial);
            GameObject dummy = BuildDummyPrefab(targetMaterial);

            BuildMainMenu();
            BuildSandbox(hero, dummy, groundMaterial);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(SandboxScene, true)
            };
            ConfigureStartup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HERO_SHOOTER_SETUP_COMPLETE");
        }

        public static void BuildFromCommandLine() => BuildStarterProject();

        [MenuItem("Hero Shooter/Build Windows Development")]
        public static void BuildWindowsDevelopment()
        {
            BuildStarterProject();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, SandboxScene },
                locationPathName = "Builds/Windows/HeroShooterPrototype.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log($"HERO_SHOOTER_WINDOWS_BUILD_COMPLETE: {report.summary.totalSize} bytes");
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { Scenes, Prefabs, Data, Materials })
                Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{Materials}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildHeroPrefab(MovementSettings movement, WeaponSettings weapon,
            Material playerMaterial, Material weaponMaterial)
        {
            GameObject root = new("Hero");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.48f;
            controller.stepOffset = 0.35f;

            HeroInput input = root.AddComponent<HeroInput>();
            HeroMotor motor = root.AddComponent<HeroMotor>();
            HitscanWeapon rifle = root.AddComponent<HitscanWeapon>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            SetLayerRecursively(body, root.layer);

            GameObject weaponBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponBody.name = "Rifle";
            Object.DestroyImmediate(weaponBody.GetComponent<Collider>());
            weaponBody.transform.SetParent(body.transform, false);
            weaponBody.transform.localPosition = new Vector3(0.55f, 0.25f, 0.35f);
            weaponBody.transform.localScale = new Vector3(0.16f, 0.16f, 0.8f);
            weaponBody.GetComponent<Renderer>().sharedMaterial = weaponMaterial;
            SetLayerRecursively(weaponBody, root.layer);

            GameObject muzzleObject = new("Muzzle");
            muzzleObject.layer = root.layer;
            muzzleObject.transform.SetParent(weaponBody.transform, false);
            muzzleObject.transform.localPosition = new Vector3(0f, 0f, 0.55f);

            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            Set(input, "actions", inputAsset);
            Set(motor, "settings", movement);
            Set(motor, "input", input);
            Set(motor, "visualRoot", body.transform);
            Set(rifle, "input", input);
            Set(rifle, "muzzle", muzzleObject.transform);
            Set(rifle, "settings", weapon);

            string path = Prefabs + "/Hero.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildDummyPrefab(Material targetMaterial)
        {
            GameObject root = new("Target Dummy");
            root.AddComponent<TargetDummy>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().sharedMaterial = targetMaterial;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            head.transform.localScale = Vector3.one * 0.7f;
            head.GetComponent<Renderer>().sharedMaterial = targetMaterial;

            string path = Prefabs + "/TargetDummy.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildMainMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera(new Color(0.018f, 0.026f, 0.05f));
            Canvas canvas = MakeCanvas();
            UiImage(canvas.transform, "Backdrop", new Color(0.018f, 0.026f, 0.05f), Vector2.zero,
                new Vector2(1280f, 720f), Vector2.one * 0.5f);
            UiImage(canvas.transform, "Accent", new Color(0.1f, 0.55f, 1f), new Vector2(-410f, 0f),
                new Vector2(7f, 720f), Vector2.one * 0.5f);

            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            Text title = Label(canvas.transform, "Title", "HERO SHOOTER", 64, new Vector2(-170f, 145f),
                new Vector2(660f, 90f), TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.65f, 0.88f, 1f);
            Text subtitle = Label(canvas.transform, "Subtitle", "MOVEMENT PROTOTYPE", 24,
                new Vector2(-165f, 88f), new Vector2(650f, 45f), TextAnchor.MiddleLeft);
            subtitle.color = new Color(0.45f, 0.68f, 0.85f);
            Label(canvas.transform, "Description", "Third-person movement and aim sandbox", 19,
                new Vector2(-165f, 30f), new Vector2(650f, 45f), TextAnchor.MiddleLeft);
            MakeButton(canvas.transform, "PLAY", new Vector2(-315f, -80f), controller.Play);
            MakeButton(canvas.transform, "QUIT", new Vector2(-315f, -155f), controller.Quit);
            Label(canvas.transform, "Version", "PROTOTYPE 01", 15, new Vector2(515f, -325f),
                new Vector2(220f, 35f), TextAnchor.MiddleRight);
            EditorSceneManager.SaveScene(scene, MenuScene);
        }

        private static void BuildSandbox(GameObject heroPrefab, GameObject dummyPrefab, Material groundMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.4f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Baseplate";
            ground.transform.SetPositionAndRotation(new Vector3(0f, -0.5f, 15f), Quaternion.identity);
            ground.transform.localScale = new Vector3(70f, 1f, 70f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
            hero.transform.position = Vector3.zero;
            HeroInput input = hero.GetComponent<HeroInput>();
            HeroMotor motor = hero.GetComponent<HeroMotor>();
            HitscanWeapon weapon = hero.GetComponent<HitscanWeapon>();

            Camera camera = AddCamera(new Color(0.48f, 0.64f, 0.82f));
            ShoulderCamera shoulder = camera.gameObject.AddComponent<ShoulderCamera>();
            Set(shoulder, "target", hero.transform);
            Set(shoulder, "input", input);
            Set(motor, "cameraTransform", camera.transform);
            Set(weapon, "aimCamera", camera);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.95f, 0.86f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            Vector3[] targetPositions =
            {
                new(-6f, 0f, 18f),
                new(0f, 0f, 24f),
                new(7f, 0f, 20f)
            };
            foreach (Vector3 position in targetPositions)
            {
                GameObject target = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
                target.transform.position = position;
            }

            Canvas canvas = MakeCanvas();
            GameObject flowObject = new("Game Flow");
            GameFlowController flow = flowObject.AddComponent<GameFlowController>();
            GameObject pausePanel = BuildPausePanel(canvas.transform, flow);
            Set(flow, "pausePanel", pausePanel);
            Set(flow, "input", input);

            Text ammo = Label(canvas.transform, "Ammo", "12 / 12", 28, new Vector2(-45f, 30f),
                new Vector2(300f, 55f), TextAnchor.MiddleRight, new Vector2(1f, 0f));
            Text dash = Label(canvas.transform, "Dash", "DASH  READY", 21, new Vector2(35f, 30f),
                new Vector2(300f, 55f), TextAnchor.MiddleLeft, Vector2.zero);
            Label(canvas.transform, "Controls",
                "WASD MOVE   SHIFT SPRINT   SPACE JUMP   Q DASH   RMB AIM   LMB FIRE   R RELOAD   ESC PAUSE",
                15, new Vector2(0f, 24f), new Vector2(1000f, 35f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f));

            HudPresenter hud = canvas.gameObject.AddComponent<HudPresenter>();
            Set(hud, "weapon", weapon);
            Set(hud, "motor", motor);
            Set(hud, "ammoText", ammo);
            Set(hud, "dashText", dash);
            BuildCrosshair(canvas.transform, weapon);

            EditorSceneManager.SaveScene(scene, SandboxScene);
        }

        private static GameObject BuildPausePanel(Transform parent, GameFlowController flow)
        {
            GameObject panel = UiImage(parent, "Pause Panel", new Color(0.015f, 0.025f, 0.05f, 0.94f),
                Vector2.zero, new Vector2(1280f, 720f), Vector2.one * 0.5f).gameObject;
            Label(panel.transform, "Title", "PAUSED", 58, new Vector2(0f, 150f), new Vector2(500f, 80f), TextAnchor.MiddleCenter);
            MakeButton(panel.transform, "RESUME", new Vector2(0f, 45f), flow.Resume);
            MakeButton(panel.transform, "RESTART", new Vector2(0f, -30f), flow.Restart);
            MakeButton(panel.transform, "MAIN MENU", new Vector2(0f, -105f), flow.MainMenu);
            panel.SetActive(false);
            return panel;
        }

        private static void BuildCrosshair(Transform parent, HitscanWeapon weapon)
        {
            GameObject root = new("Crosshair", typeof(RectTransform), typeof(CrosshairPresenter));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = Vector2.one * 0.5f;
            rootRect.sizeDelta = new Vector2(80f, 80f);

            Image top = CrosshairLine(root.transform, "Top", new Vector2(2f, 12f));
            Image bottom = CrosshairLine(root.transform, "Bottom", new Vector2(2f, 12f));
            Image left = CrosshairLine(root.transform, "Left", new Vector2(12f, 2f));
            Image right = CrosshairLine(root.transform, "Right", new Vector2(12f, 2f));
            Image dot = CrosshairLine(root.transform, "Center Dot", new Vector2(4f, 4f));

            CrosshairPresenter presenter = root.GetComponent<CrosshairPresenter>();
            Set(presenter, "weapon", weapon);
            Set(presenter, "top", top);
            Set(presenter, "bottom", bottom);
            Set(presenter, "left", left);
            Set(presenter, "right", right);
            Set(presenter, "centerDot", dot);
        }

        private static Image CrosshairLine(Transform parent, string name, Vector2 size)
        {
            Image image = UiImage(parent, name, Color.white, Vector2.zero, size, Vector2.one * 0.5f);
            image.raycastTarget = false;
            return image;
        }

        private static Camera AddCamera(Color background)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Canvas MakeCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return canvas;
        }

        private static Text Label(Transform parent, string name, string value, int size, Vector2 position,
            Vector2 dimensions, TextAnchor alignment, Vector2? anchor = null)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            Vector2 selectedAnchor = anchor ?? Vector2.one * 0.5f;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = selectedAnchor;
            text.rectTransform.pivot = selectedAnchor;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = dimensions;
            return text;
        }

        private static Image UiImage(Transform parent, string name, Color color, Vector2 position,
            Vector2 size, Vector2 anchor)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            image.rectTransform.pivot = anchor;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static void MakeButton(Transform parent, string label, Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            Image image = UiImage(parent, label + " Button", new Color(0.05f, 0.28f, 0.52f, 0.96f),
                position, new Vector2(300f, 58f), Vector2.one * 0.5f);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.4f, 0.8f, 1f);
            colors.pressedColor = new Color(0.16f, 0.55f, 0.84f);
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            Text text = Label(image.transform, "Label", label, 22, Vector2.zero, new Vector2(285f, 52f), TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
        }

        private static void Set(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new System.InvalidOperationException($"Missing serialized field {field} on {target.GetType().Name}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
