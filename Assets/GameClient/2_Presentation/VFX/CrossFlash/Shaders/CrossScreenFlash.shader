Shader "Game/VFX/CrossScreenFlash"
{
    Properties
    {
        // 基础调试属性（编辑器预览与材质常规模板）
        _ActiveFlashCount ("Active Flash Count", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "CrossScreenFlash"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One // HDR Additive

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_FLASH_COUNT 8

            CBUFFER_START(UnityPerMaterial)
                float _ActiveFlashCount;
                // xy: Viewport Center (0~1), z: Progress (0~1), w: Intensity
                float4 _FlashParams0[MAX_FLASH_COUNT];
                // x: BaseLineWidth, y: MinLineWidth, z: MaxLineWidth, w: WidthScaleRate
                float4 _FlashParams1[MAX_FLASH_COUNT];
                // x: LineSoftness, y: NearFadeDistance, z: AspectRatio (width/height), w: Unused
                float4 _FlashParams2[MAX_FLASH_COUNT];
                // rgb: Color, a: Unused
                float4 _FlashColors[MAX_FLASH_COUNT];
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                // 视口坐标归一化 (左下角为 0,0，右上角为 1,1，与 Camera.WorldToViewportPoint 严格保持物理对齐)
                output.uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                return output;
            }

            // 计算单个十字闪光的遮罩与颜色
            float EvaluateSingleCross(float2 uv, int index)
            {
                float4 p0 = _FlashParams0[index];
                float4 p1 = _FlashParams1[index];
                float4 p2 = _FlashParams2[index];

                float2 center = p0.xy;
                float progress = p0.z;

                float baseWidth = p1.x;
                float minWidth  = p1.y;
                float maxWidth  = p1.z;
                float scaleRate = p1.w;

                float softness  = p2.x;
                float nearFade  = p2.y;
                float aspect    = p2.z;

                // ----------------------------------------------------
                // 1. 横线 (沿 X 轴延伸，法向为 Y)
                // ----------------------------------------------------
                float d_parallel_H = abs(uv.x - center.x);
                float d_perp_H     = abs(uv.y - center.y) * aspect; // 宽高比校正，确保物理像素宽度一致

                float width_H = clamp(baseWidth + scaleRate * d_parallel_H, minWidth, maxWidth);
                float horizontal = 1.0 - smoothstep(width_H - softness, width_H + softness, d_perp_H);

                // 近端微衰减 (消除十字中心像素挤压)
                if (nearFade > 0.0001)
                {
                    horizontal *= smoothstep(0.0, nearFade, d_parallel_H);
                }

                // ----------------------------------------------------
                // 2. 竖线 (沿 Y 轴延伸，法向为 X)
                // ----------------------------------------------------
                float d_parallel_V = abs(uv.y - center.y);
                float d_perp_V     = abs(uv.x - center.x);

                float width_V = clamp(baseWidth + scaleRate * d_parallel_V, minWidth, maxWidth);
                float vertical = 1.0 - smoothstep(width_V - softness, width_V + softness, d_perp_V);

                if (nearFade > 0.0001)
                {
                    vertical *= smoothstep(0.0, nearFade, d_parallel_V);
                }

                // ----------------------------------------------------
                // 3. 严格等亮度合成 (消除中心高亮核心)
                // ----------------------------------------------------
                float lineMask = max(horizontal, vertical);

                // ----------------------------------------------------
                // 4. 闪光动画曲线 (前 12% 疾速暴亮，随后衰减至 0)
                // ----------------------------------------------------
                float flashCurve;
                if (progress < 0.12)
                {
                    flashCurve = smoothstep(0.0, 0.12, progress);
                }
                else
                {
                    flashCurve = 1.0 - smoothstep(0.12, 1.0, progress);
                }

                return lineMask * flashCurve;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_ActiveFlashCount <= 0.001)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 uv = input.uv;
                float3 totalColor = 0;

                int count = min((int)(_ActiveFlashCount + 0.5), MAX_FLASH_COUNT);
                for (int i = 0; i < count; i++)
                {
                    float mask = EvaluateSingleCross(uv, i);
                    if (mask > 0.0005)
                    {
                        float intensity = _FlashParams0[i].w;
                        float3 color = _FlashColors[i].rgb;
                        totalColor += color * (intensity * mask);
                    }
                }

                return half4(totalColor, 1.0);
            }

            ENDHLSL
        }
    }
    FallBack Off
}
