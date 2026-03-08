using UnityEngine;

/// <summary>
/// 高响应RTS相机控制器
/// 支持：WASD移动/边缘滚动/右键拖拽、指针中心缩放、动态边界限制、Shift加速
/// 挂载要求：需附加Camera组件（通过RequireComponent强制）
/// 输入依赖：Legacy Input Manager（旧版输入系统）
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class CameraHandler : MonoBehaviour
{
    [Header("高度/地面设置")]
    [Tooltip("地面Y轴高度（鼠标投射与边界计算的基准）")]
    public float floorY = 0f;

    [Header("移动控制（WASD / 拖拽 / 边缘）")]
    public bool enableKeyboardPan = true;
    public bool enableEdgeScroll = true;
    public bool enableRightDrag = true;
    [Tooltip("基础移动速度")]
    public float panSpeed = 40f;              
    [Tooltip("屏幕边缘触发滚动的宽度（像素）")]
    public float edgePixels = 20f;
    [Tooltip("Shift键加速倍数")]
    public float shiftBoost = 2.0f;
    [Tooltip("高度自适应速度比例（越高越快，1=关闭自适应）")]
    [Range(0.5f, 4f)] public float heightFactor = 1.6f;

    [Header("缩放控制（滚轮，指针为中心）")]    
    public bool enableZoom = true;
    [Tooltip("相机高度范围（Y轴最小值/最大值）")]
    public Vector2 zoomY = new Vector2(8f, 120f);
    [Tooltip("滚轮缩放灵敏度（数值越大，每滚一格高度变化越明显）")]
    public float zoomSensitivity = 20f;

    [Header("平滑过渡设置")]
    [Tooltip("移动/缩放平滑系数（0=无平滑，数值越大越顺滑）")]
    [Range(0f, 30f)] public float followLerp = 8f;

    [Header("边界限制设置")]
    [Tooltip("地图基础范围（XMin, YMin, Width, Height），Y对应世界空间Z轴")]
    [HideInInspector]public Rect mapSize = new Rect(-50, -50, 100, 100);
    public bool clampToBounds = true;
    public float edgeWidth = 5.0f;
    [Tooltip("视口与地图边界的预留边距（避免相机视口超出地图）")]
    public float viewportPadding = 1.0f;
    public bool enableBoundaries = true;
    public bool showHandles = true;

    [Header("键位设置")]
    [Tooltip("拖拽移动的鼠标按键（默认右键）")]
    public KeyCode dragMouseButton = KeyCode.Mouse1;
    [Tooltip("加速键（默认左Shift）")]
    public KeyCode fastKey = KeyCode.LeftShift;

    // 私有字段（统一加下划线，符合C#命名规范）
    private Camera _cam;
    private Plane _ground;
    private Vector3 _dragPrevWorld;
    private bool _dragging;
    private Vector3 _targetPos;   // 平滑移动的目标位置
    private float _lastDeltaTime;
    private bool _paused;
    private Rect _lookBounds;     // 动态计算的相机可移动范围

    // 性能优化：记录上一帧状态，减少重复计算
    private float _lastFloorY;    // 上一帧地面高度（检测动态修改）
    private Vector3 _lastCamPos;  // 上一帧相机X/Z位置（检测位置变化）
    private float _lastCamY;      // 上一帧相机Y高度（检测缩放变化）

    private void Awake()
    {
        // 初始化相机与地面平面
        _cam = GetComponent<Camera>();
        _lastFloorY = floorY;
        _ground = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
        
        // 初始化目标位置与状态记录
        _targetPos = transform.position;
        _lastCamPos = new Vector3(transform.position.x, 0f, transform.position.z);
        _lastCamY = transform.position.y;
    }

    private void Update()
    {
        // 处理暂停状态
        if (_paused) return;

        // 1. 检测地面高度变化，更新地面平面
        UpdateGroundPlaneIfNeeded();

        // 2. 仅在相机位置/高度变化时，更新动态边界（减少计算消耗）
        UpdateLookBoundsIfNeeded();

        // 3. 计算所有移动输入（键盘+边缘+拖拽）
        Vector3 panInput = GetTotalPanInput();

        // 4. 计算最终移动目标位置（含速度缩放）
        Vector3 desiredPos = CalculateDesiredPosition(panInput);

        if (enableRightDrag) 
        desiredPos += ReadDragPan();
        // 5. 处理指针中心缩放
        if (enableZoom)
            desiredPos = ApplyCursorCentricZoom(desiredPos);
        // 6. 应用边界限制
        if (clampToBounds && enableBoundaries)
            desiredPos = ClampPositionToBounds(desiredPos);

        // 7. 应用平滑移动或直接移动
        ApplyFinalPosition(desiredPos);
    }

    #region 核心逻辑：输入与位置计算

    /// <summary>
    /// 检测地面高度变化，更新地面平面
    /// </summary>
    private void UpdateGroundPlaneIfNeeded()
    {
        if (Mathf.Abs(floorY - _lastFloorY) > 0.01f)
        {
            _ground = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
            _lastFloorY = floorY;
        }
        _lastDeltaTime = Time.deltaTime <= 0f ? 0.016f : Time.deltaTime;
    }

    /// <summary>
    /// 计算所有移动输入的总和（键盘+边缘+拖拽）
    /// </summary>
    private Vector3 GetTotalPanInput()
    {
        Vector3 totalInput = Vector3.zero;
        if (enableKeyboardPan) totalInput += ReadKeyboardPan();
        if (enableEdgeScroll) totalInput += ReadEdgePan();
        // if (enableRightDrag) totalInput += ReadDragPan();
        return totalInput;
    }

    /// <summary>
    /// 计算包含速度缩放的目标位置
    /// </summary>
    private Vector3 CalculateDesiredPosition(Vector3 panInput)
    {
        // 速度缩放：高度自适应 + Shift加速
        float speedScale = GetSpeedScale();
        return _targetPos + panInput * panSpeed * speedScale * _lastDeltaTime;
    }

    /// <summary>
    /// 应用最终位置（含平滑过渡）
    /// </summary>
    private void ApplyFinalPosition(Vector3 desiredPos)
    {
        _targetPos = desiredPos;
        if (followLerp <= 0f)
        {
            // 无平滑：直接赋值
            transform.position = desiredPos;
        }
        else
        {
            // 轻量平滑：指数插值（比普通Lerp更自然）
            transform.position = Vector3.Lerp(
                transform.position, 
                desiredPos, 
                1f - Mathf.Exp(-followLerp * _lastDeltaTime)
            );
        }
    }

    #endregion

    #region 输入采样：移动相关

    /// <summary>
    /// 读取WASD/方向键输入（世界坐标X/Z方向，不随相机旋转）
    /// </summary>
    private Vector3 ReadKeyboardPan()
    {
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;

        // 归一化：避免斜向移动速度过快
        Vector3 input = new Vector3(x, 0f, z);
        return input.sqrMagnitude > 0f ? input.normalized : Vector3.zero;
    }

    /// <summary>
    /// 读取屏幕边缘滚动输入
    /// </summary>
    private Vector3 ReadEdgePan()
    {
        Vector3 mousePos = Input.mousePosition;
        float x = 0f, z = 0f;

        if (mousePos.x <= edgePixels) x -= 1f;
        else if (mousePos.x >= Screen.width - edgePixels) x += 1f;

        if (mousePos.y <= edgePixels) z -= 1f;
        else if (mousePos.y >= Screen.height - edgePixels) z += 1f;

        // 归一化：避免斜向移动速度过快
        Vector3 input = new Vector3(x, 0f, z);
        return input.sqrMagnitude > 0f ? input.normalized : Vector3.zero;
    }

    /// <summary>
    /// 读取右键拖拽移动输入
    /// </summary>
    private Vector3 ReadDragPan()
    {
        if (Input.GetKeyDown(dragMouseButton))
        {
            _dragging = true;
            _dragPrevWorld = ScreenToGround(Input.mousePosition);
            return Vector3.zero;
        }

        if (Input.GetKey(dragMouseButton) && _dragging)
        {
            Vector3 currentWorld = ScreenToGround(Input.mousePosition);
            Vector3 delta = _dragPrevWorld - currentWorld; // 拖拽反方向移动
            _dragPrevWorld = currentWorld;
            delta.y = 0f; // 锁定Y轴，避免高度变化
            return delta;
        }

        if (Input.GetKeyUp(dragMouseButton))
        {
            _dragging = false;
        }

        return Vector3.zero;
    }

    #endregion

    #region 缩放逻辑：指针中心缩放

    /// <summary>
    /// 应用指针中心缩放（滚轮控制高度，同时向鼠标位置推进）
    /// </summary>
    private Vector3 ApplyCursorCentricZoom(Vector3 desiredPos)
    {
        Vector3 originPos = desiredPos;
        // 1. 读取滚轮输入（与参考代码逻辑一致，向上滚=减小高度=靠近，向下滚=增加高度=远离）
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scrollInput, 0)) 
            return desiredPos; // 无滚轮输入时，直接返回原目标位置

        // 2. 获取鼠标指针在地面的投射点（作为缩放的“中心锚点”，避免固定target）
        Vector3 mouseGroundPoint = ScreenToGround(Input.mousePosition);
        // 投射失败时（极端情况），用相机当前XZ位置作为锚点，避免异常
        if (Mathf.Approximately(mouseGroundPoint.y, transform.position.y))
            mouseGroundPoint = new Vector3(transform.position.x, floorY, transform.position.z);
        Vector3 dir = (mouseGroundPoint - transform.position).normalized;
        desiredPos += dir * zoomSensitivity * Mathf.Sign(scrollInput);
        if (desiredPos.y < zoomY.x || desiredPos.y > zoomY.y)
            return originPos;
        // desiredPos.x = Mathf.Clamp(desiredPos.y, mapSize.xMin, mapSize.xMax);
        // desiredPos.z = Mathf.Clamp(desiredPos.z, mapSize.yMin, mapSize.yMax);
        return desiredPos;
    }
    #endregion

    #region 边界逻辑：动态边界限制

    /// <summary>
    /// 仅在相机位置/高度变化时，更新动态边界（减少计算消耗）
    /// </summary>
    private void UpdateLookBoundsIfNeeded()
    {
        if (!enableBoundaries || _cam == null) return;

        // 检测相机X/Z位置或Y高度变化（阈值0.01f避免频繁计算）
        bool posChanged = Mathf.Abs(transform.position.x - _lastCamPos.x) > 0.01f
                       || Mathf.Abs(transform.position.z - _lastCamPos.z) > 0.01f;
        bool heightChanged = Mathf.Abs(transform.position.y - _lastCamY) > 0.01f;

        if (posChanged || heightChanged)
        {
            RecalculateLookBounds();
            _lastCamPos = new Vector3(transform.position.x, 0f, transform.position.z);
            _lastCamY = transform.position.y;
        }
    }

    /// <summary>
    /// 重新计算动态边界（基于当前相机视口与地图范围）
    /// </summary>
    private void RecalculateLookBounds()
    {
        // 投射屏幕四个角到地面，获取当前视口的地面范围
        Vector3 blWorld = ScreenToGround(Vector3.zero);                  // 左下角
        Vector3 brWorld = ScreenToGround(new Vector3(Screen.width, 0));  // 右下角
        Vector3 trWorld = ScreenToGround(new Vector3(Screen.width, Screen.height)); // 右上角
        Vector3 tlWorld = ScreenToGround(new Vector3(0, Screen.height));  // 左上角

        // 计算视口在地面的极值（左/右/下/上）
        float viewMinX = Mathf.Min(blWorld.x, brWorld.x, trWorld.x, tlWorld.x);
        float viewMaxX = Mathf.Max(blWorld.x, brWorld.x, trWorld.x, tlWorld.x);
        float viewMinZ = Mathf.Min(blWorld.z, brWorld.z, trWorld.z, tlWorld.z);
        float viewMaxZ = Mathf.Max(blWorld.z, brWorld.z, trWorld.z, tlWorld.z);

        // 动态边界 = 地图范围 - 视口偏移（确保相机视口不超出地图）
        _lookBounds = new Rect(
            mapSize.xMin - (viewMinX - transform.position.x),  // 左边界
            mapSize.yMin - (viewMinZ - transform.position.z),  // 下边界（mapSize.Y对应世界Z）
            mapSize.width - (viewMaxX - viewMinX),             // 边界宽度（含视口）
            mapSize.height - (viewMaxZ - viewMinZ)             // 边界高度（含视口）
        );
    }

    private Vector3 _refPos;
    /// <summary>
    /// 将位置限制在动态边界内（含视口余量）
    /// </summary>
    /// <param name="pos">待限制的坐标</param>
    private Vector3 ClampPositionToBounds(Vector3 pos)
    {
        var xP = Mathf.Clamp(pos.x, _lookBounds.xMin, _lookBounds.xMax);
        var zP = Mathf.Clamp(pos.z, _lookBounds.yMin, _lookBounds.yMax);
        Vector3 newPosition = new Vector3(xP, pos.y, zP);
        float distance = Vector3.Distance(pos, newPosition);
        if (pos != newPosition)
        {
            Vector3 direction = (newPosition - pos).normalized;
            direction.y = 0f;
            // float reboundSpeed = Mathf.Min(distance * 5, 10f);
            Vector3 smoothedPosition = Vector3.SmoothDamp(pos, newPosition, ref _refPos, .2f);
            pos = smoothedPosition;
        }
        if (distance < 0.1f)
        {
            pos = newPosition;
        }
        pos = new Vector3(
            Mathf.Clamp(pos.x, _lookBounds.xMin - edgeWidth, _lookBounds.xMax + edgeWidth),
            pos.y,
            Mathf.Clamp(pos.z, _lookBounds.yMin - edgeWidth, _lookBounds.yMax + edgeWidth)
        );
        return pos;
    }

    /// <summary>
    /// 估算当前视口在地面的半宽/半高（适配透视相机）
    /// </summary>
    private void EstimateViewportHalfExtents(out float halfWidth, out float halfHeight)
    {
        Vector3 camCenterWorld = ScreenToGround(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        Vector3 camLeftWorld = ScreenToGround(new Vector3(0f, Screen.height * 0.5f));
        Vector3 camTopWorld = ScreenToGround(new Vector3(Screen.width * 0.5f, Screen.height));

        // 半宽 = 中心到左边缘的距离，半高 = 中心到上边缘的距离
        halfWidth = (camCenterWorld - camLeftWorld).magnitude;
        halfHeight = (camTopWorld - camCenterWorld).magnitude;
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 将屏幕点投射到地面平面（统一封装，避免重复代码）
    /// </summary>
    private Vector3 ScreenToGround(Vector3 screenPoint)
    {
        Ray ray = _cam.ScreenPointToRay(screenPoint);
        if (_ground.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        // 投射失败时返回相机当前位置（避免异常）
        return transform.position;
    }

    /// <summary>
    /// 计算最终速度缩放系数（高度自适应 + Shift加速）
    /// </summary>
    private float GetSpeedScale()
    {
        // 高度自适应：从zoomY.x到zoomY.y，速度从1线性过渡到heightFactor
        float heightScale = Mathf.Lerp(
            1f, 
            heightFactor, 
            Mathf.InverseLerp(zoomY.x, zoomY.y, transform.position.y)
        );
        // Shift加速：按下时乘shiftBoost，否则为1
        float fastScale = Input.GetKey(fastKey) ? shiftBoost : 1f;
        return heightScale * fastScale;
    }

    #endregion

    #region 外部控制接口

    /// <summary>
    /// 暂停相机控制（停止所有输入响应）
    /// </summary>
    public void Pause()
    {
        _paused = true;
        _dragging = false; // 暂停时重置拖拽状态，避免恢复后异常
    }

    /// <summary>
    /// 恢复相机控制
    /// </summary>
    public void Resume()
    {
        _paused = false;
    }

    #endregion

    #region 编辑器可视化（Scene视图边界调试）

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!enableBoundaries || !showHandles) return;

        // 绘制动态边界（红色线框）
        Gizmos.color = Color.red;
        Vector3 bl = new Vector3(_lookBounds.xMin, floorY, _lookBounds.yMin);
        Vector3 br = new Vector3(_lookBounds.xMax, floorY, _lookBounds.yMin);
        Vector3 tr = new Vector3(_lookBounds.xMax, floorY, _lookBounds.yMax);
        Vector3 tl = new Vector3(_lookBounds.xMin, floorY, _lookBounds.yMax);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        // // 绘制地图基础范围（蓝色虚线，用于对比）
        // Gizmos.color = Color.blue;
        // Gizmos.DrawWireCube(
        //     new Vector3(mapSize.xMin + mapSize.width * 0.5f, floorY, mapSize.yMin + mapSize.height * 0.5f),
        //     new Vector3(mapSize.width, 0.1f, mapSize.height)
        // );
    }
#endif

    #endregion
}