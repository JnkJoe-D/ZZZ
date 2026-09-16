Shader "UI/ZZZ_HealthBarFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Feature Toggles)]
        [Toggle(_USE_WAVE_EDGE)] _UseWaveEdge ("Enable Wave Edge", Float) = 1
        [Toggle(_USE_FLOW_LIGHT)] _UseFlowLight ("Enable Flow Light", Float) = 1
        [Toggle(_USE_FLASH)] _UseFlash ("Enable Flashing Feature (Material)", Float) = 0

        [Header(Health Fill Settings)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.8
        _EdgeSlant ("Edge Slant (Skew)", Range(-2.0, 2.0)) = -0.3
        [HDR] _EdgeColor ("Edge Color", Color) = (0.5, 1.0, 0.8, 1.0)
        _EdgeWidth ("Edge Glow Width", Range(0.001, 0.1)) = 0.02
        _WaveSpeed ("Wave Speed", Range(0.0, 50.0)) = 15.0
        _WaveFreq ("Wave Frequency", Range(0.0, 50.0)) = 20.0
        _WaveAmp ("Wave Amplitude", Range(0.0, 0.1)) = 0.01

        [Header(Flow Light Settings)]
        [HDR] _FlowColor ("Flow Color", Color) = (1, 1, 1, 0.5)
        _FlowSpeed ("Flow Speed", Range(0.1, 5.0)) = 1.0
        _FlowWidth ("Flow Width", Range(0.01, 1.0)) = 0.2
        _FlowAngle ("Flow Angle", Range(-1.57, 1.57)) = 0.5
        _FlowInterval ("Interval (Time between flows)", Range(0.0, 5.0)) = 1.0

        [Header(Flash Settings)]
        [Toggle] _FlashActive ("Preview Flash Active (0=Gray, 1=Flash)", Float) = 1
        [HDR] _FlashColor1 ("Flash Color 1", Color) = (1, 1, 1, 1)
        [HDR] _FlashColor2 ("Flash Color 2", Color) = (1, 0.5, 0, 1)
        [HDR] _DisabledColor ("Disabled Color", Color) = (0.5, 0.5, 0.5, 1)
        _FlashSpeed ("Flash Speed", Range(0.1, 20.0)) = 5.0

        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #pragma shader_feature_local _USE_WAVE_EDGE
            #pragma shader_feature_local _USE_FLOW_LIGHT
            #pragma shader_feature_local _USE_FLASH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float4 worldPosition: TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ClipRect;

            // Fill & Wave Properties
            float _FillAmount;
            float _EdgeSlant;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _WaveSpeed;
            float _WaveFreq;
            float _WaveAmp;

            // Flow Properties
            float4 _FlowColor;
            float _FlowSpeed;
            float _FlowWidth;
            float _FlowAngle;
            float _FlowInterval;

            // Flash Properties
            float _FlashActive; // C# 动态传入的开关状态
            float4 _FlashColor1;
            float4 _FlashColor2;
            float4 _DisabledColor;
            float _FlashSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Base UI Texture
                half4 color = tex2D(_MainTex, input.uv) * input.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // 2. FLASH EFFECT
                #ifdef _USE_FLASH
                    // _FlashActive 由 C# 实时传递：1 为满足阈值(闪烁)，0 为未满足(变灰)
                    if (_FlashActive > 0.5)
                    {
                        // Generate a ping-pong value between 0 and 1
                        float flashPingPong = (sin(_Time.y * _FlashSpeed) * 0.5) + 0.5;
                        half4 flashTint = lerp(_FlashColor1, _FlashColor2, flashPingPong);
                        // Multiply base color with flash tint
                        color.rgb *= flashTint.rgb;
                    }
                    else
                    {
                        color.rgb *= _DisabledColor.rgb;
                    }
                #endif

                // 3. FILL & WAVE EDGE LOGIC
                float slantOffset = abs(0.5 * _EdgeSlant);
                float skewedX = input.uv.x + (input.uv.y - 0.5) * _EdgeSlant;
                
                #ifdef _USE_WAVE_EDGE
                    // Remap Fill Amount so 0% and 100% perfectly hide/show the sloped bar
                    float minEdge = -slantOffset - _WaveAmp;
                    float maxEdge = 1.0 + slantOffset + _WaveAmp;
                    float mappedFill = lerp(minEdge, maxEdge, _FillAmount);

                    // Dampen wave near the extremes so it perfectly fits the bounding box when full/empty
                    float waveDamp = smoothstep(1.0, 0.99, _FillAmount) * smoothstep(0.0, 0.01, _FillAmount);
                    
                    // Wave offset
                    float waveOffset = sin(input.uv.y * _WaveFreq + _Time.y * _WaveSpeed) * _WaveAmp * waveDamp;
                    float currentFillEdge = mappedFill + waveOffset;
                    
                    // Clip (discard) pixels beyond the skewed fill edge
                    clip(currentFillEdge - skewedX);
                    
                    // Calculate distance to the slanted edge for glow
                    float distToEdge = currentFillEdge - skewedX;
                    
                    float edgeGlow = smoothstep(_EdgeWidth, 0.0, distToEdge);
                    float edgeGlow2 = smoothstep(_EdgeWidth * 2.5, 0.0, distToEdge) * 0.3;
                    
                    float finalEdge = (edgeGlow + edgeGlow2) * color.a * waveDamp;
                    color.rgb += _EdgeColor.rgb * finalEdge * _EdgeColor.a;
                #else
                    // Basic Slanted Fill Clip (No Wave/Glow)
                    float minEdgeSimple = -slantOffset;
                    float maxEdgeSimple = 1.0 + slantOffset;
                    float mappedFillSimple = lerp(minEdgeSimple, maxEdgeSimple, _FillAmount);
                    clip(mappedFillSimple - skewedX);
                #endif

                // 4. FLOW LIGHT LOGIC
                #ifdef _USE_FLOW_LIGHT
                    float s, c;
                    sincos(_FlowAngle, s, c);
                    float rotatedX = input.uv.x * c - input.uv.y * s;

                    float totalCycleTime = 1.0 + _FlowInterval;
                    float timeProgress = fmod(_Time.y * _FlowSpeed, totalCycleTime);

                    float currentPos = (timeProgress * 3.0) - 1.0; 
                    float dist = abs(rotatedX - currentPos);
                    
                    float flowIntensity = 1.0 - smoothstep(0.0, _FlowWidth, dist);
                    float coreIntensity = 1.0 - smoothstep(0.0, _FlowWidth * 0.2, dist);
                    flowIntensity = (flowIntensity * 0.5 + coreIntensity * 0.5) * color.a;

                    color.rgb += _FlowColor.rgb * flowIntensity * _FlowColor.a;
                #endif

                // 5. UI Clip Rect Masking (Standard Unity UI Masking)
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipFactor = step(_ClipRect.xy, input.worldPosition.xy) * step(input.worldPosition.xy, _ClipRect.zw);
                color.a *= clipFactor.x * clipFactor.y;
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
