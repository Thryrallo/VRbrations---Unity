using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Thry.VRBrations
{
    public class VRCToysUI : ShaderGUI
    {
        MaterialProperty _depthcam = null;
        MaterialProperty _pixelPosition = null;
        MaterialProperty _CameraFOV = null;

        MaterialProperty _InShaderMultiplier = null;
        MaterialProperty _OverideDepth = null;
        MaterialProperty _OverideWidth = null;

        MaterialProperty _CheckPenetratorOrface = null;

        MaterialProperty _InShaderAudioLinkMultiplier = null;

        MaterialProperty _HeaderTexture = null;

        public const int MAX_X = 39;
        public const int MAX_Y = 0;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.LabelField("<size=16><color=#EF7AFF>❤ VRC Toys ❤</color></size>", Styles.masterLabel);
            EditorGUILayout.Space();

            AssignVarialbles(properties);

            if(_HeaderTexture != null)
            {
                if( _HeaderTexture.textureValue == null || _HeaderTexture.textureValue.name != "vrbrationsheader"){
                    string[] guids = AssetDatabase.FindAssets("vrbrationsheader t:texture");
                    if(guids.Length > 0)
                    {
                        _HeaderTexture.textureValue = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
            }

            if ((materialEditor.target as Material).HasProperty("_Text0"))
            {
                GUILayout.Label("Name", EditorStyles.boldLabel);
                TextGUI(materialEditor.target as Material, "Encoded Name: ");
            }
            if (_pixelPosition != null && _depthcam != null)
            {
                GUILayout.Label("General", EditorStyles.boldLabel);
#if UDON
                _pixelPosition.vectorValue = (Vector2)EditorGUILayout.Vector2IntField(new GUIContent("Pixel Position", "Set the ouput position."), new Vector2Int((int)Mathf.Clamp(_pixelPosition.vectorValue.x, MAX_X-10, MAX_X), (int)Mathf.Clamp(_pixelPosition.vectorValue.y, 0, MAX_Y)));
#else
                _pixelPosition.vectorValue = (Vector2)EditorGUILayout.Vector2IntField(new GUIContent("Pixel Position", "Set the ouput position."), new Vector2Int((int)Mathf.Clamp(_pixelPosition.vectorValue.x, 1, MAX_X), (int)Mathf.Clamp(_pixelPosition.vectorValue.y, 0, MAX_Y)));
#endif

                materialEditor.TexturePropertySingleLine(new GUIContent(_depthcam.displayName), _depthcam);
            }
            if(_CameraFOV != null) materialEditor.ShaderProperty(_CameraFOV, _CameraFOV.displayName);

            if (_CheckPenetratorOrface != null)
            {
                GUILayout.Label("DPS Integration", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_CheckPenetratorOrface, new GUIContent(_CheckPenetratorOrface.displayName, "Use Penetrator-Oriface Distance when available"));
            }

            if (_OverideDepth != null && _OverideWidth != null)
            {
                GUILayout.Label("Overides", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_InShaderMultiplier, _InShaderMultiplier.displayName);
                GUIOveride(materialEditor, _OverideDepth);
                GUIOveride(materialEditor, _OverideWidth);
            }
            if(_InShaderAudioLinkMultiplier != null)
            {
                GUILayout.Label("Overides", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_InShaderAudioLinkMultiplier, _InShaderAudioLinkMultiplier.displayName);
            }

            EditorGUILayout.Space();
            GUILayout.Label("made by @Thryrallo", EditorStyles.miniLabel);
        }

        private void GUIOveride(MaterialEditor materialEditor, MaterialProperty property)
        {
            Vector4 vec = property.vectorValue;
            Rect r = EditorGUILayout.GetControlRect();
            Rect vecPos = new Rect(r);
            vecPos.x += 10;
            vecPos.width -= 10;
            vec.y = EditorGUI.FloatField(vecPos, "    " + property.displayName, vec.y);
            vec.x = EditorGUI.ToggleLeft(r, "", vec.x == 1) ? 1 : 0;
            property.vectorValue = vec;
        }

        private void AssignVarialbles(MaterialProperty[] properties)
        {
            foreach (MaterialProperty p in properties)
            {
                FieldInfo fieldInfo = typeof(VRCToysUI).GetField(p.name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(this, p);
                }
            }
        }

        const string sensorNameColorPropertiesStart = "_Text";
        public static void TextGUI(Material m, string label)
        {
            if (m == null)
            {
                EditorGUILayout.HelpBox("Material has been deleted.", MessageType.Error);
                return;
            }
            EditorGUI.BeginChangeCheck();
            string text = EditorGUILayout.TextField(label, GetSensorName(m));
            if (EditorGUI.EndChangeCheck())
            {
                SetSensorName(m, text);
            }
        }

        public static void SetSensorName(Material m, string name)
        {
            Color[] colors = TextToColors(name);
            int i = 0;
            while (m.HasProperty(sensorNameColorPropertiesStart + i))
            {
                if(i < colors.Length)
                {
                    m.SetColor(sensorNameColorPropertiesStart + i, colors[i]);
                }
                else
                {
                    m.SetColor(sensorNameColorPropertiesStart + i, Color.black);
                }
                i += 1;
            }
        }

        public static string GetSensorName(Material m)
        {
            if (m.HasProperty(sensorNameColorPropertiesStart+ "0"))
            {
                int i = 0;
                StringBuilder sb = new StringBuilder();
                while (m.HasProperty(sensorNameColorPropertiesStart + i))
                {
                    char[] c = ColorToChars(m.GetColor(sensorNameColorPropertiesStart + i));
                    if (c[0] != (char)0) sb.Append(c[0]);
                    if (c[1] != (char)0) sb.Append(c[1]);
                    i += 1;
                }
                return sb.ToString();
            }
            return "";
        }

        public static char[] ColorToChars(Color c)
        {
            int binaryOne = 0;
            int binaryTwo = 0;

            int r = (int)((c.r * 255 + 8) / 17);
            binaryOne += (r & 12) << 2;
            binaryTwo += (r & 3) << 4;

            binaryOne += (int)((c.g * 255 + 8) / 17);
            binaryTwo += (int)((c.b * 255 + 8) / 17);

            return new char[] { CompressedIntToChar(binaryOne), CompressedIntToChar(binaryTwo) };
        }

        public static Color[] TextToColors(string text)
        {
            Color[] colors = new Color[(text.Length+1) / 2];
            for(int i = 0; i < text.Length; i += 2)
            {
                int binaryOne = CharToCompressedInt(text[i]);
                int binaryTwo = 0;
                if (i + 1 < text.Length) binaryTwo = CharToCompressedInt(text[i + 1]);
                if (i == 0)
                {
                    Debug.Log(binaryOne + " , " + binaryTwo);
                    Debug.Log(((binaryOne & 15) / 15f) + " , " + ((binaryTwo & 15) / 15f));
                }
                colors[i / 2] = new Color( (((binaryOne >> 2) & 12) | ((binaryTwo >> 4) & 3)) / 15f , (binaryOne & 15) / 15f, (binaryTwo & 15) / 15f);
            }
            return colors;
        }

        private static int[] CharToBinary(char c)
        {
            string binaryString = System.Convert.ToString(CharToCompressedInt(c), 2).PadLeft(6, '0');
            int[] binary = new int[6];
            for (int j = 0; j < binary.Length; j++)
            {
                binary[j] = (binaryString[j] == '1') ? 1 : 0;
            }
            return binary;
        }

        public static int CharToCompressedInt(char c)
        {
            int i = (int)c;
            if (i == 0) return 0; // NULL 0
            if (i == 32) return 1; // Space 1
            if (i >= 48 && i <= 57) return i - 48 + 2; // numbers 2 - 11
            if (i >= 65 && i <= 90) return i - 65 + 12; // A-Z 12 - 37
            if (i >= 97 && i <= 122) return i - 97 + 38; // a-z 38 - 63
            return 0;
        }

        public static char CompressedIntToChar(int i)
        {
            if (i == 0) return (char)0;
            if (i == 1) return (char)32;
            if (i >= 2 && i <= 11) return (char)(i + 48 - 2);
            if (i >= 12 && i <= 37) return (char)(i + 65 - 12);
            if (i >= 38 && i <= 63) return (char)(i + 97 - 38);
            return (char)0;
        }
    }

    public class Styles
    {
        public static GUIStyle masterLabel { get; private set; } = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter };
        public static GUIStyle AreaStyleNoMargin = new GUIStyle(EditorStyles.textArea) { margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };
        public static GUIStyle RichText = new GUIStyle(GUI.skin.label) { richText = true };
        public static GUIStyle buttonSelected = new GUIStyle(GUI.skin.button) { };

        private static bool isInit = false;

        public static void Init()
        {
            if (isInit) return;
            buttonSelected.normal = buttonSelected.active;
            buttonSelected.normal.textColor = Color.cyan;
            isInit = true;
        }
    }
}