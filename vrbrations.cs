using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Thry.VRBrations
{
    public class vrbrations : MonoBehaviour
    {
        public bool editSensors = false;
        public List<GameObject> foundSensorsObjects; //set when looking for sensors to check if has vrbrations

        public void Destroy()
        {
            DestroyImmediate(this);
        }
    }
}