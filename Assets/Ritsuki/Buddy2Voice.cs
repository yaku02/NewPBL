
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Buddy2Voice : MonoBehaviour, IInteractable
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private SceneManager sceneManager;

    [System.Serializable]
    public struct SceneCanvasData
    {
        public SceneManager.SceneType sceneType;
        public Canvas choiceCanvas;
    }

    [Header("シーンごとの選択肢キャンバス設定")]
    [SerializeField]
    private List<SceneCanvasData> sceneCanvases =
        new List<SceneCanvasData>();

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("音声で呼ぶ設定")]
    [SerializeField] private float callDistance = 1.5f;

    [Header("インタラクト設定")]
    [SerializeField] private float interactDistance = 3.0f;
    [SerializeField] private float lookAtSpeed = 8.0f;

    private Rigidbody rb;

    private Vector3 currentTargetPos;
    private bool isInteracting = false;
    private bool isCanvasActive = false;
    private bool isCalling = false;

    // OpenAIRealtimeからAIの移動命令を受け取る
    private OpenAIRealtime openAIRealtime;

    public float InteractDistance => interactDistance;

    private void OnEnable()
    {
        SubscribeToAI();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 起動時は現在位置を目標に設定し、その場で停止する
        currentTargetPos = rb.position;

        HideAllCanvases();

        // OpenAIRealtimeの起動順序に備えて購読を再確認
        SubscribeToAI();
    }

    private void OnDisable()
    {
        UnsubscribeFromAI();
    }

    // OpenAIRealtimeのイベントを購読する
    private void SubscribeToAI()
    {
        OpenAIRealtime found =
            FindFirstObjectByType<OpenAIRealtime>();

        if (found == null)
        {
            return;
        }

        if (openAIRealtime != null && openAIRealtime != found)
        {
            openAIRealtime.OnComeToPlayerRequested -= CallToPlayer;
        }

        openAIRealtime = found;

        // イベントの二重登録を防止
        openAIRealtime.OnComeToPlayerRequested -= CallToPlayer;
        openAIRealtime.OnComeToPlayerRequested += CallToPlayer;

        Debug.Log("Buddy2Voice：AIの移動命令イベントを購読しました。");
    }

    // イベント購読を解除する
    private void UnsubscribeFromAI()
    {
        if (openAIRealtime != null)
        {
            openAIRealtime.OnComeToPlayerRequested -= CallToPlayer;
            openAIRealtime = null;
        }
    }

    private void Update()
    {
        // Fキーで選択肢キャンバスやインタラクトを切り替える
        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleCurrentSceneCanvas();
        }

        // 自動巡回処理は行わない
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        // インタラクト中は移動せず、プレイヤーの方向を向く
        if (isInteracting)
        {
            if (playerCamera != null)
            {
                RotateBillboardTowardsPlayer();
            }

            return;
        }

        // 呼ばれていないときは、その場で停止する
        if (!isCalling)
        {
            return;
        }

        // 呼ばれたときだけ目標位置に向かって移動する
        float distanceToTarget =
            Vector3.Distance(rb.position, currentTargetPos);

        if (distanceToTarget > 0.05f)
        {
            Vector3 nextPos = Vector3.MoveTowards(
                rb.position,
                currentTargetPos,
                moveSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(nextPos);

            Vector3 direction = currentTargetPos - rb.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                Quaternion nextRotation = Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    5.0f * Time.fixedDeltaTime
                );

                rb.MoveRotation(nextRotation);
            }
        }
        else
        {
            // 目標位置に到着したら停止する
            isCalling = false;

            Debug.Log(
                "Buddy2Voice：プレイヤーの近くに到着しました。"
            );
        }
    }

    private void CallToPlayer()
    {
        Debug.Log("【移動】CallToPlayerが呼ばれました。");

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "Buddy2Voice：Player Cameraが設定されていません。"
            );
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (isInteracting || isCanvasActive)
        {
            EndInteraction();
        }

        // 相棒からプレイヤーへ向かう方向を計算
        Vector3 direction = playerCamera.position - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            Debug.Log("Buddy2Voice：すでにプレイヤーの近くにいます。");
            return;
        }

        direction.Normalize();

        // プレイヤーから callDistance だけ離れた地点を目標にする
        Vector3 targetPosition = playerCamera.position - direction * callDistance;

        currentTargetPos = new Vector3(
            targetPosition.x,
            rb.position.y,
            targetPosition.z
        );

        isCalling = true;

        Debug.Log(
            $"Buddy2Voice：プレイヤーに近づきます。目標位置={currentTargetPos}"
        );
    }

    // プレイヤーの方向を向く処理
    private void RotateBillboardTowardsPlayer()
    {
        Vector3 direction =
            transform.position - playerCamera.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookAtPlayerRotation =
                Quaternion.LookRotation(direction);

            Quaternion targetRotation =
                lookAtPlayerRotation * Quaternion.Euler(0, 180f, 0);

            Quaternion nextRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                lookAtSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(nextRotation);
        }
    }

    // Fキーで現在のシーンの選択肢キャンバスを切り替える
    private void ToggleCurrentSceneCanvas()
    {
        if (sceneManager == null ||
            sceneCanvases == null ||
            sceneCanvases.Count == 0)
        {
            OnInteract(!isInteracting);
            return;
        }

        if (isCanvasActive)
        {
            EndInteraction();
            return;
        }

        SceneManager.SceneType currentScene =
            sceneManager.currentScene;

        SceneCanvasData targetData =
            sceneCanvases.Find(
                data => data.sceneType == currentScene
            );

        if (targetData.choiceCanvas != null)
        {
            HideAllCanvases();

            targetData.choiceCanvas.gameObject.SetActive(true);
            isCanvasActive = true;

            OnInteract(true);
        }
        else
        {
            OnInteract(!isInteracting);
        }
    }

    // インタラクト終了処理
    private void EndInteraction()
    {
        HideAllCanvases();
        isCanvasActive = false;
        OnInteract(false);
    }

    // すべての選択肢キャンバスを非表示にする
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

    public void OnInteract(bool interacting)
    {
        isInteracting = interacting;

        if (SEManager.Instance != null)
        {
            SEManager.Instance.PlayTalkBuddySound();
        }
    }

    // 指定された位置・回転にワープさせる
    public void WarpTo(Vector3 newPosition, Quaternion newRotation)
    {
        EndInteraction();

        isCalling = false;

        transform.position = newPosition;
        transform.rotation = newRotation;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.position = newPosition;
        rb.rotation = newRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ワープ先でも自動巡回は開始しない
        currentTargetPos = newPosition;
    }
}
