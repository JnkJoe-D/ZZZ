using UnityEngine;
using UnityEditor;
using System.IO;

public class TestLayerMaskJson : EditorWindow
{
    [MenuItem("Tools/Test LayerMask Json")]
    public static void RunTest()
    {
        var testObj = new LayerMaskTestClass();
        testObj.mask = LayerMask.GetMask("Water", "UI"); // Or whatever layers exist

        string json = JsonUtility.ToJson(testObj, true);
        Debug.Log("Serialized JSON: \n" + json);

        var deserializeObj = JsonUtility.FromJson<LayerMaskTestClass>(json);
        Debug.Log("Deserialized Mask Value: " + deserializeObj.mask.value);
    }
}

[System.Serializable]
public class LayerMaskTestClass
{
    public LayerMask mask;
}
