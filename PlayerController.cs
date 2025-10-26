using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    Vector2 moveInput;
    PlayerInput pi; InputAction move;

    void Awake() => pi = GetComponent<PlayerInput>();

    void OnEnable() {
        move = pi.actions["Move"];
        move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        move.canceled  += ctx => moveInput = Vector2.zero;
        move.Enable();
    }
    void OnDisable() {
        if (move != null) { move.performed -= null; move.canceled -= null; move.Disable(); }
    }
    void Update() {
        transform.position += (Vector3)(moveInput * speed * Time.deltaTime);
    }
}
