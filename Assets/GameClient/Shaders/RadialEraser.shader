Shader "UI/RadialEraser"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 你的图2（遮罩图）
        _MaskTex ("Mask Texture", 2D) = "transparent" {}
        // 擦除的比例 (0 = 不擦除/满能量, 1 = 全擦除/0能量)
        _EraseAmount ("Erase Amount", Range(0, 1)) = 0 
        
        // UI 默认所需属性
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            float _EraseAmount;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 采样图1（主贴图）和图2（遮罩贴图）
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                fixed4 mask = tex2D(_MaskTex, i.texcoord);

                // 计算当前像素的极坐标角度
                // 将UV坐标原点移到中心 (0.5, 0.5)
                float2 centeredUV = i.texcoord - float2(0.5, 0.5);
                
                // atan2(x, y) 算出的角度：正上方为0，顺时针增加到PI，逆时针减少到-PI
                float angle = atan2(centeredUV.x, centeredUV.y); 
                
                // 将角度从 [-PI, PI] 映射到 [0, 1] 的范围，0为正上方，顺时针一圈到1
                if (angle < 0) 
                {
                    angle += 6.28318530718; // 加上 2 * PI
                }
                float normalizedAngle = angle / 6.28318530718;

                // 核心逻辑：
                // 如果当前像素的角度小于需要擦除的比例，并且该像素在图2(Mask)中是不透明的（即属于白色圆弧部分）
                if (normalizedAngle <= _EraseAmount && mask.a > 0.1)
                {
                    col.a = 0; // 将图1的该像素变为完全透明
                }

                return col;
            }
            ENDCG
        }
    }
}