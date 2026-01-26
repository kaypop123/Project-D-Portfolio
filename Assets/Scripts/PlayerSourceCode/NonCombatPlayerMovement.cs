using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class NonCombatPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Jump Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.1f;
    private bool isGrounded;

    public bool IsGrounded => isGrounded;

    [SerializeField] private float lastDirection = 1f;
    public float LastDirection => lastDirection;


    private Vector2 moveInput;
    private Rigidbody2D rb;
    [SerializeField] private float lastDirectiion = 1f;

    public float LasDirection => lastDirectiion;

    //private NonCombatePlayerAnimationBinder animatorBinder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckGround();
        Movement();
    }
    void Movement()
    {

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.x != 0)
            lastDirection = Mathf.Sign(moveInput.x);
    }

    public Vector2 GetMoveInput() => moveInput;

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TryJump();
    }
    public bool TryJump()
    {
        if (!isGrounded) return false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        return true;
    }

    public void CheckGround()
    {
        if (!groundCheck) { isGrounded = true; return; }
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
}
