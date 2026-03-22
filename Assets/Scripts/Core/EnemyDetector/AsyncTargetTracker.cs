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
    public class AsyncTargetTracker : MonoBehaviour
    {
        [SerializeField] private int updateIntervalMs = 100;

        private TargetEntity targetEntity;
        private CancellationTokenSource cts;
        private Vector3 lastPosition;

        private void Awake()
        {
            targetEntity = GetComponent<TargetEntity>();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            TrackLoopAsync(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
        /// <summary>
        /// 追踪位置
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTaskVoid TrackLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Vector3 current = transform.position;

                if ((current - lastPosition).sqrMagnitude > 0.01f)
                {
                    lastPosition = current;
                    if (targetEntity != null)
                    {
                        TargetSearchSystem.UpdateEntityCell(targetEntity);
                    }
                }

                await UniTask.Delay(updateIntervalMs, cancellationToken: token);
            }
        }
    }
}