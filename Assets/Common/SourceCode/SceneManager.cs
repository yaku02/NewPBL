using UnityEngine;
using System;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    [Header("プレイヤー参照")]
    [SerializeField] private Transform playerCamera; // 判定用のカメラ（頭）
    [SerializeField] private Transform buddyTransform; // バディのトランスフォーム
    [SerializeField] private Transform playerRig;    // ワープで移動させるプレイヤー根本（XR Origin等）

    [Header("バディ参照")]
    [SerializeField] private Buddy2 buddy; // バディのスクリプト参照

    [Header("スタート位置設定")]
    [Tooltip("ゲーム開始時にプレイヤーを配置する初期位置（未設定の場合は移動しません）")]
    [SerializeField] private Transform startPoint;

    public enum SceneType
    {
        Office,
        AzumaHouse,
        Laboratory
    }

    // ★追加: 証拠品と消費フラグのペア用構造体
    [Serializable]
    public struct RequiredEvidenceData
    {
        [Tooltip("必要となる証拠品")]
        public EvidenceObject evidence;

        [Tooltip("チェックを入れると、ワープ成功時にこの証拠品をインベントリから削除します")]
        public bool consumeOnWarp;
    }

    [Serializable]
    public struct SceneAreaData
    {
        public SceneType sceneType;
        public BoxCollider areaCollider;      // エリア全体の判定領域
        public BoxCollider exitCollider;      // このエリアから出る際の出口範囲
        public Transform entrancePoint;       // このエリアに入る際の入り口座標
        public Canvas warpCanvas;              // 出口エリアに入った時に表示するワープ用キャンバス

        [Header("条件分岐用設定")]
        [Tooltip("ここに設定したすべての証拠品を所持している必要がある。空の場合は無条件で通常キャンバス")]
        public List<RequiredEvidenceData> requiredEvidences; // ★個別にフラグを持てるリストに変更

        [Tooltip("条件（証拠品の所持）を満たしていない場合に代わりに表示するキャンバス")]
        public Canvas dummyWarpCanvas;
    }

    [Header("エリア判定設定")]
    [SerializeField] private List<SceneAreaData> sceneAreas = new List<SceneAreaData>();

    public SceneType currentScene;

    void Start()
    {
        currentScene = SceneType.Office;

        // 未設定時の自動割り当て（playerRig が未設定なら自身かカメラの親にする）
        if (playerRig == null && playerCamera != null)
        {
            playerRig = playerCamera.parent != null ? playerCamera.parent : playerCamera;
        }

        // 開始時はすべてのワープキャンバスを非表示にしておく
        HideAllWarpCanvases();

        // ゲームスタート時に指定位置へワープさせる（バディは連れて行かない例）
        if (startPoint != null)
        {
            WarpPlayerOnly(startPoint);
        }
    }

    void Update()
    {
        CheckExitAreaAndToggleCanvas();
    }

    // ==========================================
    // ■ ワープ用の公開メソッド（ボタン割り当て可能）
    // ==========================================

    // 1. 【バディを連れて】指定した Transform の位置・回転へワープする
    public void WarpWithBuddy(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("WarpWithBuddy: ターゲットの Transform が設定されていません。");
            return;
        }

        // プレイヤーの移動
        Transform targetToMove = playerRig != null ? playerRig : playerCamera;
        if (targetToMove != null)
        {
            targetToMove.position = targetTransform.position;
            targetToMove.rotation = targetTransform.rotation;
        }

        // バディの移動
        if (buddy != null)
        {
            buddy.WarpTo(targetTransform.position, targetTransform.rotation);
        }

        HideAllWarpCanvases();
    }

    // 2. 【プレイヤーのみ】指定した Transform の位置・回転へワープする（バディは動かさない）
    public void WarpPlayerOnly(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("WarpPlayerOnly: ターゲットの Transform が設定されていません。");
            return;
        }

        // プレイヤーの移動のみ
        Transform targetToMove = playerRig != null ? playerRig : playerCamera;
        if (targetToMove != null)
        {
            targetToMove.position = targetTransform.position;
            targetToMove.rotation = targetTransform.rotation;
        }

        HideAllWarpCanvases();
    }


    // ==========================================
    // ■ エリア・シーン管理用の内部処理
    // ==========================================

    // 出口エリアに入っているかを常にチェックし、ワープキャンバスの描画を制御する
    private void CheckExitAreaAndToggleCanvas()
    {
        if (playerCamera == null) return;

        SceneAreaData currentAreaData = sceneAreas.Find(area => area.sceneType == currentScene);

        if (currentAreaData.exitCollider != null)
        {
            bool isInExitArea = currentAreaData.exitCollider.bounds.Contains(playerCamera.position);

            // 必要な証拠品を持っているか判定
            bool hasRequiredEvidence = HasRequiredEvidences(currentAreaData.requiredEvidences);

            // 表示すべきキャンバスと表示しないキャンバスを決定
            Canvas showCanvas = (isInExitArea && hasRequiredEvidence) ? currentAreaData.warpCanvas : null;
            Canvas dummyCanvas = (isInExitArea && !hasRequiredEvidence) ? currentAreaData.dummyWarpCanvas : null;

            // warpCanvas の表示切り替え
            if (currentAreaData.warpCanvas != null)
            {
                bool shouldActive = (currentAreaData.warpCanvas == showCanvas);
                if (currentAreaData.warpCanvas.gameObject.activeSelf != shouldActive)
                {
                    currentAreaData.warpCanvas.gameObject.SetActive(shouldActive);
                }
            }

            // dummyWarpCanvas の表示切り替え
            if (currentAreaData.dummyWarpCanvas != null)
            {
                bool shouldActive = (currentAreaData.dummyWarpCanvas == dummyCanvas);
                if (currentAreaData.dummyWarpCanvas.gameObject.activeSelf != shouldActive)
                {
                    currentAreaData.dummyWarpCanvas.gameObject.SetActive(shouldActive);
                }
            }
        }
    }

    private bool HasRequiredEvidences(List<RequiredEvidenceData> requiredList)
    {
        // 証拠品が設定されていない（または要素数0）場合は「条件なし（所持している扱い）」とする
        if (requiredList == null || requiredList.Count == 0) return true;

        if (EvidenceManager.Instance != null)
        {
            // 設定されている証拠品をリスト化してチェック
            List<EvidenceObject> checkList = new List<EvidenceObject>();
            foreach (var item in requiredList)
            {
                if (item.evidence != null)
                {
                    checkList.Add(item.evidence);
                }
            }

            return EvidenceManager.Instance.HasAllEvidences(checkList);
        }

        return false;
    }

    // すべてのワープ用キャンバスを非表示にする
    private void HideAllWarpCanvases()
    {
        foreach (var area in sceneAreas)
        {
            if (area.warpCanvas != null)
            {
                area.warpCanvas.gameObject.SetActive(false);
            }
            if (area.dummyWarpCanvas != null)
            {
                area.dummyWarpCanvas.gameObject.SetActive(false);
            }
        }
    }

    // 引数の nextScene の出口範囲内にいる場合、entrancePointへプレイヤーとバディをワープ
    public void TransportNextScene(SceneType nextScene)
    {
        if (playerCamera == null) return;
        if (buddyTransform == null) return;

        SceneAreaData currentAreaData = sceneAreas.Find(area => area.sceneType == currentScene);
        SceneAreaData nextAreaData = sceneAreas.Find(area => area.sceneType == nextScene);

        if (currentAreaData.exitCollider == null)
        {
            Debug.LogWarning($"{currentScene} の出口 (exitCollider) が設定されていません");
            return;
        }

        if (nextAreaData.entrancePoint == null)
        {
            Debug.LogWarning($"{nextScene} の入り口 (entrancePoint) が設定されていません");
            return;
        }

        // プレイヤーが出口エリア内にいるか判定
        if (currentAreaData.exitCollider.bounds.Contains(playerCamera.position))
        {
            // 条件を満たしているか再確認（満たしていない場合はワープさせない）
            if (!HasRequiredEvidences(currentAreaData.requiredEvidences))
            {
                Debug.LogWarning($"必要な証拠品を持っていないため、ワープできませんでした。");
                return;
            }

            // ★追加: 個別フラグ（consumeOnWarp）がオンになっている証拠品のみを抽出して削除
            if (currentAreaData.requiredEvidences != null && EvidenceManager.Instance != null)
            {
                List<EvidenceObject> evidencesToErase = new List<EvidenceObject>();
                foreach (var item in currentAreaData.requiredEvidences)
                {
                    if (item.consumeOnWarp && item.evidence != null)
                    {
                        evidencesToErase.Add(item.evidence);
                    }
                }

                if (evidencesToErase.Count > 0)
                {
                    EvidenceManager.Instance.EraseEvidences(evidencesToErase);
                    Debug.Log($"{evidencesToErase.Count} 個の証拠品を消費（削除）しました。");
                }
            }

            // シーン間の移動時はバディを連れてワープする
            WarpWithBuddy(nextAreaData.entrancePoint);

            // 現在のシーンを更新
            currentScene = nextScene;
            Debug.Log($"{nextScene} の入り口 ({nextAreaData.entrancePoint.position}) へワープしました！");
        }
        else
        {
            Debug.LogWarning($"プレイヤーが出口範囲外にいるため、ワープできませんでした");
        }
    }

    // --- ボタン (OnClick) 用関数 ---
    public void TransportToAzumaHouse() => TransportNextScene(SceneType.AzumaHouse);
    public void TransportToOffice() => TransportNextScene(SceneType.Office);
    public void TransportToLaboratory() => TransportNextScene(SceneType.Laboratory);
}