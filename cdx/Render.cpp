#include "pch.h"
#include "Factory.h"
#include "view.h"
#include "xmext.h"

//using namespace DirectX;

void SetTexture(ID3D11ShaderResourceView* p);
void SetIndexBuffer(ID3D11Buffer* p);
void SetMode(UINT mode);
void SetVertexBuffer(ID3D11Buffer* p);
extern CComPtr<ID3D11DeviceContext> context;


void CView::setproject()
{
  camdat = *(const cameradata*)camera.p->getbuffer(CDX_BUFFER_CAMERA)->data.p;
  auto f = camdat.znear * camdat.fov * (0.00025f / 45) * 
    (dpiscale ? dpiscale : (dpiscale = 120.0f / GetDpiForWindow(hwnd)));
  auto vpx = viewport.Width * f;
  auto vpy = viewport.Height * f;
  SetMatrix(MM_VIEWPROJ, XMMatrixInverse(0,
    camera.p->gettrans(camera.p->parent ? scene.p : 0)) *
    XMMatrixPerspectiveOffCenterLH(vpx, -vpx, -vpy, vpy, camdat.znear, camdat.zfar));
}

void CView::renderekbox(CNode* main)
{
  auto m = main->gettrans(scene.p);

  auto ms = _XMVector3Row(
    XMVector3ReciprocalLength(m.r[0]),
    XMVector3ReciprocalLength(m.r[1]),
    XMVector3ReciprocalLength(m.r[2]));
  auto is = XMVectorReciprocal(ms);
  m = XMMatrixScalingFromVector(ms) * m;
  SetMatrix(MM_WORLD, m);

  XMVECTOR box[2]; box[1] = XMVectorNegate(box[0] = g_XMFltMax);
  for (CNode* p = main; p; p = main->child() ? p->nextsibling(main) : 0)
  {
    if (!p->vb.p) continue; XMMATRIX wm;
    if (p != main) wm = p->gettrans(main);
    p->getbox(box, p != main ? &wm : 0);
  }
  auto cc = main->flags & NODE_FL_STATIC ? 0x80808080 : 0xffffffff;
  if (XMVector3Greater(box[0], box[1])) box[0] = box[1] = XMVectorZero();
  else if (flags & CDX_RENDER_BOUNDINGBOX)
  {
    SetColor(VV_DIFFUSE, 0xffffffff & cc);
    DrawBox(box[0] * is, box[1] * is, MO_DEPTHSTENCIL_ZWRITE);
  }
  if (flags & CDX_RENDER_COORDINATES)
  {
    box[1] *= is;

    auto pc = XMLoadFloat3((XMFLOAT3*)&camera->matrix._41);
    auto sc = _XMVector3Row(
      XMVector3LengthEst(XMVector3TransformCoord(XMVectorAndInt(box[1], g_XMMaskX), m) - pc),
      XMVector3LengthEst(XMVector3TransformCoord(XMVectorAndInt(box[1], g_XMMaskY), m) - pc),
      XMVector3LengthEst(XMVector3TransformCoord(XMVectorAndInt(box[1], g_XMMaskZ), m) - pc));

    auto ma = box[1] + sc * XMVectorReplicate(0.03f);
    auto va = sc * XMVectorReplicate(0.01f);
    auto vr = sc * XMVectorReplicate(0.002f);

    SetColor(VV_DIFFUSE, 0xffff0000 & cc);
    DrawLine(g_XMZero, pc = XMVectorAndInt(ma, g_XMMaskX));
    DrawArrow(pc, XMVectorAndInt(va, g_XMMaskX), XMVectorGetX(vr));

    SetColor(VV_DIFFUSE, 0xff00aa00 & cc);
    DrawLine(g_XMZero, pc = XMVectorAndInt(ma, g_XMMaskY));
    DrawArrow(pc, XMVectorAndInt(va, g_XMMaskY), XMVectorGetY(vr));

    SetColor(VV_DIFFUSE, 0xff0000ff & cc);
    DrawLine(g_XMZero, pc = XMVectorAndInt(ma, g_XMMaskZ));
    DrawArrow(pc, XMVectorAndInt(va, g_XMMaskZ), XMVectorGetZ(vr));
  }

}

#if(0)
auto recurs = [&](CNode* node, auto&& recurs)
{
  if (!node) return; auto first = node;
  do {
    if (flags & CDX_RENDER_SELONLY && !(node->flags & NODE_FL_INSEL)) continue;
    //if (node->flags & NODE_FL_SELECT) { if (!sel2) sel1 = i; sel2 = i + 1; }
    if (!(node->flags & NODE_FL_MASHOK) || (node->ib.p && !node->ib.p->p)) //drv reset
    {
      node->flags |= NODE_FL_MASHOK;
      stackptr = recs + nrecs;
      node->update(scene, -1);
    }
    if (node->bmask & (1 << CDX_BUFFER_LIGHT))
    {
      if (!plight) plight = node;
    }
    if (node->ib.p)
    {
      auto& rec = recs[nrecs++];
      rec.node = node; nflags |= node->flags;
      rec.wm = node->gettrans(scene);
      rec.tex = static_cast<CTexture*>(node->getbuffer(CDX_BUFFER_TEXTURE));
      if (rec.tex && !rec.tex->srv.p)
      {
        stackptr = recs + nrecs;
        rec.tex->init(this);
      }
      if ((node->color >> 24) != 0xff) recs[transp++].itrans = nrecs - 1;
    }
    if (node->lastchild.p) recurs(node->lastchild.p, recurs);
  } while ((node = node->nextnode.p) != first);
};
recurs(scene->lastchild.p, recurs);

#endif

__declspec(align(16)) struct REC
{
  XMMATRIX wm; CNode* node; CTexture* tex; UINT itrans;
};

void CView::RenderScene()
{
  UINT nrecs = 0; REC* recs = (REC*)stackptr;
  UINT nflags = 0, transp = 0; CNode* plight = 0; anitime = 0;
  for (auto node = scene.p->child(); node; node = node->nextsibling(0))
  {
    if (flags & CDX_RENDER_SELONLY && !(node->flags & NODE_FL_INSEL)) continue;
    if (node->flags & NODE_FL_ACTIVE && sink.p)
      sink.p->Animate(node, anitime ? anitime : (anitime = getanitime()));
    if (!(node->flags & NODE_FL_MASHOK) || (node->ib.p && !node->ib.p->p)) //drv reset
    {
      node->flags |= NODE_FL_MASHOK;
      node->update(scene, -1);
    }
    if (node->bmask & (1 << CDX_BUFFER_LIGHT))
    {
      if (!plight) plight = node;
    }
    if (node->ib.p)
    {
      auto& rec = recs[nrecs++]; stackptr = recs + nrecs;
      rec.node = node; nflags |= node->flags;
      rec.wm = node->gettrans(scene);
      rec.tex = static_cast<CTexture*>(node->getbuffer(CDX_BUFFER_TEXTURE));
      if (rec.tex && !rec.tex->srv.p)
        rec.tex->init(this);
      if ((node->color >> 24) != 0xff) recs[transp++].itrans = nrecs - 1;
    }
  }

  SetColor(VV_AMBIENT, 0x00404040);
  XMVECTOR light;
  if (plight) light = plight->gettrans(scene.p).r[2];
  else light = XMVector3Normalize(XMVectorSet(1, -1, 2, 0));
  XMVECTOR lightdir = flags & CDX_RENDER_SHADOWS ?
    XMVectorSetW(XMVectorMultiply(light, XMVectorSet(0.3f, 0.3f, 0.3f, 0)), camdat.minwz) : light;
  SetVector(VV_LIGHTDIR, lightdir);
  for (UINT i = 0; i < nrecs; i++)
  {
    auto& r = recs[i]; auto& node = *r.node;
    if ((node.color >> 24) != 0xff) continue;
    SetMatrix(MM_WORLD, r.wm);
    if (r.tex) SetTexture(r.tex->srv.p);
    SetColor(VV_DIFFUSE, node.color); SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p); SetBuffers();
    SetMode(MO_TOPO_TRIANGLELISTADJ | MO_DEPTHSTENCIL_ZWRITE | MO_VSSHADER_LIGHT | (r.tex ? MO_PSSHADER_TEXTURE3D : MO_PSSHADER_COLOR3D));
    context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subn ? node.subi : 0, 0);
  }
  if (nflags & NODE_FL_INSEL)
  {
    if (flags & CDX_RENDER_WIREFRAME)
    {
      SetColor(VV_DIFFUSE, 0x40000000);
      SetMode(MO_TOPO_TRIANGLELISTADJ | MO_RASTERIZER_WIRE | MO_BLENDSTATE_ALPHA);
      for (UINT i = 0; i < nrecs; i++)
      {
        auto& r = recs[i]; auto& node = *r.node; if (!(node.flags & NODE_FL_INSEL)) continue;
        SetMatrix(MM_WORLD, r.wm);
        SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p); SetBuffers();
        context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subi, 0);
      }
    }
    if (flags & CDX_RENDER_OUTLINES)
    {
      SetColor(VV_DIFFUSE, 0xff000000); SetVector(VV_LIGHTDIR, camera.p->getmatrix().r[3]);
      SetMode(MO_TOPO_TRIANGLELISTADJ | MO_GSSHADER_OUTL3D | MO_VSSHADER_WORLD);
      for (UINT i = 0; i < nrecs; i++)
      {
        auto& r = recs[i]; auto& node = *r.node; if (!(node.flags & NODE_FL_INSEL)) continue;
        SetMatrix(MM_WORLD, r.wm);
        SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p); SetBuffers();
        context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subi, 0);
      }
    }
    if (flags & (CDX_RENDER_COORDINATES | CDX_RENDER_BOUNDINGBOX))
    {
      if (flags & CDX_RENDER_COORDINATES) SetVector(VV_LIGHTDIR, light);
      for (UINT j = 0; j < scene->selection.n; j++)
      {
        auto main = scene->selection.p[j];

        if (!(main->flags & NODE_FL_SELECT))
        {
          scene->selection.removeat(j--);
          continue;
        }
        if (main == camera.p) continue;
        renderekbox(main);
      }
    }
  }

  if (sink.p)
    sink.p->Render(0);

  if (transp)
  {
    SetColor(VV_AMBIENT, 0x00404040);
    SetVector(VV_LIGHTDIR, light);
    for (UINT i = 0; i < transp; i++)
    {
      auto& r = recs[recs[i].itrans]; auto& node = *r.node;
      if (r.tex) SetTexture(r.tex->srv.p);
      SetColor(VV_DIFFUSE, node.color); SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p);
      SetMatrix(MM_WORLD, r.wm); SetBuffers();
      SetMode(MO_TOPO_TRIANGLELISTADJ | MO_BLENDSTATE_ALPHA | MO_VSSHADER_LIGHT | (r.tex ? MO_PSSHADER_TEXTURE3D : MO_PSSHADER_COLOR3D));
      context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subi, 0);
    }
  }
  if (flags & CDX_RENDER_SHADOWS)
  {
    SetVector(VV_LIGHTDIR, lightdir);
    if (flags & CDX_RENDER_ZPLANESHADOWS) //checkborad
    {
      const float size = 10000;
      SetMatrix(MM_WORLD, XMMatrixTranslation(0, 0, 0));
      auto vv = BeginVertices(4);
      vv[0].p.x = vv[2].p.x = -size;
      vv[0].p.y = vv[1].p.y = -size;
      vv[1].p.x = vv[3].p.x = +size;
      vv[2].p.y = vv[3].p.y = +size;
      EndVertices(4, MO_TOPO_TRIANGLESTRIP | MO_RASTERIZER_NOCULL | MO_PSSHADER_NULL | MO_DEPTHSTENCIL_ZWRITE);
    }
    SetMode(MO_TOPO_TRIANGLELISTADJ | MO_GSSHADER_SHADOW | MO_VSSHADER_WORLD | MO_PSSHADER_NULL | MO_RASTERIZER_NOCULL | MO_DEPTHSTENCIL_TWOSID);
    for (UINT i = 0; i < nrecs; i++)
    {
      auto& r = recs[i]; auto& node = *r.node;
      if ((node.color >> 24) != 0xff) continue;
      SetMatrix(MM_WORLD, r.wm);
      SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p); SetBuffers();
      context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subi, 0);
    }
    SetColor(VV_AMBIENT, 0); SetVector(VV_LIGHTDIR, XMVectorMultiply(light, XMVectorReplicate(0.7f)));
    for (UINT i = 0; i < nrecs; i++)
    {
      auto& r = recs[i]; auto& node = *r.node;
      if ((node.color >> 24) != 0xff) continue;
      SetMatrix(MM_WORLD, r.wm);
      if (r.tex) SetTexture(r.tex->srv.p);
      SetColor(VV_DIFFUSE, node.color); SetVertexBuffer(node.vb.p->p.p); SetIndexBuffer(node.ib.p->p.p); SetBuffers();
      SetMode(MO_TOPO_TRIANGLELISTADJ | MO_BLENDSTATE_ALPHAADD | MO_VSSHADER_LIGHT | (r.tex ? MO_PSSHADER_TEXTURE3D : MO_PSSHADER_COLOR3D));
      context->DrawIndexed(node.subn ? node.subn : node.ib.p->ni, node.subi, 0);
    }
    if (flags & CDX_RENDER_ZPLANESHADOWS)
    {
      SetMatrix(MM_WORLD, XMMatrixTranslation(0, 0, 0));
      SetColor(VV_DIFFUSE, 0x40000000);
      SetMode(MO_TOPO_TRIANGLESTRIP | MO_RASTERIZER_NOCULL | MO_BLENDSTATE_ALPHA | MO_DEPTHSTENCIL_REST);
      ReEndVertices(4);
    }
    context->ClearDepthStencilView(dsv.p, D3D11_CLEAR_STENCIL, 1, 0);
  }
  stackptr = recs;
}

void CView::Render()
{
  context.p->RSSetViewports(1, &viewport);
  context.p->OMSetRenderTargets(1, &rtv.p, dsv.p);
  context.p->ClearDepthStencilView(dsv.p, D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1, 0);
  context.p->ClearRenderTargetView(rtv.p, vv[VV_BKCOLOR].m128_f32);
  if (scene.p && camera.p) { setproject(); RenderScene(); }
  if (sink.p) sink.p->Render(1);
  auto hr = swapchain.p->Present(0, 0);
  XMASSERT(stackptr == baseptr);
}

