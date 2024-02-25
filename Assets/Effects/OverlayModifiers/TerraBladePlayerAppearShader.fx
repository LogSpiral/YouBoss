sampler baseTexture : register(s0);
sampler eraseNoiseTexture : register(s1);

float globalTime;
float appearanceInterpolant;
float3 blendColor;

float BlendMode(float a, float b)
{
    return a + b * (a + b);
}

float3 BlendMode(float3 a, float3 b)
{
    return float3(BlendMode(a.r, b.r), BlendMode(a.g, b.g), BlendMode(a.b, b.b));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the local brightness value.
    float brightness = 1 - appearanceInterpolant;
    
    // Calculate the noise values for the pixel erasure.
    float2 noiseCoords = coords * 3;
    noiseCoords = round(noiseCoords / 0.018) * 0.018;
    
    // Use the noise and brightness to determine if this pixel should be erased or not.
    float eraseThreshold = 1.35 - brightness;
    float eraseNoise = tex2D(eraseNoiseTexture, noiseCoords);
    float shouldErasePixel = step(1 - coords.y, eraseThreshold - eraseNoise * 0.15);
    
    // Combine colors.
    float4 color = tex2D(baseTexture, coords);
    float4 sampleMask = lerp(sampleColor, 1, brightness);
    float opacity = color.a * shouldErasePixel;
    return float4(BlendMode(color.rgb, blendColor * color.r / clamp(color.b, 0.6, 1) * brightness), 1) * sampleMask * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}