using UnityEngine;

namespace Top
{
    public class ProcedureManager: MonoBehaviour
    {
        public ProcedureBase currentProcedure;
        public void EnterProcedure(ProcedureBase procedure)
        {
            if (currentProcedure != null)
            {
                currentProcedure.OnExit();
            }
            currentProcedure = procedure;
            currentProcedure.OnEnter();
        }
        public void ExitProcedure()
        {
            currentProcedure.OnExit();
            currentProcedure = null;
        }
        public void Update()
        {
            if (currentProcedure != null)
            {
                currentProcedure.OnUpdate();
            }
        }
    }
}