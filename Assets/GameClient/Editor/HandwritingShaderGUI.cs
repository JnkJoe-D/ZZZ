using UnityEditor;
using UnityEngine;

public class HandwritingShaderGUI : TMPro.EditorUtilities.TMP_SDFShaderGUI
{
    // 使用 static 字段，确保在检视面板重绘或重新渲染时保持展开折叠的状态
    private static bool m_FoldEdge = false;
    private static bool m_FoldWarp = false;
    private static bool m_FoldInk = false;
    private static bool m_FoldHoles = false;
    private static bool m_FoldGrad = false;
    private static bool m_FoldEmboss = false;
    private static bool m_FoldFlow = false;

    // 自定义绘制符合 TextMesh Pro 原生风格的带开关折叠标题面板
    private bool DrawHeaderPanel(string title, ref bool foldout, MaterialProperty toggleProperty)
    {
        // 申请一个标准的 20 像素高度的横条区域
        Rect rect = EditorGUILayout.GetControlRect(true, 20);
        
        // 使用 Unity 内置的 Shuriken 标题样式，完美还原 TMP 原生的暗色半圆角底色背景
        GUI.Box(rect, "", "ShurikenModuleTitle");

        // 细分位置：左侧复选框，中间标题，右侧状态文字
        Rect toggleRect = new Rect(rect.x + 5, rect.y + 2, 16, 16);
        Rect titleRect = new Rect(rect.x + 24, rect.y + 2, 250, 16);
        Rect labelRect = new Rect(rect.xMax - 145, rect.y + 2, 135, 16);

        // 1. 绘制左侧的复选开关
        if (toggleProperty != null)
        {
            EditorGUI.BeginChangeCheck();
            bool val = toggleProperty.floatValue > 0.5f;
            val = GUI.Toggle(toggleRect, val, "");
            if (EditorGUI.EndChangeCheck())
            {
                toggleProperty.floatValue = val ? 1.0f : 0.0f;
            }
        }

        // 2. 绘制标题文本（加粗）
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f, 1f) : Color.black }
        };
        GUI.Label(titleRect, title, titleStyle);

        // 3. 绘制右侧提示字 (如: - Click to collapse -)
        GUIStyle foldoutLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 0.7f) }
        };
        string clickLabel = foldout ? "- Click to collapse -" : "- Click to expand -";
        GUI.Label(labelRect, clickLabel, foldoutLabelStyle);

        // 4. 监听鼠标点击事件，点击标题横条其余部分切换展开/收起状态
        Rect clickRect = new Rect(rect.x + 22, rect.y, rect.width - 22, rect.height);
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && clickRect.Contains(currentEvent.mousePosition))
        {
            foldout = !foldout;
            currentEvent.Use();
            GUI.changed = true;
        }

        return foldout;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // 1. 绘制 TextMesh Pro 原生的面片面板、描边面板、阴影面板等
        base.OnGUI(materialEditor, properties);

        EditorGUILayout.Space(10);
        
        // 绘制一个非常醒目的专属艺术主分类标题
        GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 15, 5)
        };
        
        // 使用一个轻量横线分割线
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("▼ HANDWRITING EFFECTS (手写艺术效果专属配置)", sectionHeaderStyle);
        
        // 开启缩进以便与原版 TMP 默认分组样式对齐
        EditorGUI.indentLevel++;

        // ==================== 1. 边缘与毛刺 ====================
        MaterialProperty useEdge = FindProperty("_UseEdgeSpikes", properties, false);
        DrawHeaderPanel("Edge & Spikes (边缘与毛刺)", ref m_FoldEdge, useEdge);
        if (m_FoldEdge)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useEdge == null || useEdge.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty noiseScale = FindProperty("_NoiseScale", properties, false);
                MaterialProperty edgeDistortion = FindProperty("_EdgeDistortion", properties, false);
                MaterialProperty edgeBleed = FindProperty("_EdgeBleed", properties, false);
                MaterialProperty spikeScale = FindProperty("_SpikeScale", properties, false);
                MaterialProperty spikeDistortion = FindProperty("_SpikeDistortion", properties, false);
                MaterialProperty sharpen = FindProperty("_Sharpen", properties, false);

                if (noiseScale != null) materialEditor.ShaderProperty(noiseScale, "Edge Noise Scale (边缘扭曲缩放)");
                if (edgeDistortion != null) materialEditor.ShaderProperty(edgeDistortion, "Edge Distortion (边缘毛糙扭曲)");
                if (edgeBleed != null) materialEditor.ShaderProperty(edgeBleed, "Ink Bleed Softness (常规边缘缩放)");
                if (spikeScale != null) materialEditor.ShaderProperty(spikeScale, "Spike Scale (毛刺密度缩放)");
                if (spikeDistortion != null) materialEditor.ShaderProperty(spikeDistortion, "Spike Distortion (毛刺尖锐强度)");
                if (sharpen != null) materialEditor.ShaderProperty(sharpen, "Edge Sharpen (边缘锐化倍率)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 2. 整体形变扭曲 ====================
        MaterialProperty useWarp = FindProperty("_UseWarp", properties, false);
        DrawHeaderPanel("Warp Distortion (整体形变扭曲)", ref m_FoldWarp, useWarp);
        if (m_FoldWarp)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useWarp == null || useWarp.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty vertexWarpStrength = FindProperty("_VertexWarpStrength", properties, false);
                MaterialProperty vertexWarpScale = FindProperty("_VertexWarpScale", properties, false);
                MaterialProperty warpStrength = FindProperty("_WarpStrength", properties, false);
                MaterialProperty warpScale = FindProperty("_WarpScale", properties, false);

                if (vertexWarpStrength != null) materialEditor.ShaderProperty(vertexWarpStrength, "Vertex Warp Strength (顶点几何整体扭曲强度)");
                if (vertexWarpScale != null) materialEditor.ShaderProperty(vertexWarpScale, "Vertex Warp Scale (几何扭曲频率)");
                if (warpStrength != null) materialEditor.ShaderProperty(warpStrength, "Texture Warp Strength (纹理水墨扭曲强度)");
                if (warpScale != null) materialEditor.ShaderProperty(warpScale, "Texture Warp Scale (纹理扭曲频率)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 3. 水墨与晕染 ====================
        MaterialProperty useInk = FindProperty("_UseInk", properties, false);
        DrawHeaderPanel("Ink Wash & Bleeding (水墨与晕染)", ref m_FoldInk, useInk);
        if (m_FoldInk)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useInk == null || useInk.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty inkBleedDist = FindProperty("_InkBleedDist", properties, false);
                MaterialProperty inkBleedSoftness = FindProperty("_InkBleedSoftness", properties, false);
                MaterialProperty inkBleedOpacity = FindProperty("_InkBleedOpacity", properties, false);
                MaterialProperty inkWashColor = FindProperty("_InkWashColor", properties, false);
                MaterialProperty watercolorEdge = FindProperty("_WatercolorEdge", properties, false);

                if (inkBleedDist != null) materialEditor.ShaderProperty(inkBleedDist, "Ink Bleed Distance (水墨晕染宽度)");
                if (inkBleedSoftness != null) materialEditor.ShaderProperty(inkBleedSoftness, "Bleed Softness (晕染边缘虚化)");
                if (inkBleedOpacity != null) materialEditor.ShaderProperty(inkBleedOpacity, "Bleed Opacity (晕染透明度)");
                if (inkWashColor != null) materialEditor.ColorProperty(inkWashColor, "Ink Wash Color (外圈晕染淡墨颜色)");
                if (watercolorEdge != null) materialEditor.ShaderProperty(watercolorEdge, "Watercolor Edge (水彩边沿沉积加深)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 4. 斑驳与空洞 ====================
        MaterialProperty useHoles = FindProperty("_UseHoles", properties, false);
        DrawHeaderPanel("Holes & Grains (斑驳与空洞)", ref m_FoldHoles, useHoles);
        if (m_FoldHoles)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useHoles == null || useHoles.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty holeIntensity = FindProperty("_HoleIntensity", properties, false);
                MaterialProperty holeDensity = FindProperty("_HoleDensity", properties, false);
                MaterialProperty holeScaleX = FindProperty("_HoleScaleX", properties, false);
                MaterialProperty holeScaleY = FindProperty("_HoleScaleY", properties, false);

                if (holeIntensity != null) materialEditor.ShaderProperty(holeIntensity, "Hole Intensity (空洞明显程度)");
                if (holeDensity != null) materialEditor.ShaderProperty(holeDensity, "Hole Density (空洞密集程度)");
                if (holeScaleX != null) materialEditor.ShaderProperty(holeScaleX, "Hole Scale X (横向缩放)");
                if (holeScaleY != null) materialEditor.ShaderProperty(holeScaleY, "Hole Scale Y (纵向缩放)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 5. 渐变与金箔 ====================
        MaterialProperty useGradGold = FindProperty("_UseGradGold", properties, false);
        DrawHeaderPanel("Gradient & Gold Foil (渐变与金箔)", ref m_FoldGrad, useGradGold);
        if (m_FoldGrad)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useGradGold == null || useGradGold.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty useGradient = FindProperty("_UseGradient", properties, false);
                MaterialProperty gradientColor = FindProperty("_GradientColor", properties, false);
                MaterialProperty gradDirType = FindProperty("_GradientDirectionType", properties, false);
                MaterialProperty gradientAngle = FindProperty("_GradientAngle", properties, false);
                MaterialProperty gradCenter = FindProperty("_GradCenter", properties, false);
                MaterialProperty gradWidth = FindProperty("_GradWidth", properties, false);

                MaterialProperty goldFoil = FindProperty("_GoldFoil", properties, false);
                MaterialProperty goldColor = FindProperty("_GoldColor", properties, false);
                MaterialProperty goldDensity = FindProperty("_GoldDensity", properties, false);

                if (useGradient != null) materialEditor.ShaderProperty(useGradient, "Use Gradient (启用颜色渐变)");
                
                if (useGradient != null && useGradient.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    if (gradientColor != null) materialEditor.ColorProperty(gradientColor, "Gradient Color (渐变目标颜色)");
                    
                    if (gradDirType != null)
                    {
                        string[] options = { "Horizontal (左右横向)", "Vertical (上下纵向)", "Angle (自定义角度)" };
                        int selected = (int)gradDirType.floatValue;
                        selected = EditorGUILayout.Popup("   Gradient Direction (渐变方向)", selected, options);
                        gradDirType.floatValue = selected;
                        
                        // 仅在选择 Angle (自定义角度) 时展示角度滑块
                        if (selected == 2 && gradientAngle != null)
                        {
                            materialEditor.ShaderProperty(gradientAngle, "      Gradient Angle (渐变角度)");
                        }
                    }

                    if (gradCenter != null) materialEditor.ShaderProperty(gradCenter, "   Gradient Center (渐变中心位移)");
                    if (gradWidth != null) materialEditor.ShaderProperty(gradWidth, "   Gradient Width (渐变过渡总宽度)");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);
                if (goldFoil != null) materialEditor.ShaderProperty(goldFoil, "Gold Foil Strength (金箔效果强度)");
                if (goldFoil != null && goldFoil.floatValue > 0f)
                {
                    EditorGUI.indentLevel++;
                    if (goldColor != null) materialEditor.ColorProperty(goldColor, "Gold Foil Color (金箔颜色)");
                    if (goldDensity != null) materialEditor.ShaderProperty(goldDensity, "Gold Density (金箔密度)");
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 6. 3D 压印与浮雕 ====================
        MaterialProperty useEmboss = FindProperty("_UseEmboss", properties, false);
        DrawHeaderPanel("3D Emboss & Deboss (3D 压印与浮雕)", ref m_FoldEmboss, useEmboss);
        if (m_FoldEmboss)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useEmboss == null || useEmboss.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty embossStrength = FindProperty("_EmbossStrength", properties, false);
                MaterialProperty embossHeight = FindProperty("_EmbossHeight", properties, false);
                MaterialProperty lightAngle = FindProperty("_LightAngle", properties, false);
                MaterialProperty lightDepth = FindProperty("_LightDepth", properties, false);
                MaterialProperty specularPower = FindProperty("_SpecularPower", properties, false);

                if (embossStrength != null) materialEditor.ShaderProperty(embossStrength, "Emboss/Deboss Strength (浮雕凹陷强度 - 正为浮雕负为凹印)");
                if (embossHeight != null) materialEditor.ShaderProperty(embossHeight, "Bevel Slope (边缘斜坡厚度)");
                if (lightAngle != null) materialEditor.ShaderProperty(lightAngle, "Light Direction Angle (光源投影角度)");
                if (lightDepth != null) materialEditor.ShaderProperty(lightDepth, "Light Height (光源高度)");
                if (specularPower != null) materialEditor.ShaderProperty(specularPower, "Specular Highlight (高光亮度)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // ==================== 7. 动态液态流动 ====================
        MaterialProperty useFlow = FindProperty("_UseFlow", properties, false);
        DrawHeaderPanel("Dynamic Flow (动态液态流动)", ref m_FoldFlow, useFlow);
        if (m_FoldFlow)
        {
            EditorGUILayout.Space(3);
            bool isEnabled = useFlow == null || useFlow.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isEnabled);
            {
                MaterialProperty flowSpeed = FindProperty("_FlowSpeed", properties, false);
                if (flowSpeed != null) materialEditor.ShaderProperty(flowSpeed, "Flow Speed (流动呼吸速度)");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }

        // 还原缩进
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }
}
