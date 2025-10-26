using UnityEngine;
using UnityEngine.UI;

public class BaseMenuUI : MonoBehaviour
{
    [Header("Hook up")]
    public HexSeaBuilderAndHighlighter builder; // シーン内の統合スクリプト
    public RectTransform rightPanel;            // RightMenuPanel
    public Button baseButton;                   // BaseButton

    void Awake()
    {
        if (rightPanel != null) rightPanel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (builder != null)
            builder.OnEditModeChanged += OnEditModeChanged;

        if (baseButton != null)
            baseButton.onClick.AddListener(OnClickBase);
    }

    void OnDisable()
    {
        if (builder != null)
            builder.OnEditModeChanged -= OnEditModeChanged;

        if (baseButton != null)
            baseButton.onClick.RemoveListener(OnClickBase);
    }

    void OnEditModeChanged(bool on)
    {
        if (rightPanel != null)
            rightPanel.gameObject.SetActive(on);
    }

    void OnClickBase()
    {
        if (builder != null && builder.baseTile != null)
        {
            // ブラシに Base タイルをセット（左クリックで設置できる状態に）
            builder.SelectBrushTile(builder.baseTile);
        }
    }
}
