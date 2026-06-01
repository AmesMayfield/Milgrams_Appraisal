using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

// Requires the generated C# class from your Input Actions asset
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5.0f;
    [SerializeField] private float dashDuration = 0.2f;   // NOTE: 5.0 seconds is very long — consider ~0.2s
    [SerializeField] private float dashDistance = 5.0f;
    [SerializeField] private int maxDashes = 3;
    [SerializeField] private float dashRechargeTime = 3.0f;
    [SerializeField] private float dashBuffer = 0.5f;
    private Vector2 _bufferedMoveInput;

    private Vector2 _moveInput;
    private Vector2 _mouseScreenPosition;
    private int _dashesLeft;
    private bool _movable = true;
    private bool _dashable = true;

    private PlayerInputActions _inputActions; // Generated class name — match yours
    public Rigidbody2D rb;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _dashesLeft = maxDashes;

        // Bind gameplay actions — clean, no polling needed
        _inputActions.Gameplay.Move.performed += OnMove;
        _inputActions.Gameplay.Move.canceled  += OnMove;
        _inputActions.Gameplay.Dash.performed += OnDash;
        _inputActions.Gameplay.MousePosition.performed += OnMouseMove;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void OnEnable()  => _inputActions.Gameplay.Enable();
    private void OnDisable() => _inputActions.Gameplay.Disable();

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        _inputActions.Gameplay.Move.performed -= OnMove;
        _inputActions.Gameplay.Move.canceled  -= OnMove;
        _inputActions.Gameplay.Dash.performed -= OnDash;
        _inputActions.Gameplay.MousePosition.performed -= OnMouseMove;
        _inputActions.Dispose();
    }

    // -------------------------------------------------------------------------
    // Physics update
    // -------------------------------------------------------------------------

    private void FixedUpdate()
    {
        // Movement lives in FixedUpdate since we're driving a Rigidbody
        rb.linearVelocity = _movable ? _moveInput * _speed : Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Input callbacks — these fire on input events, not every frame
    // -------------------------------------------------------------------------

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _bufferedMoveInput = ctx.ReadValue<Vector2>();
        _moveInput = _movable ? _bufferedMoveInput : Vector2.zero;
    }

    private void OnMouseMove(InputAction.CallbackContext ctx)
    {
        _mouseScreenPosition = ctx.ReadValue<Vector2>();
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_dashable && _dashesLeft > 0)
        {
            StartCoroutine(Dash(_mouseScreenPosition));
        }
    }

    // -------------------------------------------------------------------------
    // Coroutines
    // -------------------------------------------------------------------------

    private IEnumerator Dash(Vector2 mouseScreenPos)
    {
        _dashesLeft--;
        _movable = false;
        _dashable = false;

        if (_dashesLeft == 0)
            StartCoroutine(RechargeDashes());

        Vector3 clickPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        clickPosition.z = 0;
        Vector3 direction = (clickPosition - transform.position).normalized;

        float elapsed = 0f;
        float dashSpeed = dashDistance / dashDuration;

        while (elapsed < dashDuration)
        {
            rb.MovePosition(rb.position + (Vector2)(direction * dashSpeed * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;

            if (elapsed > dashDuration - dashBuffer)
                _dashable = true;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        _movable = true;
        _dashable = true;
        _moveInput = _bufferedMoveInput;
    }

    private IEnumerator RechargeDashes()
    {
        yield return new WaitForSeconds(dashDuration + dashRechargeTime);
        _dashesLeft = maxDashes;
    }

    // -------------------------------------------------------------------------
    // Public API — call these from a game manager to switch contexts
    // -------------------------------------------------------------------------

    public void SwitchToUI()
    {
        _inputActions.Gameplay.Disable();
        _inputActions.UI.Enable();
    }

    public void SwitchToGameplay()
    {
        _inputActions.UI.Disable();
        _inputActions.Gameplay.Enable();
    }

    public void SwitchToCinematic()
    {
        _inputActions.Gameplay.Disable();
        _inputActions.UI.Disable();
        _inputActions.Cinematic.Enable();
    }
}