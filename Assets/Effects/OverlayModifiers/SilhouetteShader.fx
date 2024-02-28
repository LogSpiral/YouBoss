sampler2D baseTexture : register(s0);
bool inverted;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 silhouetteColor = inverted ? float4(0, 0, 0, 1) : 1;
    return silhouetteColor * (tex2D(baseTexture, coords).a > 0) * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}