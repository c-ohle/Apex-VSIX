#include "pch.h"
#include "Factory.h"
#include "Scene.h"

CVertices* CVertices::first;
CIndices* CIndices::first;

extern CComPtr<ID3D11Device> device;
extern CComPtr<ID3D11DeviceContext> context;

CNode* CNode::getparent()
{
  return parent && *(void**)this == *(void**)parent ? static_cast<CNode*>(parent) : 0;
}
CScene* CNode::getscene()
{
  CNode* a = this; for (CNode* b; b = a->getparent(); a = b);
  return static_cast<CScene*>(a->parent);
}
CNode* CNode::parentchild()
{
  auto p = getparent();
  return p ? p->child() : parent ? static_cast<CScene*>(parent)->child() : 0;
}
CNode* CNode::nextsibling(CNode* root)
{
  auto p = child();
  if (p)
    return p;
  if (p = next())
    return p;
  for (p = this; ;)
  {
    auto l = p->getparent();
    if (l == root)
      break;
    auto t = l->next();
    if (t)
      return t;
    p = l;
  }
  return 0;
}

HRESULT CNode::get_Parent(ICDXRoot** p)
{
  if (!parent) return 0;
  if (*(void**)this == *(void**)parent) (*p = static_cast<CNode*>(parent))->AddRef();
  else (*p = static_cast<CScene*>(parent))->AddRef();
  return 0;
}
HRESULT CNode::get_Scene(ICDXScene** p)
{
  if (*p = getscene()) (*p)->AddRef(); return 0;
}
//HRESULT CNode::get_Parent(ICDXNode** p)
//{
//  if (*p = getparent()) (*p)->AddRef(); return 0;
//}
//HRESULT CNode::put_Parent(ICDXNode* p)
//{
//  return E_NOTIMPL;
//  //auto scene = getscene(); if (!scene) return E_FAIL;
//  //if (!p) { parent = scene; return 0; }
//  //auto node = static_cast<CNode*>(p);
//  //if (ispart(node)) return E_FAIL;
//  //if (node->getscene() != scene) return E_FAIL;
//  //parent = node; return 0;
//}
HRESULT CNode::get_Child(ICDXNode** p)
{
  if (*p = child()) (*p)->AddRef(); return 0;
}
HRESULT CNode::get_Next(ICDXNode** p)
{
  if (*p = next()) (*p)->AddRef(); return 0;
  return 0;
}
HRESULT CNode::NextSibling(ICDXNode* root, ICDXNode** p)
{
  auto t = static_cast<CNode*>(root); if (t == this && !lastchild.p) return 0;
  if (*p = nextsibling(t)) (*p)->AddRef(); return 0;
}

HRESULT CNode::get_Index(UINT* pc)
{
  auto p = parentchild(); if (!p) { *pc = -1; return 0; }
  UINT i = 0; for (; p != this; p = p->next(), i++);
  *pc = i; return 0;
}
HRESULT CNode::put_Index(UINT x)
{
  UINT i; get_Index(&i); if (i == -1) return -1;
  if (i == x) return 0; //if (i < x) x--;
  auto p = getparent();
  auto fl = flags; flags &= ~(NODE_FL_SELECT | NODE_FL_INSEL);
  if (p)
  {
    p->RemoveAt(i); p->InsertAt(x, this);
  }
  else
  {
    auto s = static_cast<CScene*>(parent);
    s->RemoveAt(i); s->InsertAt(x, this);
  }
  flags |= fl & (NODE_FL_SELECT | NODE_FL_INSEL);
  return 0;
}

HRESULT CNode::get_IsSelect(BOOL* p)
{
  *p = (flags & NODE_FL_SELECT) != 0; return 0;
}
HRESULT CNode::put_IsSelect(BOOL on)
{
  if ((on != 0) == ((flags & NODE_FL_SELECT) != 0)) return 0;
  auto scene = getscene(); if (!scene) return E_FAIL;
  if (on)
  {
    flags |= (NODE_FL_SELECT | NODE_FL_INSEL);
    scene->selection.add(this);
  }
  else
  {
    flags &= ~(NODE_FL_SELECT | NODE_FL_INSEL);
    scene->selection.remove(this);
  }
  for (auto p = child(); p; p = p->nextsibling(this))
  {
    if (on) p->flags |= NODE_FL_INSEL;
    else p->flags &= ~NODE_FL_INSEL;
  }
  return 0;
}

bool equals(ID3D11Buffer* buffer, const void* p, UINT n)
{
  if (!buffer) return false;
  D3D11_BUFFER_DESC bd; buffer->GetDesc(&bd);
  if (bd.ByteWidth != n) return false;
  bd.Usage = D3D11_USAGE_STAGING;
  bd.CPUAccessFlags = D3D11_CPU_ACCESS_READ; bd.BindFlags = bd.MiscFlags = bd.StructureByteStride = 0;
  CComPtr<ID3D11Buffer> tmp;
  auto hr = device.p->CreateBuffer(&bd, 0, &tmp.p);
  context.p->CopyResource(tmp.p, buffer);
  D3D11_MAPPED_SUBRESOURCE map;
  hr = context.p->Map(tmp.p, 0, D3D11_MAP_READ, 0, &map);
  n = memcmp(p, map.pData, n);
  context->Unmap(tmp.p, 0);
  return n == 0;
}

void CNode::update(XMFLOAT3* pp, UINT np, USHORT* ii, UINT ni, float smooth, void* tex, UINT fl)
{
  auto kk = (int*)__align16(stackptr); auto tt = (int*)__align16(kk + ni); XMASSERT(ni != 0);
  //auto kk = (int*)stackptr; auto tt = kk + ni; 
  DWORD e; _BitScanReverse(&e, ni); //auto e = msb(ni); 
  e = 1 << (e + 1); auto w = e - 1; //if(e > 15) e = 15 // 64k?
  auto dict = tt; memset(dict, 0, e << 2);
  for (UINT i = ni - 1, m = 0b010010; (int)i >= 0; i--, m = (m >> 1) | ((m & 1) << 5))
  {
    int j = i - (m & 3), k = j + ((m >> 2) & 3), v = j + ((m >> 1) & 3), h;
    dict[e] = k = ii[i] | (ii[k] << 16); dict[e + 1] = v;
    dict[e + 2] = dict[h = (k ^ ((k >> 16) * 31)) & w]; dict[h] = e; e += 3;
  }
  for (UINT i = 0, m = 0b100100; i < ni; i++, m = (m << 1) & 0b111111 | (m >> 5))
  {
    int j = i - (m & 3), k = (ii[j + ((m >> 2) & 3)]) | (ii[i] << 16), h = (k ^ ((k >> 16) * 31)) & w, t;
    for (t = dict[h]; t != 0; t = dict[t + 2]) if (dict[t] == k) { dict[t] = -1; break; }
    kk[i] = ii[t != 0 ? dict[t + 1] : j + ((m >> 1) & 3)];
  }
  auto vv = (VERTEX*)tt; auto vsmooth = XMVectorReplicate(smooth);
  for (UINT i = 0; i < np; i++)
  {
    XMStoreFloat4A((XMFLOAT4A*)&vv[i].p, XMLoadFloat4((XMFLOAT4*)&pp[i]));
    *(UINT64*)&vv[i].t.x = 0;
  }
  /////////////////
  for (UINT i = 0; i < ni; i += 3)
  {
    auto v1 = XMLoadFloat4A((XMFLOAT4A*)&vv[ii[i + 0]].p);
    auto v2 = XMLoadFloat4A((XMFLOAT4A*)&vv[ii[i + 1]].p);
    auto v3 = XMLoadFloat4A((XMFLOAT4A*)&vv[ii[i + 2]].p);
    auto vn = XMVector3Normalize(XMVector3Cross(v2 - v1, v3 - v1));
    for (int k = 0, j; k < 3; k++)
    {
      for (j = ii[i + k]; ;)
      {
        auto c = *(UINT*)&vv[j].t.x; if (c == 0) { XMStoreFloat3(&vv[j].n, vn); *(UINT*)&vv[j].t.x = 1; break; }
        auto nt = XMLoadFloat3(&vv[j].n);
        if (XMVector3LessOrEqual(XMVector3LengthSq((c == 1 ? nt : XMVector3Normalize(nt)) - vn), vsmooth))
        {
          XMStoreFloat3(&vv[j].n, vn + nt);
          *(UINT*)&vv[j].t.x = c + 1; break;
        }
        auto l = *(UINT*)&vv[j].t.y; if (l != 0) { j = l - 1; continue; }
        *(UINT*)&vv[j].t.y = np + 1; vv[np].p = vv[j].p; *(UINT64*)&vv[np].t.x = 0; j = np++;
      }
      kk[i + k] = (kk[i + k] << 16) | j;
    }
  }
  XMMATRIX mt; if ((fl & 1) != 0) mt = XMLoadFloat4x3((XMFLOAT4X3*)tex);
  for (UINT i = 0; i < np; i++)
  {
    XMStoreFloat3(&vv[i].n, XMVector3Normalize(XMLoadFloat3(&vv[i].n)));
    if ((fl & 1) == 0) { *(UINT64*)&vv[i].t = 0; continue; }
    XMStoreFloat2(&vv[i].t, XMVector3Transform(XMLoadFloat4A((XMFLOAT4A*)&vv[i].p), mt));
  }
  if ((fl & 2) != 0)
  {
    for (UINT i = 0, j; i < ni; i++)
    {
      auto p = ((XMFLOAT2*)tex)[i];
      if (*(UINT64*)&vv[j = kk[i] & 0xffff].t == *(UINT64*)&p) continue;
      if (*(UINT64*)&vv[j].t != 0)
      {
        vv[np] = vv[j];
        for (UINT t = i; t < ni; t++)
          if ((kk[t] & 0xffff) == j && *(UINT64*)&((XMFLOAT2*)tex)[t] == *(UINT64*)&p)
            *(USHORT*)&kk[t] = (USHORT)(np & 0xffff);
        j = np++;
      }
      vv[j].t = p;
    }
  }

  UINT nn = sizeof(GUID), bv = np * sizeof(VERTEX), bk = ni * sizeof(int);
  UINT hc_vb = bv; for (UINT i = 0, n = min(1000, bv >> 2); i < n; i++) hc_vb = hc_vb * 13 + ((UINT*)vv)[i];
  UINT hc_ib = bk; for (UINT i = 0, n = min(1000, bk >> 2); i < n; i++) hc_ib = hc_ib * 13 + ((UINT*)kk)[i];
  {
    Critical crit;
    for (auto p = CVertices::first; p; p = p->next) if (p->hash == hc_vb && equals(p->p.p, vv, bv)) { vb = p; break; }
    for (auto p = CIndices::first; p; p = p->next) if (p->hash == hc_ib && equals(p->p.p, kk, bk)) { ib = p; break; }
  }
  if (vb.p && ib.p) return;

  D3D11_BUFFER_DESC bd = { 0, D3D11_USAGE_IMMUTABLE, 0, 0, 0, 0 }; D3D11_SUBRESOURCE_DATA data = { 0 };
  if (!vb.p)
  {
    vb.p = new CVertices(); vb.p->hash = hc_vb;
    bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
    bd.ByteWidth = np * sizeof(VERTEX); data.pSysMem = vv;
    device.p->CreateBuffer(&bd, &data, &vb.p->p.p);
  }
  if (!ib.p)
  {
    ib.p = new CIndices(); ib.p->hash = hc_ib; ib.p->ni = ni << 1;
    bd.BindFlags = D3D11_BIND_INDEX_BUFFER;
    bd.ByteWidth = ni * sizeof(int); data.pSysMem = kk;
    device.p->CreateBuffer(&bd, &data, &ib.p->p.p);
#if(0)
    for (UINT i = 0; i < ni; i += 3)
    {
      auto p1 = XMLoadFloat3(&vv[LOWORD(kk[i + 0])].p);
      auto p2 = XMLoadFloat3(&vv[LOWORD(kk[i + 1])].p);
      auto p3 = XMLoadFloat3(&vv[LOWORD(kk[i + 2])].p);

      auto t1 = XMLoadFloat3(&vv[HIWORD(kk[i + 0])].p);
      auto t2 = XMLoadFloat3(&vv[HIWORD(kk[i + 1])].p);
      auto t3 = XMLoadFloat3(&vv[HIWORD(kk[i + 2])].p);

      auto c1 = XMVector3LengthSq(XMVector3Cross(p2 - p1, p3 - p1));
      auto c2 = XMVector3LengthSq(XMVector3Cross(p1 - t1, p2 - t1));
      auto c3 = XMVector3LengthSq(XMVector3Cross(p2 - t2, p3 - t2));
      auto c4 = XMVector3LengthSq(XMVector3Cross(p3 - t3, p1 - t3));
      if (c1.m128_f32[0] == 0 || isnan(c1.m128_f32[0]))
      {
        c1 = c1;
      }
      if (c2.m128_f32[0] == 0 || isnan(c2.m128_f32[0]))
      {
        c2 = c2;
      }
      if (c3.m128_f32[0] == 0 || isnan(c3.m128_f32[0]))
      {
        c2 = c2;
      }
      if (c4.m128_f32[0] == 0 || isnan(c4.m128_f32[0]))
      {
        c2 = c2;
      }
    }
#endif
  }
}

void CNode::update(CScene* scene, UINT i)
{
  ib.Release(); vb.Release();
  auto pp = getbuffer(CDX_BUFFER_POINTBUFFER); if (!pp) return;
  auto ii = getbuffer(CDX_BUFFER_INDEXBUFFER); if (!ii || !ii->data.n) return;
  auto tt = getbuffer(CDX_BUFFER_TEXCOORDS);
  float flatt = getprop("@flat", 0.2f);
  update((XMFLOAT3*)pp->data.p, pp->data.n / sizeof(XMFLOAT3),
    (USHORT*)ii->data.p, ii->data.n >> 1, flatt,
    tt ? tt->data.p : 0, tt ? 2 : 0);
}

static XMFLOAT2 _angle(float a)
{
  XMFLOAT2 p;
  p.x = cosf(a); if (abs(p.x) == 1) { p.y = 0; return p; }
  p.y = sinf(a); if (abs(p.y) == 1) { p.x = 0; } return p;
}
static XMMATRIX XM_CALLCONV rotx(float a)
{
  auto v = _angle(a);
  auto s = _mm_set_ss(v.y);
  auto c = _mm_set_ss(v.x);
  c = _mm_shuffle_ps(c, s, _MM_SHUFFLE(3, 0, 0, 3));
  XMMATRIX m;
  m.r[0] = g_XMIdentityR0;
  m.r[1] = c;
  c = XM_PERMUTE_PS(c, _MM_SHUFFLE(3, 1, 2, 0));
  c = _mm_mul_ps(c, g_XMNegateY);
  m.r[2] = c;
  m.r[3] = g_XMIdentityR3;
  return m;
}
static XMMATRIX XM_CALLCONV roty(float a)
{
  auto v = _angle(a);
  auto s = _mm_set_ss(v.y);
  auto c = _mm_set_ss(v.x);
  s = _mm_shuffle_ps(s, c, _MM_SHUFFLE(3, 0, 3, 0));
  XMMATRIX m;
  m.r[2] = s;
  m.r[1] = g_XMIdentityR1;
  s = XM_PERMUTE_PS(s, _MM_SHUFFLE(3, 0, 1, 2));
  s = _mm_mul_ps(s, g_XMNegateZ);
  m.r[0] = s;
  m.r[3] = g_XMIdentityR3;
  return m;
}
static XMMATRIX XM_CALLCONV rotz(float a)
{
  auto v = _angle(a);
  auto s = _mm_set_ss(v.y);
  auto c = _mm_set_ss(v.x);
  c = _mm_unpacklo_ps(c, s);
  XMMATRIX m;
  m.r[0] = c;
  c = XM_PERMUTE_PS(c, _MM_SHUFFLE(3, 2, 0, 1));
  c = _mm_mul_ps(c, g_XMNegateX);
  m.r[1] = c;
  m.r[2] = g_XMIdentityR2;
  m.r[3] = g_XMIdentityR3;
  return m;
}

inline XMMATRIX XM_CALLCONV _XMLoadFloat4x3(const XMFLOAT4X3* p)
{
  XMMATRIX m;
  m.r[0] = _mm_and_ps(_mm_loadu_ps(&p->_11), g_XMMask3);
  m.r[1] = _mm_and_ps(_mm_loadu_ps(&p->_21), g_XMMask3);
  m.r[2] = _mm_and_ps(_mm_loadu_ps(&p->_31), g_XMMask3);
  m.r[3] = _mm_or_ps(_mm_and_ps(_mm_loadu_ps(&p->_41), g_XMMask3), g_XMIdentityR3);
  return m;
}
XMMATRIX XM_CALLCONV CNode::getmatrix() const
{
  if (*(int*)&matrix._33 == 0x7f000001)
  {
    auto m = XMMatrixScalingFromVector(XMLoadFloat3((const XMFLOAT3*)&matrix._11));
    if (matrix._21) m = m * rotx(matrix._21);
    if (matrix._22) m = m * roty(matrix._22);
    if (matrix._23) m = m * rotz(matrix._23);
    m.r[3] = XMLoadFloat3((XMFLOAT3*)&matrix._41) + g_XMIdentityR3;
    return m;
  }
  return _XMLoadFloat4x3(&matrix);
}
void CNode::setmatrix(const XMMATRIX& m)
{
  XMStoreFloat4x3(&matrix, m);
}
HRESULT CNode::get_Transform(XMFLOAT4X3* p)
{
  if (*(int*)&matrix._33 == 0x7f000001)
    XMStoreFloat4x3(p, getmatrix());
  else
    *p = matrix;
  return 0;
}

HRESULT CNode::put_Transform(XMFLOAT4X3 p)
{
  if (*(int*)&matrix._33 == 0x7f000001 && *(int*)&p._33 != 0x7f000001)
  {
    auto m1 = getmatrix();
    auto m2 = _XMLoadFloat4x3(&p);
    if (
      _mm_movemask_ps(XMVectorEqual(m1.r[0], m2.r[0])) == 0xf &&
      _mm_movemask_ps(XMVectorEqual(m1.r[1], m2.r[1])) == 0xf &&
      _mm_movemask_ps(XMVectorEqual(m1.r[2], m2.r[2])) == 0xf)
    {
      *(XMFLOAT3*)&matrix._41 = *(XMFLOAT3*)&p._41;
      return 0;
    }
  }
  matrix = p; // setmatrix(m);
  return 0;
}

HRESULT CNode::GetTypeTransform(UINT typ, XMFLOAT4X3* p)
{
  switch (typ)
  {
  case  0:
    *p = matrix; //XMStoreFloat4x3(p, _XMLoadFloat4x3(&matrix));
    return 0;
  case 1:
  {
    if (*(int*)&matrix._33 == 0x7f000001)
    {
      *p = matrix; //XMStoreFloat4x3(p, _XMLoadFloat4x3(&matrix));
      return 0;
    }
    auto m = getmatrix();
    auto lx = XMVector3Length(m.r[0]);
    auto ly = XMVector3Length(m.r[1]);
    auto lz = XMVector3Length(m.r[2]);
    auto lv = _mm_shuffle_ps(lx, ly, _MM_SHUFFLE(0, 0, 0, 0));
    lv = XMVectorRotateLeft(lv, 1);
    lv = _mm_shuffle_ps(lv, lz, _MM_SHUFFLE(0, 0, 1, 0));

    //auto mv = XMVectorAbs(g_XMOne - lv);
    //mv = XMVectorInBounds(mv, XMVectorReplicate(0.0000001f)); //auto mask = _mm_movemask_ps(lv);
    //lv = XMVectorSelect(lv, g_XMOne, mv);

    auto r = XMVectorReplicate(100000);// 65536.0f);
    lv = XMVectorRound(lv * r) / r;

    XMStoreFloat3(&((XMFLOAT3*)p)[0], lv);
    m.r[0] /= XMVectorSplatX(lv);
    m.r[1] /= XMVectorSplatY(lv);
    m.r[2] /= XMVectorSplatZ(lv);
    ((XMFLOAT3*)p)[1] = XMFLOAT3(
      atan2f(XMVectorGetZ(m.r[1]), XMVectorGetZ(m.r[2])), //m._23, m._33
      -asinf(XMVectorGetZ(m.r[0])), //m._13
      atan2f(XMVectorGetY(m.r[0]), XMVectorGetX(m.r[0]))); //m._12, m._11
    ((XMFLOAT3*)p)[2] = XMFLOAT3(0, 0, 0);
    XMStoreFloat3(&((XMFLOAT3*)p)[3], m.r[3]); *(int*)&p->_33 = 0x7f000001;
  }
  }
  return 0;
}
HRESULT CNode::SetTypeTransform(UINT typ, const XMFLOAT4X3* p)
{
  switch (typ)
  {
  case 0:
    matrix = *p;// XMStoreFloat4x3(&matrix, XMLoadFloat4x3(p));
    return 0;
  }
  return 0;
}

XMMATRIX CNode::gettrans(IUnknown* root)
{
  XMMATRIX m = getmatrix();
  for (CNode* p = this; ;)
  {
    if (p->parent == root) break;
    if (!root && *(void**)this != *(void**)p->parent) break;
    p = static_cast<CNode*>(p->parent);
    m *= p->getmatrix();
  }
  return m;
}

CCacheBuffer* CCacheBuffer::first;
CBuffer* CBuffer::getbuffer(CDX_BUFFER id, const BYTE* p, UINT n)
{
  for (auto t = CCacheBuffer::first; t; t = t->next)
  {
    if (t->id != id) continue;
    if (t->data.n != n) continue;
    if (memcmp(t->data.p, p, n)) continue;
    return t;
  }
  CBuffer* pb =
    id == CDX_BUFFER_TEXTURE ? static_cast<CBuffer*>(new CComClass<CTexture>()) :
    id == CDX_BUFFER_SCRIPT ? static_cast<CBuffer*>(new CComClass<CTagBuffer>()) :
    static_cast<CBuffer*>(new CComClass<CCacheBuffer>());
  pb->id = id; pb->data.setsize(n); memcpy(pb->data.p, p, n); return pb;
}

HRESULT CFactory::GetBuffer(CDX_BUFFER id, const BYTE* p, UINT n, ICDXBuffer** v)
{
  Critical crit;
  (*v = CCacheBuffer::getbuffer(id, p, n))->AddRef();
  return 0;
}

CBuffer* CNode::getbuffer(CDX_BUFFER id) const
{
  //if (id < 32)
  //{
  UINT m = 1 << id; if ((bmask & m) == 0) return 0;
  return buffer.p[__popcnt(bmask & (m - 1))];
  //}
  //for (int i = __popcnt(bmask); i < buffer.m_nSize; i++)
  //{
  //  if (buffer.m_aT[i].p->id == id)
  //    return buffer.m_aT[i].p;
  //  if (buffer.m_aT[i].p->id > id)
  //    break;
  //}
  //return 0;
}

void CNode::setbuffer(CDX_BUFFER id, CBuffer* p)
{
  //XMASSERT(!p || (p->id == id || (p->id == CDX_BUFFER_TEXTURE && id >= CDX_BUFFER_TEXTURE)));
  UINT m = 1 << id, x = __popcnt(bmask & (m - 1));
  if ((bmask & m) != 0)
  {
    if (buffer.p[x] == p) return; inval(id);
    buffer.p[x]->Release();
    if (p) (buffer.p[x] = p)->AddRef();
    else { buffer.removeat(x); bmask &= ~m; }
    return;
  }
  if (!p) return; inval(id);
  buffer.insertat(x, p); p->AddRef(); bmask |= m;

  //if (!buffer.m_nAllocSize)
  //  buffer.m_aT = (CComPtr<CBuffer>*)calloc(buffer.m_nAllocSize = 4, sizeof(CComPtr<CBuffer>));
  //buffer.Add(0);
  //for (UINT t = buffer.m_nSize - 1; t > x; t--) buffer.m_aT[t].p = buffer.m_aT[t - 1].p;
  //buffer.m_aT[x].p = 0; buffer.m_aT[x] = p; bmask |= m;
  //int i = 0;
  //for (; i < buffer.m_nSize; i++)
  //{
  //  if (buffer.m_aT[i].p->id < id) continue;
  //  if (buffer.m_aT[i].p->id == id)
  //  {
  //    if (buffer.m_aT[i].p == p) return;
  //    inval(id);
  //    if (p) buffer.m_aT[i] = p;
  //    else
  //    {
  //      buffer.RemoveAt(i);
  //      if (id < 32) bmask &= ~(1 << id);
  //    }
  //    return;
  //  }
  //  break;
  //}
  //if (!p) return;
  //inval(id);
  //if (!buffer.m_nAllocSize)
  //  buffer.m_aT = (CComPtr<CBuffer>*)calloc(buffer.m_nAllocSize = 4, sizeof(CComPtr<CBuffer>));
  //if (i < buffer.m_nSize)
  //{
  //  buffer.Add(0);
  //  for (int t = buffer.m_nSize - 1; t > i; t--)
  //    buffer.m_aT[t].p = buffer.m_aT[t - 1].p;
  //  buffer.m_aT[i].p = 0; buffer.m_aT[i] = p;
  //}
  //else buffer.Add(p);
  //if (id < 32) bmask |= 1 << id;
}

HRESULT CNode::GetBuffer(CDX_BUFFER id, ICDXBuffer** p)
{
  if (*p = getbuffer(id))
    (*p)->AddRef();
  return 0;
}
HRESULT CNode::SetBuffer(CDX_BUFFER id, ICDXBuffer* p)
{
  if (id > 31) return E_INVALIDARG;
  auto pb = static_cast<CBuffer*>(p);
  if (p && !(id == pb->id || (pb->id == CDX_BUFFER_TEXTURE && id >= CDX_BUFFER_TEXTURE)))
    return E_INVALIDARG;
  setbuffer(id, pb);
  return 0;
}
HRESULT CNode::GetBufferPtr(CDX_BUFFER id, const BYTE** p, UINT* n)
{
  auto t = getbuffer(id); if (!t) return 1;
  *p = t->data.p; *n = t->data.n; return 0;
}
HRESULT CNode::SetBufferPtr(CDX_BUFFER id, const BYTE* p, UINT n)
{
  if (id & 0x1000) // int -> ushort
  {
    for (UINT i = 0, c = n >> 2; i < c; i++) ((USHORT*)stackptr)[i] = ((const UINT*)p)[i];
    return SetBufferPtr((CDX_BUFFER)(id & ~0x1000), (const BYTE*)stackptr, n >> 1);
  }
  if (id > 31) return E_INVALIDARG;
  if (!p) { setbuffer(id, 0); return 0; }
  Critical crit;
  setbuffer(id, CCacheBuffer::getbuffer(min(id, CDX_BUFFER_TEXTURE), p, n));
  return 0;
}

void CNode::inval(CDX_BUFFER id)
{
  if (id <= CDX_BUFFER_TEXCOORDS) { flags &= ~NODE_FL_MASHOK; return; }
}

HRESULT CNode::get_Texture(ICDXBuffer** tex)
{
  if (*tex = static_cast<CTexture*>(getbuffer(CDX_BUFFER_TEXTURE)))
    (*tex)->AddRef();
  return 0;
}
HRESULT CNode::put_Texture(ICDXBuffer* p)
{
  setbuffer(CDX_BUFFER_TEXTURE, static_cast<CTexture*>(p));
  return 0;
}
