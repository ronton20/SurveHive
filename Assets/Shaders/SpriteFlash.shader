// Sprite shader with a hit-flash: _FlashAmount (0..1) lerps the sprite toward
// _FlashColor (white by default; hue-shifted by active status effects — PLAN
// 2A) while keeping its alpha. Driven per-renderer through a
// MaterialPropertyBlock by SurveHive.View.HitFlash.
Shader "SurveHive/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        // Rank/status tint, set per-renderer via MaterialPropertyBlock. Lives
        // in the shader because the rig's animation clips keyframe the
        // SpriteRenderer color every frame — renderer.color writes get
        // clobbered, shader properties don't (PLAN 2A).
        _Tint ("Rank/Status Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // No _MainTex_ST: sprites never tile, and _ST/_TexelSize properties
            // disable the 2D SRP batcher for materials that declare them.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FlashAmount;
                half4 _FlashColor;
                half4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color * _Tint;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                color.rgb = lerp(color.rgb, _FlashColor.rgb, _FlashAmount);
                // Premultiplied alpha to match Unity's default sprite blending.
                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
