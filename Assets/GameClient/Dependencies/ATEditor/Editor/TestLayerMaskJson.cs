using ATEditor;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TestLayerMaskJson : EditorWindow
{
    [MenuItem("Tools/Test LayerMask Json")]
    public static void RunTest()
    {
        var testObj = new LayerMaskTestClass();
        testObj.mask = LayerMask.GetMask("Water", "UI"); // 测试用已存在的图层

        string json = JsonUtility.ToJson(testObj, true);
        ATLog.Info("Serialized JSON: \n" + json);

        var deserializeObj = JsonUtility.FromJson<LayerMaskTestClass>(json);
        ATLog.Info("Deserialized Mask Value: " + deserializeObj.mask.value);
    }
}

[System.Serializable]
public class LayerMaskTestClass
{
    public LayerMask mask;
}
