using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using BCI2000RemoteNET;
using UnityEngine;

namespace UnityBCI2000Runtime
{
    public class UnityBCI2000 : MonoBehaviour
    {
        private BCI2000Remote bci = new BCI2000Remote();
        public string OperatorPath;
        public string ParametersPath;
        public string TelnetIp;
        public int TelnetPort;
        public bool DontStartModules;
        public string Module1 = "SignalGenerator";
        public string[] Module1Args;
        public string Module2 = "DummySignalProcessing";
        public string[] Module2Args;
        public string Module3 = "DummyApplication";
        public string[] Module3Args;
        public string[] initCommands;
        private Dictionary<string, List<string>> modules;
        public string LogFile;
        public bool LogStates;
        public bool LogPrompts;

        private List<StateVariable> states = new List<StateVariable>();

        public enum StateType //copy this to any object which sends states in Start(), don't want to be copying this every frame
        {
            UnsignedInt32,
            SignedInt32,
            UnsignedInt16,
            SignedInt16,
            Boolean
        }

        public StateVariable FindState(string name)
        {
            return states.Find(x => x.Name == name);
        }

        public StateVariable AddState(string name, StateType type) //can only be called in Start()
        {
            if (states.Find(x => x.Name == name) != null)
            {
                Debug.Log("State " + name + " already exists");
                return null;
            }
            StateVariable newState = new StateVariable(name, type, bci);
            states.Add(newState);
            return (newState);
        }

        void Awake()
        {                
            bci.WindowVisible = 1;
            if (File.Exists(OperatorPath))
                bci.OperatorPath = OperatorPath;
            else    // Search in StreamingAsset folder
            {
                string [] files = Directory.GetFiles(Application.streamingAssetsPath, "*Operator.exe", SearchOption.AllDirectories);
                if (files.Length == 1)
                    bci.OperatorPath = files[0];
                else
                    throw new Exception("Operator not found, only one file named Operator.exe should exist in StreamingAsset folder");
            }

            if (!String.IsNullOrWhiteSpace(TelnetIp))
                bci.TelnetIp = TelnetIp;
            if (TelnetPort != 0)
                bci.TelnetPort = TelnetPort;
            if (!String.IsNullOrWhiteSpace(LogFile))
                bci.LogFile = LogFile;
            bci.LogStates = LogStates;
            bci.LogPrompts = LogPrompts;

            bci.Connect(initCommands);

            List<string> module1ArgsList;
            if (Module1Args.Length == 0)
                module1ArgsList = null;
            else
                module1ArgsList = Module1Args.ToList();
            List<string> module2ArgsList;
            if (Module2Args.Length == 0)
                module2ArgsList = null;
            else
                module2ArgsList = Module2Args.ToList();
            List<string> module3ArgsList;
            if (Module3Args.Length == 0)
                module3ArgsList = null;
            else
                module3ArgsList = Module3Args.ToList();

            if (!DontStartModules)
            {
                modules = new Dictionary<string, List<string>>()
                {
                {Module1, module1ArgsList },
                {Module2, module2ArgsList },
                {Module3, module3ArgsList }
                };
                bci.StartupModules(modules);
            }
            
            bci.LoadParametersRemote(ParametersPath);
            foreach (StateVariable state in states) //Add all states to BCI2000. these can't be added before or after BCI2000 starts, and must be added here.
            {
                switch (state.Type)
                {
                    case StateType.Boolean:
                        bci.AddStateVariable(state.Name, 1, 0);
                        break;
                    case StateType.UnsignedInt32:
                        bci.AddStateVariable(state.Name, 32, 0);
                        break;
                    case StateType.SignedInt32:
                        bci.AddStateVariable(state.Name, 32, 0);
                        bci.AddStateVariable(state.Name + "Sign", 1, 0);
                        break;
                    case StateType.UnsignedInt16:
                        bci.AddStateVariable(state.Name, 16, 0);
                        break;
                    case StateType.SignedInt16:
                        bci.AddStateVariable(state.Name, 16, 0);
                        bci.AddStateVariable(state.Name + "Sign", 1, 0);
                        break;
                }
            }

            bci.SetConfig();
            bci.Start();
        }

        private void OnDestroy()    // also called when OnApplicationQuit
        {
            bci = null;
            bci.Stop();
        }

        public bool Execute(string command)
        {
            return Execute(command, out _);
        }

        public bool Execute(string command, out string response)
        {
            bool ret = bci.Execute(command);
            response = bci.Received;
            return ret;
        }

        public class StateVariable
        {
            public string Name { get; }
            public StateType Type { get; }
            private readonly BCI2000Remote bci;

            private int lastSentValue;
            public StateVariable(string name, StateType type, BCI2000Remote inBci)
            {
                Name = name;
                bci = inBci;
                Type = type;

            }

            public void Set(int value)
            {
                if (value != lastSentValue) //check if the new value is different than the last sent value, to avoid unneccessary calls to bci2k
                {
                    lastSentValue = value;
                    switch (Type)
                    {
                        case StateType.Boolean:
                            if (value == 0)
                                bci.SetStateVariable(Name, 0);
                            else
                                bci.SetStateVariable(Name, 1);
                            break;
                        case StateType.SignedInt16:
                        case StateType.SignedInt32:
                            bci.SetStateVariable(Name, Mathf.Abs(value));
                            if (value < 0)
                                bci.SetStateVariable(Name + "Sign", 1);
                            else
                                bci.SetStateVariable(Name + "Sign", 0);
                            break;
                        case StateType.UnsignedInt16:
                        case StateType.UnsignedInt32:
                            bci.SetStateVariable(Name, value);
                            break;
                    }
                }
            }
            public int Get()
            {
                double value = 0;
                bci.GetStateVariable(Name, ref value);
                return (int)value;
            }
        }
    }
}
