using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UnityBCI2000Runtime
{
    [ExecuteInEditMode]
    public abstract class CustomVariableBase : MonoBehaviour //base script for handling custom variables
    {
        public List<CustomVariable> customVariables = null;

        protected abstract void AddCustomVariables(); //override with method that adds custom variables to customVariables
        
        void OnEnable() // for editor initialisation (awake only called once)
        {
            InitCustomVariables();
        }
        
        void Awake()
        {
            InitCustomVariables();
        }
        
        void InitCustomVariables()
        {
            if (customVariables == null)
            {
                customVariables = new List<CustomVariable>();
                AddCustomVariables();
            }
        }

        public abstract class CustomVariable
        {
            public string Name;
            public UnityBCI2000.StateType Type;
            public int Scale;
            public Type DelegateType;
            public Object Target;
            public MethodInfo Method;


            public CustomVariable(string name, UnityBCI2000.StateType type, int scale)
            {
                Name = name;
                Type = type;
                Scale = scale;
            }
        }

        public class CustomSendVariable : CustomVariable
        {
            public CustomSendVariable(string name, Func<float> value, int scale, UnityBCI2000.StateType type) : base(name, type, scale)
            {
                DelegateType = typeof(Func<float>);
                Target = value.Target;
                Method = value.Method;
            }
        }

        public class CustomGetVariable : CustomVariable
        {
            public CustomGetVariable(string name, Action<int> action) : base(name, UnityBCI2000.StateType.UnsignedInt32, 1)
            {
                DelegateType = typeof(Action<int>);
                Target = action.Target;
                Method = action.Method;
            }
        }
    }
}
