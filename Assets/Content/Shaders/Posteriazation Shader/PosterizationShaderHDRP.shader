Shader "Hidden/HSVPosterize"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURE2D_X(_InputTexture);

    float _Steps;
    float _Intensity;
    float _NoiseDither;

    // ── RGB → HSV ──────────────────────────────────────────────────
    float3 RgbToHsv(float3 c)
    {
        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
        float4 q = lerp(float4(p.xyw, c.r),  float4(c.r, p.yzx),  step(p.x, c.r));
        float  d = q.x - min(q.w, q.y);
        float  e = 1.0e-10;
        return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
    }

    // ── HSV → RGB ──────────────────────────────────────────────────
    float3 HsvToRgb(float3 c)
    {
        float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
    }

    // ── Interleaved Gradient Noise — стабильный экранный шум ───────
    // Исходник: Jimenez 2014, "Next Generation Post Processing in Call of Duty"
    float InterleavedGradientNoise(float2 pixelCoord)
    {
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        return frac(magic.z * frac(dot(pixelCoord, magic.xy)));
    }

    // ── Posterize с шумовым дизерингом на границе ──────────────────
    float PosterizeDithered(float v, float steps, float noise, float dither)
    {
        steps = max(steps, 1.0);
        float halfStep = 0.5 / steps;
        v += (noise - 0.5) * halfStep * 2.0 * dither;
        v  = saturate(v);
        return floor(v * steps) / steps;
    }

    // ── Vertex ─────────────────────────────────────────────────────
    struct Attributes { uint vertexID : SV_VertexID; };
    struct Varyings   { float4 pos : SV_Position; float2 uv : TEXCOORD0; };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.pos = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.uv  = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // ── Fragment ───────────────────────────────────────────────────
    float4 Frag(Varyings input) : SV_Target
    {
        uint2  positionSS = input.uv * _ScreenSize.xy;
        float3 color      = LOAD_TEXTURE2D_X(_InputTexture, positionSS).rgb;

        float  noise = InterleavedGradientNoise((float2)positionSS);

        float3 hsv   = RgbToHsv(color);
        hsv.z        = PosterizeDithered(hsv.z, _Steps, noise, _NoiseDither);
        float3 posterized = HsvToRgb(hsv);

        float3 result = lerp(color, posterized, _Intensity);

        return float4(result, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "HSVPosterize"
            ZWrite Off
            ZTest  Always
            Blend  Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}