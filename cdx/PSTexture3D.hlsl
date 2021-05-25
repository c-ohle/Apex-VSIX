cbuffer cbPsPerObject : register(b1) 
{
	float4 g_vDiffuse	: packoffset(c0);
};

Texture2D	g_txDiffuse : register(t0);

SamplerState g_samLinear : register(s0);

struct PS_INPUT
{ 
	float3 N : NORMAL;
	float2 T : TEXCOORD0;
};  

float4 main(PS_INPUT Input) : SV_TARGET
{  
	float4 c = g_txDiffuse.Sample(g_samLinear, Input.T);
	return c * float4(Input.N, 1) * g_vDiffuse; // , g_vDiffuse.w* c.w);
}