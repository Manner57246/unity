using Rouge.Core;
using UnityEngine;

namespace Rouge.Unity
{
    [CreateAssetMenu(menuName = "Rouge/Run Definition")]
    public sealed class RunDefinition : ScriptableObject
    {
        public RunRules rules = new RunRules();
    }
}
