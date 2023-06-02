using System;
using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] private Parameter[] Parameters;    // only for Unity inspector sake

        private Dictionary<string, Parameter> ParametersDict = new Dictionary<string, Parameter>();   // real used data container

        private UnityBCI2000 bci;

        public float Get(string parameterName)
        {
            Parameter ret;
            if (ParametersDict.TryGetValue(parameterName, out ret))
                return ret.Value;
            else
                throw new Exception("This parameter does not exist.");
        }
        
        public void Set(string parameterName, float value)
        {
            ParametersDict[parameterName] = new Parameter(ParametersDict[parameterName], value);
        }

        // dump all parameters to Operator
        public void Save()
        {
            foreach (var pair in ParametersDict)
            {
                Parameter param = pair.Value;
                bci.Execute("SET PARAMETER " + param.Name + " " + param.Value);
            }
        }

        public void Save(string parameterName)
        {
            Parameter param = ParametersDict[parameterName];
            bci.Execute("SET PARAMETER " + param.Name + " " + param.Value);
        }

        // load from to Operator
        public void Load()
        {
            foreach (var name in ParametersDict.Keys)
            {
                Load(name);
            }
        }

        public void Load(string parameterName)
        {
            Parameter param = ParametersDict[parameterName];
            string strValue;
            bci.Execute("GET PARAMETER " + name, out strValue);
            Debug.Log(strValue);
            ParametersDict[name] = new Parameter(ParametersDict[name], float.Parse(strValue, System.Globalization.CultureInfo.InvariantCulture));
        }

        // Awake is called before Start
        void Awake()
        {
            foreach(var param in Parameters)
            {
                ParametersDict.Add(param.Name, param);
            }

            bci = BCIObject.GetComponent<UnityBCI2000>();
            bci.initParameters = GetExecutableAddParameters();
        }

        private string[] GetExecutableAddParameters() {
            string [] ret = new string [ParametersDict.Count];
            int i = 0;
            foreach (var pair in ParametersDict)
            {
                Parameter p = pair.Value;

                ret [i] = "ADD PARAMETER " + ConfigSectionName;
                if (p.SubSection != "")
                    ret [i] += ":" + p.SubSection;
                ret [i] += " float " + p.Name + "= " + p.Value + " " + p.Value + " " + p.LowRange + " " + p.HighRange;

                ++i;
            }

            return ret;
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

            public Parameter(Parameter copy) :
            this(copy.SubSection, copy.Name, copy.Value, copy.LowRange, copy.HighRange) {}

            public Parameter(Parameter copy, float value) :
            this(copy.SubSection, copy.Name, value, copy.LowRange, copy.HighRange) {}

            public string Name;
            public string SubSection;
            public float Value;
            public float LowRange;
            public float HighRange;
        }

    }
}
