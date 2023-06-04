using UnityEngine;

public class CameraMovementsCopier : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;

    void Update()
    {
        transform.localPosition = targetCamera.localPosition;
        transform.localRotation = targetCamera.localRotation;
    }
}
