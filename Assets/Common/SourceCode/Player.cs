using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject playerModel;

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float dashSpeed = 9.0f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("視点設定")]
    [SerializeField] private float mouseSensitivity = 0.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // 【補足】小刻みな振動を防ぐため、最小移動距離を0に設定
        controller.minMoveDistance = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (playerCamera == null || Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    private void HandleMovement()
    {
        // 1. キーボード入力の判定
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
        }

        // 2. ダッシュ判定
        bool isDashing = false;
        if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
        {
            isDashing = true;
        }
        if (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
        {
            isDashing = true;
        }

        float currentSpeed = isDashing ? dashSpeed : moveSpeed;

        // 水平方向の移動ベクトル（正規化して斜め移動の加速を防ぐ）
        Vector3 moveInput = (transform.right * x + transform.forward * z).normalized;
        Vector3 horizontalMove = moveInput * currentSpeed;

        // 3. 接地判定とY軸（重力・ジャンプ）の処理
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 地面に押し付けるための軽い下向きの力
        }

        // ジャンプ判定
        bool isJumpPressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) isJumpPressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) isJumpPressed = true;

        if (isJumpPressed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力加算
        velocity.y += gravity * Time.deltaTime;

        // 4. 【重要】水平移動と垂直移動（重力）を合体させて、1回の Move で実行する
        Vector3 finalMove = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
}