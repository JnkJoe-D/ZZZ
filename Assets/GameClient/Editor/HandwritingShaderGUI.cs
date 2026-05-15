using UnityEditor;
using UnityEngine;

public class HandwritingShaderGUI : TMPro.EditorUtilities.TMP_SDFShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Handwriting Effects (手写艺术效果)", EditorStyles.boldLabel);

        // Find our custom properties
        MaterialProperty noiseScale = FindProperty("_NoiseScale", properties, false);
        MaterialProperty edgeDistortion = FindProperty("_EdgeDistortion", properties, false);
        MaterialProperty edgeBleed = FindProperty("_EdgeBleed", properties, false);
        MaterialProperty watercolorEdge = FindProperty("_WatercolorEdge", properties, false);

        MaterialProperty holeIntensity = FindProperty("_HoleIntensity", properties, false);
        MaterialProperty holeDensity = FindProperty("_HoleDensity", properties, false);
        MaterialProperty holeScaleX = FindProperty("_HoleScaleX", properties, false);
        MaterialProperty holeScaleY = FindProperty("_HoleScaleY", properties, false);

        MaterialProperty spikeScale = FindProperty("_SpikeScale", properties, false);
        MaterialProperty spikeDistortion = FindProperty("_SpikeDistortion", properties, false);
        MaterialProperty sharpen = FindProperty("_Sharpen", properties, false);

        EditorGUI.BeginChangeCheck();

        if (noiseScale != null) materialEditor.ShaderProperty(noiseScale, "Edge Noise Scale (边缘扭曲缩放)");
        if (edgeDistortion != null) materialEditor.ShaderProperty(edgeDistortion, "Edge Distortion (边缘毛糙扭曲)");
        if (edgeBleed != null) materialEditor.ShaderProperty(edgeBleed, "Ink Bleed (墨水晕染变虚)");
        if (watercolorEdge != null) materialEditor.ShaderProperty(watercolorEdge, "Watercolor Edge (水彩边缘加深)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spikes & Sharpen (毛刺与锐化)", EditorStyles.boldLabel);
        if (spikeScale != null) materialEditor.ShaderProperty(spikeScale, "Spike Scale (毛刺密度缩放)");
        if (spikeDistortion != null) materialEditor.ShaderProperty(spikeDistortion, "Spike Distortion (毛刺尖锐强度)");
        if (sharpen != null) materialEditor.ShaderProperty(sharpen, "Edge Sharpen (边缘锐化倍率)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Holes & Grains (斑驳与空洞)", EditorStyles.boldLabel);
        
        if (holeIntensity != null) materialEditor.ShaderProperty(holeIntensity, "Hole Intensity (空洞明显程度)");
        if (holeDensity != null) materialEditor.ShaderProperty(holeDensity, "Hole Density (空洞密集程度)");
        if (holeScaleX != null) materialEditor.ShaderProperty(holeScaleX, "Hole Scale X (横向缩放)");
        if (holeScaleY != null) materialEditor.ShaderProperty(holeScaleY, "Hole Scale Y (纵向缩放)");

        if (EditorGUI.EndChangeCheck())
        {
            // Apply changes
        }
    }
}
