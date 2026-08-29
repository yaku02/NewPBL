using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    // シーン内に1つだけ存在する参照を保持
    public static EvidenceManager Instance { get; private set; }

    [SerializeField] private List<EvidenceObject> collectedEvidences = new List<EvidenceObject>();
    public IReadOnlyList<EvidenceObject> CollectedEvidences => collectedEvidences;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEvidence(EvidenceObject evidence)
    {
        if (evidence != null && !collectedEvidences.Contains(evidence))
        {
            collectedEvidences.Add(evidence);
            Debug.Log($"証拠を追加しました: {evidence.name}");
        }
    }
}