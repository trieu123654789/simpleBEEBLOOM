// Generated from SIMPLE_Controller_Labels.shadergraph.
// Rebuild with Tools/Let It Bee/Controllers/Rebuild Label Depth Shader.
// Preserve LEqual occlusion; apply a small slope + unit bias to avoid coplanar flicker.
Shader "LetItBee/Controllers/SIMPLE_Controller_Labels_DepthBias"
{
Properties
{
_Color1("Color1", Color) = (0, 0, 0, 0)
[NoScaleOffset]_Texture_01("Texture_01", 2D) = "white" {}
[ToggleUI]_Texture("Texture", Float) = 0
_UV_Tiling("UV_Tiling", Vector, 2) = (1, 1, 0, 0)
_Shadow_Color("Shadow_Color", Color) = (0, 0, 0, 0)
_AlphaClipping("AlphaClipping", Float) = 0.5
_Metallic("Metallic", Range(0, 1)) = 0
_Smoothness("Smoothness", Range(0, 1)) = 0.5
_AmbientVisibility("Ambient Visibility", Range(0, 1)) = 0.15
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Opaque"
"UniversalMaterialType" = "Lit"
"Queue"="AlphaTest"
"DisableBatching"="False"
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="UniversalLitSubTarget"
}
Pass
{
    Name "ForwardLit"
    Tags
    {
        "LightMode" = "UniversalForward"
    }

// Render State
Cull Off
Blend One Zero
ZTest LEqual
Offset -1, -1
ZWrite On
AlphaToMask On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma target 4.5 _DEPTH_AS_INPUT_ATTACHMENT_MSAA
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _SCREEN_SPACE_REFLECTION
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 texCoord1 : INTERP7;
 float4 texCoord2 : INTERP8;
 float4 fogFactorAndVertexLight : INTERP9;
 float3 positionWS : INTERP10;
 float3 normalWS : INTERP11;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

struct Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float
{
};

void SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float(float _Shadow, float _ShadowLimit, float4 _BaseColor, float4 _ShadowColor, Gradient _Gradient, Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float IN, out float4 OutVector4_1)
{
float4 _Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4 = _BaseColor;
float4 _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4 = _ShadowColor;
Gradient _Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient = _Gradient;
float _Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float = _Shadow;
float _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float;
Unity_OneMinus_float(_Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float, _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float);
float _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float = _ShadowLimit;
float _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float;
Unity_Add_float(_OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float, _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float);
float4 _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4;
Unity_SampleGradientV1_float(_Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float, _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4);
float4 _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4;
Unity_Saturate_float4(_SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4, _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4);
float4 _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4, _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4, _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4, (_Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4).x);
OutVector4_1 = _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[0];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_G_2_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[1];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_B_3_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[2];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_A_4_Float = 0;
float _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float;
Unity_Remap_float(_Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float, float2 (0, 1), float2 (-1, 1), _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float);
float3 _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3;
Unity_Contrast_float((_Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float.xxx), float(3), _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3);
float3 _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3, _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3);
float _Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean = _Texture;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float4 _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4 = _Color1;
float4 _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4;
Unity_Branch_float4(_Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean, _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4, _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4);
float4 _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4 = _Shadow_Color;
Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a;
float4 _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4;
SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float((_Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3).x, float(0), _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4, _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4, NewGradient(0, 2, 2, float4(0, 0, 0, 0.5000076),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a, _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4);
float _Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float = _AmbientVisibility;
float4 _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4;
Unity_Multiply_float4_float4(_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4, (_Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float.xxxx), _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4);
float _Property_d5ede791850746ccbb1e31357e718fec_Out_0_Float = _Metallic;
float _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float = _Smoothness;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.BaseColor = (_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4.xyz);
surface.Metallic = _Property_d5ede791850746ccbb1e31357e718fec_Out_0_Float;
surface.Smoothness = _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "GBuffer"
    Tags
    {
        "LightMode" = "UniversalGBuffer"
    }

// Render State
Cull Off
Blend One Zero
ZTest LEqual
Offset -1, -1
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_REFLECTION
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_GBUFFER
#define _FOG_FRAGMENT 1
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 texCoord1 : INTERP7;
 float4 texCoord2 : INTERP8;
 float4 fogFactorAndVertexLight : INTERP9;
 float3 positionWS : INTERP10;
 float3 normalWS : INTERP11;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

struct Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float
{
};

void SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float(float _Shadow, float _ShadowLimit, float4 _BaseColor, float4 _ShadowColor, Gradient _Gradient, Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float IN, out float4 OutVector4_1)
{
float4 _Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4 = _BaseColor;
float4 _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4 = _ShadowColor;
Gradient _Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient = _Gradient;
float _Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float = _Shadow;
float _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float;
Unity_OneMinus_float(_Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float, _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float);
float _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float = _ShadowLimit;
float _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float;
Unity_Add_float(_OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float, _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float);
float4 _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4;
Unity_SampleGradientV1_float(_Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float, _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4);
float4 _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4;
Unity_Saturate_float4(_SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4, _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4);
float4 _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4, _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4, _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4, (_Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4).x);
OutVector4_1 = _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[0];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_G_2_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[1];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_B_3_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[2];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_A_4_Float = 0;
float _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float;
Unity_Remap_float(_Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float, float2 (0, 1), float2 (-1, 1), _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float);
float3 _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3;
Unity_Contrast_float((_Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float.xxx), float(3), _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3);
float3 _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3, _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3);
float _Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean = _Texture;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float4 _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4 = _Color1;
float4 _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4;
Unity_Branch_float4(_Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean, _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4, _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4);
float4 _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4 = _Shadow_Color;
Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a;
float4 _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4;
SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float((_Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3).x, float(0), _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4, _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4, NewGradient(0, 2, 2, float4(0, 0, 0, 0.5000076),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a, _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4);
float _Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float = _AmbientVisibility;
float4 _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4;
Unity_Multiply_float4_float4(_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4, (_Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float.xxxx), _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4);
float _Property_d5ede791850746ccbb1e31357e718fec_Out_0_Float = _Metallic;
float _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float = _Smoothness;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.BaseColor = (_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4.xyz);
surface.NormalTS = IN.TangentSpaceNormal;
surface.Emission = (_Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4.xyz);
surface.Metallic = _Property_d5ede791850746ccbb1e31357e718fec_Out_0_Float;
surface.Smoothness = _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float;
surface.Occlusion = float(1);
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ShadowCaster"
    Tags
    {
        "LightMode" = "ShadowCaster"
    }

// Render State
Cull Off
ZTest LEqual
Offset -1, -1
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float3 normalWS : INTERP1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Off
ZTest LEqual
Offset -1, -1
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthOnly"
    Tags
    {
        "LightMode" = "DepthOnly"
    }

// Render State
Cull Off
ZTest LEqual
Offset -1, -1
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthNormals"
    Tags
    {
        "LightMode" = "DepthNormals"
    }

// Render State
Cull Off
ZTest LEqual
Offset -1, -1
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ _WRITE_SMOOTHNESS
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 TangentSpaceNormal;
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
 float3 normalWS : INTERP2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Smoothness;
float3 NormalTS;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float = _Smoothness;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.Smoothness = _Property_ae5c2f6b6068461190dc29d5c21c1ef9_Out_0_Float;
surface.NormalTS = IN.TangentSpaceNormal;
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    



    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);



    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_INSTANCEID
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
 float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

struct Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float
{
};

void SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float(float _Shadow, float _ShadowLimit, float4 _BaseColor, float4 _ShadowColor, Gradient _Gradient, Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float IN, out float4 OutVector4_1)
{
float4 _Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4 = _BaseColor;
float4 _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4 = _ShadowColor;
Gradient _Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient = _Gradient;
float _Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float = _Shadow;
float _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float;
Unity_OneMinus_float(_Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float, _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float);
float _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float = _ShadowLimit;
float _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float;
Unity_Add_float(_OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float, _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float);
float4 _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4;
Unity_SampleGradientV1_float(_Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float, _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4);
float4 _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4;
Unity_Saturate_float4(_SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4, _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4);
float4 _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4, _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4, _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4, (_Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4).x);
OutVector4_1 = _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[0];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_G_2_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[1];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_B_3_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[2];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_A_4_Float = 0;
float _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float;
Unity_Remap_float(_Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float, float2 (0, 1), float2 (-1, 1), _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float);
float3 _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3;
Unity_Contrast_float((_Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float.xxx), float(3), _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3);
float3 _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3, _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3);
float _Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean = _Texture;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float4 _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4 = _Color1;
float4 _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4;
Unity_Branch_float4(_Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean, _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4, _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4);
float4 _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4 = _Shadow_Color;
Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a;
float4 _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4;
SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float((_Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3).x, float(0), _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4, _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4, NewGradient(0, 2, 2, float4(0, 0, 0, 0.5000076),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a, _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4);
float _Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float = _AmbientVisibility;
float4 _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4;
Unity_Multiply_float4_float4(_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4, (_Property_a17cc44b153a4a7ea467dca84e307990_Out_0_Float.xxxx), _Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4);
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.BaseColor = (_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4.xyz);
surface.Emission = (_Multiply_803b821af943432386d92cabc85c66fc_Out_2_Vector4.xyz);
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_TEXCOORD0
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float4 uv0;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


    output.uv0 = input.texCoord0;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
 float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

struct Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float
{
};

void SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float(float _Shadow, float _ShadowLimit, float4 _BaseColor, float4 _ShadowColor, Gradient _Gradient, Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float IN, out float4 OutVector4_1)
{
float4 _Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4 = _BaseColor;
float4 _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4 = _ShadowColor;
Gradient _Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient = _Gradient;
float _Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float = _Shadow;
float _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float;
Unity_OneMinus_float(_Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float, _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float);
float _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float = _ShadowLimit;
float _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float;
Unity_Add_float(_OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float, _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float);
float4 _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4;
Unity_SampleGradientV1_float(_Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float, _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4);
float4 _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4;
Unity_Saturate_float4(_SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4, _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4);
float4 _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4, _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4, _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4, (_Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4).x);
OutVector4_1 = _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[0];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_G_2_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[1];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_B_3_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[2];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_A_4_Float = 0;
float _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float;
Unity_Remap_float(_Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float, float2 (0, 1), float2 (-1, 1), _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float);
float3 _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3;
Unity_Contrast_float((_Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float.xxx), float(3), _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3);
float3 _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3, _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3);
float _Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean = _Texture;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float4 _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4 = _Color1;
float4 _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4;
Unity_Branch_float4(_Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean, _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4, _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4);
float4 _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4 = _Shadow_Color;
Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a;
float4 _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4;
SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float((_Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3).x, float(0), _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4, _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4, NewGradient(0, 2, 2, float4(0, 0, 0, 0.5000076),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a, _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4);
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.BaseColor = (_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4.xyz);
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Universal 2D"
    Tags
    {
        "LightMode" = "Universal2D"
    }

// Render State
Cull Off
Blend One Zero
ZTest LEqual
Offset -1, -1
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D
#define _ALPHATEST_ON 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float2 PixelPosition;
 float4 uv0;
 float4 uv1;
 float4 uv2;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float3 positionWS : INTERP3;
 float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _Color1;
float4 _Texture_01_TexelSize;
float _Texture;
float2 _UV_Tiling;
float4 _Shadow_Color;
float _AlphaClipping;
float _Metallic;
float _Smoothness;
float _AmbientVisibility;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Texture_01);
SAMPLER(sampler_Texture_01);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
{
    // convert to OkLab if we need perceptual color space.
    float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);

    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
        color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
    }
    color = lerp(color, OklabToLinear(color), Gradient.type == 2);

#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif

    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
    }

    Out = float4(color, alpha);
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

struct Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float
{
};

void SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float(float _Shadow, float _ShadowLimit, float4 _BaseColor, float4 _ShadowColor, Gradient _Gradient, Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float IN, out float4 OutVector4_1)
{
float4 _Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4 = _BaseColor;
float4 _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4 = _ShadowColor;
Gradient _Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient = _Gradient;
float _Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float = _Shadow;
float _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float;
Unity_OneMinus_float(_Property_d4522eac062348f1a23cc5c4d259acef_Out_0_Float, _OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float);
float _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float = _ShadowLimit;
float _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float;
Unity_Add_float(_OneMinus_b51327a03179423ab27b942f781ac0d2_Out_1_Float, _Property_0fec2376ef514d9f93004fa2877b7b8f_Out_0_Float, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float);
float4 _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4;
Unity_SampleGradientV1_float(_Property_a06e6ff9b15e47669faa7066db245c3b_Out_0_Gradient, _Add_1749f0d134854bbeb466d55d54202458_Out_2_Float, _SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4);
float4 _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4;
Unity_Saturate_float4(_SampleGradient_a293635714c74e2ea6f38bbb00779857_Out_2_Vector4, _Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4);
float4 _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Property_75e130a86b884b2a8f9675f0c6dab60c_Out_0_Vector4, _Property_090eca2a0179435db28f4e39ccf2d84c_Out_0_Vector4, _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4, (_Saturate_0f1ff6ee725c42dfb2400235b25cb393_Out_1_Vector4).x);
OutVector4_1 = _Blend_52aa67f7c6054499b5f62cd10bb730b8_Out_2_Vector4;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.Position = IN.ObjectSpacePosition;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float3 _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, IN.WorldSpaceNormal, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[0];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_G_2_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[1];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_B_3_Float = _BakedGI_f3eaa3fb5a69479cb94a9194dc330156_Out_1_Vector3[2];
float _Split_2fac2a70c1be46ac9b4cc24e2806edef_A_4_Float = 0;
float _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float;
Unity_Remap_float(_Split_2fac2a70c1be46ac9b4cc24e2806edef_R_1_Float, float2 (0, 1), float2 (-1, 1), _Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float);
float3 _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3;
Unity_Contrast_float((_Remap_3a54e8ad9a7b42f0b9ee7454f6375fb1_Out_3_Float.xxx), float(3), _Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3);
float3 _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_2aa0ae8e63fd4a5aa78de2a57c42507d_Out_2_Vector3, _Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3);
float _Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean = _Texture;
UnityTexture2D _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Texture_01, sampler_Texture_01, _Texture_01_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2 = _UV_Tiling;
float2 _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_db9c32b0eaf74abcbf9afc0d27bcbab6_Out_0_Vector2, float2 (0, 0), _TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2);
float4 _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.tex, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.samplerstate, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c97e3dcbf08d4fceac72ff77deae02a3_Out_3_Vector2) );
if (_Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_dc4e7e4ed9364d10907495a596af3cb7_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_R_4_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.r;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_G_5_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.g;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_B_6_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.b;
float _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4.a;
float4 _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4 = _Color1;
float4 _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4;
Unity_Branch_float4(_Property_525a591a135f463cb3a969fd3d706d7e_Out_0_Boolean, _SampleTexture2D_d2628618aa984248b7800424ba227b1d_RGBA_0_Vector4, _Property_1e94638529884b349ebcbc3a22496efd_Out_0_Vector4, _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4);
float4 _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4 = _Shadow_Color;
Bindings_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a;
float4 _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4;
SG_SHDLightingfunctionCEL_0d7dd8b497453044eb65ba16d7b0aa28_float((_Saturate_4ac83b2a33d14ce89850937a4056d5e4_Out_1_Vector3).x, float(0), _Branch_c9785c170058458d9fa74684f5a8d6c7_Out_3_Vector4, _Property_6d6ec220ddca4807962d7d5f001cca14_Out_0_Vector4, NewGradient(0, 2, 2, float4(0, 0, 0, 0.5000076),float4(1, 1, 1, 1),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a, _SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4);
float _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float = _AlphaClipping;
surface.BaseColor = (_SHDLightingfunctionCEL_83445db072ed4de2857cd8a15474154a_OutVector4_1_Vector4.xyz);
surface.Alpha = _SampleTexture2D_d2628618aa984248b7800424ba227b1d_A_7_Float;
surface.AlphaClipThreshold = _Property_6de552d7faf341bcbb147fb10fca9146_Out_0_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif


    output.uv0 = input.texCoord0;
    output.uv1 = input.texCoord1;
    output.uv2 = input.texCoord2;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}