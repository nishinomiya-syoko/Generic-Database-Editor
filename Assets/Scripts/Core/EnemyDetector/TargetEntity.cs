using UnityEngine;

namespace RTS.TargetSearch
{
    public class TargetEntity : MonoBehaviour, ITargetable
    {
        [SerializeField] private int id;
        [SerializeField] private int teamId;
        [SerializeField] private bool isAlive = true;

        public int Id => id;
        public Vector3 Position => transform.position;
        public bool IsAlive => isAlive;
        public int TeamId => teamId;
        public GameObject Owner => gameObject;

        public void SetAlive(bool value)
        {
            isAlive = value;
        }

        private void OnEnable()
        {
            TargetSearchSystem.Register(this);
        }

        private void OnDisable()
        {
            TargetSearchSystem.Unregister(this);
        }
    }
}