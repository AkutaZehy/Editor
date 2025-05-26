using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 1.0f;
    public float minZoomSize = 1.0f;
    public float maxZoomSize = 10.0f;

    [Header("Drag Pan Settings")]
    public KeyCode dragPanModifier = KeyCode.LeftControl;
    public int dragMouseButton = 0;
    public float dragPanSpeed = 1.0f;

    private Camera _camera;
    private Vector3 _dragOriginWorld;
    private bool _isDragging = false;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("CameraControl 脚本需要附加到带有 Camera 组件的 GameObject 上!");
            enabled = false;
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
        if (_camera == null || !_camera.orthographic) return;

        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            _camera.orthographicSize -= scrollDelta * zoomSpeed * _camera.orthographicSize;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoomSize, maxZoomSize);
        }

        if (Input.GetKey(dragPanModifier))
        {
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                _isDragging = true;
                _dragOriginWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
                _dragOriginWorld.z = 0;
            }

            if (Input.GetMouseButton(dragMouseButton) && _isDragging)
            {
                Vector3 currentMouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                currentMouseWorldPos.z = 0;

                Vector3 moveVector = _dragOriginWorld - currentMouseWorldPos;

                transform.position += moveVector * dragPanSpeed;
            }

            if (Input.GetMouseButtonUp(dragMouseButton))
            {
                _isDragging = false;
            }
        }
        else _isDragging = false;


        if (!Input.GetMouseButton(dragMouseButton))
        {
            _isDragging = false;
        }

        // TODO: 可以添加摄像机移动范围限制，防止拖拽出游戏区域
        // 例如：transform.position = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), Mathf.Clamp(transform.position.y, minY, maxY), transform.position.z);
    }

    void OnDisable()
    {
        _isDragging = false;
    }
}