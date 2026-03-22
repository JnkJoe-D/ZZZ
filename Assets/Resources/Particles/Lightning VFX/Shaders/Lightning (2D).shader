// Made with Amplify Shader Editor v1.9.1.8
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Mirza Beig/Lightning VFX/Lightning 2D"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[Toggle]_ParticleMode("Particle Mode", Float) = 1
		[HDR]_Colour("Colour", Color) = (1,1,1,1)
		_AlphaErosion("Alpha Erosion", Range( 0 , 1)) = 0
		_RemapMin("Remap Min", Range( 0 , 1)) = 0
		_RemapMax("Remap Max", Range( 0 , 1)) = 0
		_AlbedoTexture("Albedo Texture", 2D) = "white" {}
		_Duplicate("Duplicate", Range( 0 , 1)) = 0

		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		Cull Off
		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
		ENDHLSL

		
		Pass
		{
			Name "Sprite Unlit"
			Tags { "LightMode"="Universal2D" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM

			#define ASE_SRP_VERSION 140010


			#pragma vertex vert
			#pragma fragment frag

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define SHADERPASS SHADERPASS_SPRITEUNLIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR


			sampler2D _AlbedoTexture;
			CBUFFER_START( UnityPerMaterial )
			float4 _Colour;
			float _Duplicate;
			float _ParticleMode;
			float _AlphaErosion;
			float _RemapMin;
			float _RemapMax;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			float4 _RendererColor;

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( v.vertex.xyz );

				o.texCoord0 = v.uv0;
				o.color = v.color;
				o.clipPos = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float2 texCoord34 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float ParticleStableRandom42 = IN.texCoord0.w;
				float2 appendResult40 = (float2(0.0 , ParticleStableRandom42));
				float2 texCoord52 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float ifLocalVar63 = 0;
				if( ParticleStableRandom42 <= 0.5 )
				ifLocalVar63 = 1.0;
				else
				ifLocalVar63 = -1.0;
				float2 appendResult54 = (float2(0.0 , ( ParticleStableRandom42 - 0.5 )));
				float4 AlbedoTextureSample48 = saturate( ( tex2D( _AlbedoTexture, ( texCoord34 + appendResult40 ) ) + ( tex2D( _AlbedoTexture, ( ( texCoord52 * ifLocalVar63 ) + appendResult54 ) ) * _Duplicate ) ) );
				float4 temp_output_5_0 = ( AlbedoTextureSample48 * IN.color * _Colour );
				float3 ColourRGB4 = (temp_output_5_0).rgb;
				float ColourA10 = (temp_output_5_0).a;
				float ParticleErosionOverLifetime30 = IN.texCoord0.z;
				float temp_output_1_0_g2 = _RemapMin;
				float Opacity23 = saturate( ( ( saturate( ( ColourA10 - (( _ParticleMode )?( ParticleErosionOverLifetime30 ):( _AlphaErosion )) ) ) - temp_output_1_0_g2 ) / ( _RemapMax - temp_output_1_0_g2 ) ) );
				float4 appendResult90 = (float4(ColourRGB4 , Opacity23));
				
				float4 Color = appendResult90;

				#if ETC1_EXTERNAL_ALPHA
					float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
					Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
				#endif

				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				Color *= IN.color * _RendererColor;
				return Color;
			}

			ENDHLSL
		}
		
		Pass
		{
			
			Name "Sprite Unlit Forward"
            Tags { "LightMode"="UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM

			#define ASE_SRP_VERSION 140010


			#pragma vertex vert
			#pragma fragment frag

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR


			sampler2D _AlbedoTexture;
			CBUFFER_START( UnityPerMaterial )
			float4 _Colour;
			float _Duplicate;
			float _ParticleMode;
			float _AlphaErosion;
			float _RemapMin;
			float _RemapMax;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			float4 _RendererColor;

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( v.vertex.xyz );

				o.texCoord0 = v.uv0;
				o.color = v.color;
				o.clipPos = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float2 texCoord34 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float ParticleStableRandom42 = IN.texCoord0.w;
				float2 appendResult40 = (float2(0.0 , ParticleStableRandom42));
				float2 texCoord52 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float ifLocalVar63 = 0;
				if( ParticleStableRandom42 <= 0.5 )
				ifLocalVar63 = 1.0;
				else
				ifLocalVar63 = -1.0;
				float2 appendResult54 = (float2(0.0 , ( ParticleStableRandom42 - 0.5 )));
				float4 AlbedoTextureSample48 = saturate( ( tex2D( _AlbedoTexture, ( texCoord34 + appendResult40 ) ) + ( tex2D( _AlbedoTexture, ( ( texCoord52 * ifLocalVar63 ) + appendResult54 ) ) * _Duplicate ) ) );
				float4 temp_output_5_0 = ( AlbedoTextureSample48 * IN.color * _Colour );
				float3 ColourRGB4 = (temp_output_5_0).rgb;
				float ColourA10 = (temp_output_5_0).a;
				float ParticleErosionOverLifetime30 = IN.texCoord0.z;
				float temp_output_1_0_g2 = _RemapMin;
				float Opacity23 = saturate( ( ( saturate( ( ColourA10 - (( _ParticleMode )?( ParticleErosionOverLifetime30 ):( _AlphaErosion )) ) ) - temp_output_1_0_g2 ) / ( _RemapMax - temp_output_1_0_g2 ) ) );
				float4 appendResult90 = (float4(ColourRGB4 , Opacity23));
				
				float4 Color = appendResult90;

				#if ETC1_EXTERNAL_ALPHA
					float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
					Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
				#endif


				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				Color *= IN.color * _RendererColor;
				return Color;
			}

			ENDHLSL
		}
		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

			#define ASE_SRP_VERSION 140010


			#pragma vertex vert
			#pragma fragment frag

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_FRAG_COLOR


			sampler2D _AlbedoTexture;
			CBUFFER_START( UnityPerMaterial )
			float4 _Colour;
			float _Duplicate;
			float _ParticleMode;
			float _AlphaErosion;
			float _RemapMin;
			float _RemapMax;
			CBUFFER_END


            struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


            int _ObjectId;
            int _PassValue;

			
			VertexOutput vert(VertexInput v )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				float3 positionWS = TransformObjectToWorld(v.vertex);
				o.clipPos = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				float2 texCoord34 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float ParticleStableRandom42 = IN.ase_texcoord.w;
				float2 appendResult40 = (float2(0.0 , ParticleStableRandom42));
				float2 texCoord52 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float ifLocalVar63 = 0;
				if( ParticleStableRandom42 <= 0.5 )
				ifLocalVar63 = 1.0;
				else
				ifLocalVar63 = -1.0;
				float2 appendResult54 = (float2(0.0 , ( ParticleStableRandom42 - 0.5 )));
				float4 AlbedoTextureSample48 = saturate( ( tex2D( _AlbedoTexture, ( texCoord34 + appendResult40 ) ) + ( tex2D( _AlbedoTexture, ( ( texCoord52 * ifLocalVar63 ) + appendResult54 ) ) * _Duplicate ) ) );
				float4 temp_output_5_0 = ( AlbedoTextureSample48 * IN.ase_color * _Colour );
				float3 ColourRGB4 = (temp_output_5_0).rgb;
				float ColourA10 = (temp_output_5_0).a;
				float ParticleErosionOverLifetime30 = IN.ase_texcoord.z;
				float temp_output_1_0_g2 = _RemapMin;
				float Opacity23 = saturate( ( ( saturate( ( ColourA10 - (( _ParticleMode )?( ParticleErosionOverLifetime30 ):( _AlphaErosion )) ) ) - temp_output_1_0_g2 ) / ( _RemapMax - temp_output_1_0_g2 ) ) );
				float4 appendResult90 = (float4(ColourRGB4 , Opacity23));
				
				float4 Color = appendResult90;

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }

            Cull Off

            HLSLPROGRAM

			#define ASE_SRP_VERSION 140010


			#pragma vertex vert
			#pragma fragment frag

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	#define ASE_NEEDS_FRAG_COLOR


			sampler2D _AlbedoTexture;
			CBUFFER_START( UnityPerMaterial )
			float4 _Colour;
			float _Duplicate;
			float _ParticleMode;
			float _AlphaErosion;
			float _RemapMin;
			float _RemapMax;
			CBUFFER_END


            struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

            float4 _SelectionID;

			
			VertexOutput vert(VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				float3 positionWS = TransformObjectToWorld(v.vertex);
				o.clipPos = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				float2 texCoord34 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float ParticleStableRandom42 = IN.ase_texcoord.w;
				float2 appendResult40 = (float2(0.0 , ParticleStableRandom42));
				float2 texCoord52 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float ifLocalVar63 = 0;
				if( ParticleStableRandom42 <= 0.5 )
				ifLocalVar63 = 1.0;
				else
				ifLocalVar63 = -1.0;
				float2 appendResult54 = (float2(0.0 , ( ParticleStableRandom42 - 0.5 )));
				float4 AlbedoTextureSample48 = saturate( ( tex2D( _AlbedoTexture, ( texCoord34 + appendResult40 ) ) + ( tex2D( _AlbedoTexture, ( ( texCoord52 * ifLocalVar63 ) + appendResult54 ) ) * _Duplicate ) ) );
				float4 temp_output_5_0 = ( AlbedoTextureSample48 * IN.ase_color * _Colour );
				float3 ColourRGB4 = (temp_output_5_0).rgb;
				float ColourA10 = (temp_output_5_0).a;
				float ParticleErosionOverLifetime30 = IN.ase_texcoord.z;
				float temp_output_1_0_g2 = _RemapMin;
				float Opacity23 = saturate( ( ( saturate( ( ColourA10 - (( _ParticleMode )?( ParticleErosionOverLifetime30 ):( _AlphaErosion )) ) ) - temp_output_1_0_g2 ) / ( _RemapMax - temp_output_1_0_g2 ) ) );
				float4 appendResult90 = (float4(ColourRGB4 , Opacity23));
				
				float4 Color = appendResult90;
				half4 outColor = _SelectionID;
				return outColor;
			}

            ENDHLSL
        }
		
	}
	CustomEditor "ASEMaterialInspector"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=19108
Node;AmplifyShaderEditor.RegisterLocalVarNode;30;-4532.04,894.6838;Inherit;False;ParticleErosionOverLifetime;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;42;-4529.63,1039.647;Inherit;False;ParticleStableRandom;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;29;-4875.187,892.0133;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;45;-2834.576,-1077.884;Inherit;True;Property;_AlbedoTexture;Albedo Texture;5;0;Create;True;0;0;0;False;0;False;None;0ad415cb683dc65438c8fd22c34a1500;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode;46;-2546.912,-1080.454;Inherit;False;AlbedoTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-2410.602,843.3228;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;17;-2867.359,1045.198;Inherit;False;Property;_Colour;Colour;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;8,8,8,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;7;-2852.733,852.0948;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;2;-2125.706,785.3329;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;9;-2123.373,920.113;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;4;-1814.993,781.9593;Inherit;False;ColourRGB;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;10;-1814.059,918.3372;Inherit;False;ColourA;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;49;-2897.91,714.2134;Inherit;False;48;AlbedoTextureSample;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-2546.521,-433.6447;Inherit;True;Property;_Albedo;Albedo;2;0;Create;True;0;0;0;False;0;False;-1;0ad415cb683dc65438c8fd22c34a1500;0ad415cb683dc65438c8fd22c34a1500;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;50;-2541.193,-168.293;Inherit;True;Property;_Albedo1;Albedo;2;0;Create;True;0;0;0;False;0;False;-1;0ad415cb683dc65438c8fd22c34a1500;0ad415cb683dc65438c8fd22c34a1500;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;47;-2828.424,-556.1292;Inherit;False;46;AlbedoTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;51;-2803.25,-170.4918;Inherit;False;46;AlbedoTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;34;-3118.899,-463.8468;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;35;-2770.899,-408.8468;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;40;-3031.5,-289.6468;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;-2784.015,-36.08008;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;-3044.616,83.11993;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;43;-3454.442,-269.197;Inherit;False;42;ParticleStableRandom;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-3083.363,-61.91193;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-3505.815,-122.2801;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;58;-3427.291,214.0071;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-3803.417,177.1858;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;65;-3804.417,269.1858;Inherit;False;Constant;_Float1;Float 0;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;63;-3464.289,22.70003;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;55;-3887.397,4.424606;Inherit;False;42;ParticleStableRandom;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;66;-3866.417,94.18584;Inherit;False;Constant;_Float2;Float 2;6;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-2552.866,71.40936;Inherit;False;Property;_Duplicate;Duplicate;6;0;Create;True;0;0;0;False;0;False;0;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;-2079.845,-160.9764;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;56;-1810.38,-351.0695;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;57;-1582.219,-369.7292;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-1321.7,-371.4484;Inherit;False;AlbedoTextureSample;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;20;-2173.595,2325.791;Inherit;False;Inverse Lerp;-1;;2;09cbe79402f023141a4dc1fddd4c9511;0;3;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-2658.175,2289.733;Inherit;False;Property;_RemapMin;Remap Min;3;0;Create;True;0;0;0;False;0;False;0;0.065;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2649.944,2398.179;Inherit;False;Property;_RemapMax;Remap Max;4;0;Create;True;0;0;0;False;0;False;0;0.4;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;14;-2687.626,2566.406;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;15;-2481.626,2566.406;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;11;-2958.83,2508.922;Inherit;False;10;ColourA;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;21;-1934.944,2325.179;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;23;-1736.821,2317.068;Inherit;False;Opacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;33;-3033.589,2664.47;Inherit;False;Property;_ParticleMode;Particle Mode;0;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;31;-3473.074,2765.492;Inherit;False;30;ParticleErosionOverLifetime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-3464.127,2639.93;Inherit;False;Property;_AlphaErosion;Alpha Erosion;2;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;72;-2836.96,1698.101;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-2514.049,1571.927;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;69;-2862.788,1511.097;Inherit;False;Constant;_Emission;Emission;7;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;73;-2284.102,1569.408;Inherit;False;Emission_TEST_ONLY;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;24;-370.0051,267.8712;Inherit;False;23;Opacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;74;-394.8003,69.01295;Inherit;False;73;Emission_TEST_ONLY;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;86;0,0;Float;False;False;-1;2;ASEMaterialInspector;0;1;New Amplify Shader;cf964e524c8e69742b1d21fbe2ebcc4a;True;Sprite Unlit Forward;0;1;Sprite Unlit Forward;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;12;all;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;87;0,0;Float;False;False;-1;2;ASEMaterialInspector;0;1;New Amplify Shader;cf964e524c8e69742b1d21fbe2ebcc4a;True;SceneSelectionPass;0;2;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;88;0,0;Float;False;False;-1;2;ASEMaterialInspector;0;1;New Amplify Shader;cf964e524c8e69742b1d21fbe2ebcc4a;True;ScenePickingPass;0;3;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.GetLocalVarNode;8;-404.2879,-62.5931;Inherit;False;4;ColourRGB;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;85;337.3334,-0.666687;Float;False;True;-1;2;ASEMaterialInspector;0;14;Mirza Beig/Lightning VFX/Lightning 2D;cf964e524c8e69742b1d21fbe2ebcc4a;True;Sprite Unlit;0;0;Sprite Unlit;4;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;0;True;12;all;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;Hidden/InternalErrorShader;0;0;Standard;3;Vertex Position;1;0;Debug Display;0;0;External Alpha;0;0;0;4;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.DynamicAppendNode;90;-16.42529,65.60172;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
WireConnection;30;0;29;3
WireConnection;42;0;29;4
WireConnection;46;0;45;0
WireConnection;5;0;49;0
WireConnection;5;1;7;0
WireConnection;5;2;17;0
WireConnection;2;0;5;0
WireConnection;9;0;5;0
WireConnection;4;0;2;0
WireConnection;10;0;9;0
WireConnection;1;0;47;0
WireConnection;1;1;35;0
WireConnection;50;0;51;0
WireConnection;50;1;53;0
WireConnection;35;0;34;0
WireConnection;35;1;40;0
WireConnection;40;1;43;0
WireConnection;53;0;62;0
WireConnection;53;1;54;0
WireConnection;54;1;58;0
WireConnection;62;0;52;0
WireConnection;62;1;63;0
WireConnection;58;0;55;0
WireConnection;63;0;55;0
WireConnection;63;2;64;0
WireConnection;63;3;65;0
WireConnection;63;4;65;0
WireConnection;67;0;50;0
WireConnection;67;1;68;0
WireConnection;56;0;1;0
WireConnection;56;1;67;0
WireConnection;57;0;56;0
WireConnection;48;0;57;0
WireConnection;20;1;19;0
WireConnection;20;2;22;0
WireConnection;20;3;15;0
WireConnection;14;0;11;0
WireConnection;14;1;33;0
WireConnection;15;0;14;0
WireConnection;21;0;20;0
WireConnection;23;0;21;0
WireConnection;33;0;13;0
WireConnection;33;1;31;0
WireConnection;71;0;69;0
WireConnection;71;1;72;0
WireConnection;73;0;71;0
WireConnection;85;1;90;0
WireConnection;90;0;8;0
WireConnection;90;3;24;0
ASEEND*/
//CHKSM=3A685D96A696380E9908660CFBEDDB8455A89D3E