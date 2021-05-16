using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Thry.VRBrations
{
    public enum Sensor_Position { Crotch, Butt, Chest, Head }
    public enum Penetrator_Options { None, SupportPenetrator, SupportOriface, PlaceOriface }

    public class SensorOptions
    {
        public Sensor_Position position;
        public Penetrator_Options penetrator;
    }

    public class PlacedSensor
    {
        public GameObject gameObject;
        public Camera camera;
        public Material material;
    }

    //This class adds / removes vrc when selection an avatar to make sure the sdk doesnt complain
    [InitializeOnLoad]
    public class VRCBAdderAndRemover
    {
        static VRCBAdderAndRemover()
        {
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.update += FocusChanged;
        }

        static vrbrations prevvrb;
        static void SelectionChanged()
        {
#if VRC_SDK_VRCSDK3
            if (Selection.activeTransform)
            {
                if (prevvrb && prevvrb.transform != Selection.activeTransform)
                {
                    prevvrb.Destroy();
                }
                if (Selection.activeTransform.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>())
                {
                    vrbrations vrb = Selection.activeTransform.GetComponent<vrbrations>();
                    if (vrb == null && EditorWindow.focusedWindow is VRCSdkControlPanel == false)
                    {
                        //check for existing sensors
                        List<GameObject> foundSensors = new List<GameObject>();
                        ToyControllerSetup.FindDeepChildren(Selection.activeTransform, "[Toys] Sensor", foundSensors);
                        if (foundSensors.Count > 0)
                        {
                            vrb = Selection.activeTransform.gameObject.AddComponent<vrbrations>();
                            vrb.foundSensorsObjects = foundSensors;
                        }
                    }
                    prevvrb = vrb;
                }
            }
#endif
        }

        static bool wasVRCWindow;
        static void FocusChanged()
        {
#if VRC_SDK_VRCSDK3
            if (EditorWindow.focusedWindow is VRCSdkControlPanel && prevvrb)
            {
                prevvrb.Destroy();
                (EditorWindow.focusedWindow as VRCSdkControlPanel).ResetIssues();
                wasVRCWindow = true;
            }else if (wasVRCWindow)
            {
                SelectionChanged();
            }
#endif
        }
    }

    [CustomEditor(typeof(vrbrations))]
    public class VRbrationsEditor : Editor
    {
        int sensorCount = 1;
        List<SensorOptions> sensor_Positions = new List<SensorOptions>() { new SensorOptions() };
        bool addDesktopUseability;

        public List<PlacedSensor> placedSensors = new List<PlacedSensor>();
        public bool load_PlacedSensors = true;
        vrbrations avatarScript;

        #region GUI
        public override void OnInspectorGUI()
        {
            Styles.Init();
            avatarScript = target as vrbrations;

            if (sensor_Positions.Count == 0) load_PlacedSensors = true;

            GUIHeader();
            if (load_PlacedSensors)
            {
                ToyControllerSetup.FindAvatarSensors(placedSensors, new SetupData(avatarScript.gameObject, sensorCount, sensor_Positions, addDesktopUseability), avatarScript.foundSensorsObjects);
                load_PlacedSensors = false;
            }

            EditorGUIUtility.wideMode = true;
            if (placedSensors.Count > 0)
                GUIExistingSetup();
            else
                GUINewSetup();

            EditorGUIUtility.wideMode = false;
        }

        private void GUIHeader()
        {
            EditorGUILayout.LabelField("<size=20><color=#EF7AFF>❤ VRbrations ❤</color></size>", Styles.masterLabel, GUILayout.Height(40));
            EditorGUILayout.Space();
        }

        private void GUINewSetup()
        {
            sensorCount = EditorGUILayout.IntField(new GUIContent("Amount of Sensors"), sensorCount);
            if (sensorCount != sensor_Positions.Count)
            {
                while (sensorCount < sensor_Positions.Count)
                    sensor_Positions.RemoveAt(sensor_Positions.Count - 1);
                while (sensorCount > sensor_Positions.Count)
                    sensor_Positions.Add(new SensorOptions());
            }
            for (int i = 0; i < sensor_Positions.Count; i++)
            {
                Rect r = EditorGUILayout.GetControlRect();
                float offset = EditorGUIUtility.labelWidth + (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth) / 2;
                r.width -= (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth) / 2;
                sensor_Positions[i].position = (Sensor_Position)EditorGUI.EnumPopup(r, new GUIContent("Sensors " + i), sensor_Positions[i].position);
                r.x += offset;
                r.width -= EditorGUIUtility.labelWidth;
                float lW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                sensor_Positions[i].penetrator = (Penetrator_Options)EditorGUI.EnumPopup(r ,new GUIContent("", "Enable support for Raliv penetrator system"), sensor_Positions[i].penetrator);
                EditorGUIUtility.labelWidth = lW;
            }

            EditorGUILayout.Space();

            addDesktopUseability = EditorGUILayout.Toggle(new GUIContent("Desktop Useable", "Allows avatar to be used in desktop. comes with slight performance penalty."), addDesktopUseability);
            if (GUILayout.Button("Setup"))
            {
                ToyControllerSetup.SetupAvatar(placedSensors, new SetupData(avatarScript.gameObject, sensorCount, sensor_Positions, addDesktopUseability));
                load_PlacedSensors = true;
            }
        }

        private void GUIExistingSetup()
        {
            GUIExistingSensors();
            GUIUpdateSetup();
            GUIDeleteSetup();
        }
        
        private void GUIExistingSensors()
        {
            EditorGUILayout.LabelField("<b><size=15>" + "Existing Sensors" + "</size></b>", Styles.RichText, GUILayout.Height(20));

            foreach (PlacedSensor sensor in placedSensors)
            {
                EditorGUILayout.LabelField("<b><size=12>" + sensor.gameObject.name + "</size></b>", Styles.RichText);

                //Pixel Position
                Vector4 _pixelPosition = sensor.material.GetVector("_pixelPosition");
                _pixelPosition = (Vector2)EditorGUILayout.Vector2IntField(new GUIContent("Pixel Position", "The x and y position that you also have to enter in the software."), new Vector2Int((int)_pixelPosition.x, (int)_pixelPosition.y));
                sensor.material.SetVector("_pixelPosition", _pixelPosition);

                //Sensor camera length, width
                EditorGUI.BeginChangeCheck();
                sensor.camera.farClipPlane = EditorGUILayout.FloatField("Depth", sensor.camera.farClipPlane);
                if (EditorGUI.EndChangeCheck())
                    sensor.camera.transform.position = sensor.camera.transform.parent.position + sensor.camera.transform.parent.rotation * Vector3.forward * -sensor.camera.farClipPlane;
                sensor.camera.orthographicSize = EditorGUILayout.FloatField("Width", sensor.camera.orthographicSize);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Edit Sensors", avatarScript.editSensors ? Styles.buttonSelected : GUI.skin.button)){
                avatarScript.editSensors = !avatarScript.editSensors;
                Tools.hidden = avatarScript.editSensors;
                ToggleGizmos(!avatarScript.editSensors);
            }
        }

        private void GUIUpdateSetup()
        {
            EditorGUILayout.Space();
            GUILayout.Label("<b><size=15>" + "Update Setup" + "</size></b>", Styles.RichText);
            if (GUILayout.Button("Update Setup"))
            {
                ToyControllerSetup.SetupAvatar(placedSensors, new SetupData(avatarScript.gameObject, true));
            }
        }

        private void GUIDeleteSetup()
        {
            EditorGUILayout.Space();
            GUILayout.Label("<b><size=15>" + "Delete Setup" + "</size></b>", Styles.RichText);
            if (GUILayout.Button("Clear Setup"))
            {
                ToyControllerSetup.CleanOldSetup(avatarScript.gameObject);
                placedSensors.Clear();
            }
        }
        #endregion

        #region Scene GUI
        void OnSceneGUI()
        {
            avatarScript = target as vrbrations;

            if (avatarScript.editSensors)
            {
                Color def = Handles.color;
                Color sens = new Color(0, 1, 1, 0.6f);
                foreach (PlacedSensor sensor in placedSensors)
                {
                    Transform t = sensor.gameObject.transform;

                    Handles.color = def;
                    if (Tools.current == Tool.Move)
                    {
                        Vector3 pos = Handles.PositionHandle(t.position, t.rotation);
                        if(pos != t.position)
                        {
                            Undo.RecordObject(t, "Moved Sensor");
                            t.position = pos;
                            this.Repaint();
                        }
                    }else if(Tools.current == Tool.Rotate)
                    {
                        Quaternion rot = Handles.RotationHandle(t.rotation, t.position);
                        if (rot != t.rotation)
                        {
                            Undo.RecordObject(t, "Rotated Sensor");
                            t.rotation = rot;
                            this.Repaint();
                        }
                    }
                    else if (Tools.current == Tool.Scale)
                    {
                        EditorGUI.BeginChangeCheck();
                        float scale = Handles.ScaleSlider(sensor.camera.farClipPlane, t.position, t.rotation * Vector3.back, t.rotation, HandleUtility.GetHandleSize(t.position), 0 );
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Component[] { sensor.camera.transform, sensor.camera }, "Scaled Sensor");
                            sensor.camera.transform.position = t.position + t.rotation * Vector3.back * scale;
                            sensor.camera.farClipPlane = scale;
                            this.Repaint();
                        }

                        EditorGUI.BeginChangeCheck();
                        scale = Handles.ScaleSlider(sensor.camera.orthographicSize, t.position, t.rotation * Vector3.left, t.rotation, HandleUtility.GetHandleSize(t.position), 0);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(sensor.camera, "Scaled Sensor");
                            sensor.camera.orthographicSize = scale;
                            this.Repaint();
                        }
                    }

                    Handles.color = sens;
                    Matrix4x4 mat = Handles.matrix;
                    Handles.matrix = t.localToWorldMatrix * Matrix4x4.Scale(new Vector3(sensor.camera.orthographicSize * 2, sensor.camera.orthographicSize * 2, sensor.camera.farClipPlane));
                    Handles.CylinderHandleCap(1,  Vector3.back * 0.5f, Quaternion.identity, 1, EventType.Repaint);
                    Handles.matrix = mat;
                }
                Handles.color = def;
            }
        }
        #endregion

        public static void ToggleGizmos(bool gizmosOn)
        {
            int val = gizmosOn ? 1 : 0;
            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            Type type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                MethodInfo getAnnotations = type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo setGizmoEnabled = type.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo setIconEnabled = type.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                var annotations = getAnnotations.Invoke(null, null);
                foreach (object annotation in (IEnumerable)annotations)
                {
                    Type annotationType = annotation.GetType();
                    FieldInfo classIdField = annotationType.GetField("classID", BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo scriptClassField = annotationType.GetField("scriptClass", BindingFlags.Public | BindingFlags.Instance);
                    if (classIdField != null && scriptClassField != null)
                    {
                        int classId = (int)classIdField.GetValue(annotation);
                        string scriptClass = (string)scriptClassField.GetValue(annotation);
                        setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                        setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                    }
                }
            }
        }

    }

    struct SetupData
    {
        public GameObject avatarObject;
        public int sensorCount;
        public List<SensorOptions> sensor_Positions;
        public bool addDesktopUseability;
        public bool fromSave;

        public SetupData(GameObject avatarObject, int sensorCount, List<SensorOptions> sensor_Positions, bool addDesktopUseability)
        {
            this.avatarObject = avatarObject;
            this.sensorCount = sensorCount;
            this.sensor_Positions = sensor_Positions;
            this.addDesktopUseability = addDesktopUseability;
            this.fromSave = false;
        }

        public SetupData(GameObject avatarObject, bool fromSave)
        {
            this.avatarObject = avatarObject;
            this.sensorCount = 0;
            this.sensor_Positions = null;
            this.addDesktopUseability = false;
            this.fromSave = true;
        }
    }

    class ToyControllerSetup : Editor
    {
        const string SHADER_NAME_SENSOR = "VRBrations/Sensor";
        const string SHADER_NAME_MAIN = "VRBrations/Main";
        const float CAMERA_FOV = 59.5f;
        const float CAMERA_FOV_MAX_DELTA = 1;

        const string CAMERA_OVERRENDERER_NAME = "[Toys] Camera Overrender";
        const string HEAD_OBJ_NAME = "[Toys] Head";

        public static void FindAvatarSensors(List<PlacedSensor> placedSensor, SetupData setupData, List<GameObject> foundSensors = null)
        {
            placedSensor.Clear();
            if(foundSensors == null || foundSensors.Any(o => o == null))
            {
                foundSensors = new List<GameObject>();
                FindDeepChildren(setupData.avatarObject.transform, "[Toys] Sensor", foundSensors);
            }
            foreach (GameObject s in foundSensors)
            {
                PlacedSensor newS = new PlacedSensor();
                newS.gameObject = s;
                if (s.transform.childCount > 0)
                {
                    newS.material = s.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
                    newS.camera = s.transform.GetChild(0).GetComponent<Camera>();
                }
                placedSensor.Add(newS);
            }
        }

        public static void SetupAvatar(List<PlacedSensor> placedSensors, SetupData setupData)
        {
            LayerSetup.SetupLayers();

            Animator animator = setupData.avatarObject.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("[Thry][ToyController] Could not find avatar animator.");
                return;
            }
            Avatar avatar = animator.avatar;
            if (avatar == null)
            {
                Debug.LogError("[Thry][ToyController] Could not find animator avatar reference.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(avatar);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Thry][ToyController] Could not find prefab path.");
                return;
            }

            string avatarDirectory = Path.GetDirectoryName(path);
            string directory = avatarDirectory + "/toyController";
            if (Directory.Exists(directory) == false) AssetDatabase.CreateFolder(avatarDirectory, "toyController");

            HumanDescription humanDescription = new HumanDescription();
            if (!GetHumanDescription(path, ref humanDescription))
            {
                Debug.LogError("[Thry][ToyController] Could not find humanoid rig.");
                return;
            }
            HumanBone[] bones = humanDescription.human;

            if (setupData.fromSave) SaveOldSetup(placedSensors, ref setupData);
            CleanOldSetup(setupData.avatarObject);
            placedSensors.Clear();

            float fov = CAMERA_FOV + UnityEngine.Random.Range(0, CAMERA_FOV_MAX_DELTA);

            RenderTexture[] textures = CreateRenderTextures("sensor_" + setupData.avatarObject.name, directory, setupData);

            Transform constraintHead;
            Transform cameraT = CreateHeadObjectWithConstaint(bones, fov, out constraintHead, setupData);

            CreateSensorCameras(textures, bones, directory, fov, constraintHead, setupData, placedSensors);

            AddDepthGet(setupData.avatarObject);
            if (cameraT != null)
            {
                CreateOverrideCameraPixelWriter(cameraT, setupData, directory, fov);
            }

            RestoreSavedSetup(placedSensors, setupData.avatarObject.transform);
        }

        public static void CleanOldSetup(GameObject avatarObject)
        {
            DeleteDeepChildStartsWith(avatarObject.transform, "[Toys]");
        }

        struct SavedSensors
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Transform parent;
            public string parentName;
            public string name;
            public Vector4 pixelPosition;
            public Vector2 cameraScaling;
            public bool selfActive;
        }
        private static List<SavedSensors> savedSensorSetup;
        private static bool savedDesktopUseable;
        private static void SaveOldSetup(List<PlacedSensor> placedSensors, ref SetupData setupData)
        {
            savedSensorSetup = new List<SavedSensors>();
            setupData.sensorCount = placedSensors.Count();
            setupData.addDesktopUseability = FindDeepChild(setupData.avatarObject.transform, HEAD_OBJ_NAME) != null;
            setupData.sensor_Positions = new List<SensorOptions>();
            int i = 0;
            foreach (PlacedSensor s in placedSensors)
            {
                SavedSensors saved = new SavedSensors();
                saved.selfActive = s.gameObject.activeSelf;
                saved.position = s.gameObject.transform.position;
                saved.rotation = s.gameObject.transform.rotation;
                saved.scale = s.gameObject.transform.localScale;
                saved.parent = s.gameObject.transform.parent;
                saved.parentName = s.gameObject.transform.parent.name;
                saved.name = s.gameObject.transform.name;
                saved.pixelPosition = s.material.HasProperty("_pixelPosition") ? s.material.GetVector("_pixelPosition") : new Vector4(i, 0, 0,0);
                saved.cameraScaling = new Vector2(s.camera.farClipPlane, s.camera.orthographicSize);
                savedSensorSetup.Add(saved);

                SensorOptions options = new SensorOptions();
                options.penetrator = (s.material.HasProperty("_CheckPenetratorOrface") && s.material.GetFloat("_CheckPenetratorOrface") == 1) ? Penetrator_Options.SupportOriface : Penetrator_Options.None;
                if (FindDeepChild(s.gameObject.transform, ORIFACE_LIGHT_POSITION_NAME) != null) options.penetrator = Penetrator_Options.PlaceOriface;
                options.position = Sensor_Position.Crotch;
                setupData.sensor_Positions.Add(options);
                i++;
            }
        }

        private static void RestoreSavedSetup(List<PlacedSensor> placedSensors, Transform avatarRoot)
        {
            for(int i= 0;i < placedSensors.Count;i++)
            {
                placedSensors[i].gameObject.name = savedSensorSetup[i].name;
                if(savedSensorSetup[i].parent) placedSensors[i].gameObject.transform.parent = savedSensorSetup[i].parent;
                else if(savedSensorSetup[i].parentName != null) placedSensors[i].gameObject.transform.parent =  FindDeepChild(avatarRoot, savedSensorSetup[i].parentName);
                placedSensors[i].gameObject.transform.position = savedSensorSetup[i].position;
                placedSensors[i].gameObject.transform.rotation = savedSensorSetup[i].rotation;
                placedSensors[i].gameObject.transform.localScale = savedSensorSetup[i].scale;
                placedSensors[i].gameObject.SetActive(savedSensorSetup[i].selfActive);
                placedSensors[i].material.SetVector("_pixelPosition", savedSensorSetup[i].pixelPosition);
                placedSensors[i].camera.orthographicSize = savedSensorSetup[i].cameraScaling.y;
                placedSensors[i].camera.farClipPlane = savedSensorSetup[i].cameraScaling.x;
                placedSensors[i].camera.transform.position = placedSensors[i].gameObject.transform.position + placedSensors[i].gameObject.transform.rotation * Vector3.back * savedSensorSetup[i].cameraScaling.x;
            }
        }

        private static RenderTexture[] CreateRenderTextures(string name, string directory, SetupData setupData)
        {
            List<RenderTexture> renderTextures = new List<RenderTexture>();
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(10, 10, RenderTextureFormat.Depth, 24);
            descriptor.sRGB = false;
            for(int i = 0; i < setupData.sensorCount; i++)
            {
                RenderTexture textue = new RenderTexture(descriptor);
                AssetDatabase.CreateAsset(textue, directory + "/" + name + "_" + i+".rendertexture");
                renderTextures.Add(textue);
            }
            return renderTextures.ToArray();
        }

        private static Transform CreateHeadObjectWithConstaint(HumanBone[] bones, float cameraFovId, out Transform constraintHead, SetupData setupData)
        {
            GameObject cameraObj;
            Camera c;
            constraintHead = null;

            Transform head = null;
            bool addConstraintHead = setupData.addDesktopUseability || setupData.sensor_Positions.Any(s => s.position == Sensor_Position.Head);
            if (addConstraintHead)
            {
                head = FindBoneTransform(bones, "Head", setupData.avatarObject);
                Transform neck = FindBoneTransform(bones, "Neck", setupData.avatarObject);
                if (head == null || neck == null)
                {
                    Debug.LogError("[Thry][ToyController] Could not find head or neck bone.");
                    return null;
                }

                GameObject fakeHead = new GameObject(HEAD_OBJ_NAME);
                fakeHead.transform.parent = neck;
                fakeHead.transform.position = head.position;
                fakeHead.transform.rotation = head.rotation;
                UnityEngine.Animations.RotationConstraint constraint = fakeHead.AddComponent<UnityEngine.Animations.RotationConstraint>();
                constraint.enabled = true;
                constraint.constraintActive = true;
                UnityEngine.Animations.ConstraintSource source = new UnityEngine.Animations.ConstraintSource();
                source.sourceTransform = head;
                source.weight = 1;
                constraint.AddSource(source);
                constraintHead = fakeHead.transform;
            }

            if (setupData.addDesktopUseability)
            {
                cameraObj = new GameObject(CAMERA_OVERRENDERER_NAME);
                cameraObj.transform.parent = constraintHead;
                c = cameraObj.AddComponent<Camera>();

                //camera position and rotation
                int eyeTransforms = 0;
                Vector3 eyes = Vector3.zero;
                for (int i = 0; i < head.childCount; i++)
                {
                    if (head.GetChild(i).name.ToLower().Contains("eye"))
                    {
                        eyes = eyes + head.GetChild(i).localPosition;
                        eyeTransforms++;
                    }
                }
                if (eyeTransforms > 0) eyes = new Vector3(eyes.x / eyeTransforms, eyes.y / eyeTransforms, eyes.z / eyeTransforms);
                cameraObj.transform.localPosition = eyes;
                cameraObj.transform.localRotation = Quaternion.identity;
                
                c.cullingMask = ~(LayerMask.GetMask("MirrorReflection"));
            }
            else
            {
                cameraObj = new GameObject("[Toys] Camera Overrender");
                cameraObj.transform.parent = setupData.avatarObject.transform;
                cameraObj.transform.localPosition = new Vector3(0, 0, 0);
                cameraObj.transform.position = cameraObj.transform.position + new Vector3(0, -0.5f, 0);
                cameraObj.transform.rotation = Quaternion.identity;
                c = cameraObj.AddComponent<Camera>();

                c.cullingMask = LayerMask.GetMask("PlayerLocal");

                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = Color.gray;

                c.farClipPlane = 0.25f;
                
                c.useOcclusionCulling = false;
            }
            c.allowHDR = false;
            c.allowMSAA = true;
            c.nearClipPlane = 0.01f;
            c.fieldOfView = cameraFovId;

            c.stereoTargetEye = StereoTargetEyeMask.None;

            return cameraObj.transform;
        }

        private static void CreateOverrideCameraPixelWriter(Transform cameraTransform, SetupData setupData, string folderpath, float _CameraFOV)
        {
            Mesh quad = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:mesh VRCToysQuad")[0]));
            Shader shader = Shader.Find(SHADER_NAME_MAIN);

            /*GameObject o = new GameObject("[Toys] Main Writer");
            o.transform.parent = cameraTransform;
            o.transform.localPosition = Vector3.zero;
            o.transform.rotation = Quaternion.identity;
            o.transform.localScale = Vector3.one * 0.000001f;
            o.layer = LayerMask.NameToLayer("PlayerLocal");*/

            cameraTransform.localScale = Vector3.one * 0.000001f;
            cameraTransform.gameObject.layer = LayerMask.NameToLayer("PlayerLocal");

            MeshFilter meshFilter = cameraTransform.gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = quad;

            Material m = new Material(shader);
            m.SetFloat("_CameraFOV", _CameraFOV);
            AssetDatabase.CreateAsset(m, folderpath + "/mainWriter_" + setupData.avatarObject.name + ".mat");

            MeshRenderer r = cameraTransform.gameObject.AddComponent<MeshRenderer>();
            r.allowOcclusionWhenDynamic = false;
            r.sharedMaterials = new Material[] { m };
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void AddDepthGet(GameObject avatarObj)
        {
            GameObject o = new GameObject("[Toys] DepthGet");
            o.transform.parent = avatarObj.transform;
            o.transform.localPosition = Vector3.zero;
            o.transform.rotation = Quaternion.LookRotation(Vector3.down);
            Light l = o.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.001f;
            l.bounceIntensity = 0;
            l.shadows = LightShadows.Hard;
            l.shadowStrength = 1;
            l.shadowBias = 0.05f;
            l.shadowNormalBias = 0.4f;
            l.shadowNearPlane = 0.2f;
            l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            l.shadowStrength = 0;
            l.cullingMask = LayerMask.GetMask("Ignore Raycast");
        }

        private static void CreateSensorCameras(RenderTexture[] textures, HumanBone[] bones, string folderpath, float cameraFovId, Transform constrainHead, SetupData setupData, List<PlacedSensor> placedSensors)
        {
            Mesh quad = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:mesh VRCToysQuad")[0]));
            Shader shader = Shader.Find(SHADER_NAME_SENSOR);

            Dictionary<Sensor_Position, bool> doSensorPosCount = new Dictionary<Sensor_Position, bool>();
            Dictionary<Sensor_Position, int> sensorPosCount = new Dictionary<Sensor_Position, int>();
            foreach (Sensor_Position p in Enum.GetValues(typeof(Sensor_Position)))
            {
                sensorPosCount.Add(p, 0);
                doSensorPosCount.Add(p, setupData.sensor_Positions.Where(s => s.position == p).Count() > 1);
            }
            for (int i = 0; i < setupData.sensorCount; i++)
            {
                GameObject o = GetSensorTransform(i, bones, constrainHead, setupData);

                Sensor_Position position = setupData.sensor_Positions[i].position;
                o.name = "[Toys] Sensor_" + position + (doSensorPosCount[position] ? "_"+ sensorPosCount[position] : "");
                sensorPosCount[position] = sensorPosCount[position] + 1;

                o.layer = LayerMask.NameToLayer("PlayerLocal");

                GameObject oChild = new GameObject("[Toys] Camera_" + i);
                oChild.transform.position = o.transform.position + o.transform.rotation * Vector3.forward * -0.2f;
                oChild.transform.parent = o.transform;
                oChild.transform.localRotation = Quaternion.identity;
                oChild.layer = LayerMask.NameToLayer("PlayerLocal");
                oChild.transform.localScale = Vector3.one * 0.000002f;

                Camera c = oChild.AddComponent<Camera>();
                c.orthographic = true;
                c.orthographicSize = 0.015f;
                c.nearClipPlane = 0;
                c.farClipPlane = 0.2f;
                c.clearFlags = CameraClearFlags.Depth;
                c.cullingMask = LayerMask.GetMask("Player", "Pickup", "PickupNoEnvironment");
                c.useOcclusionCulling = false;
                c.allowHDR = false;
                c.allowMSAA = false;
                c.targetTexture = textures[i];

                Material m = new Material(shader);
                m.SetFloat("_CameraFOV", cameraFovId);
                m.SetTexture("_depthcam", textures[i]);
                m.SetVector("_pixelPosition", new Vector4(i, 0, 0, 0));
                m.SetInt("_CheckPenetratorOrface", setupData.sensor_Positions[i].penetrator != Penetrator_Options.None?1:0);
                if(setupData.sensor_Positions[i].penetrator != Penetrator_Options.None) m.EnableKeyword("GEOM_TYPE_BRANCH");
                AssetDatabase.CreateAsset(m, folderpath + "/pixelWriter_" + setupData.avatarObject.name + "_" + i + ".mat");

                MeshFilter meshFilter = oChild.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = quad;

                MeshRenderer r = oChild.AddComponent<MeshRenderer>();
                r.allowOcclusionWhenDynamic = false;
                r.sharedMaterials = new Material[] { m };
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (setupData.sensor_Positions[i].penetrator == Penetrator_Options.PlaceOriface)
                {
                    CreateOrfaceLights(o.transform);
                }

                PlacedSensor placed = new PlacedSensor();
                placed.camera = c;
                placed.gameObject = o;
                placed.material = m;
                placedSensors.Add(placed);
            }
        }

        const string ORIFACE_LIGHT_POSITION_NAME = "[Toys] Orfice Position";
        const string ORIFACE_LIGHT_NORMAL_NAME = "[Toys] Orfice Normal";
        private static void CreateOrfaceLights(Transform parent)
        {
            GameObject objPosition = new GameObject(ORIFACE_LIGHT_POSITION_NAME);
            GameObject objNormal = new GameObject(ORIFACE_LIGHT_NORMAL_NAME);
            objPosition.transform.parent = parent;
            objNormal.transform.parent = parent;
            objPosition.transform.localPosition = Vector3.zero;
            objPosition.transform.localRotation = Quaternion.identity;
            objNormal.transform.localPosition = Vector3.forward * 0.1f;
            objNormal.transform.localRotation = Quaternion.identity;

            AddPenetratorLight(objPosition).range = 0.42f;
            AddPenetratorLight(objNormal).range = 0.45f;
        }

        private static Light AddPenetratorLight(GameObject o)
        {
            Light l = o.AddComponent<Light>();
            l.renderMode = LightRenderMode.ForceVertex;
            l.color = Color.black;
            l.intensity = 6;
            return l;
        }

        private static GameObject GetSensorTransform(int i, HumanBone[] bones, Transform constrainHead, SetupData setupData)
        {
            // 0: v, 1: a, 2: chest, 3: head,
            Quaternion rotation = Quaternion.identity;
            Vector3 localPos = Vector3.zero;
            Transform parent = null;

            //handle stuff that shouldnt even happen, but you know, just in case
            if (setupData.sensor_Positions.Count == 0) setupData.sensor_Positions.Add(new SensorOptions());
            if (i >= setupData.sensor_Positions.Count) i = 0;

            switch (setupData.sensor_Positions[i].position)
            {
                case Sensor_Position.Head:
                    parent = constrainHead;
                    rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    //parent = FindTransfromPath(avatarRoot, "Armature", "Hip", "Spine", "Chest", "Neck", "Head");
                    break;
                case Sensor_Position.Chest:
                    parent = FindBoneTransform(bones, "Chest", setupData.avatarObject);
                    //parent = FindTransfromPath(avatarRoot, "Armature", "Hip", "Spine", "Chest");
                    break;
                case Sensor_Position.Butt:
                    parent = FindBoneTransform(bones, "Hips", setupData.avatarObject);
                    //parent = FindTransfromPath(avatarRoot, "Armature", "Hip");
                    rotation = Quaternion.Euler(new Vector3(0, 180, 0));
                    localPos = Vector3.forward * -0.1f;
                    break;
            }
            if(parent == null)
            {
                parent = FindBoneTransform(bones, "Hips", setupData.avatarObject);
                localPos = Vector3.forward * 0.1f;
            }
            if (parent == null) parent = setupData.avatarObject.transform;
            GameObject o = new GameObject();
            o.transform.SetParent(parent);
            o.transform.rotation = setupData.avatarObject.transform.rotation * rotation;
            o.transform.position = parent.transform.position + setupData.avatarObject.transform.rotation * localPos;
            return o;
        }

        private static Transform FindBoneTransform(HumanBone[] bones, string name, GameObject avatarObject)
        {
            IEnumerable<HumanBone> col = bones.Where(b => b.humanName == name);
            if (col.Count() == 0) return null;
            return FindDeepChild(avatarObject.transform, col.First().boneName);
        }

        private static Transform FindDeepChild(Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }

        public static void FindDeepChildren(Transform aParent, string aName, List<GameObject> list)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name.StartsWith(aName))
                    list.Add(c.gameObject);
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
        }

        private static void DeleteDeepChildStartsWith(Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name.StartsWith(aName))
                    DestroyImmediate(c.gameObject);
                else
                    foreach (Transform t in c)
                        queue.Enqueue(t);
            }
        }

        public static bool GetHumanDescription(string path, ref HumanDescription des)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                //Debug.Log("AssetImporter Type: " + importer.GetType());
                ModelImporter modelImporter = importer as ModelImporter;
                if (modelImporter != null)
                {
                    des = modelImporter.humanDescription;
                    //Debug.Log("## Cool stuff data by ModelImporter ##");
                    return true;
                }
            }
            return false;
        }

    }
}