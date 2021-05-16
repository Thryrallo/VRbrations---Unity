using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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