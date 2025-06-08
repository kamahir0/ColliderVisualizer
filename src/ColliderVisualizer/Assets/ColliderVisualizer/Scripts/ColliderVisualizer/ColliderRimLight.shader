Shader "Hidden/ColliderVisualizer/ColliderRimLight"
{
    Properties
    {
        _Color("Color", Color) = (0, 1, 0, 1)
        _Alpha("Alpha", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float4 _Color;
            float _Alpha;

            Varyings Vert(AppData v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.viewDir = normalize(GetWorldSpaceViewDir(v.vertex.xyz));

                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float rim = 1.0 - abs(dot(normalize(i.viewDir), normalize(i.worldNormal)));
                float4 color = lerp(_Color, float4(_Color.a, _Color.a, _Color.a, _Color.a), rim);
                color.a = _Alpha;

                return color;
            }
            ENDHLSL
        }
    }
}