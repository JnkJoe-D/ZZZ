// 带有手写/艺术效果的 SDF Shader (粉笔、水彩、毛笔、钢笔、水墨风、整体形变扭曲、颜色渐变、金箔效果、3D压印浮雕、动态流体字)
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

	// ==================== 手写/艺术效果全局控制开关 ====================
	_UseEdgeSpikes		("Use Edge & Spikes Toggle (启用边缘与毛刺)", Float) = 0
	_UseWarp			("Use Warp Toggle (启用整体形变)", Float) = 0
	_UseInk				("Use Ink Wash Toggle (启用水墨与晕染)", Float) = 0
	_UseHoles			("Use Holes Toggle (启用斑驳与空洞)", Float) = 0
	_UseGradGold		("Use Grad & Gold Toggle (启用渐变与金箔)", Float) = 0
	_UseEmboss			("Use Emboss Toggle (启用3D压印浮雕)", Float) = 0
	_UseFlow			("Use Flow Toggle (启用液态流动)", Float) = 0

	// ==================== 手写/艺术效果专属参数 ====================
	[Header(Handwriting Effects)]
	_NoiseScale			("Procedural Noise Scale (边缘扭曲噪声缩放)", float) = 50
	_EdgeDistortion		("Edge Distortion (边缘扭曲度)", Range(0, 0.5)) = 0
	_EdgeBleed			("Ink Bleed Softness (常规边缘缩放)", Range(0, 5)) = 0
	_WatercolorEdge		("Watercolor Edge Darken (水彩边缘沉积加深)", Range(0, 1)) = 0

	[Header(Spikes and Sharpening)]
	_SpikeScale			("Spike Noise Scale (毛刺密度缩放)", float) = 150
	_SpikeDistortion	("Spike Distortion (尖锐毛刺强度)", Range(0, 0.5)) = 0
	_Sharpen			("Edge Sharpening (边缘锐化程度)", Range(1, 10)) = 1

	[Header(Warp Distortion)]
	_VertexWarpStrength	("Vertex Warp Strength (顶点几何整体扭曲强度)", Range(0, 50)) = 0
	_VertexWarpScale	("Vertex Warp Scale (几何扭曲频率)", float) = 0.05
	_WarpStrength		("Texture Warp Strength (纹理水墨扭曲强度)", Range(0, 0.05)) = 0
	_WarpScale			("Texture Warp Scale (纹理扭曲频率)", float) = 15

	[Header(Ink Wash and Bleeding)]
	_InkBleedDist		("Ink Bleed Distance (水墨晕染宽度)", Range(0, 0.5)) = 0
	_InkBleedSoftness	("Bleed Softness (晕染边缘虚化)", Range(0, 0.95)) = 0.5
	_InkBleedOpacity	("Bleed Opacity (晕染透明度)", Range(0, 1)) = 0.5
	[HDR]_InkWashColor	("Ink Wash Color (晕染淡墨颜色)", Color) = (0.1, 0.1, 0.1, 0.8)

	[Header(Gradient Effect)]
	_UseGradient		("Use Gradient (启用颜色渐变)", Float) = 0
	[HDR]_GradientColor	("Gradient Color (渐变目标颜色)", Color) = (0, 0.5, 1, 1)
	_GradientDirectionType("Gradient Type (0横, 1纵, 2角度)", Float) = 0
	_GradientAngle		("Gradient Angle (渐变角度)", Range(0, 360)) = 0
	_GradCenter			("Gradient Center (渐变中心位移)", float) = 0
	_GradWidth			("Gradient Width (渐变过渡宽度)", float) = 250

	[Header(Gold Foil Effect)]
	_GoldFoil			("Gold Foil Strength (金箔效果强度)", Range(0, 1)) = 0
	[HDR]_GoldColor		("Gold Foil Color (金箔颜色)", Color) = (1, 0.82, 0.35, 1)
	_GoldDensity		("Gold Density (金箔密度)", Range(0, 1)) = 0.3

	[Header(Holes and Grains)]
	_HoleIntensity		("Hole Intensity (空洞明显程度)", Range(0, 1)) = 0
	_HoleDensity		("Hole Density (空洞密集程度)", Range(0, 1)) = 0.5
	_HoleScaleX			("Hole Scale X (空洞横向缩放)", float) = 50
	_HoleScaleY			("Hole Scale Y (空洞纵向缩放)", float) = 50

	[Header(3D Emboss and Deboss)]
	_EmbossStrength		("Emboss/Deboss Strength (浮雕凹陷强度)", Range(-2.0, 2.0)) = 0.5
	_EmbossHeight		("Bevel Slope (边缘斜坡厚度)", Range(0.1, 10.0)) = 2.0
	_LightAngle			("Light Direction Angle (光源投影角度)", Range(0, 360)) = 135
	_LightDepth			("Light Height (光源高度)", Range(0.1, 3.0)) = 1.0
	_SpecularPower		("Specular Highlight (高光亮度)", Range(0, 2.0)) = 1.0

	[Header(Dynamic Flow)]
	_FlowSpeed			("Flow Speed (流动呼吸速度)", Range(0.01, 1.0)) = 0.1
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

		float _UseEdgeSpikes;
		float _UseWarp;
		float _UseInk;
		float _UseHoles;
		float _UseGradGold;
		float _UseEmboss;
		float _UseFlow;

		float _NoiseScale;
		float _EdgeDistortion;
		float _EdgeBleed;
		float _WatercolorEdge;
		
		float _SpikeScale;
		float _SpikeDistortion;
		float _Sharpen;

		float _VertexWarpStrength;
		float _VertexWarpScale;
		float _WarpStrength;
		float _WarpScale;

		float _InkBleedDist;
		float _InkBleedSoftness;
		float _InkBleedOpacity;
		fixed4 _InkWashColor;

		float _UseGradient;
		fixed4 _GradientColor;
		float _GradientDirectionType;
		float _GradientAngle;
		float _GradCenter;
		float _GradWidth;

		float _GoldFoil;
		fixed4 _GoldColor;
		float _GoldDensity;

		float _HoleIntensity;
		float _HoleDensity;
		float _HoleScaleX;
		float _HoleScaleY;

		float _EmbossStrength;
		float _EmbossHeight;
		// float _LightAngle; // 已经在 TMPro_Properties.cginc 中声明，直接使用，无需重新声明以防冲突
		float _LightDepth;
		// float _SpecularPower; // 已经在 TMPro_Properties.cginc 中声明，直接使用，无需重新声明以防冲突

		float _FlowSpeed;

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

		// 3. 分形布朗运动噪声 (FBM)，通过叠加多个不同频率 of 噪声，生成极其自然且不规则的形状
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

			// 新增：顶点级整体弯曲扭曲 (Vertex Warp) - 根据 _UseWarp 开关控制
			if (_UseWarp > 0.5) {
				float2 flowOffset = float2(0, 0);
				if (_UseFlow > 0.5) {
					flowOffset = float2(_Time.y * _FlowSpeed * 0.2, _Time.y * _FlowSpeed * 0.15);
				}
				float2 vWarpUV = vert.xy * _VertexWarpScale + flowOffset;
				float2 vWarpOffset = float2(
					fbm(vWarpUV),
					fbm(vWarpUV + float2(37.0, 71.0))
				) - 0.5;
				vert.xy += vWarpOffset * _VertexWarpStrength;
			}

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

			// 为了防止字符网格 Quad 裁剪，在最外层读取原始 SDF 并生成平滑淡出系数
			// 在接近 Quad 边缘的地方（d_raw接近0时），将任何 UV 偏移淡出为 0，实现完美的零裁剪保障！
			half d_raw_base = tex2D(_MainTex, input.texcoord0.xy).a;
			half uvSafeFade = smoothstep(0.12, 0.38, d_raw_base);

			// 1. 计算纹理水墨扭曲 (Texture Warp) - 融入 _UseFlow 动态流动
			float2 warpOffset = float2(0, 0);
			if (_UseWarp > 0.5) {
				float2 flowOffset = float2(0, 0);
				if (_UseFlow > 0.5) {
					flowOffset = float2(_Time.y * _FlowSpeed * 0.5, _Time.y * _FlowSpeed * 0.3);
				}
				float2 warpUV = input.localPos.xy * _WarpScale + flowOffset;
				warpOffset = float2(
					fbm(warpUV),
					fbm(warpUV + float2(17.0, 31.0))
				) - 0.5;
			}

			// 2. 独立水波起伏 (即使关闭 Warp 整体形变，开启 Flow 时，字脊边缘依然会缓慢波动)
			float2 flowWobble = float2(0, 0);
			if (_UseFlow > 0.5) {
				float flowTime = _Time.y * _FlowSpeed * 6.0;
				// 将幅度缩小 4 倍（调到极度细腻自然的 0.00075 级别），避免眼花
				flowWobble.x = sin(input.localPos.y * 0.05 + flowTime) * 0.00075;
				flowWobble.y = cos(input.localPos.x * 0.05 + flowTime * 0.8) * 0.00075;
			}

			// 将所有偏移应用到采样 UV 上，并乘以边界保护系数 uvSafeFade，彻底拒绝超出字面 Quad 裁剪！
			float2 finalUV = input.texcoord0.xy + (warpOffset * _WarpStrength + flowWobble) * uvSafeFade;

			// 3. 边缘微观扭曲与毛刺 - 融入 _UseFlow 动态微颤
			half edgeDistortionVal = 0.0;
			half spikeDistortionVal = 0.0;
			half smoothNoise = 0.0;
			half spikeNoise = 0.0;
			
			if (_UseEdgeSpikes > 0.5) {
				float2 flowOffset = float2(0, 0);
				if (_UseFlow > 0.5) {
					flowOffset = float2(_Time.y * _FlowSpeed * 0.8, -_Time.y * _FlowSpeed * 0.4);
				}
				float2 noiseUV = input.localPos.xy * _NoiseScale + flowOffset;
				smoothNoise = valueNoise(noiseUV);
				edgeDistortionVal = (smoothNoise - 0.5) * _EdgeDistortion;
				
				float2 spikeUV = input.localPos.xy * _SpikeScale + flowOffset * 1.5;
				spikeNoise = hash(spikeUV);
				spikeDistortionVal = (spikeNoise - 0.5) * _SpikeDistortion;
			}

			// 4. 读取原始文字的距离场 (SDF) 并应用微观边缘扭曲
			half d_raw = tex2D(_MainTex, finalUV).a; 
			
			// 引入边缘淡出，保护透明面片边缘不产生“直边裁剪下划线”
			half edgeFade = smoothstep(0.2, 0.45, d_raw);
			d_raw += edgeDistortionVal * edgeFade;
			d_raw += spikeDistortionVal * edgeFade;

			// 5. 动态计算墨水边缘常规晕染 (Ink Bleed)
			float softnessScale = 1.0;
			if (_UseEdgeSpikes > 0.5) {
				softnessScale = 1.0 / (1.0 + _EdgeBleed * smoothNoise);
			}

			// 结合 TMP 自带的偏置参数，计算最终的表面距离
			half d = d_raw * input.param.x * softnessScale;
			float coreDist = d - input.param.w;

			// ==================== 水墨与晕染核心渲染逻辑 ====================
			// 核心文字 Alpha
			float finalInkAlpha = saturate(coreDist * _Sharpen);

			// 未预乘主色还原
			half4 unmultipliedFace = input.faceColor;
			if (unmultipliedFace.a > 0.001) {
				unmultipliedFace.rgb /= unmultipliedFace.a;
			}

			// 默认主颜色
			half3 finalRGB = unmultipliedFace.rgb;

			if (_UseInk > 0.5) {
				// 启用晕染外圈部分 - 融入 _UseFlow 动态宣纸纤维漂移
				float bleedDist = d - (input.param.w - _InkBleedDist * input.param.x * softnessScale);
				float coreAlpha = saturate(coreDist * _Sharpen);
				
				float2 fiberUV = input.localPos.xy * 200.0;
				if (_UseFlow > 0.5) {
					fiberUV += float2(_Time.y * _FlowSpeed * 0.3, _Time.y * _FlowSpeed * 0.2);
				}
				float fiberNoise = hash(fiberUV);
				float bleedSoft = (1.0 - _InkBleedSoftness) * 2.0;
				float bleedAlpha = saturate(bleedDist * bleedSoft * (0.8 + fiberNoise * 0.2)) * _InkBleedOpacity;
				
				// 组合浓墨与淡墨晕染外圈的 Alpha
				finalInkAlpha = max(coreAlpha, bleedAlpha);
				// 浓淡墨双色融合
				finalRGB = lerp(_InkWashColor.rgb, unmultipliedFace.rgb, saturate(coreAlpha));
			}
			// ================================================================

			if (_UseGradGold > 0.5) {
				// --- 双色颜色渐变 (Gradient Effect) ---
				if (_UseGradient > 0.5) {
					float gradCoord = 0.0;
					if (_GradientDirectionType < 0.5) {
						gradCoord = input.localPos.x;
					} else if (_GradientDirectionType < 1.5) {
						gradCoord = input.localPos.y;
					} else {
						float angleRad = _GradientAngle * 0.0174532925;
						float2 dirVec = float2(cos(angleRad), sin(angleRad));
						gradCoord = dot(input.localPos.xy, dirVec);
					}
					
					float halfWidth = max(0.1, _GradWidth * 0.5);
					float gradT = saturate((gradCoord - (_GradCenter - halfWidth)) / max(0.1, _GradWidth));
					
					finalRGB = lerp(finalRGB, _GradientColor.rgb, gradT);
				}

				// --- 闪亮金箔效果 (Gold Foil) ---
				if (_GoldFoil > 0.0) {
					float goldNoise = hash(input.localPos.xy * 80.0);
					float isCore = smoothstep(0.1, 0.6, coreDist);
					float goldMask = step(1.0 - _GoldDensity * 0.1, goldNoise) * isCore * _GoldFoil;
					finalRGB = lerp(finalRGB, _GoldColor.rgb, goldMask);
				}
			}

			// --- 强力动态液态波纹波光 (Dynamic Liquid Caustics & Ripple Shine) - 大幅柔和化（降低为温润的高雅反光） ---
			if (_UseFlow > 0.5) {
				float flowTime = _Time.y * _FlowSpeed * 6.0;
				// 降低频率，让波光流动更加沉稳大方
				float wave = sin(input.localPos.x * 0.02 + input.localPos.y * 0.015 + flowTime);
				float wave2 = cos(input.localPos.x * -0.015 + input.localPos.y * 0.02 - flowTime * 0.7);
				float combinedWave = saturate((wave * wave2) * 0.5 + 0.5);
				
				// 将叠加亮度从 0.35 降低至超细腻的 0.08，提供若隐若现的水光感，绝不刺眼
				finalRGB += float3(1.0, 1.0, 1.0) * combinedWave * 0.08 * saturate(finalInkAlpha);
			}

			// 重新构建预乘 Alpha 的最终文字颜色
			half4 c = half4(finalRGB, finalInkAlpha * input.faceColor.a);
			c.rgb *= c.a;

			// ==================== 3D 纸张凹凸压印与浮雕 (Emboss/Deboss) ====================
			if (_UseEmboss > 0.5) {
				// 使用屏幕空间偏导数（SDF 梯度）极其优雅高效地实时推导字脊的 3D 法线
				float2 dGrad = float2(ddx(d), ddy(d));
				float dGradLen = length(dGrad);
				float2 norm2D = dGradLen > 0.0001 ? dGrad / dGradLen : float2(0, 0);

				// 组合为 3D 法线向量，_EmbossHeight 控制边缘斜坡的厚度和陡峭度
				float3 normal3D = normalize(float3(norm2D.x * _EmbossHeight, norm2D.y * _EmbossHeight, 1.0));

				// 构造虚拟光照向量
				float lightRad = _LightAngle * 0.0174532925;
				float3 lightDir = normalize(float3(cos(lightRad), sin(lightRad), _LightDepth));

				// Lambert 点乘散射强度计算
				float ndotl = dot(normal3D, lightDir);
				float lightIntensity = ndotl - 0.5; // 分布平移到 [-0.5, 0.5]

				// 通过 _EmbossStrength 的正负号决定浮雕(Emboss)或凹陷压印(Deboss)
				float factor = lightIntensity * _EmbossStrength;

				// 阴影区叠加调暗（模拟凹折射光斑）
				float shadow = saturate(-factor);
				c.rgb = lerp(c.rgb, c.rgb * 0.35, shadow * saturate(finalInkAlpha * 2.0));

				// 高光区叠加亮白（模拟纸张/墨水表面的高亮漫反射）
				float specular = saturate(factor) * _SpecularPower;
				c.rgb = lerp(c.rgb, float3(1.0, 1.0, 1.0), specular * saturate(finalInkAlpha * 2.0));
			}
			// ==============================================================================

			// 处理描边 (Outline)
			#ifdef OUTLINE_ON
			c = lerp(input.outlineColor, c, saturate((d - input.param.z) * _Sharpen));
			c *= saturate((d - input.param.y) * _Sharpen);
			#endif

			// 处理阴影 (Underlay)
			#if UNDERLAY_ON
			float2 finalUnderlayUV = input.texcoord1.xy + warpOffset * _WarpStrength;
			half underlay_d = tex2D(_MainTex, finalUnderlayUV).a;
			half underlayFade = smoothstep(0.2, 0.45, underlay_d);
			if (_UseEdgeSpikes > 0.5) {
				underlay_d += (smoothNoise - 0.5) * _EdgeDistortion * underlayFade;
				underlay_d += (spikeNoise - 0.5) * _SpikeDistortion * underlayFade;
			}
			underlay_d *= input.underlayParam.x * softnessScale;
			c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate((underlay_d - input.underlayParam.y) * _Sharpen) * (1 - c.a);
			#endif

			// 处理内发光式阴影 (Underlay Inner)
			#if UNDERLAY_INNER
			half sd = saturate((d - input.param.z) * _Sharpen);
			float2 finalUnderlayUV_in = input.texcoord1.xy + warpOffset * _WarpStrength;
			half underlay_d_in = tex2D(_MainTex, finalUnderlayUV_in).a;
			half underlayFadeIn = smoothstep(0.2, 0.45, underlay_d_in);
			if (_UseEdgeSpikes > 0.5) {
				underlay_d_in += (smoothNoise - 0.5) * _EdgeDistortion * underlayFadeIn;
				underlay_d_in += (spikeNoise - 0.5) * _SpikeDistortion * underlayFadeIn;
			}
			underlay_d_in *= input.underlayParam.x * softnessScale;
			c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate((underlay_d_in - input.underlayParam.y) * _Sharpen)) * sd * (1 - c.a);
			#endif
			
			// 6. 均匀镂空飞白应用 (由 _UseHoles 控制)
			if (_UseHoles > 0.5) {
				float2 holeUV = float2(input.localPos.x * _HoleScaleX, input.localPos.y * _HoleScaleY);
				float dust = hash(holeUV * 3.0); 
				float stroke = fbm(holeUV);
				half holeNoise = dust * 0.4 + stroke * 0.6; 
				float holeMask = smoothstep(_HoleDensity - 0.2, _HoleDensity + 0.2, holeNoise);
				
				float finalAlpha = lerp(1.0, holeMask, _HoleIntensity);
				c *= finalAlpha; 
			}

			// 7. 水彩边缘沉积加深 (Watercolor Edge Darken - 仅当启用水墨或边缘时生效)
			if (_UseInk > 0.5 && _UseEdgeSpikes > 0.5) {
				float edgeDist = saturate(d - input.param.w); 
				float edgeDarken = (1.0 - smoothstep(0.0, 0.5, edgeDist)) * _WatercolorEdge * smoothNoise;
				c.rgb = lerp(c.rgb, c.rgb * 0.5, edgeDarken);
			}

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
