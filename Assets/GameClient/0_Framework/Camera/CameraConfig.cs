using UnityEngine;
namespace Game.Framework
{
[CreateAssetMenu(fileName = "NewCameraConfig", menuName = "Config/CameraConfig")]
public class CameraConfig : ScriptableObject
{
    public int id;
    public GameObject prefab;
}
}