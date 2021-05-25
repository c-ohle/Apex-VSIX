#include "pch.h"
#include "Factory.h"
#include "view.h"

HRESULT CView::Collision(const XMFLOAT4X3* pm, UINT xi, FLOAT* diff)
{
  auto tt = (UINT*)stackptr;
  if (!pm)
  {
    tt[0] = xi & 0xffff; tt[1] = xi >> 16; UINT nt = tt[0] * tt[1] * sizeof(float);
    tt[3] = 4 + (((nt >> 2) + 1) << 2); memset(tt + 4, 0, nt);
    return 0;
  }

  auto bkg = (xi & (1 << 31)) == 0;
  auto node = boxes[xi & 0x0fffffff].p;
  const auto pb = node->getbuffer(CDX_BUFFER_POINTBUFFER);
  const auto ib = node->getbuffer(CDX_BUFFER_INDEXBUFFER);

  XMVECTOR* vv = (XMVECTOR*)(tt + tt[3]);
  XMVector3TransformStream((XMFLOAT4*)vv, sizeof(XMVECTOR),
    (const XMFLOAT3*)pb->data.p, sizeof(XMFLOAT3), pb->data.n / sizeof(XMFLOAT3), XMLoadFloat4x3(pm));

  //auto m = XMLoadFloat4x3(pm); auto pp = (const XMFLOAT3*)pb->data.p;
  //for (UINT i = 0, n = pb->data.n / sizeof(XMFLOAT3); i < n; i++)
  //  vv[i] = XMVector3Transform(XMLoadFloat3(pp + i), m);

  int dx = (int)tt[0], dy = (int)tt[1]; auto maxzdz = 0.0f;
  auto minmask = XMVectorSet((float)dx, (float)dy, 1, 0);
  //auto maxmask = XMVectorSet(0, 0, bkg ? 0 : -10000, 0);
  UINT ni = ib->data.n >> 1; auto ii = (const USHORT*)ib->data.p;
  for (UINT i = 0; i < ni; i += 3)
  {
    XMVECTOR p1 = vv[ii[i]], p2 = vv[ii[i + 1]], p3 = vv[ii[i + 2]];
    XMVECTOR min = XMVectorMin(p1, XMVectorMin(p2, p3)); if (_mm_movemask_ps(_mm_cmpge_ps(min, minmask)) & 7) continue;  // x >= dx || y >= dz || z >= 1
    XMVECTOR max = XMVectorMax(p1, XMVectorMax(p2, p3)); if (_mm_movemask_ps(_mm_cmple_ps(max, g_XMZero)) & 7) continue; // x < 0 || y < 0 || z <= 0

    XMVECTOR e = XMVector3Cross(p2 - p1, p3 - p1);
    if (_mm_movemask_ps(bkg ? _mm_cmple_ps(e, g_XMZero) : _mm_cmpge_ps(e, g_XMZero)) & 4) continue;

    if (_mm_movemask_ps(_mm_cmpgt_ps(p1, p2)) & 2) { auto t = p1; p1 = p2; p2 = t; }
    if (_mm_movemask_ps(_mm_cmpgt_ps(p2, p3)) & 2) { auto t = p2; p2 = p3; p3 = t; }
    if (_mm_movemask_ps(_mm_cmpgt_ps(p1, p2)) & 2) { auto t = p1; p1 = p2; p2 = t; }

    int y1 = (int)XMVectorGetY(p1), y2 = (int)XMVectorGetY(p2), y3 = (int)XMVectorGetY(p3);
    if (y1 > dy) continue;
    if (y3 <= 0) continue;
    if (y2 <= 0) y1 = y2; else if (y2 > dy) y2 = dy;
    if (y3 > dy) y3 = dy;
    int dy31 = y3 - y1; if (dy31 == 0) continue;
    
    //PermuteHelper
    //e = XMVector3Cross(p2 - p1, p3 - p1); //var zz = new float3(e.x, e.y, -(p1->x * e.x + p1->y * e.y + p1->z * e.z)) * (-1 / e.z);
    XMVECTOR ze = _mm_shuffle_ps(e, -XMVector3Dot(p1, e), _MM_SHUFFLE(0, 0, 1, 0)) / -XMVectorSplatZ(e);
    XMFLOAT4 zz; XMStoreFloat4(&zz, ze);
    
    auto zmin = XMVectorGetZ(min);
    auto zmax = XMVectorGetZ(max);
    auto p1_x = XMVectorGetX(p1);

    auto ad31 = p3 - p1;
    auto ad21 = p2 - p1;
    auto ad32 = p3 - p2;

    auto zline = (float*)(tt + 4) + y1 * dx;
    auto ax31 = XMVectorGetX(ad31) / XMVectorGetY(ad31);
    auto dy21 = y2 - y1;
    if (dy21 != 0)
    {
      float ax21 = XMVectorGetX(ad21) / XMVectorGetY(ad21), xx1 = p1_x, xx2 = xx1;
      float dx1 = ax21, dx2 = ax31; if (dx1 > dx2) { auto t = dx1; dx1 = dx2; dx2 = t; }
      auto y = y1; if (y < 0) { xx1 -= dx1 * y; xx2 -= dx2 * y; zline -= dx * y; y = 0; }
      for (; y < y2; y++, xx1 += dx1, xx2 += dx2, zline += dx)
      {
        int x1 = max((int)xx1, 0), x2 = min((int)xx2, dx);
        auto yz = zz.y * y + zz.z;
        for (int x = x1; x < x2; x++)
        {
          //auto z = XMVectorGetZ(XMVectorMax(min, XMVectorMin(max, 
          //  XMVector3Dot(zzz, XMVectorSet(x, y, 1, 0))))); 
          auto z = zz.x * x + yz;// zz.y * y + zz.z; 
          if (z < zmin) z = zmin; else if (z > zmax) z = zmax;
          if (z > 1) continue;
          if (bkg)
          {
            if (z > zline[x]) zline[x] = z;
          }
          else
          {
            if (z < 0) continue;
            if (z < zline[x]) maxzdz = max(maxzdz, zline[x] - z);
          }
        }
      }
    }
    int dy32 = y3 - y2;
    if (dy32 != 0)
    {
      float ax32 = XMVectorGetX(ad32) / XMVectorGetY(ad32);
      float xx1 = XMVectorGetX(p2);
      float xx2 = p1_x + ax31 * XMVectorGetY(ad21);
      float dx1 = ax32, dx2 = ax31; if (xx1 > xx2) { auto t = dx1; dx1 = dx2; dx2 = t; t = xx1; xx1 = xx2; xx2 = t; }
      auto y = y2; if (y < 0) { xx1 -= dx1 * y; xx2 -= dx2 * y; zline -= dx * y; y = 0; }
      for (; y < y3; y++, xx1 += dx1, xx2 += dx2, zline += dx)
      {
        int x1 = max((int)xx1, 0), x2 = min((int)xx2, dx);
        auto yz = zz.y * y + zz.z;
        for (int x = x1; x < x2; x++)
        {
          //auto z = XMVectorGetZ(XMVectorMax(min, XMVectorMin(max,
          //  XMVector3Dot(zzz, XMVectorSet(x, y, 1, 0)))));
          auto z = zz.x * x + yz; // zz.y * y + zz.z;
          if (z < zmin) z = zmin; else if (z > zmax) z = zmax;
          if (z > 1) continue;
          if (bkg)
          {
            if (z > zline[x]) zline[x] = z;
          }
          else
          {
            if (z < 0) continue;
            if (z < zline[x]) maxzdz = max(maxzdz, zline[x] - z);
          }
        }
      }
    }
  }
  *diff = maxzdz; return 0;
}
