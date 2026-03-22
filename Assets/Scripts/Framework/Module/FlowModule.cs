using UnityEngine;

namespace Top
{
    public class ModuleBase : MonoBehaviour
    {
        public GlobalManager GM => GlobalManager.Instance;
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}