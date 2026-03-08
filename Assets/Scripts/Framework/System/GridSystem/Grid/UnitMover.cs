using UnityEngine;
using System.Collections.Generic;

namespace Debugger
{
    /// <summary>
    /// 简单单位移动控制器：如果你使用 FlowField，请先调用 FlowFieldPathfinder.BuildFlowField(target)
    /// 单位会读取当前格子的 bestDirection 并朝该方向移动。
    /// 当目标改变或建筑更新时，记得重新 BuildFlowField 或使用 A* 单独请求路径。
    /// </summary>
    public class UnitMover : MonoBehaviour
    {
        public enum MoveMode
        {
            FlowField,
            AStar,
            BStar,
            BFS,
            JPS
        }
        public float speed = 3f;
        // public bool useFlowField = true; // 启用 Flow Field（推荐大量单位）
        public MoveMode moveMode = MoveMode.FlowField;
        public Vector3 manualTarget; // 如果 useFlowField == false，可用 A* 去查询路径

        List<Vector3> routePath = null;
        int routeIndex = 0;


        void Update()
        {
            var gm = GridManager.Instance;
            if (gm == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                bool isCollider = Physics.Raycast(ray, out hit);
                manualTarget = hit.point;
                GetPath();

            }
            MoveAlong();
            // if (useFlowField)
            // {
            //     var node = gm.GetNodeFromWorldPos(transform.position);
            //     if (node != null && node.bestDirection != Vector2.zero)
            //     {
            //         Vector3 dir = new Vector3(node.bestDirection.x, 0f, node.bestDirection.y);
            //         // 简单避免碰撞：稍微随机偏移或绕行逻辑可加入
            //         MoveAlong(dir);
            //     }
            //     else
            //     {
            //         // 如果没有direction，可能已到达目标或没有可达路径
            //         // 可尝试停下或切换 A* 搜索
            //     }
            // }
            // else
            // {
            //     // 简单 A* 路径跟随

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
                        case MoveMode.JPS:
                        routePath = JPSPathfinder.FindPath(transform.position, manualTarget, true);
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

                // transform.rotation = Quaternion.LookRotation(target);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), Time.deltaTime * 10f);
                transform.position += delta.normalized * speed * Time.deltaTime;
            }
            // else
            //     routeIndex++;
        }
        void OnDrawGizmos()
        {
            if (routePath == null)
                return;
            for (int i = routeIndex; i < routePath.Count; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(routePath[i], Vector3.one * 0.3f);

                if (i == routeIndex)
                {
                    Gizmos.DrawLine(transform.position, routePath[i]);
                }
                else
                {
                    Gizmos.DrawLine(routePath[i - 1], routePath[i]);
                }
            }
        }
    }
}