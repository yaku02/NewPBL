using UnityEngine;
using UnityEngine.InputSystem; // 新しい Input System の名前空間
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Buddy2 : MonoBehaviour, IInteractable
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera; // XR Origin > Camera Offset > Main Camera を割り当てる
    [SerializeField] private SceneManager sceneManager; // SceneManager の参照

    [System.Serializable]
    public struct SceneCanvasData
    {
        public SceneManager.SceneType sceneType;
        public Canvas choiceCanvas; // 対象シーンで表示する選択肢キャンバス
    }

    [Header("シーンごとの選択肢キャンバス設定")]
    [SerializeField] private List<SceneCanvasData> sceneCanvases = new List<SceneCanvasData>();

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
    private Vector3 currentTargetPos; // ワールド空間での目標位置
    private float timer;
    private bool isInteracting = false; // インタラクト中フラグ
    private bool isCanvasActive = false; // キャンバス表示中フラグ

    public float InteractDistance => interactDistance;

    public void OnInteract(bool interacting)
    {
        isInteracting = interacting;
    }

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

        // 初期状態ではすべての選択肢キャンバスを非アクティブ化
        HideAllCanvases();
    }

    private void Update()
    {
        // Fキーの入力検知（新しい Input System）
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleCurrentSceneCanvas();
        }

        if (playerCamera == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerCamera.position);

        // プレイヤーから十分離れた場合の処理
        if (distToPlayer > maxDistanceFromPlayer)
        {
            // インタラクト中またはキャンバス表示中であれば解除する
            if (isInteracting || isCanvasActive)
            {
                EndInteraction();
            }

            timer = 0.0f;
            PickNewOffset();
        }
        else
        {
            // インタラクト中（キャンバス表示中など）はタイマーによる場所移動を行わない
            if (isInteracting) return;

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

        // 固定された目標位置（currentTargetPos）に向かって移動
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

    // プレイヤーの現在の位置を基準に、ワールド座標としての新しい目標地点を決定する
    private void PickNewOffset()
    {
        if (playerCamera == null) return;

        // UnityEngine.Random を明示的に指定
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float currentRadius = UnityEngine.Random.Range(cannotInsideRadius, radius);

        float x = Mathf.Cos(angle) * currentRadius;
        float z = Mathf.Sin(angle) * currentRadius;

        Vector3 center = playerCamera.position + Vector3.up * offsetY;

        currentTargetPos = center + new Vector3(x, UnityEngine.Random.Range(-0.3f, 0.3f), z);
    }

    // Fキーが押された時のトグル処理
    private void ToggleCurrentSceneCanvas()
    {
        if (sceneManager == null)
        {
            Debug.LogWarning("SceneManager の参照がアサインされていません。");
            return;
        }

        // 表示中の場合は非表示にする
        if (isCanvasActive)
        {
            EndInteraction();
            return;
        }

        // 非表示の場合は現在のシーンに対応するキャンバスのみ表示
        SceneManager.SceneType currentScene = sceneManager.currentScene;
        SceneCanvasData targetData = sceneCanvases.Find(data => data.sceneType == currentScene);

        if (targetData.choiceCanvas != null)
        {
            HideAllCanvases(); // 他のキャンバスを念のため全て非表示
            targetData.choiceCanvas.gameObject.SetActive(true);
            isCanvasActive = true;
            OnInteract(true); // メニュー開閉に合わせて回転・移動停止イベントを呼び出す
        }
        else
        {
            Debug.LogWarning($"{currentScene} に対応する選択肢キャンバスが設定されていません。");
        }
    }

    // インタラクト終了処理（キャンバスを閉じ移動再開状態へ戻す）
    private void EndInteraction()
    {
        HideAllCanvases();
        isCanvasActive = false;
        OnInteract(false);
    }

    // すべてのキャンバスを非表示
    private void HideAllCanvases()
    {
        foreach (var data in sceneCanvases)
        {
            if (data.choiceCanvas != null)
            {
                data.choiceCanvas.gameObject.SetActive(false);
            }
        }
    }
}