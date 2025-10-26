using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;   // ← ここが v3 で正しい名前空間

public class CameraScrollZoom : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcam;

    [Header("Zoom limits")]
    [SerializeField, Min(0.01f)] float minSize = 3f;
    [SerializeField] float maxSize = 20f;

    [Header("Feel")]
    [SerializeField] float stepPerTick = 1.0f;   // ホイール1刻みの増減
    [SerializeField] float smooth = 15f;         // 追従の速さ
    [SerializeField] bool invert = false;        // 方向反転

    float targetSize;

    void Reset() => vcam = GetComponent<CinemachineCamera>();

    void Awake()
    {
        if (vcam == null) vcam = GetComponent<CinemachineCamera>();
        targetSize = vcam.Lens.OrthographicSize;   // 初期値
    }

    void Update()
    {
        float scrollY = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            float dir = invert ? -1f : 1f;
            targetSize -= dir * (scrollY / 120f) * stepPerTick;  // 1刻み≈±120
            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
        }

        var lens = vcam.Lens; // struct を取り出して
        lens.OrthographicSize = Mathf.Lerp(
            lens.OrthographicSize,
            targetSize,
            1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime)
        );
        vcam.Lens = lens;     // 書き戻す
    }
}
