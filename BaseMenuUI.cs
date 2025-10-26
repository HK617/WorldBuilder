using UnityEngine;
using UnityEngine.UI;

public class BaseMenuUI : MonoBehaviour
{
    [Header("Hook up")]
    public HexSeaBuilderAndHighlighter builder; // �V�[�����̓����X�N���v�g
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
            // �u���V�� Base �^�C�����Z�b�g�i���N���b�N�Őݒu�ł����ԂɁj
            builder.SelectBrushTile(builder.baseTile);
        }
    }
}
