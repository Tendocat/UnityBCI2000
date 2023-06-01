using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityBCI2000Runtime
{
    /*
     * This class registers BCI2000 parameters. It have to be loaded in the same scene as UnityBCI2000.
     * Only float parameters are supported. int and string are not done for simplification.
     */
    public class BCI2000Parameter : MonoBehaviour
    {
        [SerializeField] private GameObject BCIObject;
        
        [SerializeField] private string ConfigSectionName = "Unity";

        [SerializeField] private Parameter[] Parameters;

        private UnityBCI2000 bci;

        private string[] ExecutableParameters {
            get {
                string [] ret = new string [Parameters.Length];

                for (int i=0; i< ret.Length; ++i)
                {
                    Parameter p = Parameters[i];

                    ret [i] = "ADD PARAMETER " + ConfigSectionName;
                    if (p.SubSection != "")
                        ret [i] += ":" + p.SubSection;
                    ret [i] += " float " + p.Name + "= " + p.Value + " " + p.Value + " " + p.LowRange + " " + p.HighRange;
                }

                return ret;
            }
        } 

        // Awake is called before Start
        void Awake()
        {
            bci = BCIObject.GetComponent<UnityBCI2000>();
            bci.initParameters = ExecutableParameters;
        }

        [System.Serializable]
        private struct Parameter
        {
            public Parameter(string subSection, string name, float value, float lowRange, float highRange)
            {
                Name = name;
                SubSection = subSection;
                Value = value;
                LowRange = lowRange;
                HighRange = highRange;
            }

            public string Name;
            public string SubSection;
            public float Value;
            public float LowRange;
            public float HighRange;
        }

    }
}
