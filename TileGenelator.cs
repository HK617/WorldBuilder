using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class HexSeaBuilderAndHighlighter : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;        // Hexagonal (Point Top)
    public TileBase seaTile;       // 海タイル
    public TileBase baseTile;      // (0,0) 用の Base タイル (BasePalette)

    [Header("Build Area")]
    public int width = 30;         // X方向の枚数
    public int height = 30;        // Y方向の枚数
    public Transform player;       // (任意) スポーンTransform。これの位置セルを(0,0)とみなす

    [Header("Highlight")]
    public KeyCode toggleKey = KeyCode.F; // 編集モード切替キー
    public float lineWidth = 0.03f;       // 枠線太さ
    public Color lineColor = Color.white; // 枠線色

    // 内部状態
    bool editMode = false;
    LineRenderer lr;
    Vector3Int lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    Camera cam;

    // 新Input System
    InputAction toggleAction;
    InputAction pointerPosition;

    void Awake()
    {
        cam = Camera.main;

        // LineRenderer 準備
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 7; // 6頂点 + 閉じる
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.enabled = false;

        // InputActions 準備（Input System）
        toggleAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/f");
        toggleAction.performed += _ => ToggleEditMode();
        pointerPosition = new InputAction(type: InputActionType.Value, binding: "<Pointer>/position");
    }

    void OnEnable()
    {
        toggleAction.Enable();
        pointerPosition.Enable();
    }

    void OnDisable()
    {
        toggleAction.Disable();
        pointerPosition.Disable();
    }

    void Start()
    {
        if (tilemap == null || seaTile == null)
        {
            Debug.LogWarning("Tilemap or seaTile is not assigned.");
            return;
        }

        // === 原点セル（= プレイヤー位置セル or (0,0,0) ワールド） ===
        Vector3 originWorld = (player != null) ? player.position : Vector3.zero;
        Vector3Int originCell = tilemap.WorldToCell(originWorld);

        // === 敷き詰め（原点を中心に広げる） ===
        int startX = originCell.x - width / 2;
        int startY = originCell.y - height / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int c = new Vector3Int(startX + x, startY + y, 0);
                tilemap.SetTile(c, seaTile);
            }
        }

        // === 論理タイル(0,0) → 実セル originCell を Base に差し替え ===
        if (baseTile != null)
        {
            tilemap.SetTile(originCell, baseTile);
        }

        tilemap.RefreshAllTiles();
        Debug.Log($"Filled hex tiles around {originCell}. Set Base at {originCell} as logical (0,0).");
    }

    void ToggleEditMode()
    {
        editMode = !editMode;
        if (!editMode) lr.enabled = false;
    }

    void Update()
    {
        if (!editMode || tilemap == null || cam == null) return;

        // マウス座標（スクリーン）→ ワールド → セル
        Vector2 m = pointerPosition.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, 0));
        world.z = 0f;
        Vector3Int cell = tilemap.WorldToCell(world);

        // タイルが存在しないセルは非表示
        if (!tilemap.HasTile(cell))
        {
            lr.enabled = false;
            lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
            return;
        }

        // セルが変わったときだけ描き直し
        if (cell != lastCell)
        {
            lastCell = cell;
            UpdateHexOutline(cell);
        }

        lr.enabled = true;
    }

    // Point-Top 六角の外周をLineRendererで描画
    void UpdateHexOutline(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);

        // Grid の Cell Size を利用（Hex タイルの見た目幅・高さ）
        Vector3 cs = tilemap.layoutGrid.cellSize;
        float w = cs.x;
        float h = cs.y;

        // 幾何計算（Point-Top）
        float s = h * 0.5f; // 上下の尖りまで
        float hx = w * 0.5f; // 斜め右/左までの横距離
        float hy = s * 0.5f; // 斜め頂点の縦オフセット

        Vector3[] p = new Vector3[7];
        p[0] = center + new Vector3(0f, +s, 0f);   // 上
        p[1] = center + new Vector3(+hx, +hy, 0f);  // 右上
        p[2] = center + new Vector3(+hx, -hy, 0f);  // 右下
        p[3] = center + new Vector3(0f, -s, 0f);   // 下
        p[4] = center + new Vector3(-hx, -hy, 0f);  // 左下
        p[5] = center + new Vector3(-hx, +hy, 0f);  // 左上
        p[6] = p[0];                                 // 閉じる

        lr.positionCount = 7;
        lr.SetPositions(p);
    }
}
