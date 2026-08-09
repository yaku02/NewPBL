using UnityEngine;
using UnityEngine.InputSystem; // 新しい Input System の名前空間

[RequireComponent(typeof(Rigidbody))]
public class Buddy : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera; // XR Origin > Camera Offset > Main Camera を割り当てる

    [Header("移動・追従設定")]
    public float radius = 3.0f;               // 周回・再配置する外側の半径
    public float cannotInsideRadius = 1.0f;   // 入れない内側の半径
    public float moveSpeed = 2.0f;
    public float targetChangeInterval = 3.0f; // 定期的に場所を変える間隔
    public float offsetY = 0.0f;              // カメラからの縦方向オフセット

    [Header("追従トリガー距離")]
    [SerializeField] private float maxDistanceFromPlayer = 5.0f; // この距離以上プレイヤーが離れたら近くに移動を開始する

    [Header("インタラクト設定")]
    [SerializeField] private float interactDistance = 3.0f; // インタラクトを受け付ける距離
    [SerializeField] private float lookAtSpeed = 8.0f;       // ビルボード回転の速度

    private Rigidbody rb;
    private Vector3 currentTargetPos; // 【変更】ワールド空間での目標位置
    private float timer;
    private bool isInteracting = false; // インタラクト中フラグ

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cannotInsideRadius >= radius)
        {
            cannotInsideRadius = radius * 0.5f;
        }

        // 初期目標地点の設定
        if (playerCamera != null)
        {
            PickNewOffset();
        }
    }

    private void Update()
    {
        if (playerCamera == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerCamera.position);

        // キー入力判定（Fキー または QuestのAボタン）
        bool isFKeyPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool isAButtonPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        // Fキー または Aボタン が押されたときに距離チェック
        if ((isFKeyPressed || isAButtonPressed) && distToPlayer <= interactDistance)
        {
            isInteracting = !isInteracting; // フラグを反転
        }

        // 離れすぎたら自動解除
        if (isInteracting && distToPlayer > interactDistance + 1.0f)
        {
            isInteracting = false;
        }

        // インタラクト中はこれ以降の追従更新を行わない
        if (isInteracting) return;

        // --- 【変更点】プレイヤーとの距離チェックと目的地の更新 ---

        // プレイヤーとバディの距離が「maxDistanceFromPlayer」を超えたら即座にプレイヤーの近くを再設定
        if (distToPlayer > maxDistanceFromPlayer)
        {
            timer = 0.0f;
            PickNewOffset();
        }
        else
        {
            // 一定時間ごとにもランダムに付近を移動させる
            timer += Time.deltaTime;
            if (timer >= targetChangeInterval)
            {
                timer = 0.0f;
                PickNewOffset();
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerCamera == null) return;

        // インタラクト中は移動を停止し、ビルボード回転を行う
        if (isInteracting)
        {
            RotateBillboardTowardsPlayer();
            return;
        }

        // --- 【変更点】固定された目標位置（currentTargetPos）に向かって移動 ---
        float distanceToTarget = Vector3.Distance(rb.position, currentTargetPos);

        // 目標位置に十分近づいたらその場に留まる
        if (distanceToTarget > 0.05f)
        {
            Vector3 nextPos = Vector3.MoveTowards(
                rb.position,
                currentTargetPos,
                moveSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(nextPos);

            // 移動方向を向く処理
            Vector3 direction = currentTargetPos - rb.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                float rotationSpeed = 5.0f;

                Quaternion nextRotation = Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );

                rb.MoveRotation(nextRotation);
            }
        }
    }

    // ビルボード処理（カメラに正面を向ける回転）
    private void RotateBillboardTowardsPlayer()
    {
        Vector3 direction = transform.position - playerCamera.position;
        direction.y = 0; // Y軸のみ回転（水平に固定）

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookAtPlayerRotation = Quaternion.LookRotation(direction);
            Quaternion targetRotation = lookAtPlayerRotation * Quaternion.Euler(0, 180f, 0);

            Quaternion nextRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                lookAtSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(nextRotation);
        }
    }

    // 【変更】プレイヤーの現在の位置を基準に、ワールド座標としての新しい目標地点を決定する
    private void PickNewOffset()
    {
        if (playerCamera == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float currentRadius = Random.Range(cannotInsideRadius, radius);

        float x = Mathf.Cos(angle) * currentRadius;
        float z = Mathf.Sin(angle) * currentRadius;

        Vector3 center = playerCamera.position + Vector3.up * offsetY;

        // プレイヤーの現在地からの相対位置を「固定のワールド座標」としてセット
        currentTargetPos = center + new Vector3(x, Random.Range(-0.3f, 0.3f), z);
    }
}