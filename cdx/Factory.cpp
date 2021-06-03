#include "pch.h"
#include "Factory.h"
#include "view.h"
#include "font.h"

#include "ShaderInc\VSMain.h"
#include "ShaderInc\VSWorld.h"
#include "ShaderInc\VSLight.h"
#include "ShaderInc\PSMain.h"
#include "ShaderInc\PSTexture.h"
#include "ShaderInc\PSFont.h"
#include "ShaderInc\PSMain3D.h"
#include "ShaderInc\PSTexture3D.h"
#include "ShaderInc\PSTextureMask.h"
#include "ShaderInc\PSTexturePts.h"
#include "ShaderInc\PSSpec3D.h"
#include "ShaderInc\GSShadows.h"
#include "ShaderInc\GSOutline3D.h"

const D3D11_INPUT_ELEMENT_DESC layout[] =
{
  { "POSITION",  0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0,  D3D11_INPUT_PER_VERTEX_DATA, 0 },
  { "NORMAL",    0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
  { "TEXCOORD",  0, DXGI_FORMAT_R32G32_FLOAT,    0, 24, D3D11_INPUT_PER_VERTEX_DATA, 0 },
};
struct CB_VS_PER_FRAME
{
  XMMATRIX g_mViewProjection;
  XMVECTOR g_fAmbient;
  XMVECTOR g_vLightDir;
};
struct CB_VS_PER_OBJECT
{
  XMMATRIX g_mWorld;
};
struct CB_PS_PER_OBJECT
{
  XMVECTOR g_vDiffuse;
};
struct shaders { const BYTE* p; UINT n; };
static shaders _vsshaders[MO_VSSHADER_COUNT] =
{
  { _VSMain,			sizeof(_VSMain)			  },
  { _VSWorld,			sizeof(_VSWorld)		  },
  { _VSLight,			sizeof(_VSLight)		  },
};
static shaders _psshaders[MO_PSSHADER_COUNT] =
{
  { _PSMain,			  sizeof(_PSMain)				  }, //INV_VV_DIFFUSE
  { _PSTexture,		  sizeof(_PSTexture)		  }, //INV_VV_DIFFUSE | INV_TT_DIFFUSE
  { _PSFont,			  sizeof(_PSFont)				  }, //INV_VV_DIFFUSE | INV_TT_DIFFUSE
  { _PSMain3D,		  sizeof(_PSMain3D)			  },
  { _PSTexture3D,	  sizeof(_PSTexture3D)	  },
  { _PSTextureMask,	sizeof(_PSTextureMask)	},
  { _PSTexturePts,	sizeof(_PSTexturePts)	  },
};
static shaders _gsshaders[MO_GSSHADER_COUNT] =
{
  { _GSShadows,		sizeof(_GSShadows)		},
  { _GSOutline3D,	sizeof(_GSOutline3D)	},
};

static void CreateDepthStencilState(ID3D11Device* device, UINT m, ID3D11DepthStencilState** p)
{
  D3D11_DEPTH_STENCIL_DESC desc;
  desc.DepthEnable = 1;
  desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ZERO;
  desc.DepthFunc = D3D11_COMPARISON_LESS_EQUAL;
  desc.StencilEnable = 1;
  desc.StencilReadMask = D3D11_DEFAULT_STENCIL_READ_MASK;
  desc.StencilWriteMask = D3D11_DEFAULT_STENCIL_WRITE_MASK;
  desc.FrontFace.StencilFunc = D3D11_COMPARISON_EQUAL;
  desc.FrontFace.StencilDepthFailOp = D3D11_STENCIL_OP_KEEP;
  desc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_KEEP;
  desc.FrontFace.StencilFailOp = D3D11_STENCIL_OP_KEEP; desc.BackFace = desc.FrontFace;
  switch (m & MO_DEPTHSTENCIL_MASK)
  {
  case MO_DEPTHSTENCIL_ZWRITE:
    desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
    break;
  case MO_DEPTHSTENCIL_STEINC:
    desc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_INCR;
    desc.BackFace.StencilPassOp = D3D11_STENCIL_OP_INCR;
    break;
  case MO_DEPTHSTENCIL_STEDEC:
    desc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_DECR;
    desc.BackFace.StencilPassOp = D3D11_STENCIL_OP_DECR;
    break;
  case MO_DEPTHSTENCIL_CLEARZ:
    desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
    desc.DepthFunc = D3D11_COMPARISON_ALWAYS;
    desc.FrontFace.StencilFunc = D3D11_COMPARISON_LESS_EQUAL;
    desc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_REPLACE;
    break;
  case MO_DEPTHSTENCIL_TWOSID:
    desc.DepthFunc = D3D11_COMPARISON_LESS;
    desc.FrontFace.StencilFunc = D3D11_COMPARISON_ALWAYS;
    desc.BackFace.StencilFunc = D3D11_COMPARISON_ALWAYS;
    desc.FrontFace.StencilDepthFailOp = D3D11_STENCIL_OP_DECR;
    desc.BackFace.StencilDepthFailOp = D3D11_STENCIL_OP_INCR;
    break;
  case MO_DEPTHSTENCIL_CLEARS:
    desc.DepthEnable = 0;
    desc.FrontFace.StencilFunc = D3D11_COMPARISON_LESS;
    desc.FrontFace.StencilPassOp = D3D11_STENCIL_OP_REPLACE;
    break;
  case MO_DEPTHSTENCIL_REST:
    desc.FrontFace.StencilFunc = D3D11_COMPARISON_LESS;
    break;
  }
  auto hr = device->CreateDepthStencilState(&desc, p); XMASSERT(!hr); //todo: sink.p->OnMessage
}
static void CreateBlendState(ID3D11Device* device, UINT m, ID3D11BlendState** p)
{
  D3D11_BLEND_DESC desc;
  desc.AlphaToCoverageEnable = 0;
  desc.IndependentBlendEnable = 0;
  desc.RenderTarget[0].BlendEnable = 0;
  desc.RenderTarget[0].SrcBlend = D3D11_BLEND_ONE;
  desc.RenderTarget[0].DestBlend = D3D11_BLEND_ZERO;
  desc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
  desc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
  desc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
  desc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
  desc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
  if ((m & MO_BLENDSTATE_MASK) == MO_BLENDSTATE_ALPHA)
  {
    desc.RenderTarget[0].BlendEnable = 1;
    desc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
    desc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
    desc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_SRC_ALPHA;
    desc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_INV_SRC_ALPHA;
  }
  if ((m & MO_BLENDSTATE_MASK) == MO_BLENDSTATE_ALPHAADD)
  {
    desc.RenderTarget[0].BlendEnable = 1;
    desc.RenderTarget[0].SrcBlend = D3D11_BLEND_ONE;
    desc.RenderTarget[0].DestBlend = D3D11_BLEND_ONE;
  }
  auto hr = device->CreateBlendState(&desc, p); XMASSERT(!hr);
}
static void CreateRasterizerState(ID3D11Device* device, UINT m, UINT rh, ID3D11RasterizerState** p)
{
  D3D11_RASTERIZER_DESC desc;
  desc.FillMode = (m & MO_RASTERIZER_MASK) == MO_RASTERIZER_WIRE ? D3D11_FILL_WIREFRAME : D3D11_FILL_SOLID;
  desc.CullMode = (m & MO_RASTERIZER_MASK) == MO_RASTERIZER_NOCULL ? D3D11_CULL_NONE : (m & MO_RASTERIZER_MASK) == MO_RASTERIZER_FRCULL ? D3D11_CULL_FRONT : D3D11_CULL_BACK;
  desc.FrontCounterClockwise = rh;
  desc.DepthBias = 0;
  desc.DepthBiasClamp = 0;
  desc.SlopeScaledDepthBias = 0;
  desc.DepthClipEnable = 1;
  desc.ScissorEnable = 0;
  desc.MultisampleEnable = 1;
  desc.AntialiasedLineEnable = 0;
  auto hr = device->CreateRasterizerState(&desc, p); XMASSERT(!hr);
}
static void CreateSamplerState(ID3D11Device* device, UINT m, ID3D11SamplerState** p)
{
  D3D11_SAMPLER_DESC desc;
  desc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
  desc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
  desc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
  desc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
  desc.MipLODBias = 0.0f;
  desc.MaxAnisotropy = 1;
  desc.ComparisonFunc = D3D11_COMPARISON_ALWAYS;
  desc.BorderColor[0] = desc.BorderColor[1] = desc.BorderColor[2] = desc.BorderColor[3] = 0;//0x00ffffff;
  desc.MinLOD = 0;
  desc.MaxLOD = D3D11_FLOAT32_MAX;
  switch (m & MO_SAMPLERSTATE_MASK)
  {
  case MO_SAMPLERSTATE_VBORDER:
    desc.AddressV = D3D11_TEXTURE_ADDRESS_BORDER;
    break;
  case MO_SAMPLERSTATE_FONT:
    desc.AddressU = D3D11_TEXTURE_ADDRESS_BORDER;
    desc.AddressV = D3D11_TEXTURE_ADDRESS_BORDER;
    desc.MipLODBias = -0.5f;
    break;
  case MO_SAMPLERSTATE_IMAGE:
    //desc.BorderColor[0] = desc.BorderColor[1] = desc.BorderColor[2] = desc.BorderColor[3] = 0x00ffffff;
    desc.AddressU = D3D11_TEXTURE_ADDRESS_BORDER;
    desc.AddressV = D3D11_TEXTURE_ADDRESS_BORDER;
    break;
  }
  auto hr = device->CreateSamplerState(&desc, p); XMASSERT(!hr); //todo: sink.p->OnMessage
}

UINT                              adapterid, currentmode, stencilref, invstate;
CComPtr<ID3D11Device>             device;
CComPtr<ID3D11DeviceContext>      context;
CComPtr<ID3D11DepthStencilState>	depthstencilstates[MO_DEPTHSTENCIL_COUNT];
CComPtr<ID3D11BlendState>					blendstates[MO_BLENDSTATE_COUNT];
CComPtr<ID3D11RasterizerState>		rasterizerstates[MO_RASTERIZER_COUNT];
CComPtr<ID3D11SamplerState>				samplerstates[MO_SAMPLERSTATE_COUNT];
CComPtr<ID3D11VertexShader>				vertexshader[MO_VSSHADER_COUNT];
CComPtr<ID3D11GeometryShader>			geometryshader[MO_GSSHADER_COUNT];
CComPtr<ID3D11PixelShader>				pixelshader[MO_PSSHADER_COUNT];
CComPtr<ID3D11InputLayout>        vertexlayout;
CComPtr<ID3D11Buffer>             cbbuffer[3];
void* currentbuffer[3];

CComPtr<CFont>                    d_font; UINT d_blend;
CComPtr<CTexture>                 d_texture;

void SetMode(UINT mode)
{
  if (mode == currentmode) return;
  auto mask = mode ^ currentmode; currentmode = mode;
  if (mask)
  {
    if (mask & MO_DEPTHSTENCIL_MASK)
    {
      UINT i = (mode & MO_DEPTHSTENCIL_MASK) >> MO_DEPTHSTENCIL_SHIFT;
      if (i < MO_DEPTHSTENCIL_COUNT && !depthstencilstates[i].p) CreateDepthStencilState(device, mode, &depthstencilstates[i].p);
      context->OMSetDepthStencilState(i < MO_DEPTHSTENCIL_COUNT ? depthstencilstates[i].p : 0, stencilref);
    }
    if (mask & MO_BLENDSTATE_MASK)
    {
      UINT i = (mode & MO_BLENDSTATE_MASK) >> MO_BLENDSTATE_SHIFT;
      if (i < MO_BLENDSTATE_COUNT && !blendstates[i].p) CreateBlendState(device, mode, &blendstates[i].p);
      float ffff[4] = { 0 };
      context->OMSetBlendState(i < MO_BLENDSTATE_COUNT ? blendstates[i].p : 0, ffff, -1);
    }
    if (mask & MO_RASTERIZER_MASK)
    {
      UINT i = (mode & MO_RASTERIZER_MASK) >> MO_RASTERIZER_SHIFT;
      if (i < MO_RASTERIZER_COUNT && !rasterizerstates[i].p) CreateRasterizerState(device, currentmode, /*moderh*/1, &rasterizerstates[i].p);
      context->RSSetState(i < MO_RASTERIZER_COUNT ? rasterizerstates[i].p : 0);
    }
    if (mask & MO_SAMPLERSTATE_MASK)
    {
      UINT i = (mode & MO_SAMPLERSTATE_MASK) >> MO_SAMPLERSTATE_SHIFT;
      if (i < MO_SAMPLERSTATE_COUNT && !samplerstates[i].p) CreateSamplerState(device, mode, &samplerstates[i].p);
      context->PSSetSamplers(0, 1, &samplerstates[i].p);
    }
    if (mask & MO_VSSHADER_MASK)
    {
      UINT i = (mode & MO_VSSHADER_MASK) >> MO_VSSHADER_SHIFT;
      if (!vertexshader[i].p) {
        auto hr = device->CreateVertexShader(_vsshaders[i].p, _vsshaders[i].n, 0, &vertexshader[i].p); XMASSERT(!hr);
      }
      context->VSSetShader(vertexshader[i].p, 0, 0);
    }
    if (mask & MO_PSSHADER_MASK)
    {
      UINT i = (mode & MO_PSSHADER_MASK) >> MO_PSSHADER_SHIFT;
      if (i < MO_PSSHADER_COUNT && !pixelshader[i].p) { auto hr = device->CreatePixelShader(_psshaders[i].p, _psshaders[i].n, 0, &pixelshader[i].p); XMASSERT(!hr); }
      context->PSSetShader(i < MO_PSSHADER_COUNT ? pixelshader[i].p : 0, 0, 0);
    }
    if (mask & MO_GSSHADER_MASK)
    {
      UINT i = ((mode & MO_GSSHADER_MASK) >> MO_GSSHADER_SHIFT) - 1;
      if (i < MO_GSSHADER_COUNT && !geometryshader[i].p) { auto hr = device->CreateGeometryShader(_gsshaders[i].p, _gsshaders[i].n, 0, &geometryshader[i].p); XMASSERT(!hr); }
      context->GSSetShader(i < MO_GSSHADER_COUNT ? geometryshader[i].p : 0, 0, 0);
    }
    if (mask & MO_TOPO_MASK)
    {
      UINT i = (mode & MO_TOPO_MASK) >> MO_TOPO_SHIFT;
      XMASSERT(i > D3D_PRIMITIVE_TOPOLOGY_UNDEFINED && i <= D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP_ADJ);
      context->IASetPrimitiveTopology((D3D_PRIMITIVE_TOPOLOGY)i);
    }
  }
}
void SetVertexBuffer(ID3D11Buffer* p)
{
  if (currentbuffer[0] == p) return; currentbuffer[0] = p;
  const UINT stride = 32, offs = 0;
  context->IASetVertexBuffers(0, 1, &p, &stride, &offs);
}
void SetIndexBuffer(ID3D11Buffer* p)
{
  if (currentbuffer[1] == p) return; currentbuffer[1] = p;
  context->IASetIndexBuffer(p, DXGI_FORMAT_R16_UINT, 0);
}
void SetTexture(ID3D11ShaderResourceView* p)
{
  if (currentbuffer[2] == p) return; currentbuffer[2] = p;
  context->PSSetShaderResources(0, 1, &p);
}
void CView::SetBuffers()
{
  if (!(invstate & (INV_CB_VS_PER_FRAME | INV_CB_VS_PER_OBJECT | INV_CB_PS_PER_OBJECT | INV_TT_DIFFUSE))) return;
  if (invstate & INV_CB_VS_PER_FRAME)
  {
    invstate &= ~INV_CB_VS_PER_FRAME;
    auto& cb = cbbuffer[0].p; D3D11_MAPPED_SUBRESOURCE map;
    context->Map(cb, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
    auto p = (CB_VS_PER_FRAME*)map.pData;
    p->g_mViewProjection = mm[MM_VIEWPROJ];
    p->g_fAmbient = vv[VV_AMBIENT];
    p->g_vLightDir = vv[VV_LIGHTDIR];
    context->Unmap(cb, 0);
    context->VSSetConstantBuffers(0, 1, &cb);
    context->GSSetConstantBuffers(0, 1, &cb); //todo: mask out !!!
  }
  if (invstate & INV_CB_VS_PER_OBJECT)
  {
    invstate &= ~INV_CB_VS_PER_OBJECT;
    auto& cb = cbbuffer[1].p; D3D11_MAPPED_SUBRESOURCE map;
    context->Map(cb, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
    auto p = (CB_VS_PER_OBJECT*)map.pData;
    p->g_mWorld = mm[MM_WORLD];
    context->Unmap(cb, 0);
    context->VSSetConstantBuffers(1, 1, &cb);
  }
  if (invstate & INV_CB_PS_PER_OBJECT)
  {
    invstate &= ~INV_CB_PS_PER_OBJECT;
    auto& cb = cbbuffer[2].p; D3D11_MAPPED_SUBRESOURCE map;
    context->Map(cb, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
    auto p = (CB_PS_PER_OBJECT*)map.pData;
    p->g_vDiffuse = vv[VV_DIFFUSE];
    context->Unmap(cb, 0);
    context->PSSetConstantBuffers(1, 1, &cb);
  }
  if (invstate & INV_TT_DIFFUSE)
  {
    invstate &= ~INV_TT_DIFFUSE;
    //ID3D11ShaderResourceView* srv = 0;
    //if (tt[TT_DIFFUSE].p) { auto t = (CTexture*)tt[TT_DIFFUSE].p; if (!t->srv11.p) t->init11(device, context); srv = t->srv11.p; }
    //context->PSSetShaderResources(0, 1, &srv);
  }
}
void CView::SetColor(UINT i, UINT v)
{
  auto p = XMLoadColor((const XMCOLOR*)&v);
  if (XMComparisonAllTrue(XMVector4EqualR(vv[i], p))) return;
  vv[i] = p; invstate |= (0x000001 << i);
}
void CView::SetVector(UINT i, const XMVECTOR& p)
{
  if (XMComparisonAllTrue(XMVector4EqualR(vv[i], p))) return;
  vv[i] = p; invstate |= (0x000001 << i);
}
void CView::SetMatrix(UINT i, const XMMATRIX& p)
{
  //if(!XMComparisonAllTrue(XMVector4EqualR(vv[i], p))) return;
  mm[i] = p; invstate |= (0x000100 << i);
}

CComPtr<ID3D11Texture2D> rtvtex1, dsvtex1, rtvcpu1, dsvcpu1;
CComPtr<ID3D11RenderTargetView> rtv1;
CComPtr<ID3D11DepthStencilView> dsv1;

static void initpixel()
{
  D3D11_TEXTURE2D_DESC td = { 0 };
  td.Width = td.Height = td.ArraySize = td.MipLevels = td.SampleDesc.Count = 1;
  td.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
  td.Format = DXGI_FORMAT_B8G8R8A8_UNORM; //td.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
  device->CreateTexture2D(&td, 0, &rtvtex1);

  D3D11_RENDER_TARGET_VIEW_DESC rdesc; memset(&rdesc, 0, sizeof(rdesc));
  rdesc.Format = td.Format;
  rdesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
  device->CreateRenderTargetView(rtvtex1.p, &rdesc, &rtv1);

  td.BindFlags = D3D11_BIND_DEPTH_STENCIL;
  td.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
  device->CreateTexture2D(&td, 0, &dsvtex1);

  D3D11_DEPTH_STENCIL_VIEW_DESC ddesc; memset(&ddesc, 0, sizeof(ddesc));
  ddesc.Format = td.Format;
  ddesc.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
  device->CreateDepthStencilView(dsvtex1, &ddesc, &dsv1);

  td.BindFlags = 0;
  td.CPUAccessFlags = D3D11_CPU_ACCESS_READ | D3D11_CPU_ACCESS_WRITE;
  td.Usage = D3D11_USAGE_STAGING;
  td.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
  device->CreateTexture2D(&td, 0, &rtvcpu1);

  td.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
  device->CreateTexture2D(&td, 0, &dsvcpu1);
}

CComPtr<ID3D11Buffer> ringbuffer;
UINT rbindex, rbcount;
static void rballoc(UINT nv)
{
  ringbuffer.Release();
  D3D11_BUFFER_DESC bd;
  bd.Usage = D3D11_USAGE_DYNAMIC;
  bd.ByteWidth = (rbcount = (((nv >> 11) + 1) << 11)) << 5; //64kb 2kv
  bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
  bd.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
  bd.MiscFlags = 0; bd.StructureByteStride = 0;
  device->CreateBuffer(&bd, 0, &ringbuffer);
}

VERTEX* CView::BeginVertices(UINT nv)
{
  if (rbindex + nv > rbcount) { if (nv > rbcount) rballoc(nv); rbindex = 0; }
  D3D11_MAPPED_SUBRESOURCE map;
  context->Map(ringbuffer, 0, rbindex != 0 ? D3D11_MAP_WRITE_NO_OVERWRITE : D3D11_MAP_WRITE_DISCARD, 0, &map);
  auto vv = (VERTEX*)map.pData + rbindex; memset(vv, 0, nv * sizeof(VERTEX)); return vv;
}
void CView::EndVertices(UINT nv, UINT mode)
{
  if (pickprim)
  {
    auto t1 = vv[VV_DIFFUSE]; SetColor(VV_DIFFUSE, pickprim != -1 ? pickprim++ : pickprim);
    auto t2 = pickprim; pickprim = 0;
    if (mode)
    {
      auto ma = mode & (MO_BLENDSTATE_MASK | MO_PSSHADER_MASK);
      if (ma == (MO_PSSHADER_TEXTURE | MO_BLENDSTATE_ALPHA) || ma == (MO_PSSHADER_TEXTUREPTS | MO_BLENDSTATE_ALPHA))
        mode = (mode & ~(MO_BLENDSTATE_MASK | MO_PSSHADER_MASK)) | (MO_PSSHADER_TEXMASK | MO_BLENDSTATE_SOLID);
      if ((mode & MO_PSSHADER_MASK) == MO_PSSHADER_COLOR3D) //Arrows
        mode = (mode & ~MO_PSSHADER_MASK) | MO_PSSHADER_COLOR;
    }
    else SetBuffers();
    EndVertices(nv, mode); 
    vv[VV_DIFFUSE] = t1; pickprim = t2; 
    invstate |= (0x000001 << VV_DIFFUSE); return;
  }

  context->Unmap(ringbuffer, 0);
  if (mode != 0) { SetVertexBuffer(ringbuffer.p); SetMode(mode); SetBuffers(); }
  context->Draw(nv, rbindex); rbindex += nv;
}

void CView::ReEndVertices(UINT nv)
{
  SetVertexBuffer(ringbuffer.p); SetBuffers();
  context->Draw(nv, rbindex - nv);
}

HRESULT CView::get_OverNode(ICDXNode** p)
{
  if (overnode.p) (*p = overnode.p)->AddRef();
  return 0;
}
void CView::Pick(const short* pt)
{
  iover = 0; if (!swapchain.p) return;
  XMFLOAT2 pc(pt[0], pt[1]);
  //POINT mp; GetCursorPos(&mp); ScreenToClient(hwnd, &mp); XMFLOAT2 pc(mp.x, mp.y);
  if (!rtvtex1.p) initpixel();
  auto vp = viewport; vp.TopLeftX = -pc.x; vp.TopLeftY = -pc.y;
  context->RSSetViewports(1, &vp); //pixelscale = 0;
  context->OMSetRenderTargets(1, &rtv1.p, dsv1.p); float bk[] = { 0,0,0,0 };
  context->ClearRenderTargetView(rtv1.p, bk);
  context->ClearDepthStencilView(dsv1.p, D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1, 0);
  if (scene.p && camera.p)
  {
    setproject(); const UINT stride = 32, offs = 0; //UINT nflags = 0;
    SetMode(MO_TOPO_TRIANGLELISTADJ | MO_PSSHADER_COLOR | MO_DEPTHSTENCIL_ZWRITE);

    UINT i = 1;
    for (auto p = scene.p->child(); p; p = p->nextsibling(0), i++)
    {
      p->setscount(i);
      if (!p->ib.p || !p->ib.p->p.p) continue;
      SetMatrix(MM_WORLD, p->gettrans(scene.p));
      SetColor(VV_DIFFUSE, i << 16); SetBuffers();
      SetVertexBuffer(p->vb.p->p.p); SetIndexBuffer(p->ib.p->p.p);
      context->DrawIndexed(p->subn ? p->subn : p->ib.p->ni, p->subi, 0);
      //if (p->flags & NODE_FL_SELECT && flags & (CDX_RENDER_COORDINATES | CDX_RENDER_BOUNDINGBOX))
      //{
      //  if (p == camera.p) continue;
      //  pickprim = (i << 16) | 0x8000; renderekbox(p); pickprim = 0;
      //}
    }

    if (flags & (CDX_RENDER_COORDINATES | CDX_RENDER_BOUNDINGBOX))
      for (UINT i = 0; i < scene.p->selection.n; i++)
      {
        auto p = scene.p->selection.p[i];
        if (p == camera.p) continue;
        pickprim = (p->getscount() << 16) | 0x8000; renderekbox(p); pickprim = 0;
      }
  }

  if (sink.p) { pickprim = -1; sink.p->Render(0); pickprim = 0; }

  context->CopyResource(rtvcpu1, rtvtex1);
  D3D11_MAPPED_SUBRESOURCE map;
  context->Map(rtvcpu1, 0, D3D11_MAP_READ, 0, &map);
  iover = *(UINT*)map.pData; context->Unmap(rtvcpu1, 0);
  context->CopyResource(dsvcpu1, dsvtex1);
  context->Map(dsvcpu1, 0, D3D11_MAP_READ, 0, &map);
  auto ppz = *(UINT*)map.pData; context->Unmap(dsvcpu1, 0);

  //TRACE(L"ppc  %x\n", iover);

  setproject(); mm[MM_PLANE] = mm[MM_VIEWPROJ];
  if (iover)
  {
    overnode = scene.p->findscount(iover >> 16);
    //UINT i = (iover >> 16) - 1; auto p = scene.p->child();
    //for (; p && i; p = p->nextsibling(0), i--); overnode = p;
  }
  else overnode = 0;
  if (overnode.p)
  {
    auto pickp = XMVectorSet(
      +((pc.x * 2) / viewport.Width - 1),
      -((pc.y * 2) / viewport.Height - 1), (ppz & 0xffffff) * (1.0f / 0xffffff), 0);
    auto m = XMMatrixInverse(0, overnode.p->gettrans(scene.p) * mm[MM_VIEWPROJ]);
    vv[VV_OVERPOS] = XMVector3TransformCoord(pickp, m);
  }
  else
  {
    vv[VV_OVERPOS] = XMLoadFloat2(&pc);
    Command(CDX_CMD_PICKPLANE, (UINT*)&vv[VV_OVERPOS]);
  }
}

HRESULT CView::get_Samples(BSTR* p)
{
  CComBSTR ss; WCHAR tt[32];
  DXGI_SWAP_CHAIN_DESC desc; swapchain->GetDesc(&desc); wsprintf(tt, L"%i", desc.SampleDesc.Count); ss += tt;
  for (UINT i = 1, q; i <= 16; i++)
    if (device->CheckMultisampleQualityLevels(DXGI_FORMAT_B8G8R8A8_UNORM, i, &q) == 0 && q > 0) { wsprintf(tt, L"\n%i", i); ss += tt; }
  return ss.CopyTo(p);
}
HRESULT CView::get_BkColor(UINT* p)
{
  XMStoreColor((XMCOLOR*)p, vv[VV_BKCOLOR]);
  return 0;
}
HRESULT CView::put_BkColor(UINT p)
{
  SetColor(VV_BKCOLOR, p); return 0;
}

DXGI_SAMPLE_DESC chksmp(ID3D11Device* device, UINT samples)
{
  DXGI_SAMPLE_DESC desc; desc.Count = 1; desc.Quality = 0;
  for (UINT i = samples, q; i > 0; i--)
    if (device->CheckMultisampleQualityLevels(DXGI_FORMAT_B8G8R8A8_UNORM, i, &q) == 0 && q > 0)
    {
      desc.Count = i; desc.Quality = q - 1; break;
    }
  return desc;
}

HRESULT CView::Resize()
{
  viewport.Width = (float)size.cx;// vv[VV_RCSIZE].m128_f32[0] / dpiscale;
  viewport.Height = (float)size.cy;//vv[VV_RCSIZE].m128_f32[1] / dpiscale;
  viewport.MinDepth = 0;
  viewport.MaxDepth = 1;
  viewport.TopLeftX = 0;
  viewport.TopLeftY = 0;
  DXGI_SWAP_CHAIN_DESC desc;
  if (!swapchain.p)
  {
    CComPtr<IDXGIDevice> pDXGIDevice; device->QueryInterface(__uuidof(IDXGIDevice), (void**)&pDXGIDevice.p);
    CComPtr<IDXGIAdapter> adapter; pDXGIDevice->GetAdapter(&adapter.p); pDXGIDevice.Release();
    CComPtr<IDXGIFactory> factory; adapter->GetParent(__uuidof(IDXGIFactory), (void**)&factory.p); adapter.Release();
    desc.BufferDesc.Width = size.cx;
    desc.BufferDesc.Height = size.cy;
    desc.BufferDesc.RefreshRate.Numerator = 60;
    desc.BufferDesc.RefreshRate.Denominator = 1;
    desc.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM; // DXGI_FORMAT_R8G8B8A8_UNORM_SRGB
    desc.BufferDesc.ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
    desc.BufferDesc.Scaling = DXGI_MODE_SCALING_UNSPECIFIED;
    desc.SampleDesc = chksmp(device, sampels); sampels = desc.SampleDesc.Count;
    desc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    desc.BufferCount = 1;
    desc.OutputWindow = hwnd;
    desc.Windowed = 1;
    desc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
    desc.Flags = 0;
    CHR(factory->CreateSwapChain(device, &desc, &swapchain.p));
    CHR(factory->MakeWindowAssociation(hwnd, DXGI_MWA_NO_ALT_ENTER | DXGI_MWA_NO_WINDOW_CHANGES));
  }
  else
  {
    swapchain.p->GetDesc(&desc); rtv.Release(); dsv.Release(); //tds.Release(); 
    CHR(swapchain.p->ResizeBuffers(desc.BufferCount, size.cx, size.cy, desc.BufferDesc.Format, 0));
  }
  CComPtr<ID3D11Texture2D> backbuffer;
  CHR(swapchain.p->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&backbuffer.p));
  D3D11_TEXTURE2D_DESC texdesc; backbuffer.p->GetDesc(&texdesc);
  D3D11_RENDER_TARGET_VIEW_DESC renderDesc; memset(&renderDesc, 0, sizeof(renderDesc));
  renderDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
  renderDesc.ViewDimension = desc.SampleDesc.Count > 1 ? D3D11_RTV_DIMENSION_TEXTURE2DMS : D3D11_RTV_DIMENSION_TEXTURE2D;
  CHR(device->CreateRenderTargetView(backbuffer.p, &renderDesc, &rtv.p));
  D3D11_TEXTURE2D_DESC descDepth;
  descDepth.Width = texdesc.Width;
  descDepth.Height = texdesc.Height;
  descDepth.MipLevels = 1;
  descDepth.ArraySize = 1;
  descDepth.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
  descDepth.SampleDesc.Count = desc.SampleDesc.Count;
  descDepth.SampleDesc.Quality = desc.SampleDesc.Quality;
  descDepth.Usage = D3D11_USAGE_DEFAULT;
  descDepth.BindFlags = D3D11_BIND_DEPTH_STENCIL;
  descDepth.CPUAccessFlags = 0;
  descDepth.MiscFlags = 0;
  CComPtr<ID3D11Texture2D> tds;
  CHR(device->CreateTexture2D(&descDepth, 0, &tds.p));
  D3D11_DEPTH_STENCIL_VIEW_DESC descDSV;
  descDSV.Format = descDepth.Format;
  descDSV.Flags = 0;
  descDSV.ViewDimension = descDepth.SampleDesc.Count > 1 ? D3D11_DSV_DIMENSION_TEXTURE2DMS : D3D11_DSV_DIMENSION_TEXTURE2D;
  descDSV.Texture2D.MipSlice = 0;
  CHR(device->CreateDepthStencilView(tds.p, &descDSV, &dsv.p));
  return 0;
}

HRESULT CreateDevice()
{
  CComPtr<IDXGIAdapter> adapter;
  if (adapterid)
  {
    CComPtr<IDXGIFactory> factory; CHR(CreateDXGIFactory(__uuidof(IDXGIFactory), (void**)&factory.p));
    for (UINT i = 0; factory->EnumAdapters(i, &adapter) == 0; i++, adapter.Release())
    {
      DXGI_ADAPTER_DESC desc; adapter->GetDesc(&desc);
      if (desc.DeviceId == adapterid) break;
    }
  }
  D3D_FEATURE_LEVEL level, ff[] = { D3D_FEATURE_LEVEL_11_0 };
  CHR(D3D11CreateDevice(adapter, adapter.p ? D3D_DRIVER_TYPE_UNKNOWN : D3D_DRIVER_TYPE_HARDWARE, 0,
    D3D11_CREATE_DEVICE_SINGLETHREADED | D3D11_CREATE_DEVICE_BGRA_SUPPORT | (Debug ? D3D11_CREATE_DEVICE_DEBUG : 0),
    ff, sizeof(ff) / sizeof(ff[0]), D3D11_SDK_VERSION, &device, &level, &context));
  CHR(device->CreateVertexShader(_VSMain, sizeof(_VSMain), 0, &vertexshader[0].p));
  CHR(device->CreateInputLayout(layout, 3, _VSMain, sizeof(_VSMain), &vertexlayout.p));
  D3D11_BUFFER_DESC desc;
  desc.Usage = D3D11_USAGE_DYNAMIC;
  desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
  desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
  desc.MiscFlags = 0;
  desc.ByteWidth = sizeof(CB_VS_PER_FRAME);  CHR(device->CreateBuffer(&desc, 0, &cbbuffer[0].p));
  desc.ByteWidth = sizeof(CB_VS_PER_OBJECT); CHR(device->CreateBuffer(&desc, 0, &cbbuffer[1].p));
  desc.ByteWidth = sizeof(CB_PS_PER_OBJECT); CHR(device->CreateBuffer(&desc, 0, &cbbuffer[2].p));
  context->IASetInputLayout(vertexlayout);
  currentmode = 0x7fffffff; invstate = INV_CB_VS_PER_FRAME | INV_CB_VS_PER_OBJECT | INV_CB_PS_PER_OBJECT | INV_TT_DIFFUSE;
  return 0;
}

void releasedx()
{
  {
    Critical crit;
    for (auto p = CView::first; p; p = p->next) p->relres();
    for (auto p = CCacheBuffer::first; p; p = p->next) p->releasedx();
    for (auto p = CVertices::first; p; p = p->next) p->p.Release();
    for (auto p = CIndices::first; p; p = p->next) p->p.Release();
    for (auto p = CFont::first; p; p = p->next) p->relres(0);
  }
  for (UINT i = 0; i < sizeof(cbbuffer) / sizeof(void*); i++) cbbuffer[i].Release();
  for (UINT i = 0; i < sizeof(vertexshader) / sizeof(void*); i++) vertexshader[i].Release();
  for (UINT i = 0; i < sizeof(depthstencilstates) / sizeof(void*); i++) depthstencilstates[i].Release();
  for (UINT i = 0; i < sizeof(blendstates) / sizeof(void*); i++) blendstates[i].Release();
  for (UINT i = 0; i < sizeof(rasterizerstates) / sizeof(void*); i++) rasterizerstates[i].Release();
  for (UINT i = 0; i < sizeof(samplerstates) / sizeof(void*); i++) samplerstates[i].Release();
  for (UINT i = 0; i < sizeof(geometryshader) / sizeof(void*); i++) geometryshader[i].Release();
  for (UINT i = 0; i < sizeof(pixelshader) / sizeof(void*); i++) pixelshader[i].Release();
  for (UINT i = 0; i < sizeof(currentbuffer) / sizeof(void*); i++) currentbuffer[i] = 0;
  ringbuffer.Release(); rbindex = rbcount = 0;
  rtvtex1.Release(); dsvtex1.Release(); rtvcpu1.Release(); dsvcpu1.Release();
  rtv1.Release(); dsv1.Release();
  vertexlayout.Release(); context.Release();
  //CComQIPtr<ID3D11Debug> dbg(device.p); if (dbg.p) dbg.p->ReportLiveDeviceObjects(D3D11_RLDO_SUMMARY);
  int rc = device.p->Release(); device.p = 0;
  XMASSERT(rc == 0);
}

HRESULT __stdcall CFactory::get_Devices(BSTR* p)
{
  if (!device.p) CHR(CreateDevice());
  CComQIPtr<IDXGIDevice> dev(device);
  CComPtr<IDXGIAdapter> adapter; dev->GetAdapter(&adapter.p);
  CComPtr<IDXGIFactory> factory; adapter->GetParent(__uuidof(IDXGIFactory), (void**)&factory);
  DXGI_ADAPTER_DESC desc; adapter->GetDesc(&desc);
  CComBSTR ss; WCHAR tt[32]; wsprintf(tt, L"%i", desc.DeviceId); ss = tt; adapter.Release();
  for (UINT t = 0, last = -1; factory->EnumAdapters(t, &adapter) == 0; adapter.Release(), t++)
  {
    adapter->GetDesc(&desc); if (desc.DeviceId == last) continue;
    wsprintf(tt, L"\n%i\n", last = desc.DeviceId); ss += tt; ss += desc.Description;
  }
  return ss.CopyTo(p);
}

HRESULT __stdcall CFactory::SetDevice(UINT id)
{
  if (id == -1) { if (device.p) { d_font.Release(); d_texture.Release();  releasedx(); } return 0; }
  if (adapterid == id) return 0;
  adapterid = id; if (device.p == 0) return 0;
  releasedx(); CHR(CreateDevice());
  for (auto p = CView::first; p; p = p->next) p->initres();
  return 0;
}
HRESULT __stdcall CFactory::CreateView(HWND hwnd, ICDXSink* sink, UINT samp, ICDXView** p)
{
  if (!device.p) CHR(CreateDevice());
  auto view = new CView(); view->sampels = samp; view->sink = sink; GetClientRect(view->hwnd = hwnd, &view->rcclient);
  auto olddat = SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)view);
  view->proc = (WNDPROC)SetWindowLongPtr(hwnd, GWLP_WNDPROC, (LONG_PTR)CView::WndProc);
  *p = view; SetTimer(hwnd, 0, 0, 0); return 0;
}

HRESULT __stdcall CFactory::GetInfo(CDX_INFO id, UINT* v)
{
  Critical crit;
  switch (id)
  {
  case CDX_INFO_VERTEXBUFFER: for (auto p = CVertices::first; p; p = p->next, (*v)++); return 0;
  case CDX_INFO_INDEXBUFFER:  for (auto p = CIndices::first; p; p = p->next, (*v)++); return 0;
  case CDX_INFO_MAPPINGS:     for (auto p = CCacheBuffer::first; p; p = p->next, (*v)++); return 0;
  case CDX_INFO_TEXTURES:     for (auto p = CCacheBuffer::first; p; p = p->next) if (p->id == CDX_BUFFER_TEXTURE) (*v)++; return 0;
  case CDX_INFO_FONTS:        for (auto p = CFont::first; p; p = p->next, (*v)++); return 0;
  case CDX_INFO_VIEWS:        for (auto p = CView::first; p; p = p->next, (*v)++); return 0;
  }
  return 0;
}

void CView::mapping(VERTEX* vv, UINT nv)
{
  for (UINT i = 0; i < nv; i++)
    XMStoreFloat2(&vv[i].t, XMVector3TransformCoord(XMLoadFloat4A((const XMFLOAT4A*)&vv[i].p), mm[MM_MAPPING]));
}
XMMATRIX XM_CALLCONV CView::W2Screen()
{
  //auto vs = XMMatrixTranslation(1, -1, 0) * XMMatrixScaling(viewport.Width * 0.5f, viewport.Height * -0.5f, 1); 
  return mm[MM_WORLD] * mm[MM_VIEWPROJ] * XMMatrixScaling(viewport.Width * 0.5f, viewport.Height * 0.5f, 1);
}

HRESULT __stdcall CView::Draw(CDX_DRAW id, UINT* data)
{
  switch (id)
  {
  case CDX_DRAW_ORTHOGRAPHIC:
    SetMatrix(MM_VIEWPROJ, XMMatrixOrthographicOffCenterLH(0, viewport.Width, viewport.Height, 0, -1, 1));
    SetMatrix(MM_WORLD, XMMatrixIdentity());
    return 0;
  case CDX_DRAW_GET_TRANSFORM:
    XMStoreFloat4x3((XMFLOAT4X3*)data, mm[MM_WORLD]);
    return 0;
  case CDX_DRAW_SET_TRANSFORM:
    SetMatrix(MM_WORLD, XMLoadFloat4x3((XMFLOAT4X3*)data));
    return 0;
  case CDX_DRAW_GET_COLOR:
    XMStoreColor((XMCOLOR*)data, vv[VV_DIFFUSE]);
    return 0;
  case CDX_DRAW_SET_COLOR:
    SetColor(VV_DIFFUSE, data[0]);
    d_blend = (d_blend & ~MO_BLENDSTATE_MASK) | (data[0] >> 24 != 0xff ? MO_BLENDSTATE_ALPHA : 0);
    return 0;
  case CDX_DRAW_GET_FONT:
    return d_font.CopyTo((CFont**)data);
  case CDX_DRAW_SET_FONT:
    d_font = (CFont*)data;
    return 0;
  case CDX_DRAW_GET_TEXTURE:
    return d_texture.CopyTo((CTexture**)data);
  case CDX_DRAW_SET_TEXTURE:
  {
    d_texture = (CTexture*)data; if (d_texture.p) { if (!d_texture.p->srv.p) d_texture.p->init(this); SetTexture(d_texture->srv.p); }
    d_blend = (d_blend & ~MO_PSSHADER_MASK) | (!d_texture.p ? MO_PSSHADER_COLOR : d_texture.p->fl & 2 ? MO_PSSHADER_FONT : MO_PSSHADER_TEXTURE);
    return 0;
  }
  case CDX_DRAW_GET_MAPPING:
    XMStoreFloat4x3((XMFLOAT4X3*)data, mm[MM_MAPPING]);
    return 0;
  case CDX_DRAW_SET_MAPPING:
    SetMatrix(MM_MAPPING, XMLoadFloat4x3((XMFLOAT4X3*)data));
    return 0;
  case CDX_DRAW_FILL_RECT:
  {
    if (pickprim) return 0;
    auto vv = BeginVertices(4);
    vv[0].p.x = vv[2].p.x = ((float*)data)[0];
    vv[0].p.y = vv[1].p.y = ((float*)data)[1];
    vv[1].p.x = vv[3].p.x = ((float*)data)[0] + ((float*)data)[2];
    vv[2].p.y = vv[3].p.y = ((float*)data)[1] + ((float*)data)[3];
    if (d_texture.p) mapping(vv, 4);
    EndVertices(4, MO_TOPO_TRIANGLESTRIP | MO_RASTERIZER_NOCULL | d_blend);
    return 0;
  }
  case CDX_DRAW_FILL_ELLIPSE:
  case CDX_DRAW_DRAW_ELLIPSE:
  {
    if (pickprim) return 0;
    auto pm = ((XMFLOAT2*)data)[0];
    auto sc = ((XMFLOAT2*)data)[1]; auto s = ((UINT*)data)[4];
    auto tt = (XMFLOAT2*)stackptr;
#if(0)
    if (s & 3) s = ((s >> 2) + 1) << 2;
    auto fa = (2 * XM_PI) / s;
    UINT e = s >> 1, n = e >> 1;
    tt[s] = tt[0] = XMFLOAT2(pm.x, pm.y + sc.y);
    tt[e] = XMFLOAT2(pm.x, pm.y - sc.y);
    tt[n] = XMFLOAT2(pm.x + sc.x, pm.y);
    tt[s - n] = XMFLOAT2(pm.x - sc.x, pm.y);
    for (UINT i = 1; i < n; i++)
    {
      float u, v; XMScalarSinCosEst(&u, &v, i * fa);
      tt[e - i].x = tt[i].x = pm.x + (u *= sc.x);
      tt[s - i].y = tt[i].y = pm.y + (v *= sc.y);
      tt[s - i].x = tt[e + i].x = pm.x - u;
      tt[e + i].y = tt[e - i].y = pm.y - v;
    }
#else
    auto fa = (2 * XM_PI) / s;
    for (UINT i = 0; i <= s; i++)
    {
      float u, v; XMScalarSinCosEst(&u, &v, i * fa);
      tt[i].x = pm.x + u * sc.x;
      tt[i].y = pm.y + v * sc.y;
    }
#endif
    if (id == CDX_DRAW_FILL_ELLIPSE)
    {
      UINT nv = s + 2; auto vv = BeginVertices(nv);
      for (UINT i = 0, j = 0; j < nv; i++) { *(XMFLOAT2*)&vv[j++] = tt[i]; *(XMFLOAT2*)&vv[j++] = tt[s - i]; }
      if (d_texture.p) mapping(vv, nv);
      EndVertices(nv, MO_TOPO_TRIANGLESTRIP | MO_RASTERIZER_NOCULL | d_blend);
    }
    else
    {
      UINT nv = s + 1; auto vv = BeginVertices(nv);
      for (UINT i = 0; i < nv; i++) *(XMFLOAT2*)&vv[i] = tt[i];
      EndVertices(nv, MO_TOPO_LINESTRIP | MO_PSSHADER_COLOR | MO_RASTERIZER_NOCULL | d_blend);
    }
    return 0;
  }
  case CDX_DRAW_GET_TEXTEXTENT:
    *(XMFLOAT2*)data = d_font.p->getextent(*(LPCWSTR*)(data + 2), *(UINT*)((LPCWSTR*)(data + 2) + 1));
    return 0;
  case CDX_DRAW_DRAW_TEXT:
    if (pickprim) return 0;
    d_font.p->draw(this, *(XMFLOAT2*)data, *(LPCWSTR*)(data + 2), *(UINT*)((LPCWSTR*)(data + 2) + 1));
    return 0;
  case CDX_DRAW_DRAW_RECT:
  {
    if (pickprim) return 0;
    auto vv = BeginVertices(5);
    vv[0].p.x = vv[3].p.x = ((float*)data)[0];
    vv[0].p.y = vv[1].p.y = ((float*)data)[1];
    vv[1].p.x = vv[2].p.x = ((float*)data)[0] + ((float*)data)[2];
    vv[2].p.y = vv[3].p.y = ((float*)data)[1] + ((float*)data)[3]; *(INT64*)&vv[4].p = *(INT64*)&vv[0].p;
    EndVertices(5, MO_TOPO_LINESTRIP | MO_PSSHADER_COLOR | MO_RASTERIZER_NOCULL | d_blend);
    return 0;
  }
  case CDX_DRAW_DRAW_POLYLINE:
  {
    if (pickprim) return 0;
    auto np = *(UINT*)data; auto pp = (XMFLOAT3*)((UINT*)data + 1);
    auto vv = BeginVertices(np);
    for (UINT i = 0; i < np; i++) vv[i].p = pp[i];
    EndVertices(np, MO_TOPO_LINESTRIP | MO_PSSHADER_COLOR | MO_RASTERIZER_NOCULL | d_blend);
    return 0;
  }
  case CDX_DRAW_DRAW_BOX:
  {
    if (pickprim) return 0;
    auto pp = (XMFLOAT3*)data;
    DrawBox(XMLoadFloat3(pp), XMLoadFloat3(pp + 1), MO_PSSHADER_COLOR | MO_RASTERIZER_NOCULL | d_blend);
    return 0;
  }
  case CDX_DRAW_DRAW_POINTS:
  {
    if (pickprim == -1) return 0;
    auto radius = *(float*)&data[0]; auto np = data[1]; auto pp = *(const XMFLOAT3**)&data[2];
    auto m1 = W2Screen(); auto m2 = XMMatrixInverse(0, m1);
    for (UINT i = 0; i < np; i++)
    {
      auto vv = BeginVertices(4);
      auto mp = XMVector3TransformCoord(XMLoadFloat3(&pp[i]), m1);
      for (UINT t = 0; t < 4; t++)
      {
        auto ep = mp + XMVectorSet(t & 2 ? +radius : -radius, t & 1 ? +radius : -radius, 0, 0);
        XMStoreFloat4A((XMFLOAT4A*)(vv + t), XMVector3TransformCoord(ep, m2));
      }
      vv[1].t.x = vv[3].t.x = vv[2].t.y = vv[3].t.y = 1;
      EndVertices(4, i == 0 ? MO_TOPO_TRIANGLESTRIP | MO_BLENDSTATE_ALPHA |
        MO_PSSHADER_TEXTUREPTS | MO_RASTERIZER_NOCULL | MO_DEPTHSTENCIL_ZWRITE : 0);
    }
    return 0;
  }
  case CDX_DRAW_CATCH: //if (pickprim) pickprim = data[0] ? data[0] : -1;
  {
    if (!pickprim) return 0;
    auto p = (CNode**)data; if (!p[0]) { pickprim = -1; return 0; }
    pickprim = (p[0]->getscount() << 16) | (0x7fff & *(UINT*)(p + 1));
    return 0;
  }

  }
  return E_FAIL;
}
