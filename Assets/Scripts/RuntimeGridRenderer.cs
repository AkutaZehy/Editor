using UnityEngine;
using System.Collections.Generic;

public class RuntimeGridRenderer : MonoBehaviour
{
  [Header("Grid Settings")]
  public float gridSize = 1.0f;
  public GameObject gridLinePrefab;

  [Header("Visibility")]
  public bool enableGrid = true;

  private Camera _camera;
  private List<GameObject> _activeGridLines = new List<GameObject>();
  private Transform _gridLinesParent;

  void Awake()
  {
    _camera = GetComponent<Camera>();
    if (_camera == null)
    {
      Debug.LogError("RuntimeGridRenderer 需要附加到带有 Camera 组件的 GameObject 上!");
      enabled = false;
      return;
    }

    if (gridLinePrefab == null)
    {
      Debug.LogError("RuntimeGridRenderer: Grid Line Prefab 未赋值! 请在 Inspector 中赋值一个带有 LineRenderer 的 Prefab.");
      enabled = false;
      return;
    }

    _gridLinesParent = new GameObject("RuntimeGridLines").transform;
    _gridLinesParent.SetParent(this.transform);

    // _gridLinesParent.localPosition = new Vector3(gridSize / 2f, gridSize / 2f, 0);
  }

  void Update()
  {
    if (enableGrid)
    {
      RenderGrid();
    }
    else
    {
      ClearGridLines();
    }
  }

  void ClearGridLines()
  {
    foreach (GameObject lineObj in _activeGridLines)
    {
      if (lineObj != null)
      {
        Destroy(lineObj);
      }
    }
    _activeGridLines.Clear();
  }

  void RenderGrid()
  {
    if (_camera == null || !_camera.orthographic || !enableGrid) return;

    float camHeight = _camera.orthographicSize * 2f;
    float camWidth = camHeight * _camera.aspect;

    Vector3 camPos = _camera.transform.position;

    float xOffset = gridSize / 2f;
    float yOffset = gridSize / 2f;

    float startX = Mathf.Floor((camPos.x - camWidth / 2f - xOffset) / gridSize) * gridSize + xOffset;
    float endX = Mathf.Ceil((camPos.x + camWidth / 2f - xOffset) / gridSize) * gridSize + xOffset;
    float startY = Mathf.Floor((camPos.y - camHeight / 2f - yOffset) / gridSize) * gridSize + yOffset;
    float endY = Mathf.Ceil((camPos.y + camHeight / 2f - yOffset) / gridSize) * gridSize + yOffset;

    List<Vector3> lineSegments = new List<Vector3>();

    for (float x = startX; x <= endX; x += gridSize)
    {
      lineSegments.Add(new Vector3(x, startY, 0));
      lineSegments.Add(new Vector3(x, endY, 0));
    }

    for (float y = startY; y <= endY; y += gridSize)
    {
      lineSegments.Add(new Vector3(startX, y, 0));
      lineSegments.Add(new Vector3(endX, y, 0));
    }

    if (lineSegments.Count / 2 != _activeGridLines.Count)
    {
      ClearGridLines();
      for (int i = 0; i < lineSegments.Count / 2; i++)
      {
        GameObject newLineObj = Instantiate(gridLinePrefab, _gridLinesParent);
        _activeGridLines.Add(newLineObj);
      }
    }

    for (int i = 0; i < lineSegments.Count / 2; i++)
    {
      GameObject lineObj = _activeGridLines[i];
      if (lineObj != null)
      {
        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr != null)
        {
          lr.SetPosition(0, lineSegments[i * 2]);
          lr.SetPosition(1, lineSegments[i * 2 + 1]);
        }
      }
    }
  }

  public void SetGridVisibility(bool isVisible)
  {
    enableGrid = isVisible;
    if (!isVisible)
    {
      ClearGridLines();
    }
  }

  public void ToggleGridVisibility()
  {
    SetGridVisibility(!enableGrid);
  }
}