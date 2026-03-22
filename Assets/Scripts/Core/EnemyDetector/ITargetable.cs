using UnityEngine;

namespace RTS.TargetSearch
{
    public interface ITargetable
    {
        int Id { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        int TeamId { get; }
        // GameObject Owner { get; }
    }
}