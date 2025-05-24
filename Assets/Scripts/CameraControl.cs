using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 1.0f; // 缩放速度
    public float minZoomSize = 1.0f; // 最小缩放级别 (Orthographic Size)
    public float maxZoomSize = 10.0f; // 最大缩放级别

    [Header("Drag Pan Settings")]
    public KeyCode dragPanModifier = KeyCode.LeftControl; // 按住哪个键作为拖拽平移的修饰键 (Ctrl)
    public int dragMouseButton = 0; // 使用哪个鼠标按钮触发拖拽 (0=左键, 1=右键, 2=中键)
    public float dragPanSpeed = 1.0f; // 拖拽平移的速度乘数 (通常设为 1.0f 即可实现 1:1 拖拽)

    private Camera _camera; // 缓存摄像机组件
    private Vector3 _dragOriginWorld; // 存储拖拽开始时鼠标点击的世界坐标位置
    private bool _isDragging = false; // 标志是否正在进行拖拽

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("CameraControl 脚本需要附加到带有 Camera 组件的 GameObject 上!");
            enabled = false; // 如果没有摄像机组件，禁用脚本
        }

        if (_camera != null && !_camera.orthographic)
        {
            Debug.LogWarning("CameraControl 脚本附加的摄像机不是正交模式，滚轮缩放和拖拽平移可能无法正常工作.");
        }

        if (_camera != null)
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoomSize, maxZoomSize);
        }
    }

    void Update()
    {
        // 确保摄像机存在且是正交模式
        if (_camera == null || !_camera.orthographic) return;

        // ===== 滚轮缩放 =====
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            // 调整摄像机的正交大小
            _camera.orthographicSize -= scrollDelta * zoomSpeed * _camera.orthographicSize; // 根据当前缩放级别调整速度
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoomSize, maxZoomSize); // 限制范围
        }

        // ===== 拖拽平移 =====
        // 检测是否按下了组合键 (Ctrl) 和指定的鼠标按钮 (左键)
        if (Input.GetKey(dragPanModifier)) // 组合键被按住
        {
            if (Input.GetMouseButtonDown(dragMouseButton)) // 指定的鼠标按钮被按下
            {
                _isDragging = true;
                // 记录拖拽开始时鼠标点击的“世界坐标”位置
                // 这一步很重要，后续计算位移基于这个世界坐标起点
                _dragOriginWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
                // 注意：这里的 Z 坐标可能会是非零的，但对于 2D 平移，我们只关心 X 和 Y
                _dragOriginWorld.z = 0; // 忽略 Z 坐标，保持在 2D 平面
            }

            if (Input.GetMouseButton(dragMouseButton) && _isDragging) // 指定的鼠标按钮被按住且正在拖拽
            {
                // 获取当前鼠标位置的“世界坐标”
                Vector3 currentMouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                currentMouseWorldPos.z = 0; // 忽略 Z 坐标

                // 计算从拖拽起点到当前鼠标位置的世界坐标差值
                // 场景（摄像机）需要向这个差值的反方向移动
                Vector3 moveVector = _dragOriginWorld - currentMouseWorldPos;

                // 应用平移到摄像机
                transform.position += moveVector * dragPanSpeed;

                // 注意：_dragOriginWorld 不需要每帧更新。
                // 它的作用是提供一个固定的“抓手”点在世界坐标系中。
                // 当鼠标移动时，我们计算当前鼠标位置相对于这个“抓手”点的世界坐标差值，
                // 然后将摄像机移动这个差值的反向，以保持“抓手”点在屏幕上看起来没有移动。
            }

            if (Input.GetMouseButtonUp(dragMouseButton)) // 指定的鼠标按钮被释放
            {
                _isDragging = false;
            }
        }
        else // 如果 Ctrl 键没有被按住，取消拖拽状态
        {
            _isDragging = false;
        }

        // 确保在鼠标未被按下指定按钮时，拖拽状态也是关闭的
        if (!Input.GetMouseButton(dragMouseButton))
        {
            _isDragging = false;
        }

        // TODO: 可以添加摄像机移动范围限制，防止拖拽出游戏区域
        // 例如：transform.position = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), Mathf.Clamp(transform.position.y, minY, maxY), transform.position.z);
    }

    // 可选：在 OnDisable 或 OnDestroy 中确保 _isDragging 被重置
    void OnDisable()
    {
        _isDragging = false;
    }
}