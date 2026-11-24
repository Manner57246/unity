using System;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeApp
    {
        void ApplyAutomationArguments()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] args = Environment.GetCommandLineArgs();
            string role = Array.Find(args, a => a.StartsWith("--dodge-smoke="));
            if (role != null) gameObject.AddComponent<DodgeMvpSmoke>().Initialize(role.Substring("--dodge-smoke=".Length), args);
#endif
        }
    }
}
