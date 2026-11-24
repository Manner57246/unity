using UnityEngine;

namespace SuperDodgeball
{
    // Only these colliders count as a body hit. The movement capsule never does.
    public sealed class DodgeHitbox : MonoBehaviour
    {
        public DodgeActor actor;
    }
}
