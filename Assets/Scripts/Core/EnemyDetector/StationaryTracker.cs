using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    /// <summary>
    /// 追踪目标位置 这个组件用于 **异步低频更新单位所在网格**。
    ///比每帧 `UpdateEntityCell()` 更平滑。
    /// </summary>
    [DisallowMultipleComponent]
    public class StationaryTracker : MonoBehaviour
    {
        private TargetEntity targetEntity;
        private Vector3 lastPosition;

        private void Awake()
        {
            targetEntity = GetComponent<TargetEntity>();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            TargetSearchSystem.UpdateEntityCell(targetEntity);
        }
    }
}