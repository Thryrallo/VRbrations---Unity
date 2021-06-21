using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
#endif

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
#if VRC_SDK_VRCSDK3 && !UDON
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
                        List<GameObject> foundObjs = new List<GameObject>();
                        VRbrationsSetup.FindDeepChildren(Selection.activeTransform, VRbrationsSetup.VRBRATIONS_PREFIX, foundObjs);
                        VRbrationsSetup.FindDeepChildren(Selection.activeTransform, "[Toys]"+" Sensor", foundObjs);
                        if (foundObjs.Count > 0)
                        {
                            vrb = Selection.activeTransform.gameObject.AddComponent<vrbrations>();
                            vrb.isSetup = true;
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
#if VRC_SDK_VRCSDK3 && !UDON
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

        public List<PlacedSensor> placedSensors = new List<PlacedSensor>();
        public bool load_PlacedSensors = true;
        vrbrations vrbrationsScript;

        bool isInit = false;

        #region GUI
        public override void OnInspectorGUI()
        {
            if (!isInit)
            {
                Styles.Init();
                Init();
            }
            vrbrationsScript = target as vrbrations;

            GUIHeader();

#if VRC_SDK_VRCSDK3
#if !UDON
            if (vrbrationsScript.GetComponent<VRCAvatarDescriptor>() != null)
            {
                if (load_PlacedSensors)
                {
                    VRbrationsSetup.FindAvatarSensors(placedSensors, new SetupData(vrbrationsScript.gameObject, sensorCount, sensor_Positions), vrbrationsScript.foundSensorsObjects);
                    load_PlacedSensors = false;
                }

                EditorGUIUtility.wideMode = true;
                if (vrbrationsScript.isSetup)
                    GUIExistingSetup();
                else
                    GUINewSetup();

                EditorGUIUtility.wideMode = false;
            }
            else
            {
                EditorGUILayout.LabelField("<size=20>No avatar descriptor found.</size>", Styles.masterLabel, GUILayout.Height(40));
            }
#else

#endif
#else
            EditorGUILayout.LabelField("<size=24>VRbrations setup only works with sdk3</size>", Styles.masterLabel, GUILayout.Height(40));
#endif
        }

        void Init()
        {
#if VRC_SDK_VRCSDK3
            if (!UpdateLayers.AreLayersSetup())
            {
                UpdateLayers.SetupEditorLayers();
            }
#endif
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

            if (GUILayout.Button("Setup"))
            {
                VRbrationsSetup.SetupAvatar(placedSensors, new SetupData(vrbrationsScript.gameObject, sensorCount, sensor_Positions));
                load_PlacedSensors = true;
                vrbrationsScript.isSetup = true;
            }
        }

        private void GUIExistingSetup()
        {
            GUIExistingSensors();
            GUIUpdateSetup();
            GUIDeleteSetup();
            ValidateSensorPositions();
        }
        
        private void GUIExistingSensors()
        {
            EditorGUILayout.LabelField("<b><size=15>" + "Existing Sensors" + "</size></b>", Styles.RichText, GUILayout.Height(20));

            foreach (PlacedSensor sensor in placedSensors)
            {
                EditorGUILayout.LabelField("<b><size=12>" + sensor.gameObject.name + "</size></b>", Styles.RichText);

                VRCToysUI.TextGUI(sensor.material, "Encoded Name");

                //Pixel Position
                //Vector4 _pixelPosition = sensor.material.GetVector("_pixelPosition");
                //_pixelPosition = (Vector2)EditorGUILayout.Vector2IntField(new GUIContent("Pixel Position", "The x and y position that you also have to enter in the software."), new Vector2Int((int)_pixelPosition.x, (int)_pixelPosition.y));
                //sensor.material.SetVector("_pixelPosition", _pixelPosition);

                //Sensor camera length, width
                //EditorGUI.BeginChangeCheck();
                //sensor.camera.farClipPlane = EditorGUILayout.FloatField("Depth", sensor.camera.farClipPlane);
                //if (EditorGUI.EndChangeCheck())
                //    sensor.camera.transform.position = sensor.camera.transform.parent.position + sensor.camera.transform.parent.rotation * Vector3.forward * -sensor.camera.farClipPlane;
                //sensor.camera.orthographicSize = EditorGUILayout.FloatField("Width", sensor.camera.orthographicSize);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Edit Sensors", vrbrationsScript.editSensors ? Styles.buttonSelected : GUI.skin.button)){
                vrbrationsScript.editSensors = !vrbrationsScript.editSensors;
                Tools.hidden = vrbrationsScript.editSensors;
                ToggleGizmos(!vrbrationsScript.editSensors);
            }
        }

        private void ValidateSensorPositions()
        {
            HashSet<(int, int)> usedPositions = new HashSet<(int, int)>();
            List<PlacedSensor> invalids = new List<PlacedSensor>();
            foreach(PlacedSensor s in placedSensors)
            {
                if (s.material == null) continue;
                Vector4 pos = s.material.GetVector("_pixelPosition");
                bool valid = pos.x >= 1 && pos.x <= VRCToysUI.MAX_X && pos.y >= 0 && pos.y <= VRCToysUI.MAX_Y && usedPositions.Contains(((int)pos.x, (int)pos.y)) == false;
                if (valid)
                {
                    usedPositions.Add(((int)pos.x, (int)pos.y));
                }
                else
                {
                    invalids.Add(s);
                }
            }
            foreach(PlacedSensor s in invalids)
            {
                Vector4 pos = s.material.GetVector("_pixelPosition");
                pos.x = 1;
                pos.y = 0;
                while(usedPositions.Contains(((int)pos.x, (int)pos.y)))
                {
                    pos.x += 1;
                    if(pos.x > VRCToysUI.MAX_X)
                    {
                        pos.x = 0;
                        pos.y += 1;
                    }
                }
                s.material.SetVector("_pixelPosition", pos);
            }
        }

        private void GUIUpdateSetup()
        {
            EditorGUILayout.Space();
            GUILayout.Label("<b><size=15>" + "Update Setup" + "</size></b>", Styles.RichText);
            if (GUILayout.Button("Update Setup"))
            {
                VRbrationsSetup.SetupAvatar(placedSensors, new SetupData(vrbrationsScript.gameObject, true));
                load_PlacedSensors = true;
            }
        }

        private void GUIDeleteSetup()
        {
            EditorGUILayout.Space();
            GUILayout.Label("<b><size=15>" + "Delete Setup" + "</size></b>", Styles.RichText);
            if (GUILayout.Button("Clear Setup"))
            {
                VRbrationsSetup.CleanOldSetup(vrbrationsScript.gameObject);
                placedSensors.Clear();
                vrbrationsScript.isSetup = false;
            }
        }
#endregion

#region Scene GUI
        void OnSceneGUI()
        {
            vrbrationsScript = target as vrbrations;

            if (vrbrationsScript.editSensors)
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
        public bool fromSave;

        public SetupData(GameObject avatarObject, int sensorCount, List<SensorOptions> sensor_Positions)
        {
            this.avatarObject = avatarObject;
            this.sensorCount = sensorCount;
            this.sensor_Positions = sensor_Positions;
            this.fromSave = false;
        }

        public SetupData(GameObject avatarObject, bool fromSave)
        {
            this.avatarObject = avatarObject;
            this.sensorCount = 0;
            this.sensor_Positions = null;
            this.fromSave = true;
        }
    }

    class VRbrationsSetup : Editor
    {
        const string SHADER_NAME_SENSOR = "VRBrations/Sensor";
        const string SHADER_NAME_MAIN = "VRBrations/Main";
        const float CAMERA_FOV = 59.5f;
        const float CAMERA_FOV_MAX_DELTA = 1;

        public const string VRBRATIONS_PREFIX = "[VRbrations]";

        public static void FindAvatarSensors(List<PlacedSensor> placedSensor, SetupData setupData, List<GameObject> foundSensors = null)
        {
            placedSensor.Clear();
            if(foundSensors == null || foundSensors.Any(o => o == null) || foundSensors.Count == 0)
            {
                foundSensors = new List<GameObject>();
                FindDeepChildren(setupData.avatarObject.transform, VRBRATIONS_PREFIX+" Sensor", foundSensors);
                FindDeepChildren(setupData.avatarObject.transform, "[Toys]"+" Sensor", foundSensors);
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
                Debug.LogError("[Thry][VRbrations] Could not find avatar animator.");
                return;
            }
            Avatar avatar = animator.avatar;
            if (avatar == null)
            {
                Debug.LogError("[Thry][VRbrations] Could not find animator avatar reference.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(avatar);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Thry][VRbrations] Could not find prefab path.");
                return;
            }

            string avatarDirectory = Path.GetDirectoryName(path);
            string vrbrationsDirectory = avatarDirectory + "/VRbrations";
            if (Directory.Exists(vrbrationsDirectory) == false) AssetDatabase.CreateFolder(avatarDirectory, "VRbrations");
            string vrationsAvatarDirectory = avatarDirectory + "/VRbrations/"+setupData.avatarObject.name;
            if (Directory.Exists(vrationsAvatarDirectory) == false) AssetDatabase.CreateFolder(vrbrationsDirectory, setupData.avatarObject.name);

            HumanDescription humanDescription = new HumanDescription();
            if (!GetHumanDescription(path, ref humanDescription))
            {
                Debug.LogError("[Thry][VRbrations] Could not find humanoid rig.");
                return;
            }
            HumanBone[] bones = humanDescription.human;

            if (setupData.fromSave) SaveOldSetup(placedSensors, ref setupData);
            CleanOldSetup(setupData.avatarObject);
            placedSensors.Clear();

            float fov = CAMERA_FOV + UnityEngine.Random.Range(0, CAMERA_FOV_MAX_DELTA);

            RenderTexture[] textures = CreateRenderTextures("sensor_" + setupData.avatarObject.name, vrationsAvatarDirectory, setupData);

            Transform overrenderer;
            Transform constrainedHead = CreateHeadObjectWithConstaint(bones, 60, out overrenderer, setupData);

            CreateSensorCameras(textures, bones, vrationsAvatarDirectory, fov, setupData, placedSensors, constrainedHead);

            GameObject mainWriter = CreateMainWriter(setupData, vrationsAvatarDirectory, fov, overrenderer);
            AddDepthGet(overrenderer.gameObject);

            if (setupData.fromSave) RestoreSavedSetup(placedSensors, setupData.avatarObject.transform);

            //Scale All to zero, animations will scale back up
            overrenderer.localScale = Vector3.zero;
            foreach (PlacedSensor p in placedSensors) p.camera.transform.localScale = Vector3.zero;

            CreateAnimations(setupData.avatarObject, placedSensors, overrenderer.gameObject, vrationsAvatarDirectory);
        }

        public static void CleanOldSetup(GameObject avatarObject)
        {
            DeleteDeepChildStartsWith(avatarObject.transform, "[Toys]");
            DeleteDeepChildStartsWith(avatarObject.transform, VRBRATIONS_PREFIX);
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
            public string encodedName;
        }
        private static List<SavedSensors> savedSensorSetup;
        private static bool savedDesktopUseable;
        private static void SaveOldSetup(List<PlacedSensor> placedSensors, ref SetupData setupData)
        {
            savedSensorSetup = new List<SavedSensors>();
            setupData.sensorCount = placedSensors.Count();
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
                saved.encodedName = VRCToysUI.GetSensorName(s.material);
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
            for(int i= 0;i < placedSensors.Count && i<savedSensorSetup.Count;i++)
            {
                placedSensors[i].gameObject.name = savedSensorSetup[i].name;
                if(savedSensorSetup[i].parent) placedSensors[i].gameObject.transform.parent = savedSensorSetup[i].parent;
                else if(savedSensorSetup[i].parentName != null) placedSensors[i].gameObject.transform.parent =  FindDeepChild(avatarRoot, savedSensorSetup[i].parentName);
                placedSensors[i].gameObject.transform.position = savedSensorSetup[i].position;
                placedSensors[i].gameObject.transform.rotation = savedSensorSetup[i].rotation;
                //placedSensors[i].gameObject.transform.localScale = savedSensorSetup[i].scale;
                //placedSensors[i].gameObject.SetActive(savedSensorSetup[i].selfActive);
                placedSensors[i].material.SetVector("_pixelPosition", savedSensorSetup[i].pixelPosition);
                VRCToysUI.SetSensorName(placedSensors[i].material, savedSensorSetup[i].encodedName);
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

        private static Transform CreateHeadObjectWithConstaint(HumanBone[] bones, float cameraFovId, out Transform cameraTransform, SetupData setupData)
        {
            GameObject cameraObj;
            Camera c;
            cameraTransform = null;

            Transform head = FindBoneTransform(bones, "Head", setupData.avatarObject);
            Transform neck = FindBoneTransform(bones, "Neck", setupData.avatarObject);
            if (head == null || neck == null)
            {
                Debug.LogError("[Thry][ToyController] Could not find head or neck bone.");
                return null;
            }

            GameObject fakeHead = new GameObject(VRBRATIONS_PREFIX + "[Head]");
            fakeHead.transform.parent = neck;
            fakeHead.transform.position = head.position;
            fakeHead.transform.rotation = head.rotation;
            fakeHead.transform.localScale = Vector3.one;
            UnityEngine.Animations.RotationConstraint constraint = fakeHead.AddComponent<UnityEngine.Animations.RotationConstraint>();
            constraint.enabled = true;
            constraint.constraintActive = true;
            UnityEngine.Animations.ConstraintSource source = new UnityEngine.Animations.ConstraintSource();
            source.sourceTransform = head;
            source.weight = 1;
            constraint.AddSource(source);

            cameraObj = new GameObject(VRBRATIONS_PREFIX+" Overrenderer");
            cameraObj.transform.parent = fakeHead.transform;
            cameraObj.SetActive(false);
            cameraTransform = cameraObj.transform;
            cameraTransform.localScale = Vector3.one;
            c = cameraObj.AddComponent<Camera>();

            //camera position and rotation
#if VRC_SDK_VRCSDK3 && !UDON
            VRCAvatarDescriptor descriptor = setupData.avatarObject.GetComponent<VRCAvatarDescriptor>();
            cameraObj.transform.position = setupData.avatarObject.transform.position + descriptor.ViewPosition;
#else
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
#endif
            cameraObj.transform.localRotation = Quaternion.identity;

            c.cullingMask = ~(LayerMask.GetMask("MirrorReflection"));

            c.allowHDR = false;
            c.allowMSAA = true;
            c.nearClipPlane = 0.01f;
            c.fieldOfView = cameraFovId;

            c.stereoTargetEye = StereoTargetEyeMask.None;

            return fakeHead.transform;
        }

        private static GameObject CreateMainWriter(SetupData setupData, string folderpath, float _CameraFOV, Transform parent)
        {
            Mesh quad = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:mesh VRCToysQuad")[0]));
            Shader shader = Shader.Find(SHADER_NAME_MAIN);

            GameObject o = new GameObject(VRBRATIONS_PREFIX+" Main Writer");
            o.SetActive(true);
            o.transform.parent = parent;
            o.transform.localPosition = Vector3.zero;
            o.transform.rotation = Quaternion.identity;
            o.transform.localScale = Vector3.one;
            o.layer = LayerMask.NameToLayer("PlayerLocal");

            MeshFilter meshFilter = o.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = quad;

            Material m = new Material(shader);
            m.SetFloat("_CameraFOV", _CameraFOV);
            AssetDatabase.CreateAsset(m, folderpath + "/mainWriter_" + setupData.avatarObject.name + ".mat");

            MeshRenderer r = o.AddComponent<MeshRenderer>();
            r.allowOcclusionWhenDynamic = false;
            r.sharedMaterials = new Material[] { m };
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return o;
        }

        private static void AddDepthGet(GameObject avatarObj)
        {
            GameObject o = new GameObject(VRBRATIONS_PREFIX+" DepthGet");
            o.transform.parent = avatarObj.transform;
            o.transform.localPosition = Vector3.zero;
            o.transform.rotation = Quaternion.LookRotation(Vector3.down);
            o.transform.localScale = Vector3.one;
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

        private static void CreateSensorCameras(RenderTexture[] textures, HumanBone[] bones, string folderpath, float cameraFovId, SetupData setupData, List<PlacedSensor> placedSensors, Transform constainedHead)
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
                GameObject o = GetSensorTransform(i, bones, setupData, constainedHead);

                Sensor_Position position = setupData.sensor_Positions[i].position;
                string name = position + (doSensorPosCount[position] ? "_" + sensorPosCount[position] : "");
                o.name = VRBRATIONS_PREFIX+" Sensor_" + name;
                o.SetActive(false);
                o.transform.localScale = Vector3.one;
                sensorPosCount[position] = sensorPosCount[position] + 1;

                o.layer = LayerMask.NameToLayer("PlayerLocal");

                GameObject oChild = new GameObject(VRBRATIONS_PREFIX+" Camera_" + i);
                oChild.transform.position = o.transform.position + o.transform.rotation * Vector3.forward * -0.2f;
                oChild.transform.parent = o.transform;
                oChild.transform.localRotation = Quaternion.identity;
                oChild.layer = LayerMask.NameToLayer("PlayerLocal");

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
                m.SetVector("_pixelPosition", new Vector4(i % (VRCToysUI.MAX_X+1), i / (VRCToysUI.MAX_X+1), 0, 0));
                m.SetInt("_CheckPenetratorOrface", setupData.sensor_Positions[i].penetrator != Penetrator_Options.None?1:0);
                VRCToysUI.SetSensorName(m, name);
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

        const string ORIFACE_LIGHT_POSITION_NAME = VRBRATIONS_PREFIX+" Orfice Position";
        const string ORIFACE_LIGHT_NORMAL_NAME = VRBRATIONS_PREFIX+" Orfice Normal";
        private static void CreateOrfaceLights(Transform parent)
        {
            GameObject objPosition = new GameObject(ORIFACE_LIGHT_POSITION_NAME);
            GameObject objNormal = new GameObject(ORIFACE_LIGHT_NORMAL_NAME);
            objPosition.transform.parent = parent;
            objNormal.transform.parent = parent;
            objPosition.transform.localPosition = Vector3.zero;
            objPosition.transform.localRotation = Quaternion.identity;
            objNormal.transform.localPosition = Vector3.forward * 0.01f;
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

        private static GameObject GetSensorTransform(int i, HumanBone[] bones, SetupData setupData, Transform constrainedHead)
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
                    parent = constrainedHead;
                    rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    localPos = Vector3.up * 0.2f;
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

        private static void CreateAnimations(GameObject avatarObject, List<PlacedSensor> placedSensors, GameObject overrenderer, string avatarVRbrationsDirectory)
        {
#if VRC_SDK_VRCSDK3 && !UDON
            VRCAvatarDescriptor descriptor = avatarObject.GetComponent<VRCAvatarDescriptor>();
            //make sure custom layers are enabled
            descriptor.customizeAnimationLayers = true;
            //Get fx animator
            CustomAnimLayer[] layers = descriptor.baseAnimationLayers;
            //CustomAnimLayer fxLayer = layers.Where(l => l.type == AnimLayerType.FX).FirstOrDefault();
            CustomAnimLayer fxLayer = layers[4];
            AnimatorController fxAnimator = null;
            //If no fx animator exists create one
            if (fxLayer.type != AnimLayerType.FX || fxLayer.animatorController == null)
            {
                fxAnimator = AnimatorController.CreateAnimatorControllerAtPath(avatarVRbrationsDirectory + "/vrbrationsFXLayer.controller");
                fxLayer.type = AnimLayerType.FX;
                fxLayer.isEnabled = true;
                fxLayer.isDefault = false;
                fxLayer.animatorController = fxAnimator;
                layers[4] = fxLayer;
                descriptor.baseAnimationLayers = layers;
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(fxLayer.animatorController);
                if(path != null)
                {
                    fxAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                }
                else
                {
                    Debug.LogError("Error creating animator: path now found.");
                }
                if (fxAnimator == null)
                {
                    Debug.LogError("Error creating animator: animator is null.");
                }
            }
            //else check if [Toys] Layers exists and remove them
            for(int i= fxAnimator.layers.Length - 1; i >= 0; i--)
            {
                if (fxAnimator.layers[i].name.StartsWith(VRBRATIONS_PREFIX))
                {
                    fxAnimator.RemoveLayer(i);
                }
            }
            //Add IsLocal Parameter if ddoesnt exisit
            bool hasLocalParameter = fxAnimator.parameters.Where(p => p.name == "IsLocal" && p.type == AnimatorControllerParameterType.Bool).Count() > 0;
            if (!hasLocalParameter)
            {
                fxAnimator.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
            }

            string mainParam = "VRbrations Main";
            CreateLayerForToggle(null, overrenderer, true, VRBRATIONS_PREFIX+" Main", new string[] { mainParam }, avatarVRbrationsDirectory, avatarObject, fxAnimator);
            foreach(PlacedSensor s in placedSensors)
            {
                string name = s.gameObject.name.Replace("[Toys]", "").Replace(VRBRATIONS_PREFIX,"");
                //CreateLayerForToggle(s.gameObject, s.camera.gameObject, true, VRBRATIONS_PREFIX+" " + name, new string[] { mainParam, "VRbrations " + name }, avatarVRbrationsDirectory, avatarObject, fxAnimator);
                CreateLayerForToggle(s.gameObject, s.camera.gameObject, true, VRBRATIONS_PREFIX+" " + name, new string[] { "VRbrations " + name }, avatarVRbrationsDirectory, avatarObject, fxAnimator);
            }

            EditorUtility.SetDirty(fxAnimator);
            AssetDatabase.SaveAssets();
#endif
        }

        private static void CreateLayerForToggle(GameObject objectToToggle, GameObject objectToToggleLocalOnly, bool scaleLocalObject, string layername, string[] onParameterNames, string directory, GameObject avatarObject, AnimatorController fxAnimator)
        {
            //Add parameter
            foreach (string param in onParameterNames)
            {
                bool hasParameter = fxAnimator.parameters.Where(p => p.name == param && p.type == AnimatorControllerParameterType.Bool).Count() > 0;
                if (!hasParameter)
                {
                    AnimatorControllerParameter animatorControllerParameter = new AnimatorControllerParameter();
                    animatorControllerParameter.name = param;
                    animatorControllerParameter.type = AnimatorControllerParameterType.Bool;
                    animatorControllerParameter.defaultBool = true;
                    fxAnimator.AddParameter(animatorControllerParameter);
                }
            }

            //Create Layer
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.name = layername;
            layer.defaultWeight = 1;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = layername;
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            {
                AssetDatabase.AddObjectToAsset(layer.stateMachine,
                    AssetDatabase.GetAssetPath(fxAnimator));
            }
            fxAnimator.AddLayer(layer);

            //Add states
            AnimatorState onState = layer.stateMachine.AddState("On");
            AnimatorState onStateLocal = layer.stateMachine.AddState("On Local");
            AnimatorState offState = layer.stateMachine.AddState("Off");
            onState.writeDefaultValues = false;
            onStateLocal.writeDefaultValues = false;
            offState.writeDefaultValues = false;
            layer.stateMachine.defaultState = offState;

#if VRC_SDK_VRCSDK3 && !UDON
            //Add animatordriver for main
            VRCAvatarParameterDriver driver = onState.AddStateMachineBehaviour(typeof(VRCAvatarParameterDriver)) as VRCAvatarParameterDriver;
            VRC.SDKBase.VRC_AvatarParameterDriver.Parameter driverParameter = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
            driverParameter.name = "VRbrations Main";
            driverParameter.value = 1;
            driver.parameters.Add(driverParameter);
#endif

            //add state transitions
            AnimatorStateTransition transitionOn = layer.stateMachine.AddAnyStateTransition(onState);
            AnimatorStateTransition transitionOnLocal = layer.stateMachine.AddAnyStateTransition(onStateLocal);
            transitionOnLocal.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
            transitionOn.AddCondition(AnimatorConditionMode.IfNot, 0, "IsLocal");
            foreach (string param in onParameterNames)
            {
                transitionOn.AddCondition(AnimatorConditionMode.If, 0, param);
                transitionOnLocal.AddCondition(AnimatorConditionMode.If, 0, param);

                //off transitions
                AnimatorStateTransition transitionOff = layer.stateMachine.AddAnyStateTransition(offState);
                transitionOff.AddCondition(AnimatorConditionMode.IfNot, 0, param);
            }
                

            //add motions
            onState.motion = CreateClip(new (GameObject, bool, bool)[] { (objectToToggle,true,false) }, avatarObject, directory, layername + "_On");
            onStateLocal.motion = CreateClip(new (GameObject, bool, bool)[] { (objectToToggle, true, false), (objectToToggleLocalOnly, true, scaleLocalObject) }, avatarObject, directory, layername + "_OnLocal");
            offState.motion = CreateClip(new (GameObject, bool, bool)[] { (objectToToggle, false, false), (objectToToggleLocalOnly, false, scaleLocalObject) }, avatarObject, directory, layername + "_Off");
        }

        private static AnimationClip CreateClip((GameObject,bool,bool)[]  gameObjectStateScale, GameObject avatarObject, string directory, string name)
        {
            //create curves
            AnimationCurve onCurve = new AnimationCurve();
            onCurve.AddKey(0, 1);
            onCurve.AddKey(60, 1);
            AnimationCurve offCurve = new AnimationCurve();
            offCurve.AddKey(0, 0);
            offCurve.AddKey(60, 0);

            AnimationClip clip = new AnimationClip();
            foreach((GameObject, bool, bool) gss in gameObjectStateScale)
            {
                if(gss.Item1 != null) AddActiveAndScaleCurves(gss.Item1, avatarObject, gss.Item2?onCurve:offCurve, clip, gss.Item3);
            }
            AssetDatabase.CreateAsset(clip, directory + "/"+ name + ".anim");
            return clip;
        }

        private static void AddActiveAndScaleCurves(GameObject objectToToggle, GameObject avatarObject, AnimationCurve curve, AnimationClip clip, bool doScale)
        {
            string path = GetPath(objectToToggle, avatarObject);
            clip.SetCurve(path, typeof(GameObject), "m_IsActive", curve);
            if (doScale)
            {
                clip.SetCurve(path, typeof(Transform), "m_LocalScale.x", curve);
                clip.SetCurve(path, typeof(Transform), "m_LocalScale.y", curve);
                clip.SetCurve(path, typeof(Transform), "m_LocalScale.z", curve);
            }
        }

        private static string GetPath(GameObject sensor, GameObject avatar)
        {
            Transform o = sensor.transform.parent;
            List<Transform> path = new List<Transform>();
            while(o != avatar.transform && o != null)
            {
                path.Add(o);
                o = o.parent;
            }
            path.Reverse();
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in path)
            {
                sb.Append(t.name + "/");
            }
            sb.Append(sensor.name);
            return sb.ToString();
        }
    }
}