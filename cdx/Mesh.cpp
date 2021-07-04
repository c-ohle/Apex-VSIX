#include "pch.h"
#include "Factory.h"
#include "Mesh.h"

struct RT { XRANGE r; CBuffer* t; };

static void join(
  const XMFLOAT3* pp, const USHORT* ii, UINT iiLength, XMFLOAT2* tt,
  const XMFLOAT3* pporg, const USHORT* iiorg, UINT iiorgLength, const XMFLOAT2* ttorg, const RT* rtorg, UINT rtorgLength,
  RT* rr, UINT& nr)
{
  for (int x = 0, i = 0, nc = nr; ; i = rr[x].r.start + rr[x].r.count, x++)
  {
    for (int ni = x < nc ? rr[x].r.start : iiLength; i < ni; i += 3)
    {
      auto p1 = _mm_loadu_ps(&pp[ii[i + 0]].x);
      auto p2 = _mm_loadu_ps(&pp[ii[i + 1]].x);
      auto p3 = _mm_loadu_ps(&pp[ii[i + 2]].x);
      auto be = XMPlaneFromPoints(p1, p2, p3);
      auto w = XMVectorAbs(be);
      auto l = w.m128_f32[0] > w.m128_f32[1] && w.m128_f32[0] > w.m128_f32[2] ? 0 : w.m128_f32[1] > w.m128_f32[2] ? 1 : 2;
      auto d = (l == 0 ? -be.m128_f32[0] : l == 1 ? +be.m128_f32[1] : -be.m128_f32[2]) > 0 ? +1.0f : -1.0f;
      auto s1 = XMVectorSwizzle(p1, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
      auto s2 = XMVectorSwizzle(p2, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
      auto s3 = XMVectorSwizzle(p3, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
      auto mp = (s1 + s2 + s3) * (1.0f / 3);
      for (int k = 0; k < iiorgLength; k += 3)
      {
        auto u1 = _mm_loadu_ps(&pporg[iiorg[k + 0]].x); if (abs(XMVectorGetX(XMPlaneDotCoord(be, u1))) > 1e-3f) continue;
        auto u2 = _mm_loadu_ps(&pporg[iiorg[k + 1]].x); if (abs(XMVectorGetX(XMPlaneDotCoord(be, u2))) > 1e-3f) continue;
        auto u3 = _mm_loadu_ps(&pporg[iiorg[k + 2]].x); if (abs(XMVectorGetX(XMPlaneDotCoord(be, u3))) > 1e-3f) continue;
        auto t1 = XMVectorSwizzle(u1, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto t2 = XMVectorSwizzle(u2, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto t3 = XMVectorSwizzle(u3, l == 0 ? 1 : 0, l == 2 ? 1 : 2, 0, 0);
        auto f1 = XMVector2Cross(t2 - t1, mp - t1); if (XMVectorGetX(f1) * d > 1e-6f) continue;
        auto f2 = XMVector2Cross(t3 - t2, mp - t2); if (XMVectorGetX(f2) * d > 1e-6f) continue;
        auto f3 = XMVector2Cross(t1 - t3, mp - t3); if (XMVectorGetX(f3) * d > 1e-6f) continue;
        /////
        UINT t = 0; for (; t < rtorgLength && !(rtorg[t].r.start <= k && rtorg[t].r.start + rtorg[t].r.count > k); t++);
        auto& r = rtorg[t]; auto ra = &rr[(nr != 0 ? nr : nr = 1) - 1];
        if (ra->r.start + ra->r.count != i || ra->r.color != r.r.color || ra->t != r.t)
        {
          if (ra->r.count != 0) ra = &rr[nr++];
          ra->r.start = i; ra->r.count = 0; ra->r.color = r.r.color; ra->t = r.t;
        }
        ra->r.count += 3;
        /////
        if (!ttorg) break;
        auto va = t2 - t1; auto vb = t3 - t1;
        auto de = XMVector2Cross(va, vb); if (XMVectorGetX(de) == 0) break; de = XMVectorReciprocal(de);
        auto ua = s1 - t1; auto ub = s2 - t1; auto uc = s3 - t1;
        auto c1 = XMLoadFloat2(&ttorg[k + 0]);
        auto c2 = (XMLoadFloat2(&ttorg[k + 1]) - c1) * de;
        auto c3 = (XMLoadFloat2(&ttorg[k + 2]) - c1) * de;
        XMStoreFloat2(&tt[i + 0], c1 + c2 * XMVector2Cross(ua, vb) + c3 * XMVector2Cross(va, ua));
        XMStoreFloat2(&tt[i + 1], c1 + c2 * XMVector2Cross(ub, vb) + c3 * XMVector2Cross(va, ub));
        XMStoreFloat2(&tt[i + 2], c1 + c2 * XMVector2Cross(uc, vb) + c3 * XMVector2Cross(va, uc)); break;
      }
    }
    if (x == nc) break;
  }
}

HRESULT CFactory::RestoreRanges(const XMFLOAT3* pp, UINT np, const USHORT* ii, UINT ni, ICDXNode* pn1, ICDXNode* pn2, const XMFLOAT4X3* rm, UINT fl, FLOAT eps, ICDXNode** pret)  //fl 1:inv
{
  auto n1 = static_cast<CNode*>(pn1);
  auto ppa = n1->getbuffer(CDX_BUFFER_POINTBUFFER); auto ppaPtr = (const XMFLOAT3*)ppa->data.p; //auto ppaLength = ppa->data.n / sizeof(XMFLOAT3);
  auto iia = n1->getbuffer(CDX_BUFFER_INDEXBUFFER); auto iiaPtr = (const USHORT*)iia->data.p; auto iiaLength = iia->data.n / sizeof(USHORT);
  auto tta = n1->getbuffer(CDX_BUFFER_TEXCOORDS); auto ttaPtr = tta ? (XMFLOAT2*)tta->data.p : 0;
  auto rra = n1->getbuffer(CDX_BUFFER_RANGES);
  auto rta = (RT*)stackptr; auto rtaLength = rra ? rra->data.n / sizeof(XRANGE) : 1;
  if (rra)
    for (UINT i = 0; i < rtaLength; i++) {
      rta[i].r = ((const XRANGE*)rra->data.p)[i];
      rta[i].t = i < 16 ? n1->getbuffer((CDX_BUFFER)((UINT)CDX_BUFFER_TEXTURE + i)) : 0;
    }
  else {
    rta[0].r.start = 0; rta[0].r.count = iiaLength; rta[0].r.color = n1->color;
    rta[0].t = n1->getbuffer(CDX_BUFFER_TEXTURE);
  }

  auto n2 = static_cast<CNode*>(pn2);
  auto ppb = n2->getbuffer(CDX_BUFFER_POINTBUFFER); auto ppbPtr = (const XMFLOAT3*)ppb->data.p; auto ppbLength = ppb->data.n / sizeof(XMFLOAT3);
  auto iib = n2->getbuffer(CDX_BUFFER_INDEXBUFFER); auto iibPtr = (USHORT*)iib->data.p; auto iibLength = iib->data.n / sizeof(USHORT);
  auto ttb = n2->getbuffer(CDX_BUFFER_TEXCOORDS); auto ttbPtr = ttb ? (XMFLOAT2*)ttb->data.p : 0;
  auto rrb = n2->getbuffer(CDX_BUFFER_RANGES);
  auto rtb = rta + rtaLength; auto rtbLength = rrb ? rrb->data.n / sizeof(XRANGE) : 1;
  if (rrb)
    for (UINT i = 0; i < rtbLength; i++) {
      rtb[i].r = ((const XRANGE*)rrb->data.p)[i];
      rtb[i].t = i < 16 ? n2->getbuffer((CDX_BUFFER)((UINT)CDX_BUFFER_TEXTURE + i)) : 0;
    }
  else {
    rtb[0].r.start = 0; rtb[0].r.count = iibLength; rtb[0].r.color = n2->color;
    rtb[0].t = n2->getbuffer(CDX_BUFFER_TEXTURE);
  }

  auto stackp = (void*)(rtb + rtbLength);
  if (rm) {
    auto t = (XMFLOAT3*)stackp;
    XMVector3TransformCoordStream(t, sizeof(XMFLOAT3), ppbPtr, sizeof(XMFLOAT3), ppbLength, XMLoadFloat4x3(rm));
    ppbPtr = t; stackp = (void*)(ppbPtr + ppbLength);
  }
  if (fl & 1) //inv
  {
    { auto t = (USHORT*)stackp; memcpy(t, iibPtr, iibLength * sizeof(USHORT)); iibPtr = t; stackp = (void*)(iibPtr + iibLength); }
    if (ttbPtr) { auto t = (XMFLOAT2*)stackp; memcpy(t, ttbPtr, iibLength * sizeof(XMFLOAT2)); ttbPtr = t; stackp = (void*)(ttbPtr + iibLength); }
    for (UINT i = 0; i < iibLength; i += 3)
    {
      auto t1 = iibPtr[i]; iibPtr[i] = iibPtr[i + 1]; iibPtr[i + 1] = t1; if (!ttbPtr) continue;
      auto t2 = ttbPtr[i]; ttbPtr[i] = ttbPtr[i + 1]; ttbPtr[i + 1] = t2;
    }
  }

  auto tt = tta || ttb ? (XMFLOAT2*)stackp : 0; if (tt) stackp = (void*)(tt + ni);
  UINT nu = 0; auto uu = (RT*)stackp; uu[0].r.count = 0;
  join(pp, ii, ni, tt, ppaPtr, iiaPtr, iiaLength, ttaPtr, rta, rtaLength, uu, nu);
  join(pp, ii, ni, tt, ppbPtr, iibPtr, iibLength, ttbPtr, rtb, rtbLength, uu, nu);

  auto node = new CComClass<CNode>();
  node->SetBufferPtr(CDX_BUFFER_POINTBUFFER, (const BYTE*)pp, np * sizeof(XMFLOAT3));

  UINT c = 1; for (; c < nu && uu[c - 1].r.color == uu[c].r.color && uu[c - 1].t == uu[c].t; c++);
  if (c != nu)
  {
    UINT nuu = 0, nii = 0; stackp = (void*)(uu + nu);
    auto iii = (USHORT*)stackp; stackp = (void*)(iii + ni);
    auto ttt = tt ? (XMFLOAT2*)stackp : 0; if (tt)stackp = (void*)(ttt + ni);
    for (UINT i = 0, iab; i < nu; i++)
    {
      if (uu[i].r.count == -1) continue; iab = nii;
      for (UINT k = i; k < nu; k++)
      {
        if (uu[i].r.color != uu[k].r.color || uu[i].t != uu[k].t) continue;
        for (UINT j = 0, sj = uu[k].r.start, nj = uu[k].r.count; j < nj; j++, nii++)
        {
          iii[nii] = ii[sj + j]; if (!tt) continue;
          ttt[nii] = tt[sj + j];
        }
        uu[k].r.count = -1;
      }
      uu[nuu] = uu[i]; uu[nuu].r.start = iab; uu[nuu++].r.count = nii - iab;
    }
    ii = iii; tt = ttt; nu = nuu;
    auto rr = (XRANGE*)stackp; for (UINT i = 0; i < nu; i++) rr[i] = uu[i].r;
    node->SetBufferPtr(CDX_BUFFER_RANGES, (const BYTE*)rr, nu * sizeof(XRANGE));
  }
  node->SetBufferPtr(CDX_BUFFER_INDEXBUFFER, (const BYTE*)ii, ni * sizeof(USHORT));
  if (tt) node->SetBufferPtr(CDX_BUFFER_TEXCOORDS, (const BYTE*)tt, ni * sizeof(XMFLOAT2));
  for (UINT i = 0, n = min(nu, 15); i < n; i++) if (uu[i].t)
    node->setbuffer((CDX_BUFFER)((UINT)CDX_BUFFER_TEXTURE + i), uu[i].t);

  (*pret = static_cast<ICDXNode*>(node))->AddRef(); return 0;
}

HRESULT CFactory::CopyCoords(const XMFLOAT3* appp, const USHORT* aiip, UINT aiin, const XMFLOAT2* attp, const XMFLOAT3* bppp, const USHORT* biip, UINT biin, XMFLOAT2* bttp, FLOAT eps)
{
  auto aee = (XMVECTOR*)__align16(stackptr); UINT aeen = aiin / 3;
  UINT nc = 0; auto cc = (UINT*)(aee + aeen);
  //auto bttp = (XMFLOAT2*)(cc + aiin / 3); memset(bttp, 0, biin * sizeof(XMFLOAT2));
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

static void writecount(BYTE* bb, UINT& is, UINT c)
{
  for (; c >= 0x80; bb[is++] = c | 0x80, c >>= 7); bb[is++] = c;
}
static UINT readcount(const BYTE* bb, UINT& is)
{
  UINT c = 0;
  for (UINT shift = 0; ; shift += 7) { UINT b = bb[is++]; c |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; }
  return c;
}
const BYTE* getprop(const CBuffer* p, const char* name, UINT* np)
{
  auto dp = p->data.p;
  for (UINT i = 0; i < p->data.n;)
  {
    auto t1 = readcount(dp, i);
    UINT t = 0; for (; t < t1 && dp[i + t] == (BYTE)name[t]; t++);
    auto t2 = t == t1 && name[t] == 0; i += t1;
    auto t3 = readcount(dp, i);
    if (t2) { *np = t3; return dp + i + 1; }
    i += t3 + 1;
  }
  *np = 0; return 0;
}

const BYTE* CNode::getpropptr(const char* s, UINT n)
{
  auto pb = getbuffer(CDX_BUFFER_PROPS);
  if (pb)
  {
    UINT c; auto t = ::getprop(pb, s, &c);
    if (c == n)// && t[-1] == 13)
      return t;
  }
  return 0;
}

float CNode::getprop(const char* s, float def)
{
  auto p = getpropptr(s, 4);
  if (p && p[-1] == 13)
    return *(float*)p;
  return def;
  //auto pb = getbuffer(CDX_BUFFER_PROPS);
  //if (pb)
  //{
  //  UINT c; auto t = ::getprop(pb, s, &c);
  //  if (c == 4 && t[-1] == 13)
  //    return *(float*)t;
  //}
  //return def;
}

void CNode::propschk()
{
  if (flags & NODE_FL_PROPCHK)
    return;
  flags |= NODE_FL_PROPCHK; flags &= ~(NODE_FL_CAMERA | NODE_FL_LIGHT);
  auto pb = getbuffer(CDX_BUFFER_PROPS); if (!pb) return;
  auto dp = pb->data.p;
  for (UINT i = 0; i < pb->data.n;)
  {
    auto t1 = readcount(dp, i); auto t2 = i; i += t1;
    auto t3 = readcount(dp, i); auto t4 = i; i += t3 + 1;
    if (dp[t2] != '@') continue;
    if (t1 == 5 && t3 == 16 && !memcmp("@cfov", dp + t2, t1)) { flags |= NODE_FL_CAMERA; continue; }
    if (t1 == 5 && t3 == 12 && !memcmp("@ldir", dp + t2, t1)) { flags |= NODE_FL_LIGHT; continue; }
    if (t1 == 5 && t3 == 04 && !memcmp("@flat", dp + t2, t1)) { flags |= NODE_FL_MESHOPS; continue; }
  }
  //UINT n; auto p = ::getprop(pb, "@ldir", &n); if (n == 12) flags |= NODE_FL_LIGHT;
  //p = ::getprop(pb, "@fov", &n); if (n == 16) flags |= NODE_FL_CAMERA;
}


HRESULT CNode::SetProp(LPCWSTR s, const BYTE* p, UINT n, UINT typ)
{
  auto nb = lstrlen(s); auto sb = (char*)_alloca(nb + 1);
  for (int i = 0; i <= nb; i++) sb[i] = (BYTE)s[i];

  auto pb = getbuffer(CDX_BUFFER_PROPS); UINT updp = 0, delc = 0;
  if (pb)
  {
    UINT np; auto pp = ::getprop(pb, sb, &np);
    if (pp)
    {
      if (np == n)
      {
        if (pp[-1] == (BYTE)typ && !memcmp(pp, p, n))
          return 0;
        updp = (UINT)(pp - pb->data.p) - 1;
      }
      else
      {
        UINT h = 3;
        for (auto c = nb; c >= 0x80; h++, c >>= 7);
        for (auto c = np; c >= 0x80; h++, c >>= 7);
        updp = (UINT)(pp - pb->data.p) - nb - h;
        delc = (UINT)(pp - pb->data.p) + np - updp;
      }
    }
  }
  auto ss = (BYTE*)stackptr; UINT ns = 0;
  if (pb) memcpy(ss, pb->data.p, ns = pb->data.n);
  if (delc)
  {
    memcpy(ss + updp, ss + (updp + delc), ns - (updp + delc)); ns -= delc; updp = 0;
  }
  if (updp)
  {
    ss[updp] = (BYTE)typ; memcpy(ss + updp + 1, p, n);
  }
  else if (n)
  {
    writecount(ss, ns, nb); memcpy(ss + ns, sb, nb); ns += nb;
    writecount(ss, ns, n); ss[ns++] = (BYTE)typ; memcpy(ss + ns, p, n); ns += n;
  }
  if (sb[0] == '@')
  {
    flags &= ~NODE_FL_PROPCHK;
    if (flags & NODE_FL_MASHOK)
      if (!strcmp("@flat", sb))
        flags &= ~NODE_FL_MASHOK;
  }
  return SetBufferPtr(CDX_BUFFER_PROPS, ns ? ss : 0, ns);
}

HRESULT CNode::GetProp(LPCWSTR s, const BYTE** p, UINT* typ, UINT* n)
{
  auto pb = getbuffer(CDX_BUFFER_PROPS); if (!pb) return 1;

  auto nb = lstrlen(s); auto sb = (char*)_alloca(nb + 1);
  for (int i = 0; i <= nb; i++) sb[i] = (BYTE)s[i];

  if (*p = ::getprop(pb, sb, n)) *typ = (*p)[-1];
  return 0;
}

HRESULT __stdcall CNode::GetProps(BSTR* p)
{
  auto pb = getbuffer(CDX_BUFFER_PROPS); if (!pb) return 0;
  auto ss = (OLECHAR*)stackptr; UINT ns = 0;
  auto dp = pb->data.p;
  for (UINT i = 0; i < pb->data.n;)
  {
    auto t1 = readcount(dp, i);
    if (ns) ss[ns++] = '\n';
    for (UINT t = 0; t < t1; t++) ss[ns++] = dp[i + t];
    i += t1; auto t3 = readcount(dp, i); i += t3 + 1;
  }
  *p = SysAllocStringLen(ss, ns);
  return 0;
}
