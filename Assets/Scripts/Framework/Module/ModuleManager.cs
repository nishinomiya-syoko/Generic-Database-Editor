using UnityEngine;
using System.Collections.Generic;

namespace Top
{
    public class ModuleManager : MonoBehaviour
    {
        private readonly LinkedList<ModuleBase> modules = new LinkedList<ModuleBase>();
        public void EnterModule(ModuleBase module)
        {
            modules.AddLast(module);
            module.OnEnter();
        }
        private void Update()
        {
            LinkedListNode<ModuleBase> node = modules.First;
            while (node != null)
            {
                LinkedListNode<ModuleBase> next = node.Next;
                node.Value.OnUpdate();
                node = next;
            }
        }
        public void ExitModule(ModuleBase module)
        {
            module.OnExit();
            modules.Remove(module);
        }

    }
}