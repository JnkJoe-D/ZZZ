// 带有手写/艺术效果的 SDF Shader (粉笔、水彩、毛笔、钢笔)
// 基于 TMP_SDF-Mobile 修改，完全使用程序化噪声 (Procedural Noise)

Shader "TextMeshPro/Mobile/Distance Field - Handwriting" {

Properties {
	[HDR]_FaceColor     ("Face Color (主颜色)", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate (文字粗细)", Range(-1,1)) = 0

	[HDR]_OutlineColor	("Outline Color (描边颜色)", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness (描边厚度)", Range(0,1)) = 0
	_OutlineSoftness	("Outline Softness (描边柔和度)", Range(0,1)) = 0

	[HDR]_UnderlayColor	("Border Color (阴影颜色)", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Border OffsetX (阴影偏移X)", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Border OffsetY (阴影偏移Y)", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate (阴影扩散)", Range(-1,1)) = 0
	_UnderlaySoftness 	("Border Softness (阴影柔和度)", Range(0,1)) = 0

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = .5

	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			("Font Atlas (字体图集)", 2D) = "white" {}
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5
	_ScaleX				("Scale X", float) = 1
	_ScaleY				("Scale Y", float) = 1
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0

	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_MaskSoftnessX		("Mask SoftnessX", float) = 0
	_MaskSoftnessY		("Mask SoftnessY", float) = 0

	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 0
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255

	_CullMode			("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15

	// ==================== 手写/艺术效果专属参数 ====================
	[Header(Handwriting Effects)]
	_NoiseScale			("Procedural Noise Scale (边缘扭曲噪声缩放)", float) = 50
	_EdgeDistortion		("Edge Distortion (边缘扭曲度)", Range(0, 0.5)) = 0
	_EdgeBleed			("Ink Bleed Softness (墨水晕染柔和度)", Range(0, 5)) = 0
	_WatercolorEdge		("Watercolor Edge Darken (水彩边缘沉积加深)", Range(0, 1)) = 0

	[Header(Spikes and Sharpening)]
	_SpikeScale			("Spike Noise Scale (毛刺密度缩放)", float) = 150
	_SpikeDistortion	("Spike Distortion (尖锐毛刺强度)", Range(0, 0.5)) = 0
	_Sharpen			("Edge Sharpening (边缘锐化程度)", Range(1, 10)) = 1

	[Header(Holes and Grains)]
	_HoleIntensity		("Hole Intensity (空洞明显程度)", Range(0, 1)) = 0
	_HoleDensity		("Hole Density (空洞密集程度)", Range(0, 1)) = 0.5
	_HoleScaleX			("Hole Scale X (空洞横向缩放)", float) = 50
	_HoleScaleY			("Hole Scale Y (空洞纵向缩放)", float) = 50
}

SubShader {
	Tags
	{
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}


	Stencil
	{
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting Off
	Fog { Mode Off }
	ZTest [unity_GUIZTestMode]
	Blend One OneMinusSrcAlpha // TMP 默认的预乘 Alpha 混合模式
	ColorMask [_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex VertShader
		#pragma fragment PixShader
		#pragma shader_feature __ OUTLINE_ON
		#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"
		// 引用 TMP 的属性宏
		#include "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc"

		struct vertex_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			float4	vertex			: POSITION;
			float3	normal			: NORMAL;
			fixed4	color			: COLOR;
			float2	texcoord0		: TEXCOORD0;
			float2	texcoord1		: TEXCOORD1;
		};

		struct pixel_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
			float4	vertex			: SV_POSITION;
			fixed4	faceColor		: COLOR;
			fixed4	outlineColor	: COLOR1;
			float4	texcoord0		: TEXCOORD0;			// Texture UV, Mask UV
			half4	param			: TEXCOORD1;			// Scale(x), BiasIn(y), BiasOut(z), Bias(w)
			half4	mask			: TEXCOORD2;			// Position in clip space(xy), Softness(zw)
			#if (UNDERLAY_ON | UNDERLAY_INNER)
			float4	texcoord1		: TEXCOORD3;			// Texture UV, alpha, reserved
			half2	underlayParam	: TEXCOORD4;			// Scale(x), Bias(y)
			#endif
			// 用于记录顶点的局部坐标，用来保证程序化噪声会随着文字移动而移动
			float2  localPos        : TEXCOORD5;
		};

		float _NoiseScale;
		float _EdgeDistortion;
		float _EdgeBleed;
		float _WatercolorEdge;
		
		float _HoleIntensity;
		float _HoleDensity;
		float _HoleScaleX;
		float _HoleScaleY;

		float _SpikeScale;
		float _SpikeDistortion;
		float _Sharpen;

		// ==================== 程序化噪声函数 (Procedural Noise) ====================
		
		// 1. 基础哈希伪随机数 (Hash)
		float hash(float2 p) {
			p = fmod(p, 1000.0); // 防止在移动端因为坐标过大导致浮点数精度丢失
			return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
		}
		
		// 2. 平滑数值噪声 (Value Noise)，用于生成平缓过渡的云雾状区块
		float valueNoise(float2 p) {
			float2 i = floor(p);
			float2 f = frac(p);
			// 经典平滑插值函数
			f = f * f * (3.0 - 2.0 * f);
			return lerp(
				lerp(hash(i + float2(0.0, 0.0)), hash(i + float2(1.0, 0.0)), f.x),
				lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x),
				f.y
			);
		}

		// 3. 分形布朗运动噪声 (FBM)，通过叠加多个不同频率的噪声，生成极其自然且不规则的形状
		// 用于生成大小不一、形状随机的镂空斑块
		float fbm(float2 p) {
			float v = 0.0;
			v += 0.5000 * valueNoise(p); p *= 2.01;
			v += 0.2500 * valueNoise(p); p *= 2.03;
			v += 0.1250 * valueNoise(p);
			return v / 0.875; // 归一化到 0~1
		}

		// ==========================================================================

		pixel_t VertShader(vertex_t input)
		{
			pixel_t output;

			UNITY_INITIALIZE_OUTPUT(pixel_t, output);
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_TRANSFER_INSTANCE_ID(input, output);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float bold = step(input.texcoord1.y, 0);

			float4 vert = input.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;
			float4 vPosition = UnityObjectToClipPos(vert);

			float2 pixelSize = vPosition.w;
			pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

			float scale = rsqrt(dot(pixelSize, pixelSize));
			scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
			if(UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

			float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
			weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

			float layerScale = scale;

			scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
			float bias = (0.5 - weight) * scale - 0.5;
			float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

			float opacity = input.color.a;
			#if (UNDERLAY_ON | UNDERLAY_INNER)
			opacity = 1.0;
			#endif

			fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
			faceColor.rgb *= faceColor.a;

			fixed4 outlineColor = _OutlineColor;
			outlineColor.a *= opacity;
			outlineColor.rgb *= outlineColor.a;
			outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

			#if (UNDERLAY_ON | UNDERLAY_INNER)
			layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
			float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

			float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
			float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
			float2 layerOffset = float2(x, y);
			#endif

			// Generate UV for the Masking Texture
			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

			// Populate structure for pixel shader
			output.vertex = vPosition;
			output.faceColor = faceColor;
			output.outlineColor = outlineColor;
			output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
			output.param = half4(scale, bias - outline, bias + outline, bias);
			output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
			#if (UNDERLAY_ON || UNDERLAY_INNER)
			output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
			output.underlayParam = half2(layerScale, layerBias);
			#endif
			
			// 将顶点的本地坐标传给片段着色器，作为噪声的采样坐标 (保证噪声分布与字符贴合)
			output.localPos = vert.xy;

			return output;
		}


		// 像素着色器 (PIXEL SHADER)
		fixed4 PixShader(pixel_t input) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(input);

			// 1. 计算边缘扭曲的坐标缩放
			float2 noiseUV = input.localPos.xy * _NoiseScale;
			
			// 2. 生成平滑噪声 (Smooth Noise)：用于边缘扭曲 (Edge Distortion) 和墨水晕染 (Ink Bleed)
			half smoothNoise = valueNoise(noiseUV);
			
			// 3. 计算均匀遍布的空洞噪声 (Hole Noise)
			// 利用新增的横向(_HoleScaleX)和纵向(_HoleScaleY)比例生成UV
			float2 holeUV = float2(input.localPos.x * _HoleScaleX, input.localPos.y * _HoleScaleY);
			
			// 混合高频粉末噪点(Hash)与低频笔触噪点(FBM)，生成更真实的粉笔/枯笔质感，彻底打破块状感
			float dust = hash(holeUV * 3.0); 
			float stroke = fbm(holeUV);
			half holeNoise = dust * 0.4 + stroke * 0.6; 
			
			// 使用 _HoleDensity 做阈值裁切。
			float holeMask = smoothstep(_HoleDensity - 0.2, _HoleDensity + 0.2, holeNoise);

			// 4. 读取原始文字的距离场 (SDF) 并应用边缘扭曲
			half d_raw = tex2D(_MainTex, input.texcoord0.xy).a;
			// 圆滑的边缘扭曲：仅改变 SDF，使得文字的外轮廓变得粗糙不平整
			d_raw += (smoothNoise - 0.5) * _EdgeDistortion;
			
			// 新增：尖锐的边缘毛刺效果
			float2 spikeUV = input.localPos.xy * _SpikeScale;
			half spikeNoise = hash(spikeUV); // 使用纯高频哈希噪声产生像素级的锐利刺断
			d_raw += (spikeNoise - 0.5) * _SpikeDistortion;

			// 5. 动态计算墨水边缘晕染 (Ink Bleed)
			float softnessScale = 1.0 / (1.0 + _EdgeBleed * smoothNoise);

			// 结合 TMP 自带的偏置参数，计算最终的表面距离
			half d = d_raw * input.param.x * softnessScale;
			
			// 新增：边缘锐化 (Sharpen)
			// 通过乘以 _Sharpen 放大 SDF 梯度，使原本柔和的抗锯齿边缘变得刀切般锋利
			half4 c = input.faceColor * saturate((d - input.param.w) * _Sharpen);

			// 处理描边 (Outline)
			#ifdef OUTLINE_ON
			c = lerp(input.outlineColor, input.faceColor, saturate((d - input.param.z) * _Sharpen));
			c *= saturate((d - input.param.y) * _Sharpen);
			#endif

			// 处理底层阴影 (Underlay)
			#if UNDERLAY_ON
			half underlay_d = tex2D(_MainTex, input.texcoord1.xy).a;
			underlay_d += (smoothNoise - 0.5) * _EdgeDistortion;
			underlay_d += (spikeNoise - 0.5) * _SpikeDistortion;
			underlay_d *= input.underlayParam.x * softnessScale;
			c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate((underlay_d - input.underlayParam.y) * _Sharpen) * (1 - c.a);
			#endif

			// 处理内发光式阴影 (Underlay Inner)
			#if UNDERLAY_INNER
			half sd = saturate((d - input.param.z) * _Sharpen);
			half underlay_d_in = tex2D(_MainTex, input.texcoord1.xy).a;
			underlay_d_in += (smoothNoise - 0.5) * _EdgeDistortion;
			underlay_d_in += (spikeNoise - 0.5) * _SpikeDistortion;
			underlay_d_in *= input.underlayParam.x * softnessScale;
			c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate((underlay_d_in - input.underlayParam.y) * _Sharpen)) * sd * (1 - c.a);
			#endif
			
			// 6. 均匀镂空应用：直接在最终颜色上相乘！
			// 这样无论是在文字的边缘还是最中心，斑驳的概率都是绝对均匀的（如粉笔字参考图）
			float finalAlpha = lerp(1.0, holeMask, _HoleIntensity);
			c *= finalAlpha; // 预乘 Alpha 混合：同时降低 RGB 和 A，确保镂空处真正透明

			// 6. 水彩边缘加深 (Watercolor Edge Darken)
			// SDF 距离中，d - input.param.w 在文字物理边缘处恰好为 0，向内为正
			float edgeDist = saturate(d - input.param.w); 
			float edgeDarken = (1.0 - smoothstep(0.0, 0.5, edgeDist)) * _WatercolorEdge * smoothNoise;
			c.rgb = lerp(c.rgb, c.rgb * 0.5, edgeDarken);

			// TMP 原生的裁剪区域支持
			#if UNITY_UI_CLIP_RECT
			half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
			c *= m.x * m.y;
			#endif

			#if (UNDERLAY_ON | UNDERLAY_INNER)
			c *= input.texcoord1.z;
			#endif

			#if UNITY_UI_ALPHACLIP
			clip(c.a - 0.001);
			#endif

			return c;
		}
		ENDCG
	}
}

CustomEditor "HandwritingShaderGUI"
}
