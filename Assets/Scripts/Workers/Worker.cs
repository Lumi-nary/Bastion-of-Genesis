using UnityEngine;

public class Worker : MonoBehaviour
{
    [Header("Worker Data")]
    [SerializeField] private WorkerData workerData;

    public WorkerData WorkerData => workerData;
}
