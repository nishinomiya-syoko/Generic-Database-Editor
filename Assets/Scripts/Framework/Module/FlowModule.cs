using UnityEngine;
namespace Top
{
    public class ModuleBase : MonoBehaviour
    {
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}