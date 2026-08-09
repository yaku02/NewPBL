using UnityEngine;

public interface IInteractable
{
    // インタラクトを受け付ける距離
    float InteractDistance { get; }

    // インタラクト実行（オン/オフのトグル状態を受け取る）
    void OnInteract(bool isInteracting);

    // オブジェクトのTransform（距離計算用）
    Transform transform { get; }
}