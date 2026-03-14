using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class Groundeditor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase groundTile;

    [Header("Editor State")]
    [SerializeField] private bool isEditMode = true;
    [SerializeField] private bool eraseMode;

    private Vector3Int? lastEditedCell;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (!isEditMode)
        {
            lastEditedCell = null;
            return;
        }

        HandleTouchInput();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif
    }

    public void SetEditMode(bool enabled)
    {
        isEditMode = enabled;
        if (!isEditMode)
            lastEditedCell = null;
    }

    public void SetBrushMode()
    {
        eraseMode = false;
    }

    public void SetEraseMode()
    {
        eraseMode = true;
    }

    public void ClearAllGround()
    {
        if (groundTilemap == null)
            return;

        groundTilemap.ClearAllTiles();
        lastEditedCell = null;
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            lastEditedCell = null;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (IsPointerOverUi(touch.fingerId))
            return;

        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            EditAtScreenPosition(touch.position);
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            lastEditedCell = null;
    }

    private void HandleMouseInput()
    {
        if (!Input.GetMouseButton(0))
        {
            lastEditedCell = null;
            return;
        }

        if (IsPointerOverUi())
            return;

        EditAtScreenPosition(Input.mousePosition);
    }

    private void EditAtScreenPosition(Vector2 screenPosition)
    {
        if (worldCamera == null || groundTilemap == null)
            return;

        Vector3 world = worldCamera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;

        Vector3Int cell = groundTilemap.WorldToCell(world);

        if (lastEditedCell.HasValue && lastEditedCell.Value == cell)
            return;

        if (eraseMode)
            groundTilemap.SetTile(cell, null);
        else if (groundTile != null)
            groundTilemap.SetTile(cell, groundTile);

        lastEditedCell = cell;
    }

    private static bool IsPointerOverUi(int fingerId = -1)
    {
        if (EventSystem.current == null)
            return false;

        if (fingerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
