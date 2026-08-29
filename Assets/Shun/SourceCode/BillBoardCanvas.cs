using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform targetCamera; // プレイヤーのカメラ

    [Header("回転設定")]
    [Tooltip("チェックを入れると上下の傾き（X軸回転）を無視して水平（Y軸のみ）回転します")]
    [SerializeField] private bool lockXRotation = true;

    void Start()
    {
        // 未設定の場合、MainCameraタグのついたカメラを自動検索
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // カメラの方を向くベクトルを計算
        Vector3 targetDirection = transform.position - targetCamera.position;

        // 上下の傾きを固定（Y軸回転のみ）する場合
        if (lockXRotation)
        {
            targetDirection.y = 0;
        }

        // ベクトルがゼロでない場合に回転を適用
        if (targetDirection != Vector3.zero)
        {
            // カメラの逆方向（キャンバスの表側）を向くように回転設定
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }
    }
}