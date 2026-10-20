Shader "LetItBee/HoneyFeedback"
{
    Properties { _Ribbon ("Ribbon", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Ribbon;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.uv * 2 - 1;
                half radius = lerp(length(p), abs(p.y), _Ribbon);
                half halo = saturate(1 - radius);
                half core = 1 - smoothstep(0.12, 0.45, radius);
                half alpha = (halo * halo * 0.34 + core * 0.76) * input.color.a;
                return half4(lerp(input.color.rgb, half3(1, 0.96, 0.73), core * lerp(0.4,0.1,_Ribbon)), alpha);
            }
            ENDHLSL
        }
    }
}
