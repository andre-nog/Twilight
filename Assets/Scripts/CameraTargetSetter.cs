using UnityEngine;
using Unity.Netcode;

public class CameraTargetSetter : NetworkBehaviour
{
    void Start()
    {
        if (!IsOwner) return;

        var camera = Camera.main;
        if (camera != null && camera.TryGetComponent(out CameraController camController))
        {
            camController.target = transform;
        }
    }
}
