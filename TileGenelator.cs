using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class HexSeaBuilderAndHighlighter : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;        // Hexagonal (Point Top)
    public TileBase seaTile;       // �C�^�C��
    public TileBase baseTile;      // (0,0) �p�� Base �^�C�� (BasePalette)

    [Header("Build Area")]
    public int width = 30;         // X�����̖���
    public int height = 30;        // Y�����̖���
    public Transform player;       // (�C��) �X�|�[��Transform�B����̈ʒu�Z����(0,0)�Ƃ݂Ȃ�

    [Header("Highlight")]
    public KeyCode toggleKey = KeyCode.F; // �ҏW���[�h�ؑփL�[
    public float lineWidth = 0.03f;       // �g������
    public Color lineColor = Color.white; // �g���F

    // �������
    bool editMode = false;
    LineRenderer lr;
    Vector3Int lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    Camera cam;

    // �VInput System
    InputAction toggleAction;
    InputAction pointerPosition;

    void Awake()
    {
        cam = Camera.main;

        // LineRenderer ����
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 7; // 6���_ + ����
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.enabled = false;

        // InputActions �����iInput System�j
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

        // === ���_�Z���i= �v���C���[�ʒu�Z�� or (0,0,0) ���[���h�j ===
        Vector3 originWorld = (player != null) ? player.position : Vector3.zero;
        Vector3Int originCell = tilemap.WorldToCell(originWorld);

        // === �~���l�߁i���_�𒆐S�ɍL����j ===
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

        // === �_���^�C��(0,0) �� ���Z�� originCell �� Base �ɍ����ւ� ===
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

        // �}�E�X���W�i�X�N���[���j�� ���[���h �� �Z��
        Vector2 m = pointerPosition.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, 0));
        world.z = 0f;
        Vector3Int cell = tilemap.WorldToCell(world);

        // �^�C�������݂��Ȃ��Z���͔�\��
        if (!tilemap.HasTile(cell))
        {
            lr.enabled = false;
            lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
            return;
        }

        // �Z�����ς�����Ƃ������`������
        if (cell != lastCell)
        {
            lastCell = cell;
            UpdateHexOutline(cell);
        }

        lr.enabled = true;
    }

    // Point-Top �Z�p�̊O����LineRenderer�ŕ`��
    void UpdateHexOutline(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);

        // Grid �� Cell Size �𗘗p�iHex �^�C���̌����ڕ��E�����j
        Vector3 cs = tilemap.layoutGrid.cellSize;
        float w = cs.x;
        float h = cs.y;

        // �􉽌v�Z�iPoint-Top�j
        float s = h * 0.5f; // �㉺�̐��܂�
        float hx = w * 0.5f; // �΂߉E/���܂ł̉�����
        float hy = s * 0.5f; // �΂ߒ��_�̏c�I�t�Z�b�g

        Vector3[] p = new Vector3[7];
        p[0] = center + new Vector3(0f, +s, 0f);   // ��
        p[1] = center + new Vector3(+hx, +hy, 0f);  // �E��
        p[2] = center + new Vector3(+hx, -hy, 0f);  // �E��
        p[3] = center + new Vector3(0f, -s, 0f);   // ��
        p[4] = center + new Vector3(-hx, -hy, 0f);  // ����
        p[5] = center + new Vector3(-hx, +hy, 0f);  // ����
        p[6] = p[0];                                 // ����

        lr.positionCount = 7;
        lr.SetPositions(p);
    }
}
