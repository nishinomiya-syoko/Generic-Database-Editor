using UnityEngine;
using System.Collections.Generic;

namespace Top
{
    /// <summary>
    /// 简单单位移动控制器：如果你使用 FlowField，请先调用 FlowFieldPathfinder.BuildFlowField(target)
    /// 单位会读取当前格子的 bestDirection 并朝该方向移动。
    /// 当目标改变或建筑更新时，记得重新 BuildFlowField 或使用 A* 单独请求路径。
    /// </summary>
    public partial class UnitAI : MonoBehaviour
    {
        public enum MoveMode
        {
            FlowField,
            AStar,
            BStar,
            BFS
        }
        public float speed = 3f;
        public float stoppingDistance = 0.1f;
        //
        public float remainingDistance = 0f;
        // public bool useFlowField = true; // 启用 Flow Field（推荐大量单位）
        public MoveMode moveMode = MoveMode.FlowField;
        public Vector3 manualTarget; // 如果 useFlowField == false，可用 A* 去查询路径

        List<Vector3> routePath = null;
        int routeIndex = 0;


        void Update()
        {
            var gm = GridManager.Instance;
            if (gm == null) return;

            MoveAlong();
        }
        public void SetDestination(Vector3 target)
        {
            manualTarget = target;
        }
        public void ResetPath()
        {
            routePath = null;
            routeIndex = 0;
            GetPath();
        }
        void GetPath()
        {
            routeIndex = 0;
            var a = GridManager.Instance.GetNodeFromWorldPos(transform.position);
            var fromPos = new Vector2Int(a.x, a.y);
            var b = GridManager.Instance.GetNodeFromWorldPos(manualTarget);
            var toPos = new Vector2Int(b.x, b.y);
            routePath = PathPool.GetPath(fromPos, toPos);


            if (routePath == null)
            {
                switch (moveMode)
                {
                    case MoveMode.FlowField:
                        // FlowFieldJobRunner.BuildFlowFieldWithJob(manualTarget, GridManager.Instance, true);
                        routePath = FlowFieldPathfinder.GetPath(transform.position, manualTarget, 100);
                        break;
                    case MoveMode.AStar:
                        routePath = AStarPathfinder.FindPath(transform.position, manualTarget, true);
                        break;
                    case MoveMode.BStar:
                        routePath = BStarPathfinder.FindPath(transform.position, manualTarget, true);
                        break;
                    case MoveMode.BFS:
                        routePath = BFSPathfinder.FindPath(transform.position, manualTarget, true);
                        break;
                }
                if (moveMode != MoveMode.FlowField)
                    PathPool.AddPath(fromPos, toPos, routePath);
            }
            else
                for (int i = 0; i < routePath.Count; i++)
                {
                    if (Vector3.Distance(routePath[i], transform.position) < Vector3.Distance(routePath[i], manualTarget))
                        routePath.RemoveAt(i);
                    else
                        break;
                }
        }

        void MoveAlong()
        {
            if (routePath != null && routeIndex < routePath.Count)
            {
                Vector3 target = routePath[routeIndex];
                Vector3 delta = target - transform.position;
                delta.y = 0;

                if (delta.magnitude < .5f) routeIndex++;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), Time.deltaTime * 10f);
                transform.position += delta.normalized * speed * Time.deltaTime;

                remainingDistance = Vector3.Distance(transform.position, target);
            }
        }
    }
}