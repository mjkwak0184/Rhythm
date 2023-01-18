/*
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRenderFPS: MonoBehaviour
{
    public float FPS = 30f;
    private bool shouldUpdate = true;

    private Camera camera;
    // Use this for initialization
    void Start () {
        camera = GetComponent<Camera>();
        camera.enabled = false;
    }

    void Update()
    {
        if(shouldUpdate){
            camera.Render();
            shouldUpdate = false;
        }else{
            shouldUpdate = true;
        }
    }
}
*/