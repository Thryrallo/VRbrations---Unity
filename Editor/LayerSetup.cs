using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class LayerSetup
    {
        [MenuItem("VRChat SDK/Setup Layers")]
        public static void SetupLayers()
        {
#if VRC_SDK_VRCSDK3 || VRC_SDK_VRCSDK2
        UpdateLayers.SetupEditorLayers();
#endif
        }

    }
}