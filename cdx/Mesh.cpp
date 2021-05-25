#include "pch.h"
#include "Factory.h"
#include "Mesh.h"

HRESULT CNode::CopyCoords(ICDXBuffer* bpp, ICDXBuffer* bii, float eps, ICDXBuffer** btt)
{ 
  const auto patt = this->getbuffer(CDX_BUFFER_TEXCOORDS); if (!patt) return 1;
  const auto papp = this->getbuffer(CDX_BUFFER_POINTBUFFER); if (!papp) return 1;
  const auto paii = this->getbuffer(CDX_BUFFER_INDEXBUFFER); if (!paii) return 1;
  const auto pbpp = static_cast<CBuffer*>(bpp); if (pbpp->id != CDX_BUFFER_POINTBUFFER) return E_INVALIDARG;
  const auto pbii = static_cast<CBuffer*>(bii); if (pbii->id != CDX_BUFFER_INDEXBUFFER) return E_INVALIDARG;
  
  const auto appp = (const XMFLOAT3*)papp->data.p; auto appn = papp->data.n / sizeof(XMFLOAT3);
  const auto aiip = (const USHORT*)paii->data.p;   auto aiin = paii->data.n / sizeof(USHORT);
  const auto attp = (const XMFLOAT2*)patt->data.p; auto attn = patt->data.n / sizeof(XMFLOAT2);
  const auto bppp = (const XMFLOAT3*)pbpp->data.p; auto bppn = pbpp->data.n / sizeof(XMFLOAT3);
  const auto biip = (const USHORT*)pbii->data.p;   auto biin = pbii->data.n / sizeof(USHORT);
 
  auto aee = (XMVECTOR*)__align16(stackptr); UINT aeen = aiin / 3;
  UINT nc = 0; auto cc = (UINT*)(aee + aeen); 
  auto bttp = (XMFLOAT2*)(cc + aiin / 3); memset(bttp, 0, biin * sizeof(XMFLOAT2));
  for (UINT j = 0; j < aiin; j += 3)
  {
    auto e = XMPlaneFromPoints(
      _mm_loadu_ps(&appp[aiip[j + 0]].x),
      _mm_loadu_ps(&appp[aiip[j + 1]].x),
      _mm_loadu_ps(&appp[aiip[j + 2]].x));
    if (nc != 0 && XMVector4Equal(e, aee[nc - 1])) { cc[nc - 1] = j + 3; continue; }
    cc[nc] = j + 3; aee[nc++] = e;
  }
  for (UINT i = 0, lk = 0; i < biin; i += 3)
  {
    auto P1 = _mm_loadu_ps(&bppp[biip[i + 0]].x);
    auto P2 = _mm_loadu_ps(&bppp[biip[i + 1]].x);
    auto P3 = _mm_loadu_ps(&bppp[biip[i + 2]].x);
    auto be = XMPlaneFromPoints(P1, P2, P3);
    auto w = XMVectorAbs(be);
    auto l = w.m128_f32[0] > w.m128_f32[1] && w.m128_f32[0] > w.m128_f32[2] ? 0 : w.m128_f32[1] > w.m128_f32[2] ? 1 : 2;
    auto dir = (l == 0 ? -be.m128_f32[0] : l == 1 ? +be.m128_f32[1] : -be.m128_f32[2]) > 0 ? +1.0f : -1.0f;
    auto p1 = XMVectorSwizzle(P1, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
    auto p2 = XMVectorSwizzle(P2, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
    auto p3 = XMVectorSwizzle(P3, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
    auto mp = (p1 + p2 + p3) * (1.0f / 3);
    for (UINT t = 0; t < nc; t++)
    {
      UINT k = (lk + t) % nc;
      if (XMVectorGetX(XMVector3LengthSq(aee[k] - be)) > 1e-8f) continue;
      if (fabsf(XMVectorGetW(aee[k] - be)) > 1e-4f) continue;
      for (UINT j = k == 0 ? 0 : cc[k - 1]; j < cc[k]; j += 3)
      {
        auto T1 = _mm_loadu_ps(&appp[aiip[j + 0]].x);
        auto T2 = _mm_loadu_ps(&appp[aiip[j + 1]].x);
        auto T3 = _mm_loadu_ps(&appp[aiip[j + 2]].x);
        auto t1 = XMVectorSwizzle(T1, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto t2 = XMVectorSwizzle(T2, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto t3 = XMVectorSwizzle(T3, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto f1 = XMVector2Cross(t2 - t1, mp - t1); if (XMVectorGetX(f1) * dir > 1e-8) continue;
        auto f2 = XMVector2Cross(t3 - t2, mp - t2); if (XMVectorGetX(f2) * dir > 1e-8) continue;
        auto f3 = XMVector2Cross(t1 - t3, mp - t3); if (XMVectorGetX(f3) * dir > 1e-8) continue;
        auto va = t2 - t1;
        auto vb = t3 - t1;
        auto d = XMVector2Cross(va, vb); if (XMVectorGetX(d) == 0) break; d = XMVectorReciprocal(d);
        auto ua = p1 - t1;
        auto ub = p2 - t1;
        auto uc = p3 - t1;
        auto c1 = XMLoadFloat2(&attp[j + 0]);
        auto c2 = (XMLoadFloat2(&attp[j + 1]) - c1) * d;
        auto c3 = (XMLoadFloat2(&attp[j + 2]) - c1) * d;
        XMStoreFloat2(&bttp[i + 0], c1 + c2 * XMVector2Cross(ua, vb) + c3 * XMVector2Cross(va, ua));
        XMStoreFloat2(&bttp[i + 1], c1 + c2 * XMVector2Cross(ub, vb) + c3 * XMVector2Cross(va, ub));
        XMStoreFloat2(&bttp[i + 2], c1 + c2 * XMVector2Cross(uc, vb) + c3 * XMVector2Cross(va, uc));
        lk = k; goto next;
      }
    }
  next:;
  }
  Critical crit;
  (*btt = CCacheBuffer::getbuffer(CDX_BUFFER_TEXCOORDS, (const BYTE*)bttp, biin * sizeof(XMFLOAT2)))->AddRef();
  return 0;
}

void CNode::getbox(XMVECTOR box[2], const XMMATRIX* pm, CBuffer* pb)
{
  if (!pb) { pb = getbuffer(CDX_BUFFER_POINTBUFFER); if (!pb) return; }
  auto vv = (XMFLOAT3*)pb->data.p;
  for (UINT i = 0, n = pb->data.n / sizeof(XMFLOAT3); i < n; i++)
  {
    auto p = _mm_loadu_ps(&vv[i].x);
    if (pm) p = XMVector3Transform(p, *pm);
    box[0] = XMVectorMin(box[0], p);
    box[1] = XMVectorMax(box[1], p);
  }
}
HRESULT CNode::GetBox(XMFLOAT3 box[2], const XMFLOAT4X3* pm)
{
  auto pb = getbuffer(CDX_BUFFER_POINTBUFFER); if (!pb) return 0;
  XMVECTOR bb[2]; XMMATRIX m; if (pm) m = XMLoadFloat4x3(pm);
  bb[0] = _mm_loadu_ps(&box[0].x);
  bb[1] = _mm_loadu_ps(&box[1].x);
  getbox(bb, pm ? &m : 0, pb);
  _mm_storeu_ps(&box[0].x, bb[0]);
  XMStoreFloat3(&box[1], bb[1]);
  return 0;
}

//HRESULT __stdcall get_Enum(SAFEARRAY** p)
//{
//  SAFEARRAYBOUND rgsabound; rgsabound.cElements = (ULONG)Count; rgsabound.lLbound = 0;
//  auto psa = SafeArrayCreate(VT_UNKNOWN, 1, &rgsabound);
//  for (LONG i = 0; i < Count; i++) { IOptAngle* t; get_Items(i, &t); SafeArrayPutElement(psa, &i, t); t->Release(); }
//  *p = psa; return 0;
//}
