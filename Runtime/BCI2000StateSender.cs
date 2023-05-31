using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityBCI2000Runtime
{
    /*
     * This class registers simple GameObject States. It have to be loaded in the same scene as UnityBCI2000
     */
    public class BCI2000StateSender : MonoBehaviour
    {
        [SerializeField] private GameObject BCIObject;

        [SerializeField] private CustomVariableBase customVarsObject;

        [SerializeField] private bool GlobalCoords;
        [SerializeField] private bool GlobalX;
        [SerializeField] private int GXScale = 1000;
        [SerializeField] private bool GlobalY;
        [SerializeField] private int GYScale = 1000;
        [SerializeField] private bool GlobalZ;
        [SerializeField] private int GZScale = 1000;


        [SerializeField] private bool ScreenPosition;
        [SerializeField] private bool ScreenX;
        [SerializeField] private int SXScale = 1;
        [SerializeField] private bool ScreenY;
        [SerializeField] private int SYScale = 1;
        [SerializeField] private bool ScreenZ;
        [SerializeField] private int SZScale = 1;

        [SerializeField] private bool IsOnScreen;
        [SerializeField] private bool Speed = false;
        [SerializeField] private int VelScale = 1;

        private List<StateBase> variables = new List<StateBase>();

        private UnityBCI2000 bci;

        private Camera screenCamera;

        // Awake is called before Start
        void Awake()
        {
            bci = BCIObject.GetComponent<UnityBCI2000>();

            screenCamera = Camera.main;

            if (GlobalX)
            {
                AddSendState("GlobalX", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => gameObject.transform.position.x), GXScale);
            }
            if (GlobalY)
            {
                AddSendState("GlobalY", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => gameObject.transform.position.y), GYScale);
            }
            if (GlobalZ)
            {
                AddSendState("GlobalZ", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => gameObject.transform.position.z), GZScale);
            }
            if (ScreenX)
            {
                AddSendState("ScreenX", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => screenCamera.WorldToScreenPoint(gameObject.transform.position).x), SXScale);
            }
            if (ScreenY)
            {
                AddSendState("ScreenY", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => screenCamera.WorldToScreenPoint(gameObject.transform.position).y), SYScale);
            }
            if (ScreenZ)
            {
                AddSendState("ScreenZ", UnityBCI2000.StateType.SignedInt32, new Func<float>(() => screenCamera.WorldToScreenPoint(gameObject.transform.position).z), SZScale);
            }

            if (IsOnScreen)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                if (renderer != null)
                    AddSendState("Is on screen", UnityBCI2000.StateType.Boolean, new Func<float>(() => {
                        if (renderer.isVisible)
                            return 1;
                        else
                            return 0;
                    }), 1);
            }

            if (Speed)
            {
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody == null) //there is no rigidbody, so there must be a rigidbody2d
                {
                    Rigidbody2D rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
                    AddSendState("Speed", UnityBCI2000.StateType.UnsignedInt32, new Func<float>(() => rigidbody2D.velocity.magnitude), VelScale);
                }
                else
                {
                    AddSendState("Speed", UnityBCI2000.StateType.UnsignedInt32, new Func<float>(() => rigidbody.velocity.magnitude), VelScale);
                }
            }

            if (customVarsObject != null)
            {
                InitializeRuntime();
            }
        }

        // Update is called once per frame
        void Update()
        {
            foreach (StateBase state in variables)
            {
                state.Update();
            }
        }

        private void InitializeRuntime() //Custom variables cannot be serialized, so they must be reinitialized whenever assembly is reloaded, before the first frame
        {
            foreach (CustomVariableBase.CustomVariable customVar in customVarsObject.CustomVariables) //uses reflection so that there can be one central custom variable list, ths is only called before the scene loads, so overhead doesnt matter.
            {
                if (customVar is CustomVariableBase.CustomSendVariable)
                    AddCustomSendVariable(customVar.Name, (Func<float>)Delegate.CreateDelegate(customVar.DelegateType, customVar.Target, customVar.Method), customVar.Scale, customVar.Type);
                else if (customVar is CustomVariableBase.CustomGetVariable)
                    AddCustomGetVariable(customVar.Name, (Action<int>)Delegate.CreateDelegate(customVar.DelegateType, customVar.Target, customVar.Method));
            }
        }

        public void AddSendState(string name, UnityBCI2000.StateType type, Func<float> value, int scale)
        {
            UnityBCI2000.StateVariable state = bci.AddState(GetStateNameNoWS(name), type);
            int scale2 = scale;
            if (type == UnityBCI2000.StateType.Boolean) //scale must be 1 if the value is boolean
                scale2 = 1;
            variables.Add(new SendState(state, value, scale2));
        }

        public void AddSendExistingState(string name, Func<float> value, int scale)
        {
            UnityBCI2000.StateVariable state = bci.FindState(name);
            if (state == null)
            {
                Debug.Log("State " + name + " does not exist.");
                return;
            }
            variables.Add(new SendState(state, value, scale));
        }

        // Need to add a way to check for existing states on BCI2k operator, and add them to UnityBCI2000
        public void AddGetState(string name, Action<int> action)
        {
            if (bci.FindState(name) == null)
                variables.Add(new GetState(bci.AddState(name, UnityBCI2000.StateType.UnsignedInt16), action));
            else
                variables.Add(new GetState(bci.FindState(name), action));
        }

        public void AddCustomSendVariable(string name, Func<float> value, int scale, UnityBCI2000.StateType type)
        {
            if (bci.FindState(GetStateNameNoWS(name)) == null)
                AddSendState(name, type, value, scale);
            else
                AddSendExistingState(name, value, scale);
        }

        public void AddCustomGetVariable(string name, Action<int> action)
        {
            AddGetState(name, action);
        }

        private string GetStateNameNoWS(string stateName)
        {
            string objNameNoWS = new string(gameObject.name.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());//remove whitespace from names
            string nameNoWS = new string(stateName.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
            return objNameNoWS + nameNoWS;
        }

        private abstract class StateBase
        {
            public string Name { get; }
            public int Scale { get; }
            public UnityBCI2000.StateVariable state;

            public StateBase(UnityBCI2000.StateVariable inState, int scale)
            {
                Scale = scale;
                state = inState;
            }
            public abstract void Update();
        }

        private class SendState : StateBase //class which sends values to a StateVariable. Two of these can point to the same StateVariable, use AddSendExistingState()
        {

            private Func<float> StoredVar { get; }

            public SendState(UnityBCI2000.StateVariable inState, Func<float> storedVar, int scale) : base(inState, scale)
            {
                StoredVar = storedVar;
            }
            public override void Update()
            {
                state.Set((int)(StoredVar.Invoke() * Scale));
            }
        }

        private class GetState : StateBase
        {
            private Action<int> GetVar;

            public GetState(UnityBCI2000.StateVariable inState, Action<int> getVar) : base(inState, 1)
            {
                GetVar = getVar;
            }

            public override void Update()
            {
                int i = state.Get();
                GetVar.Invoke(i);
            }
            public int GetValue()
            {
                return state.Get();
            }
        }

    #if UNITY_EDITOR

        [CustomEditor(typeof(BCI2000StateSender))]
        public class StateSenderEditor : Editor
        {
            bool showCustomVars = false;
            
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                
                var sender = target as BCI2000StateSender;
                SerializedProperty serializedProperty;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("BCIObject"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customVarsObject"));

                //Global coordinate toggles and scales
                EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("GlobalCoords"));
                if (serializedProperty.boolValue)
                {
                    GUI.BeginGroup(new Rect(20,0,200,10000));
                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("GlobalX"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("GYScale"));

                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("GlobalY"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("GZScale"));

                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("GlobalZ"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("GXScale"));
                    GUI.EndGroup();
                }
                //Screen coordinate toggles and formats
                EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("ScreenPosition"));
                if (serializedProperty.boolValue)
                {
                    GUI.BeginGroup(new Rect(20,0,200,10000));
                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("ScreenX"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("SXScale"));

                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("ScreenY"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("SYScale"));

                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("ScreenZ"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("SZScale"));
                    GUI.EndGroup();
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsOnScreen"));


                //check for rigidbody before showing speed toggle
                if (sender.gameObject.GetComponent<Rigidbody>() != null || sender.gameObject.GetComponent<Rigidbody2D>() != null)
                {
                    EditorGUILayout.PropertyField(serializedProperty=serializedObject.FindProperty("Speed"));
                    if (serializedProperty.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("VelScale"));
                }

                if (showCustomVars = EditorGUILayout.Foldout(showCustomVars, "Custom Vars", EditorStyles.foldoutHeader))
                {
                    GUI.BeginGroup(new Rect(20,0,420,10000));

                    List<string> customSendVariables = new List<string>();
                    List<string> customGetVariables = new List<string>();
                    CustomVariableBase customVariables = sender.customVarsObject;
                    if (customVariables != null)
                    {
                        foreach (CustomVariableBase.CustomVariable customVar in customVariables.CustomVariables)
                        {
                            if (customVar is CustomVariableBase.CustomSendVariable)
                                customSendVariables.Add(customVar.Name);
                            else
                                customGetVariables.Add(customVar.Name);
                        }
                    }
                    if (customSendVariables.Count > 0)
                    {
                        EditorGUILayout.LabelField("Custom Send Variables", EditorStyles.boldLabel);

                        GUI.BeginGroup(new Rect(20,0,420,10000));
                        foreach (string name in customSendVariables)
                        {
                            EditorGUILayout.LabelField(name);
                        }
                        GUI.EndGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No custom Send Variables", EditorStyles.boldLabel);
                    }

                    if (customGetVariables.Count > 0)
                    {
                        EditorGUILayout.LabelField("Custom Get Variables", EditorStyles.boldLabel);
                        
                        GUI.BeginGroup(new Rect(20,0,420,10000));
                        foreach (string name in customGetVariables)
                        {
                            EditorGUILayout.LabelField(name);
                        }
                        GUI.EndGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No custom Get Variables", EditorStyles.boldLabel);
                    }
                    GUI.EndGroup();
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

    #endif

    }
}
