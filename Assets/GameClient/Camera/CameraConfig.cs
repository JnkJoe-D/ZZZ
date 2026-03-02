using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
namespace Game.Camera{
[CreateAssetMenu(fileName = "NewCameraConfig", menuName = "Config/CameraConfig")]
public class CameraConfig : ScriptableObject
{
    public int id;
    public GameObject prefab;
}
}