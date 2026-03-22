using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    public interface IBatchedSeeker
    {
        void PerformSearch();
        bool IsValid { get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class BatchedSearchManager : MonoBehaviour
    {
        // public static BatchedSearchManager Instance { get; private set; }

        [SerializeField] private int batchSizePerFrame = 20;
        [SerializeField] private int loopIntervalMs = 50;

        private readonly List<IBatchedSeeker> seekers = new List<IBatchedSeeker>();
        private CancellationTokenSource cts;
        private int currentIndex;

        

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            LoopAsync(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void Register(IBatchedSeeker seeker)
        {
            if (seeker == null || seekers.Contains(seeker))
                return;

            seekers.Add(seeker);
        }

        public void Unregister(IBatchedSeeker seeker)
        {
            seekers.Remove(seeker);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTaskVoid LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (seekers.Count == 0)
                {
                    await UniTask.Delay(loopIntervalMs, cancellationToken: token);
                    continue;
                }

                int processed = 0;
                int safeCount = seekers.Count;

                while (processed < batchSizePerFrame && safeCount > 0 && seekers.Count > 0)
                {
                    if (currentIndex >= seekers.Count)
                        currentIndex = 0;

                    var seeker = seekers[currentIndex];

                    if (seeker == null || !seeker.IsValid)
                    {
                        seekers.RemoveAt(currentIndex);
                        continue;
                    }

                    seeker.PerformSearch();
                    currentIndex++;
                    processed++;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}