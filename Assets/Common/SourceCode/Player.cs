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

        // キーボード入力の判定
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
        }

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Spaceキーでジャンプ
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}