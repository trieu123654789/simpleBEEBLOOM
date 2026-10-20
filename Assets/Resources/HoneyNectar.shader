Shader "BeeBloom/HoneyNectar"
{
    Properties
    {
        _BaseMap ("Existing nectar texture", 2D) = "white" {}
        _BaseColor ("Amber", Color) = (1,0.72,0.1,1)
        _Warmth ("Confirmation warmth", Range(0,1)) = 0
        _FlowTime ("Liquid time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Warmth;
                float _FlowTime;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float3 positionOS : TEXCOORD3; half3 normalOS : TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv,_BaseMap);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }
            float Square(float value) { return value*value; }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 p = input.positionOS / 0.08;
                half face = pow(abs(input.normalOS.z),4);
                // Gentle convex shading gives the existing flat faces a syrup-like glaze.
                half3 curvedOS = normalize(half3(p.xy*0.33,input.normalOS.z >= 0 ? 1 : -1));
                half3 n = normalize(lerp(input.normalWS,TransformObjectToWorldNormal(curvedOS),face*0.45));
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 light = normalize(half3(-0.4,0.75,-0.55));
                half diffuse = saturate(dot(n,light));
                half rim = pow(1-saturate(dot(n,view)),3);
                half shine = pow(saturate(dot(n,normalize(view+light))),48);
                half3 base = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb * _BaseColor.rgb;
                half3 amber = base * (0.48 + diffuse*0.45);
                half core = saturate(1-length(p.xy)*0.72);
                half swirl = sin(p.x*5+p.y*3+sin(p.y*4-_FlowTime*0.55))*0.5+0.5;
                amber += half3(0.18,0.075,0.005)*core*(0.65+swirl*0.35);
                amber = lerp(amber,half3(1,0.78,0.18),rim*0.52);
                // Two broad reflections, plus small enclosed bubbles. No scene-color/refraction pass.
                float stripePosition = p.x+p.y*0.42+sin(_FlowTime*0.55)*0.035;
                half stripe = exp(-Square((stripePosition-0.18)*11))*0.6 + exp(-Square((stripePosition+0.43)*28))*0.24;
                stripe *= (1-smoothstep(0.3,1,abs(p.y)))*face;
                half bubbles = 0;
                [unroll] for (int i=0;i<6;i++) {
                    float seed = i*2.39996;
                    float2 centre = float2(sin(seed)*0.52,cos(seed*1.7)*0.56);
                    centre.y += sin(_FlowTime*0.6+seed)*0.025;
                    float radius = 0.035+frac(i*0.37)*0.04;
                    float d = length(p.xy-centre);
                    bubbles += exp(-Square((d-radius)*95))*0.2 + exp(-Square(length(p.xy-centre-float2(-0.014,0.018))*110))*0.5;
                }
                amber += half3(1,0.93,0.65)*(shine*0.65 + stripe*0.48 + bubbles*face*0.42);
                amber += half3(0.15,0.075,0.008)*_Warmth;
                return half4(amber,1);
            }
            ENDHLSL
        }
    }
}
