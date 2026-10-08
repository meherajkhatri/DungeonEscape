using UnityEngine;
namespace DungeonEscape
{
    [RequireComponent(typeof(Camera))]
    public class FitDungeonCamera : MonoBehaviour
    {
        void LateUpdate() { var camera = GetComponent<Camera>(); camera.orthographicSize = Mathf.Max(11.7f, 16.4f / camera.aspect); }
    }
}
