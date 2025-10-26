using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(LineRenderer))]
public class HexSeaBuilderAndHighlighter : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;           // Hexagonal (Point Top) : 実タイル用
    public Tilemap previewTilemap;    // ★半透明プレビュー用の別Tilemap（上に重ねる）
    public TileBase seaTile;          // 海
    public TileBase baseTile;         // Base（0,0用 & 設置ブラシ）

    [Header("Build Area")]
    public int width = 30;
    public int height = 30;
    public Transform player;          // 原点(0,0)にしたいTransform（なくてもOK）

    [Header("Hover Outline")]
    public float lineWidth = 0.03f;
    public Color lineColor = Color.black;  // ← 黒に変更

    [Header("Base Borders")]
    public Color borderColor = new Color(0.7f, 0.7f, 0.7f, 1f); // 灰色
    public float borderWidth = 0.04f;

    // 編集モード通知（UI側で拾う用）
    public event System.Action<bool> OnEditModeChanged;

    // 内部状態
    bool editMode = false;
    LineRenderer hoverLR;
    Vector3Int lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    Camera cam;

    // ブラシ（このタイルで設置）
    TileBase selectedBrush = null;

    // Input System
    InputAction toggleEdit;     // Fで編集トグル
    InputAction pointerPos;     // マウス座標
    InputAction pointerPress;   // 左クリック

    // Base境界線の管理
    readonly List<LineRenderer> borderLines = new();
    Transform bordersParent;

    void Awake()
    {
        cam = Camera.main;

        // --- Hover（枠）LineRenderer 設定 ---
        hoverLR = GetComponent<LineRenderer>();
        hoverLR.useWorldSpace = true;
        hoverLR.loop = false;
        hoverLR.positionCount = 7;
        hoverLR.startWidth = hoverLR.endWidth = lineWidth;
        hoverLR.material = new Material(Shader.Find("Sprites/Default"));
        hoverLR.startColor = hoverLR.endColor = lineColor; // ← 黒
        hoverLR.enabled = false;

        // 入力
        toggleEdit   = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/f");
        toggleEdit.performed += _ => ToggleEditMode();

        pointerPos   = new InputAction(type: InputActionType.Value,  binding: "<Pointer>/position");
        pointerPress = new InputAction(type: InputActionType.Button, binding: "<Pointer>/press");
        pointerPress.performed += _ => TryPlaceBrushAtMouse();

        // 境界線親オブジェクト
        var go = new GameObject("Borders");
        go.transform.SetParent(transform, false);
        bordersParent = go.transform;
    }

    void OnEnable()
    {
        toggleEdit.Enable();
        pointerPos.Enable();
        pointerPress.Enable();
    }

    void OnDisable()
    {
        toggleEdit.Disable();
        pointerPos.Disable();
        pointerPress.Disable();
    }

    void Start()
    {
        if (tilemap == null || seaTile == null)
        {
            Debug.LogWarning("Tilemap or seaTile not assigned.");
            return;
        }

        // 原点セル（プレイヤー位置 or (0,0,0)）
        Vector3 originWorld = (player != null) ? player.position : Vector3.zero;
        Vector3Int originCell = tilemap.WorldToCell(originWorld);

        // 敷き詰め
        int startX = originCell.x - width  / 2;
        int startY = originCell.y - height / 2;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;  x++)
        {
            tilemap.SetTile(new Vector3Int(startX + x, startY + y, 0), seaTile);
        }

        // (0,0)相当セルをBaseに
        if (baseTile != null)
            tilemap.SetTile(originCell, baseTile);

        tilemap.RefreshAllTiles();

        // 初期ブラシは未選択（ボタンで設定）
        selectedBrush = null;

        // ★ 境界線を初回構築
        RebuildBaseBorders();
    }

    void ToggleEditMode()
    {
        editMode = !editMode;
        if (!editMode)
        {
            hoverLR.enabled = false;
            ClearPreview();
        }
        OnEditModeChanged?.Invoke(editMode);
    }

    public void SelectBrushTile(TileBase tile)
    {
        selectedBrush = tile;
        if (!editMode) ToggleEditMode();
    }

    void Update()
    {
        if (!editMode || tilemap == null || cam == null) return;

        // マウスからセル
        Vector2 m = pointerPos.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, 0));
        world.z = 0f;
        Vector3Int cell = tilemap.WorldToCell(world);

        // 枠表示（そのセルにタイルがあるときのみ）
        if (!tilemap.HasTile(cell))
        {
            hoverLR.enabled = false;
            lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
            ClearPreview();
            return;
        }

        if (cell != lastCell)
        {
            lastCell = cell;
            UpdateHexOutline(hoverLR, cell);
            hoverLR.enabled = true;

            // ★ Base設置モード時の半透明プレビュー
            if (selectedBrush == baseTile && previewTilemap != null && baseTile != null)
                ShowPreview(cell);
            else
                ClearPreview();
        }
    }

    void TryPlaceBrushAtMouse()
    {
        if (!editMode || selectedBrush == null || tilemap == null || cam == null) return;

        Vector2 m = pointerPos.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, 0));
        world.z = 0f;
        Vector3Int cell = tilemap.WorldToCell(world);

        if (!tilemap.HasTile(cell)) return;

        // 設置
        tilemap.SetTile(cell, selectedBrush);
        tilemap.RefreshTile(cell);

        // プレビュー更新（位置は継続）
        if (selectedBrush == baseTile)
            ShowPreview(cell);

        // ★ 境界線を更新（置いたセルとその近傍でSea↔Base境界が変わるため）
        RebuildBaseBorders();
    }

    // ========= 半透明プレビュー =========
    void ShowPreview(Vector3Int cell)
    {
        if (previewTilemap == null || baseTile == null) return;

        previewTilemap.ClearAllTiles();
        previewTilemap.SetTile(cell, baseTile);
        // 半透明
        previewTilemap.SetColor(cell, new Color(1f, 1f, 1f, 0.5f));
        previewTilemap.RefreshAllTiles();
    }

    void ClearPreview()
    {
        if (previewTilemap != null)
        {
            previewTilemap.ClearAllTiles();
            previewTilemap.RefreshAllTiles();
        }
    }

    // ========= Hover枠（Point-Top六角） =========
    void UpdateHexOutline(LineRenderer lr, Vector3Int cell)
    {
        Vector3 c = tilemap.GetCellCenterWorld(cell);
        GetHexVertices(c, out var p);

        lr.positionCount = 7;
        lr.SetPosition(0, p[0]);
        lr.SetPosition(1, p[1]);
        lr.SetPosition(2, p[2]);
        lr.SetPosition(3, p[3]);
        lr.SetPosition(4, p[4]);
        lr.SetPosition(5, p[5]);
        lr.SetPosition(6, p[0]);
    }

    // ========= Base境界（Seaと接する辺に灰線） =========
    void RebuildBaseBorders()
    {
        // 既存ライン削除
        foreach (var lr in borderLines)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        borderLines.Clear();

        // tilemap内の使用セルをざっくり走査
        tilemap.CompressBounds();
        var b = tilemap.cellBounds;

        for (int y = b.yMin; y < b.yMax; y++)
        for (int x = b.xMin; x < b.xMax; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            var t = tilemap.GetTile(cell);
            if (t != baseTile) continue; // Baseセルのみ対象

            // 六角の頂点列
            Vector3 center = tilemap.GetCellCenterWorld(cell);
            GetHexVertices(center, out var p);

            // 6辺チェック：各辺の外側へ少しサンプル→隣セルがSeaなら線を引く
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                Vector3 mid = (p[i] + p[j]) * 0.5f;
                // 中心→辺の法線方向へ少し外へ
                Vector3 sample = center + (mid - center) * 1.2f;
                var ncell = tilemap.WorldToCell(sample);

                var ntile = tilemap.GetTile(ncell);
                if (ntile == seaTile) // Sea と接している
                {
                    MakeBorderLine(p[i], p[j]);
                }
            }
        }
    }

    void MakeBorderLine(Vector3 a, Vector3 b)
    {
        var go = new GameObject("BaseBorder");
        go.transform.SetParent(bordersParent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = lr.endWidth = borderWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = borderColor;
        borderLines.Add(lr);
    }

    // ========= 六角頂点（Point-Top） =========
    void GetHexVertices(Vector3 center, out Vector3[] p)
    {
        if (pVertsCache == null) pVertsCache = new Vector3[6];
        p = pVertsCache;

        // Grid の Cell Size に追従
        Vector3 cs = tilemap.layoutGrid.cellSize;
        float w = cs.x;
        float h = cs.y;

        float s  = h * 0.5f; // 上下の尖り
        float hx = w * 0.5f; // 左右斜めの横距離
        float hy = s * 0.5f; // 斜め頂点の縦オフセット

        p[0] = center + new Vector3(0f,  +s, 0f);   // 上
        p[1] = center + new Vector3(+hx, +hy, 0f);  // 右上
        p[2] = center + new Vector3(+hx, -hy, 0f);  // 右下
        p[3] = center + new Vector3(0f,  -s, 0f);   // 下
        p[4] = center + new Vector3(-hx, -hy, 0f);  // 左下
        p[5] = center + new Vector3(-hx, +hy, 0f);  // 左上
    }
    Vector3[] pVertsCache;
}
