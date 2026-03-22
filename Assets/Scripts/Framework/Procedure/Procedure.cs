using UnityEngine;

namespace Top
{
    public class ProcedureBase : MonoBehaviour
    {
        public GlobalManager GM => GlobalManager.Instance;
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}