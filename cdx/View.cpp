#include "pch.h"
#include "cdx_i.h"
#include "scene.h"
#include "view.h"

CView* CView::first;

static void wheelpick(CView* view, LPARAM lParam)
{
  POINT pt = { ((short*)&lParam)[0], ((short*)&lParam)[1] };
  ScreenToClient(view->hwnd, &pt); short pp[2] = { (short)pt.x, (short)pt.y };
  view->Pick(pp);
}

UINT CView::getanitime()
{
  auto t = GetTickCount();
  return t ? t : 1;
}
void CView::ontimer()
{
  if (anitime)
  {
    anitime = 0;
    for (auto node = scene.p->child(); node; node = node->nextsibling(0))
      if (node->flags & NODE_FL_ACTIVE)
        sink.p->Animate(node, anitime ? anitime : (anitime = getanitime()));
  }
  sink.p->Timer();
}

LRESULT CALLBACK CView::WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
  auto view = (CView*)GetWindowLongPtr(hWnd, GWLP_USERDATA);
  switch (message)
  {
  case WM_TIMER: view->ontimer(); return 0;
  case WM_ERASEBKGND:
    return 1;
  case WM_PAINT:
    ValidateRect(hWnd, 0);
    if (*(UINT64*)&view->size != *(UINT64*)&view->rcclient.right)
    {
      *(UINT64*)&view->size = *(UINT64*)&view->rcclient.right;
      if (view->Resize()) return 0;
    }
    view->Render();
    return 0;
  case WM_MOUSEMOVE:
    //if (wParam & (MK_LBUTTON | MK_MBUTTON | MK_RBUTTON)) break;
    if (GetCapture() == hWnd) break;
  case WM_LBUTTONDOWN:
  case WM_MBUTTONDOWN:
  case WM_RBUTTONDOWN:
    view->Pick((short*)&lParam);
    break;
  case WM_MOUSEWHEEL:
    wheelpick(view, lParam);
    break;
  case WM_SIZE:
    GetClientRect(hWnd, &view->rcclient); InvalidateRect(hWnd, 0, 0);
    break;
  case WM_DESTROY:
    view->hwnd = 0; view->sink.Release(); view->relres();
    SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)view->proc);
    break;
  case WM_DPICHANGED:
    view->dpiscale = 0;
    break;
    //case WM_KEYDOWN:
    //case WM_KEYUP:
    //case WM_CHAR:
    //case WM_DEADCHAR:
    //case WM_SYSKEYDOWN:
    //case WM_SYSKEYUP:
    //case WM_SYSCHAR:
    //case WM_SYSDEADCHAR:
    //  return 0;
  }
  return view->proc(hWnd, message, wParam, lParam);
}

HRESULT CView::get_Scene(ICDXScene** p)
{
  if (*p = scene) scene.p->AddRef(); return 0;
}
HRESULT CView::put_Scene(ICDXScene* p)
{
  //if (camera.p && camera.p->parent) camera.Release();
  scene = static_cast<CScene*>(p); return 0;
}
HRESULT CView::get_Camera(ICDXNode** p)
{
  if (*p = camera) camera.p->AddRef();
  return 0;
}
HRESULT CView::put_Camera(ICDXNode* p)
{
  camera = static_cast<CNode*>(p);
  return 0;
}

void XM_CALLCONV CView::DrawLine(XMVECTOR a, XMVECTOR b)
{
  auto p = BeginVertices(2); //XMStoreFloat4A()
  XMStoreFloat4A((XMFLOAT4A*)&p[0], a);
  XMStoreFloat4A((XMFLOAT4A*)&p[1], b);
  EndVertices(2, MO_TOPO_LINELIST | MO_DEPTHSTENCIL_ZWRITE);
}
void XM_CALLCONV CView::DrawBox(XMVECTOR a, XMVECTOR b, UINT mode)
{
  auto p = BeginVertices(10);
  XMStoreFloat4A((XMFLOAT4A*)&p[0], XMVectorSelect(a, b, XMVectorSelectControl(0, 0, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[1], XMVectorSelect(a, b, XMVectorSelectControl(1, 0, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[2], XMVectorSelect(a, b, XMVectorSelectControl(1, 1, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[3], XMVectorSelect(a, b, XMVectorSelectControl(0, 1, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[4], XMVectorSelect(a, b, XMVectorSelectControl(0, 0, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[5], XMVectorSelect(a, b, XMVectorSelectControl(0, 0, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[6], XMVectorSelect(a, b, XMVectorSelectControl(1, 0, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[7], XMVectorSelect(a, b, XMVectorSelectControl(1, 1, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[8], XMVectorSelect(a, b, XMVectorSelectControl(0, 1, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[9], XMVectorSelect(a, b, XMVectorSelectControl(0, 0, 1, 0)));
  EndVertices(10, MO_TOPO_LINESTRIP | mode);// MO_DEPTHSTENCIL_ZWRITE);
  p = BeginVertices(6);
  XMStoreFloat4A((XMFLOAT4A*)&p[0], XMVectorSelect(a, b, XMVectorSelectControl(1, 0, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[1], XMVectorSelect(a, b, XMVectorSelectControl(1, 0, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[2], XMVectorSelect(a, b, XMVectorSelectControl(1, 1, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[3], XMVectorSelect(a, b, XMVectorSelectControl(1, 1, 1, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[4], XMVectorSelect(a, b, XMVectorSelectControl(0, 1, 0, 0)));
  XMStoreFloat4A((XMFLOAT4A*)&p[5], XMVectorSelect(a, b, XMVectorSelectControl(0, 1, 1, 0)));
  EndVertices(6, MO_TOPO_LINELIST | mode);// MO_DEPTHSTENCIL_ZWRITE);
}
void XM_CALLCONV CView::DrawArrow(XMVECTOR p, XMVECTOR v, float r, int s)
{
  auto fa = (float)(2 * XM_PI) / s++;
  auto rr = XMVectorReplicate(r);
  auto rl = XMVector3ReciprocalLength(v);
  auto r1 = XMVectorMultiply(_mm_shuffle_ps(v, v, _MM_SHUFFLE(0, 1, 0, 2)), rl);  //todo: _mm_shuffle_ps -> XM???
  auto r2 = XMVectorMultiply(_mm_shuffle_ps(v, v, _MM_SHUFFLE(0, 0, 2, 1)), rl);
  auto vv = BeginVertices(s << 1);
  for (int i = 0; i < s; i++)
  {
    auto si = XMVectorReplicate(sinf(i * fa));
    auto co = XMVectorReplicate(cosf(i * fa));
    auto no = XMVectorAdd(XMVectorMultiply(r1, si), XMVectorMultiply(r2, co));
    XMStoreFloat4A((XMFLOAT4A*)&vv[(i << 1) + 0], XMVectorAdd(p, XMVectorMultiply(no, rr)));
    XMStoreFloat4A((XMFLOAT4A*)&vv[(i << 1) + 1], XMVectorAdd(p, v));
    XMStoreFloat4((XMFLOAT4*)&vv[(i << 1) + 0].n, no);
    XMStoreFloat4((XMFLOAT4*)&vv[(i << 1) + 1].n, no);
  }
  EndVertices(s << 1, MO_TOPO_TRIANGLESTRIP | MO_PSSHADER_COLOR3D | MO_DEPTHSTENCIL_ZWRITE | MO_VSSHADER_LIGHT | MO_RASTERIZER_NOCULL);
}

static bool tri_intersect_xy(const XMVECTOR a[3], const XMVECTOR b[3])
{
  XMVECTOR mz = XMVectorZero(), ma = mz, mb = mz, mh = g_XMOneHalf;
  for (UINT i1 = 2, i2 = 0; i2 < 3; i1 = i2++)
  {
    auto va = a[i2] - a[i1];
    for (UINT k1 = 2, k2 = 0; k2 < 3; k1 = k2++)
    {
      auto vb = b[k2] - b[k1];
      ma = _mm_or_ps(ma, _mm_cmple_ps(XMVector2Cross(va, b[k2] - a[i1]), mz));
      mb = _mm_or_ps(mb, _mm_cmple_ps(XMVector2Cross(vb, a[i2] - b[k1]), mz));
      auto de = XMVector2Cross(va, vb); if (_mm_movemask_ps(_mm_cmpeq_ps(de, mz))) continue;
      de = XMVectorReciprocal(de); auto vc = a[i1] - b[k1];
      if (!_mm_movemask_ps(XMVectorInBounds(XMVector2Cross(vb, vc) * de - mh, mh))) continue;
      if (!_mm_movemask_ps(XMVectorInBounds(XMVector2Cross(va, vc) * de - mh, mh))) continue;
      return true;
    }
  }
  return !_mm_movemask_ps(ma) || !_mm_movemask_ps(mb);
}
static void selectrect(CView& view, UINT* data)
{
  void* layer = view.scene.p;
  XMVECTOR r[5];
  r[0] = XMLoadFloat2((XMFLOAT2*)data + 0);
  r[1] = XMLoadFloat2((XMFLOAT2*)data + 1);
  r[2] = XMVectorMax(r[0], r[1]);
  r[0] = r[4] = XMVectorMin(r[0], r[1]);
  r[1] = XMVectorPermute<4, 1, 3, 3>(r[0], r[2]);
  r[3] = XMVectorPermute<0, 5, 3, 3>(r[0], r[2]);
  auto vv = (XMVECTOR*)__align16(stackptr);

  auto scene = view.scene.p;
  for (int i = scene->selection.n - 1; i >= 0; i--)
    if (scene->selection.p[i]->parent != scene)
      scene->selection.p[i]->put_IsSelect(false);
  for (auto main = scene->child(); main; main = main->next())
  {
    auto ok = false;
    if (!(main->flags & NODE_FL_STATIC))
      for (auto node = main; node && !ok; node = main->child() ? node->nextsibling(main) : 0)
      {
        auto pb = node->getbuffer(CDX_BUFFER_POINTBUFFER); if (!pb) continue;
        auto ib = node->getbuffer(CDX_BUFFER_INDEXBUFFER); if (!ib) continue;
        auto wm = node->gettrans(view.scene.p);
        XMVector3TransformStream((XMFLOAT4*)vv, sizeof(XMVECTOR), (const XMFLOAT3*)pb->data.p, sizeof(XMFLOAT3), pb->data.n / sizeof(XMFLOAT3), wm);
        UINT ni = ib->data.n / sizeof(USHORT);
        auto ii = (const USHORT*)ib->data.p;
        for (UINT t = 0; t < ni; t += 3)
        {
          XMVECTOR tt[] = { vv[ii[t + 0]], vv[ii[t + 1]], vv[ii[t + 2]] };
          if (!tri_intersect_xy(tt, r) && !tri_intersect_xy(tt, r + 2)) continue;
          ok = true; break;
        }
      }
    main->put_IsSelect(ok);
  }
}

HRESULT __stdcall CView::Command(CDX_CMD cmd, UINT* data)
{
  switch (cmd)
  {
  case CDX_CMD_CENTER:
  case CDX_CMD_CENTERSEL:
  {
    if (!camera.p) return 0;
    auto rand = ((float*)data)[0];
    setproject();
    auto cwm = camera.p->getmatrix(); // gettrans(camera.p->parent ? scene.p : 0);
    auto icw = XMMatrixInverse(0, cwm);
    auto f = camdat.fov * (0.00025f / 45);
    auto ab = XMVectorSet((rcclient.right - rand) * f, (rcclient.bottom - rand) * f, 0, 0);
    XMVECTOR box[4]; box[1] = box[3] = -(box[0] = box[2] = g_XMFltMax);

    for (auto node = scene->child(); node; node = node->nextsibling(0))
    {
      if (cmd == CDX_CMD_CENTERSEL && !(node->flags & NODE_FL_INSEL)) continue;
      auto pb = node->getbuffer(CDX_BUFFER_POINTBUFFER); if (!pb) continue;
      auto pp = (XMFLOAT3*)pb->data.p;
      auto np = pb->data.n / sizeof(XMFLOAT3);
      auto wm = node->gettrans(scene.p); auto tm = wm * icw;
      for (UINT i = 0; i < np; i++)
      {
        auto op = _mm_loadu_ps(&pp[i].x);
        auto wp = XMVector3Transform(op, wm);
        box[0] = XMVectorMin(box[0], wp);
        box[1] = XMVectorMax(box[1], wp); //not in use
        wp = XMVector3Transform(op, tm); auto z = XMVectorSplatZ(wp) * ab;
        box[2] = XMVectorMin(box[2], wp + z);
        box[3] = XMVectorMax(box[3], wp - z);
      }
    }
    if (box[0].m128_f32[0] > box[1].m128_f32[0]) return 0;
    float fm = 0;
    if (((float*)data)[1] != 0)
    {
      auto mp = (box[3] - box[2]) * 0.5;
      fm = -max(
        mp.m128_f32[0] / ab.m128_f32[0],
        mp.m128_f32[1] / ab.m128_f32[1]);
      camera.p->setmatrix(XMMatrixTranslation(
        box[2].m128_f32[0] + mp.m128_f32[0],
        box[2].m128_f32[1] + mp.m128_f32[1],
        fm) * cwm);
    }
    auto& cd = *(cameradata*)data;
    cd.fov = camdat.fov;
    cd.znear = box[2].m128_f32[2] - fm;
    cd.zfar = box[3].m128_f32[2] - fm;
    cd.minwz = box[0].m128_f32[2];
    return 0;
  }
  case CDX_CMD_GETBOX:
  case CDX_CMD_GETBOXSEL:
    return E_NOTIMPL;
    //case CDX_CMD_GETBOX:
    //case CDX_CMD_GETBOXSEL:
    //{
    //  auto ma = XMLoadFloat4x3((XMFLOAT4X3*)data);
    //  auto& nodes = scene.p->nodes;
    //  XMVECTOR box[2]; box[1] = -(box[0] = g_XMFltMax);
    //  for (UINT i = 0; i < scene.p->count; i++)
    //  {
    //    auto node = nodes.p[i]; //if (!node->mesh.p) continue;
    //    if (cmd == CDX_CMD_GETBOXSEL && !(node->flags & NODE_FL_INSEL)) continue;
    //    auto pb = node->getbuffer(CDX_BUFFER_POINTBUFFER); if (!pb) continue;
    //    auto wm = node->gettrans(scene.p) * ma;
    //    node->getbox(box, &wm, pb);
    //  }
    //  XMStoreFloat4(((XMFLOAT4*)data) + 0, box[0]);
    //  XMStoreFloat4(((XMFLOAT4*)data) + 1, box[1]);
    //  return 0;
    //}
  case CDX_CMD_SETPLANE:
  {
    if (!data) { setproject(); mm[MM_PLANE] = mm[MM_VIEWPROJ]; return 0; }
    if (data) mm[MM_PLANE] = XMLoadFloat4x3((XMFLOAT4X3*)data) * mm[MM_PLANE];
    return 0;
  }
  case CDX_CMD_PICKPLANE:
  {
    auto p = (XMFLOAT2*)data;
    if (isnan(p->x))
    {
      POINT cp; GetCursorPos(&cp); ScreenToClient(hwnd, &cp);
      p->x = (float)cp.x;
      p->y = (float)cp.y;
    }
    auto& r = mm[MM_PLANE].r;
    auto t0 = (XMLoadFloat2(p) * 2 / XMLoadFloat4((XMFLOAT4*)&viewport.Width) + g_XMNegativeOne) * g_XMNegateY;
    auto t1 = t0 * XMVectorSplatW(r[0]) - r[0];
    auto t2 = t0 * XMVectorSplatW(r[1]) - r[1];
    auto t3 = t0 * XMVectorSplatW(r[3]) - r[3]; t3 = -t3;
    auto f1 = _mm_shuffle_ps(t1, t3, _MM_SHUFFLE(1, 0, 1, 0)); f1 = _mm_shuffle_ps(f1, f1, _MM_SHUFFLE(0, 0, 0, 2));
    auto f2 = _mm_shuffle_ps(t2, t3, _MM_SHUFFLE(1, 0, 1, 0)); f2 = _mm_shuffle_ps(f2, f2, _MM_SHUFFLE(0, 1, 3, 1));
    auto f3 = _mm_shuffle_ps(t2, t3, _MM_SHUFFLE(1, 0, 1, 0)); f3 = _mm_shuffle_ps(f3, f3, _MM_SHUFFLE(0, 0, 2, 0));
    auto f4 = _mm_shuffle_ps(t1, t3, _MM_SHUFFLE(1, 0, 1, 0)); f4 = _mm_shuffle_ps(f4, f4, _MM_SHUFFLE(0, 1, 1, 3));
    auto f5 = f1 * f2 - f3 * f4;
    XMStoreFloat2(p, f5 / XMVectorSplatZ(f5));
    return 0;
  }
  case CDX_CMD_SELECTRECT:
    selectrect(*this, data);
    return 0;
  case CDX_CMD_BOXESSET: //BOOL
  {
    if (data[0] == 0) { boxes.clear(); return 0; }

    auto last = scene.p->lastchild.p; for (; last->lastchild.p; last = last->lastchild.p);
    auto nc = last->getscount();

    auto pp = (CNode**)stackptr; UINT na = 0, nb = 0;
    for (auto node = scene.p->child(); node; node = node->nextsibling(0))
    {
      if (!node->vb.p) continue;
      if (node->flags & (NODE_FL_SELECT | NODE_FL_INSEL)) pp[na++] = node; else pp[nc - ++nb] = node;
    }
    if (na == 0 || nb == 0) { data[0] = data[1] = 0; return 0; }
    boxes.setsize(na + nb);
    for (UINT i = 0; i < boxes.n; i++)
    {
      auto& inf = boxes.p[i];
      auto node = inf.p = pp[i < na ? i : nc - (i - na) - 1];
      auto m = node->gettrans(scene.p);
      XMVECTOR box[2]; box[1] = -(box[0] = g_XMFltMax);
      node->getbox(box, &m, 0);
      XMStoreFloat4x3(&inf.m, m);
      XMStoreFloat3(&inf.b[0], box[0]);
      XMStoreFloat3(&inf.b[1], box[1]);
    }
    data[0] = na; data[1] = na + nb;
    return 0;
  }
  case CDX_CMD_BOXESGET:
  {
    auto i = data[0]; if (i >= boxes.n) return -1;
    ((XMFLOAT3*)data)[0] = boxes.p[i].b[0];
    ((XMFLOAT3*)data)[1] = boxes.p[i].b[1];
    return 0;
  }
  case CDX_CMD_BOXESTRA:
  {
    auto i = data[0]; if (i >= boxes.n) return -1;
    ((XMFLOAT4X3*)data)[0] = boxes.p[i].m;
    return 0;
  }
  case CDX_CMD_BOXESIND:
    return Collision(*(const XMFLOAT4X3**)((UINT*)data + 1), *(UINT*)data, (float*)data);
  }
  return E_FAIL;
}

