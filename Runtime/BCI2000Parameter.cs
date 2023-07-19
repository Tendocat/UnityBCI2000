using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityBCI2000Runtime
{
     /// <summary>
     ///  This class registers BCI2000 parameters. It have to be loaded in the same scene as UnityBCI2000.
     ///  Only float parameters are supported in Unity editor for code simplification.
     ///  int and string are only available through functions for a better BCI2000 built in parameters ease of access.
     /// </summary>
    public class BCI2000Parameter : MonoBehaviour
    {
        [SerializeField] private GameObject BCIObject;
        
        [SerializeField] private string ConfigSectionName = "Unity";    // name of the tab in BCI2000

        [SerializeField] private Parameter[] Parameters;    // only for Unity inspector and foreach sake

        private Dictionary<string, Parameter> ParametersDict = new Dictionary<string, Parameter>();   // optimized data container

        private UnityBCI2000 bci;
        
        private static readonly char[] trimChars =  new char[] { '\r', '\n', ' ', '>' };

        // returns cached parameter value, use Load to sync with BCI2000
        public float Get(string parameterName)
        {
            Parameter ret;
            if (ParametersDict.TryGetValue(parameterName, out ret))
                return ret.Value;
            else
                throw new Exception("This parameter does not exist. (" + parameterName + ")");
        }

        // set cached parameter value, use Save to sync with BCI2000
        public void Set(string parameterName, float value)
        {
            ParametersDict[parameterName] = new Parameter(ParametersDict[parameterName], value);
        }

        // sync all parameters to Operator
        public void Save()
        {
            foreach (var param in Parameters)
            {
                bci.SetParameter(param.Name, param.Value.ToString());
            }
        }

        // sync specified parameter to Operator
        public void Save(string parameterName)
        {
            Parameter param = ParametersDict[parameterName];
            bci.SetParameter(parameterName, param.Value.ToString());
        }

        // load all parameters from Operator
        public void Load()
        {
            bci.readyCallback -= Load;
            foreach (var param in Parameters)
            {
                Load(param.Name);
            }
        }

        // load specified parameters from Operator, value type should be float
        public void Load(string parameterName)
        {
            Parameter param = ParametersDict[parameterName];
            string strValue;
            float value = 0;
            bci.GetParameter(parameterName, out strValue);
            try
            {
                value = float.Parse(strValue, System.Globalization.CultureInfo.InvariantCulture);
                ParametersDict[parameterName] = new Parameter(ParametersDict[parameterName], value);
            }
            catch(Exception e)
            {
                Debug.LogError(" String for the parameter <" + parameterName + "> was: \"" + strValue + "\"\n" + e);
            }
        }

        // returns string value of existing BCI2000 parameter
        public string GetString(string parameterName)
        {
            string strValue;
            bci.GetParameter(parameterName, out strValue);
            return strValue;
        }

        // modify string value of existing BCI2000 parameter
        public void SetString(string parameterName, string strValue)
        {
            bci.SetParameter(parameterName, strValue);
        }
        
        // returns int value of existing BCI2000 parameter
        public int GetInt(string parameterName)
        {
            string strValue;
            bci.GetParameter(parameterName, out strValue);
            return int.Parse(strValue, System.Globalization.CultureInfo.InvariantCulture);
        }

        // modify int value of existing BCI2000 parameter
        public void SetInt(string parameterName, int strValue)
        {
            bci.SetParameter(parameterName, strValue.ToString());
        }

        // Awake is called before Start
        private void Awake()
        {
            foreach(var param in Parameters)
            {
                ParametersDict.Add(param.Name, param);
            }

            bci = BCIObject.GetComponent<UnityBCI2000>();
            bci.initParameters = GetExecutableAddParameters();
            bci.readyCallback += Load;
        }

        // used to set BCI2000 initialization of parameters
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

        // structure of a BCI2000 float parameter
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
