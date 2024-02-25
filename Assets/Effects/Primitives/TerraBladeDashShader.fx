sampler streakTexture : register(s1);

float globalTime;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color + smoothstep(0.2, 0.09, coords.x);
    float cutoffThreshold = coords.x * 0.7;
    float brightnessAccent = tex2D(streakTexture, coords * 0.6 + float2(globalTime * -1.5, 0));
    if (step(pow(coords.x, 1.56) * 1.3, tex2D(streakTexture, coords * 0.45 + float2(globalTime * -1.7, 0)).r * 1.5) <= 0)
        return 0;
    
    float edgeFade = smoothstep(0.5, 0.3, distance(coords.y, 0.5));
    return color * lerp(1, 1.9, brightnessAccent) * edgeFade;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
