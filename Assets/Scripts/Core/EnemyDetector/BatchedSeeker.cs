using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.TargetSearch
{   
    /// <summary>
    /// 批量搜索
    /// </summary>
    public class BatchedSeeker : MonoBehaviour, IBatchedSeeker
    {
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private bool enemyOnly = true;
        [SerializeField] private int selfTeamId = 0;

        private readonly List<ITargetable> results = new List<ITargetable>(32);

        public bool IsValid => this != null && gameObject.activeInHierarchy;

        public Action<List<ITargetable>> OnSearchCompleted;

        private void OnEnable()
        {
            GlobalManager.Instance.BatchedSearchManager?.Register(this);
        }

        private void OnDisable()
        {
            GlobalManager.Instance.BatchedSearchManager?.Unregister(this);
        }

        public void PerformSearch()
        {
            int? excludeTeamId = enemyOnly ? selfTeamId : null;

            TargetSearchSystem.SearchInRangeNonAlloc(
                transform.position,
                searchRadius,
                results,
                excludeTeamId);

            OnSearchCompleted?.Invoke(results);
        }
    }
}