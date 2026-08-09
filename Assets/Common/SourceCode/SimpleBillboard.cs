using UnityEngine;

public class CharacterBillboard : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Transform targetCamera; // プレイヤーのカメラを指定

    [Header("回転設定")]
    [SerializeField] private float rotationSpeed = 3.0f;

    private void Start()
    {
        // インスペクターで設定されていない場合、タグが MainCamera のオブジェクトを自動取得
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 targetPos = targetCamera.position;

        // 自分 → プレイヤー（カメラ）方向
        Vector3 direction = transform.position - targetPos;

        // Y軸だけ回転（上下は無視）
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookAtPlayerRotation = Quaternion.LookRotation(direction);
            Quaternion targetRotation = lookAtPlayerRotation * Quaternion.Euler(0, 180f, 0);

            // 現在の回転から目標の回転へ滑らかに近づける
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}