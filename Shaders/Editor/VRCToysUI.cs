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

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.LabelField("<size=16><color=#EF7AFF>❤ VRC Toys ❤</color></size>", Styles.masterLabel);
            EditorGUILayout.Space();

            AssignVarialbles(properties);

            if ((materialEditor.target as Material).HasProperty("_Text0"))
            {
                GUILayout.Label("Name", EditorStyles.boldLabel);
                TextGUI("_Text", materialEditor.target as Material, "Encoded Name: ");
            }
            if (_pixelPosition != null && _depthcam != null)
            {
                GUILayout.Label("General", EditorStyles.boldLabel);
                _pixelPosition.vectorValue = (Vector2)EditorGUILayout.Vector2IntField(new GUIContent("Pixel Position", "Set the ouput position."), new Vector2Int((int)Mathf.Clamp(_pixelPosition.vectorValue.x, 0, 10), (int)Mathf.Clamp(_pixelPosition.vectorValue.y, 0, 10)));

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

        public static void TextGUI(string propertyNames, Material m, string label)
        {
            int i = 0;
            List<Color> colorsList = new List<Color>();
            while (m.HasProperty(propertyNames + i))
            {
                colorsList.Add(m.GetColor(propertyNames + i));
                i++;
            }
            Color[] colors = colorsList.ToArray();
            EditorGUI.BeginChangeCheck();
            string text = EditorGUILayout.TextField(label, ColorsToText(colors));
            if (EditorGUI.EndChangeCheck())
            {
                colors = TextToColors(text, colors.Length);
                for (int j = 0; j < colors.Length; j++) m.SetColor(propertyNames + j, colors[j]);
            }
        }

        public static string ColorsToText(Color[] colors)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < colors.Length / 2; i++)
            {
                string binaryString = "" + ((colors[i * 2].r == 1)? "1":"0") + ((colors[i * 2].g == 1) ? "1" : "0") + ((colors[i * 2].b == 1) ? "1" : "0") +
                    ((colors[i*2 + 1].r == 1) ? "1" : "0") +((colors[i * 2 + 1].g == 1) ? "1" : "0") +((colors[i * 2 + 1].b == 1) ? "1" : "0");
                int asInt = Convert.ToInt32(binaryString, 2);
                sb.Append(CompressedIntToChar(asInt));
            }
            return sb.ToString();
        }

        public static Color[] TextToColors(string text, int length)
        {
            Color[] colors = new Color[length];
            for(int i = 0; i < text.Length && i < length / 2; i++)
            {
                string binaryString = System.Convert.ToString(CharToCompressedInt(text[i]), 2).PadLeft(6, '0');
                int[] binary = new int[6];
                for(int j = 0; j < binary.Length; j++)
                {
                    binary[j] = (binaryString[j] == '1') ? 1 : 0;
                }
                colors[i * 2 + 0] = new Color(binary[0], binary[1], binary[2]);
                colors[i * 2 + 1] = new Color(binary[3], binary[4], binary[5]);
            }
            return colors;
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