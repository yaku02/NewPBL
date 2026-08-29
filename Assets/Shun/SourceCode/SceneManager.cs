using UnityEngine;
using System;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    [Header("プレイヤー参照")]
    [SerializeField] private Transform playerCamera; // 判定用のカメラ（頭）
    [SerializeField] private Transform buddyTransform; // バディのトランスフォーム
    [SerializeField] private Transform playerRig;    // ワープで移動させるプレイヤー根本（XR Origin等）

    public enum SceneType
    {
        Office,
        AzumaHouse,
        Laboratory
    }

    [Serializable]
    public struct SceneAreaData
    {
        public SceneType sceneType;
        public BoxCollider areaCollider;      // エリア全体の判定領域
        public BoxCollider exitCollider;      // このエリアから出る際の出口範囲
        public Transform entrancePoint;       // このエリアに入る際の入り口座標
        public Canvas warpCanvas;             // 出口エリアに入った時に表示するワープ用キャンバス
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
    }

    void Update()
    {
        CheckExitAreaAndToggleCanvas();
    }

    // 出口エリアに入っているかを常にチェックし、ワープキャンバスの描画を制御する
    private void CheckExitAreaAndToggleCanvas()
    {
        if (playerCamera == null) return;

        SceneAreaData currentAreaData = sceneAreas.Find(area => area.sceneType == currentScene);

        if (currentAreaData.exitCollider != null && currentAreaData.warpCanvas != null)
        {
            bool isInExitArea = currentAreaData.exitCollider.bounds.Contains(playerCamera.position);

            if (currentAreaData.warpCanvas.gameObject.activeSelf != isInExitArea)
            {
                currentAreaData.warpCanvas.gameObject.SetActive(isInExitArea);
            }
        }
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
        }
    }

    // 引数の nextScene の出口範囲内にいる場合、entrancePointへプレイヤーをワープ
    public void TransportNextScene(SceneType nextScene)
    {
        if (playerCamera == null) return;
        if (buddyTransform == null) return;

        // 移動対象（playerRig がなければ playerCamera を移動）
        Transform targetToMove = playerRig != null ? playerRig : playerCamera;

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
            // --- プレイヤーのワープ処理 ---
            targetToMove.position = nextAreaData.entrancePoint.position;
            targetToMove.rotation = nextAreaData.entrancePoint.rotation;

            // --- Buddy のワープ処理 ---
            // プレイヤーと完全に同じ位置・回転にする場合：
            buddyTransform.position = nextAreaData.entrancePoint.position;
            buddyTransform.rotation = nextAreaData.entrancePoint.rotation;

            // ワープキャンバスを非表示にする
            HideAllWarpCanvases();

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