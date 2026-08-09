using UnityEngine;
using UnityEngine.InputSystem; // 新しいInput Systemの定義

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject playerModel;

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float dashSpeed = 9.0f; // ダッシュ時の移動速度
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("視点設定")]
    [SerializeField] private float mouseSensitivity = 0.1f; // Input Systemでは感度数値を小さめに設定

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

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

        // 新しいInput Systemでのマウス操作取得
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    private void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // キーボード入力の判定（前後左右）
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
        }

        // --- ダッシュ入力の判定 ---
        bool isDashing = false;

        // 1. PC: 左Shift または 右Shift
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            {
                isDashing = true;
            }
        }

        // 2. VR / ゲームパッド: 左スティック押し込み（L3ボタン）
        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStickButton.isPressed)
            {
                isDashing = true;
            }
        }

        // ダッシュ中かどうかで速度を切り替え
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- 【追加・更新】ジャンプ入力の判定 ---
        bool isJumpPressed = false;

        // 1. PC: Spaceキー
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isJumpPressed = true;
        }

        // 2. VR / ゲームパッド: Aボタン（Gamepad South Button）
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            isJumpPressed = true;
        }

        // 地面に接地している状態でジャンプ入力があった場合
        if (isJumpPressed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力適用
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}