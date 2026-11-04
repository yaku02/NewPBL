using UnityEngine;

public class DestinationSpawner : MonoBehaviour
{
    [Header("教授の家")]
    [SerializeField] private GameObject professorHouse;

    public bool SpawnProfessorHouse()
    {
        if (professorHouse == null)
        {
            Debug.LogError("教授の家のオブジェクトが設定されていません");
            return false;
        }

        // すでにONなら何もしない
        if (professorHouse.activeSelf)
        {
            Debug.Log("教授の家はすでに表示されています");
            return true;
        }

        // OFFからONにする
        professorHouse.SetActive(true);

        Debug.Log("教授の家を表示しました");
        return true;
    }
}