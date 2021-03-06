#include "pch.h"
#include "TesselatorDbl.h"
#include "TesselatorRat.h"
#include "Mesh.h"

concurrency::combinable<ATL::CComPtr<CTesselatorRat>> __tess;
concurrency::critical_section __crit;

HRESULT CTesselatorRat::Cut(ICSGMesh* mesh, CSGVAR vplane)
{
  auto& m = *static_cast<CMesh*>(mesh);
  if (!m.ee.n)
  {
    initplanes(m); 
    if (vplane.vt == CSG_TYPE_INT) { memcpy((int*)vplane.p, csg.ii.p, min(csg.ii.n, vplane.length) << 2); return 0; } //get plane sort
  }
  if (vplane.vt == 0) return 0;
  Vector4R plane; conv(&plane.x, 4, vplane);
  UINT np = m.pp.n, xe = np, mf = 0; auto ff = csg.ff.getptr(np + m.ee.n + 1);
  for (UINT i = 0; i < np; i++) mf |= ff[i] = 1 << (1 + (0 ^ plane.DotCoord(m.pp[i])));
  if (mf == 1) return 0;
  if (mf == 4) { m.clear(); return 0; }
  auto vv = csg.pp.getptr(csg.np = np); for (UINT i = 0; i < np; i++) vv[i] = m.pp[i];
  auto nk = 0; auto kk = csg.ii.getptr(m.ii.n); UINT ss[4];
  auto nt = 0; auto tt = csg.tt.getptr(128); csg.clearab();
  mode = (CSG_TESS)(CSG_TESS_POSITIVE | CSG_TESS_INDEXONLY | CSG_TESS_NOTRIM | CSG_TESS_FILL);
  for (int e = 0, h = 0, b, fl, x; h < (int)m.ii.n; h = b, e++)
  {
    for (b = h, fl = 0; ;)
    {
      fl |= ff[m.ii[b]] | ff[m.ii[b + 1]] | ff[m.ii[b + 2]];
      b += 3; if (b == m.ii.n || decode(m.ii.p + b)) break;
    }
    if ((fl & 4) == 0)
    {
      if (fl == 2) continue; //if (!((const Vector3R*)&plane)->Equals(*((const Vector3R*)&m.ee[e]))) //backface
      kk = csg.ii.getptr(nk + (b - h)); ff[xe++] = e;
      if ((fl & 2) != 0) tt = csg.tt.getptr(nt + (b - h)); // / 3 * 2
      for (int i = h, j; i < b; i++)
      {
        kk[nk++] = m.ii[i];
        if (ff[m.ii[i]] != 2) continue;
        if (ff[m.ii[j = i % 3 == 2 ? i - 2 : i + 1]] != 2) continue;
        tt[nt++] = m.ii[j]; tt[nt++] = m.ii[i];
      }
      continue;
    }
    if ((fl & 1) == 0) continue;
    setnormal(*(const Vector3R*)&m.ee[e].x);
    beginsex(); vv = csg.pp.getptr(csg.np + (x = (b - h) / 3 + 1)); tt = csg.tt.getptr(nt + x);
    for (int i = h; i < b; i += 3)
    {
      UINT ns = 0;
      for (int k = 0; k < 3; k++)
      {
        int z, ik, iz, f1 = ff[ik = m.ii[i + k]], f2 = ff[iz = m.ii[i + (z = (k + 1) % 3)]];
        if ((f1 & 4) == 0) ss[ns++] = ik; if (f1 == f2 || ((f1 | f2) & 2) != 0) continue;
        auto t = csg.getab(iz, ik); if (t != -1) { ss[ns++] = t; continue; }
        csg.setab(ik, iz, ss[ns++] = csg.np);
        vv[csg.np++] = 0 | plane.Intersect(m.pp[ik], m.pp[iz]);
      }
      for (UINT k = 0, l = ns - 1; k < ns; l = k++)
      {
        addsex(ss[l], ss[k]);
        if (ss[l] < np && ff[ss[l]] != 2) continue;
        if (ss[k] < np && ff[ss[k]] != 2) continue;
        tt[nt++] = ss[k]; tt[nt++] = ss[l];
      }
    }
    endsex();
    filloutlines();
    kk = csg.ii.getptr(nk + this->ns); auto j = nk;
    for (int i = 0; i < this->ns; i++) kk[nk++] = this->ll[this->ss[i]] & 0x0fffffff;
    for (int i = j; i < nk; i += 3) encode((UINT*)kk + i, i == j); ff[xe++] = e;
  }
  if (nt != 0)
  {
    beginsex();
    for (int i = 0; i < nt; i += 2) addsex(tt[i], tt[i + 1]);
    endsex();
    if (this->nl != 0)
    {
      setnormal(*(const Vector3R*)&plane.x);
      filloutlines();
      kk = csg.ii.getptr(nk + this->ns); auto j = nk;
      for (int i = 0; i < this->ns; i++) kk[nk++] = this->ll[this->ss[i]] & 0x0fffffff;
      for (int i = j; i < nk; i += 3) encode((UINT*)kk + i, i == j); ff[xe++] = -1;
    }
  }
  xe -= np; if (xe > m.ee.n) m.ee.setsize(xe);
  for (UINT i = 0, k; i < xe; i++) if ((k = ff[np + i]) == -1) m.ee[i] = plane; else if (i != k) m.ee[i] = m.ee[k];
  m.ee.setsize(xe);
  csg.trim(nk);
  m.pp.copy(csg.pp.p, csg.np);
  m.ii.copy((const UINT*)csg.ii.p, nk);
  return 0;
}
HRESULT CTesselatorRat::Join(ICSGMesh* pa, ICSGMesh* pb, CSG_JOIN op)
{
  if (!pb) { memset(pb = (CMesh*)_alloca(sizeof(CMesh)), 0, sizeof(CMesh)); op = (CSG_JOIN)(op | 0x10); }
  auto& a = *static_cast<CMesh*>(pa);
  auto& b = *static_cast<CMesh*>(pb);
  if (!a.ee.n) initplanes(a);
  if (!b.ee.n) initplanes(b);
  UINT ni = 0, ne = 0, an = a.pp.n, bn = b.pp.n, cn, dz, mp = op & 3;
  auto ii = csg.ii.getptr(a.ii.n + b.ii.n);
  auto tt = csg.tt.getptr((cn = an + bn) + b.ee.n);
  auto ff = csg.ff.getptr((dz = a.ee.n + b.ee.n) << 1); memset(ff, 0, dz * sizeof(int)); auto fm = ff + dz;
  csg.dictee(a.ee.n + b.ee.n);
  for (UINT i = 0; i < a.ee.n; i++) csg.addee(a.ee[i]);
  for (UINT i = 0; i < b.ee.n; i++) if ((tt[cn + i] = csg.addee(mp == 1 ? -b.ee[i] : b.ee[i])) < (int)a.ee.n) ff[tt[cn + i]] = 8;
  csg.dictpp(cn);
  for (UINT i = 0; i < an; i++) tt[/* */i] = csg.addpp(a.pp[i]);
  for (UINT i = 0; i < bn; i++) tt[an + i] = csg.addpp(b.pp[i]);
  csg.begindot(); auto dir = a.ee.n > b.ee.n;
  for (UINT i = 0, pc = bn != 0 ? csg.ne : 0; i < pc; i++)
  {
    UINT e = dir ? pc - 1 - i : i; if (ff[e] != 0) continue;
    UINT af = 0, bf = 0, cf = dir ? 6 : 3; rep:
    if ((cf & 1) != 0)
    {
      for (UINT t = 0; t < an; t++) af |= csg.dot(e, t);
      if (af == 1 || af == 4)
      {
        if (mp == 1 ? (af == 1 && bf == 6) : (af == 4 && bf == 3)) goto ex;
        ff[e] = mp == 0 ? 3 : 1; continue;
      }
    }
    if ((cf & 2) != 0)
    {
      for (UINT t = 0; t < bn; t++) bf |= csg.dot(e, tt[an + t]);
      if (bf == 1 || bf == 4)
      {
        if (bf == 4 && af == 3) goto ex;
        ff[e] = mp == 2 ? 1 : 2; continue;
      }
    }
    if ((cf >>= 2) != 0) goto rep;
    if (bf == (mp == 1 ? 6 : 3)) //convex b
    {
      if ((af & (mp == 1 ? 1 : 4)) == 0) continue;
      for (UINT j = 0, k, o = 0, t, f; j < a.ii.n; j = k, o++)
      {
        for (k = j + 3; k < a.ii.n && !decode(a.ii.p + k); k += 3);
        if (ff[o] != 0) continue;
        for (t = j, f = 0; t < k; t++) f |= csg.dot(e, a.ii[t]);
        if (f == (mp == 1 ? 1 : 4)) ff[o] = mp == 2 ? 1 : 2;
      }
      continue;
    }
    if (af == 3) //convex a
    {
      if ((bf & 4) == 0) continue;
      for (UINT j = 0, k, o = 0, t, f, z; j < b.ii.n; j = k, o++)
      {
        for (k = j + 3; k < b.ii.n && !decode(b.ii.p + k); k += 3);
        if (ff[z = tt[cn + o]] != 0) continue;
        for (t = j, f = 0; t < k; t++) f |= csg.dot(e, tt[an + b.ii[t]]);
        if (f == 4) ff[z] = mp == 0 ? 3 : 1;
      }
    }
    continue; ex:;
    if (mp == 0) //a + b
    {
      auto an = a.pp.n; a.pp.setsize(a.pp.n + b.pp.n); for (UINT i = 0; i < b.pp.n; i++) a.pp.p[an + i] = b.pp.p[i];
      auto in = a.ii.n; a.ii.setsize(a.ii.n + b.ii.n); for (UINT i = 0; i < b.ii.n; i++) a.ii.p[in + i] = an + b.ii.p[i];
      if (csg.ne < a.ee.n + b.ee.n) a.resetee();
      else { auto en = a.ee.n; a.ee.setsize(a.ee.n + b.ee.n); for (UINT i = 0; i < b.ee.n; i++) a.ee[en + i] = b.ee[i]; }
      return 0;
    }
    if (mp != 1) { a.clear(); } return 0;
  }
  mode = (CSG_TESS)((mp == 0 ? CSG_TESS_POSITIVE : mp == 1 ? CSG_TESS_ABSGEQTWO : CSG_TESS_GEQTHREE) | CSG_TESS_FILL | CSG_TESS_NOTRIM);
#if(1)
  csg._pp_.copy(csg.pp.p, csg.np);
  concurrency::parallel_for(size_t(0), size_t(csg.ne), [&](size_t _i)
    {
      auto e = (UINT)_i; auto hh = 0;
      switch (ff[e])
      {
      case 1: return;
      case 2: __crit.lock(); goto ta;
      case 3: __crit.lock(); goto tb;
      }
      auto& rt = __tess.local(); if (!rt.p) rt.p = new CTesselatorRat(); auto& tess = *rt.p;
      tess.mode = mode; 
      tess.csg.dictpp(32);
      tess.beginsex();
      const auto& plane = csg.ee[e];
      for (int r = 0; r < 2; r++)
      {
        auto& m = r == 0 ? a : b; auto d = r == 0 ? 0 : an;
        auto d1 = mp == 1 ? r == 1 : false;
        auto d2 = mp == 0 ? 0 : d1 ? 1 : 0;
        auto d3 = mp == 0 ? 4 : 1; tess.csg.clearab();
        for (int i = 0, o = -1, j; i < (int)m.ii.n; i += 3)
        {
          if (decode(m.ii.p + i)) o++;
          auto f = csg._dot_(e, tt[d + m.ii[i + 0]]) | csg._dot_(e, tt[d + m.ii[i + 1]]) | csg._dot_(e, tt[d + m.ii[i + 2]]);
          if (f == 1 || f == 4) continue;
          if (f == 2)
          {
            for (j = i; i + 3 < (int)m.ii.n && !decode(m.ii.p + i + 3); i += 3);
            if (e != (r == 0 ? o : tt[cn + o])) continue;
            for (int i0, i1, i2; j <= i; j += 3)
            {
              tess.addsex(i0 = tt[d + m.ii[j + 0]], i1 = tt[d + m.ii[j + (d1 ? 2 : 1)]]);
              tess.addsex(i1, i2 = tt[d + m.ii[j + (d1 ? 1 : 2)]]); tess.addsex(i2, i0);
            }
            continue;
          }
          if (f == (7 & ~d3)) continue;
          if ((op & 0x20) != 0) continue; //pure plane retess
          int ss[2], ns = 0, s = 0; for (; csg._dot_(e, tt[d + m.ii[i + s]]) != d3; s++);
          for (int k = 0, u, v; k < 3; k++)
          {
            auto f1 = csg._dot_(e, tt[d + m.ii[u = i + (k + s) % 3]]);
            if (f1 == 2) { ss[ns++] = tt[d + m.ii[u]]; continue; }
            auto f2 = csg._dot_(e, tt[d + m.ii[v = i + (k + s + 1) % 3]]); if (f1 == f2 || ((f1 | f2) & 2) != 0) continue;
            int t = tess.csg.getab(m.ii[u], m.ii[v]); if (t != -1) { ss[ns++] = t; continue; }
            auto sp = 0 | plane.Intersect(m.pp[m.ii[u]], m.pp[m.ii[v]]);
            ss[ns++] = cn + tess.csg.addpp(sp);
            tess.csg.setab(m.ii[v], m.ii[u], ss[ns - 1]);
          }
          if (ns == 2) tess.addsex(ss[d2 ^ 1], ss[d2]);
        }
      }
      tess.endsex(); auto nnl = tess.nl;
      tess.setnormal(*(const Vector3R*)&plane);
      tess.BeginPolygon();
      for (int i = 0, f = 0, n = tess.nl; i < n; i++)
      {
        if (f == 0) { tess.BeginContour(); f = 1; }
        UINT k = tess.ll[i], j = k & 0x0fffffff; tess.addvertex(j < cn ? csg._pp_[j] : tess.csg.pp[j - cn]);
        if ((k & 0x40000000) != 0) { tess.EndContour(); f = 0; }
      }
      tess.EndPolygon();
      auto ic = tess.ns; if (ic == 0) return;
      __crit.lock(); ii = csg.ii.getptr(ni + ic); auto at = ni;
      //for (int i = 0; i < ic; i++) ii[ni++] = csg.addpp(*(const Vector3R*)&tess.pp[tess.ss[i]].x);
      for (int i = 0; i < tess.np; i++) tess.pp.p[i].ic = -1;
      for (int i = 0; i < ic; i++) { auto& r = tess.pp[tess.ss[i]]; ii[ni++] = r.ic != -1 ? r.ic : (r.ic = csg.addpp(*(const Vector3R*)&r.x)); }
      goto encode; tb: hh = 1; ta: auto& c = hh == 0 ? a : b;
      int i1 = 0, i0 = 0, i2; for (; i1 < (int)c.ii.n && !(decode(c.ii.p + i1) && (e == (hh == 0 ? i0++ : tt[cn + i0++]))); i1 += 3);
      for (i2 = i1; ;) { i2 += 3; if (i2 == c.ii.n || decode(c.ii.p + i2)) break; }
      ii = csg.ii.getptr(ni + (i2 - i1)); auto of = hh == 0 ? 0 : an; at = ni;
      for (int i = i1; i < i2; i++) ii[ni++] = tt[of + c.ii[i]]; encode: _ASSERT(at != ni);
      for (int i = at; i < (int)ni; i += 3) encode((UINT*)ii + i, i == at); fm[ne++] = e;
      __crit.unlock();
    });//, concurrency::static_partitioner());
#else
  for (int e = 0, i0, i1, i2; e < (int)csg.ne; e++)
  {
    auto hh = 0;
    switch (ff[e])
    {
    case 1: continue;
    case 2: goto ta;
    case 3: goto tb;
    }
    auto& plane = csg.ee[e];
    beginsex();
    for (int r = 0; r < 2; r++)
    {
      auto& m = r == 0 ? a : b; auto d = r == 0 ? 0 : an;
      auto d1 = mp == 1 ? r == 1 : false;
      auto d2 = mp == 0 ? 0 : d1 ? 1 : 0;
      auto d3 = mp == 0 ? 4 : 1; csg.clearab();
      for (int i = 0, o = -1, j; i < (int)m.ii.n; i += 3)
      {
        if (decode(m.ii.p + i)) o++;
        auto f = csg.dot(e, tt[d + m.ii[i + 0]]) | csg.dot(e, tt[d + m.ii[i + 1]]) | csg.dot(e, tt[d + m.ii[i + 2]]);
        if (f == 1 || f == 4) continue;
        if (f == 2)
        {
          for (j = i; i + 3 < (int)m.ii.n && !decode(m.ii.p + i + 3); i += 3);
          if (e != (r == 0 ? o : tt[cn + o])) continue;
          for (; j <= i; j += 3)
          {
            addsex(i0 = tt[d + m.ii[j + 0]], i1 = tt[d + m.ii[j + (d1 ? 2 : 1)]]);
            addsex(i1, i2 = tt[d + m.ii[j + (d1 ? 1 : 2)]]); addsex(i2, i0);
          }
          continue;
        }
        if (f == (7 & ~d3)) continue;
        if ((op & 0x20) != 0) continue; //pure plane retess
        int ss[2], ns = 0, s = 0; for (; csg.dot(e, tt[d + m.ii[i + s]]) != d3; s++);
        for (int k = 0, u, v; k < 3; k++)
        {
          auto f1 = csg.dot(e, tt[d + m.ii[u = i + (k + s) % 3]]);
          if (f1 == 2) { ss[ns++] = tt[d + m.ii[u]]; continue; }
          auto f2 = csg.dot(e, tt[d + m.ii[v = i + (k + s + 1) % 3]]); if (f1 == f2 || ((f1 | f2) & 2) != 0) continue;
          int t = csg.getab(m.ii[u], m.ii[v]); if (t != -1) { ss[ns++] = t; continue; }
          ss[ns++] = csg.addpp(0 | plane.Intersect(m.pp[m.ii[u]], m.pp[m.ii[v]]));
          csg.setab(m.ii[v], m.ii[u], ss[ns - 1]);
        }
        if (ns == 2) addsex(ss[d2 ^ 1], ss[d2]);
      }
    }
    endsex(); auto nc = this->nl;
    setnormal(*(const Vector3R*)&plane);
    filloutlines();
    auto ic = this->ns; if (ic == 0) { ff[e] = 1; continue; }
    ii = csg.ii.getptr(ni + ic); auto at = ni;
    for (int i = 0, t; i < ic; i++) ii[ni++] = (t = this->ss[i]) < nc ? this->ll[t] & 0x0fffffff : csg.addpp(*(const Vector3R*)&this->pp[t].x);
    goto encode; tb: hh = 1; ta: auto& c = hh == 0 ? a : b;
    for (i1 = 0, i0 = 0; i1 < (int)c.ii.n && !(decode(c.ii.p + i1) && (e == (hh == 0 ? i0++ : tt[cn + i0++]))); i1 += 3);
    for (i2 = i1; ;) { i2 += 3; if (i2 == c.ii.n || decode(c.ii.p + i2)) break; }
    ii = csg.ii.getptr(ni + (i2 - i1)); auto of = hh == 0 ? 0 : an; at = ni;
    for (int i = i1; i < i2; i++) ii[ni++] = tt[of + c.ii[i]]; encode: _ASSERT(at != ni);
    for (int i = at; i < (int)ni; i += 3) encode((UINT*)ii + i, i == at); fm[ne++] = e;
  }
#endif
  if ((op & 0x40) == 0 && (ni = join(ni, 0)) == -1)
  {
    if (op == 0x10) { Join(pa, pb, (CSG_JOIN)(0x20 | 0x40)); return Join(pa, pb, (CSG_JOIN)0x80); }
    return 0x8C066001; //degenerated input mesh
  }
  a.ee.setsize(ne); for (UINT i = 0; i < ne; i++) a.ee[i] = csg.ee[fm[i]];
  csg.trim(ni);
  a.pp.copy(csg.pp.p, csg.np);
  a.ii.copy((const UINT*)csg.ii.p, ni);
  return 0;
}

void CTesselatorRat::initplanes(CMesh& m)
{
  if (m.ii.n == 0) { m.flags |= MESH_FL_ENCODE; return; }
  if (m.flags & MESH_FL_ENCODE)
  {
    UINT c = 1; for (UINT i = 3; i < m.ii.n; i += 3) if (decode(m.ii.p + i)) c++;
    m.ee.setsize(c);
    for (UINT i = 0, k = 0; i < m.ii.n; i += 3)
      if (decode(m.ii.p + i))
        m.ee[k++] = 0 | Vector4R::PlaneFromPoints(m.pp[m.ii[i + 0]], m.pp[m.ii[i + 1]], m.pp[m.ii[i + 2]]);
    return;
  }
  auto nd = m.ii.n / 3;
  auto ff = csg.ff.getptr(nd + m.pp.n);
  auto ii = csg.ii.getptr(nd + m.ii.n);
  csg.dictee(64); memset(ff + nd, -1, m.pp.n * sizeof(int)); //for (int i = 0; i < m.pp.n; i++) ff[nd + i] = -1;
  for (UINT i = 0, k = 0, l = 0, x = 0, i1, i2, i3; k < nd; i += 3, k++)
  {
    const auto& a = m.pp[i1 = m.ii[i + 0]];
    const auto& b = m.pp[i2 = m.ii[i + 1]];
    const auto& c = m.pp[i3 = m.ii[i + 2]]; if ((ii[k] = i) == 0) goto m1;
    const auto& e = csg.ee[l];
    if (ff[nd + i1] != l) if ((0 ^ e.DotCoord(a)) != 0) goto m1;
    if (ff[nd + i2] != l) if ((0 ^ e.DotCoord(b)) != 0) goto m1;
    if (ff[nd + i3] != l) if ((0 ^ e.DotCoord(c)) != 0) goto m1;
    switch (x != -1 ? x : (x = ((const Vector3R*)&e)->LongAxis()))
    {
    case 0: if ((0 ^ (b.y - a.y) * (c.z - a.z) - (b.z - a.z) * (c.y - a.y)) != e.x.sign()) goto m1; break;
    case 1: if ((0 ^ (b.z - a.z) * (c.x - a.x) - (b.x - a.x) * (c.z - a.z)) != e.y.sign()) goto m1; break;
    case 2: if ((0 ^ (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) != e.z.sign()) goto m1; break;
    }
    ff[k] = l; goto m2; m1: //TRACE(L"plane %i at %i\n", csg.ne, i);
    ff[k] = l = csg.addee(0 | Vector4R::PlaneFromPoints(a, b, c)); x = -1; m2:
    ff[nd + i1] = ff[nd + i2] = ff[nd + i3] = l;
  }
  qsort(ff, ii, 0, nd - 1);
  for (UINT i = 0, j = 0; i < nd; i++, j += 3) for (UINT k = 0; k < 3; k++) ii[nd + j + k] = m.ii[ii[i] + k];
  m.ii.copy((const UINT*)ii + nd, m.ii.n);
  for (UINT i = 0, k = 0; i < m.ii.n; i += 3, k++) encode(m.ii.p + i, k == 0 || ff[k - 1] != ff[k]);
  m.ee.copy(csg.ee.p, csg.ne); m.flags |= MESH_FL_ENCODE;
}
void CTesselatorRat::setnormal(const Vector3R& v)
{
  auto i = v.LongAxis(); auto s = (&v.x)[i].sign();
  mode = (CSG_TESS)((mode & 0xffff) | ((int)CSG_TESS_NORMX << i) | (s & (int)CSG_TESS_NORMNEG));
}
void CTesselatorRat::addvertex(const Vector3R& v)
{
  if (np == pp.n) resize(64);
  if (np != fi) pp[np - 1].next = np;
  auto a = &pp[np++]; a->next = fi; *(Vector3R*)&a->x = v;
}
void CTesselatorRat::beginsex()
{
  if (dict.n == 0)
  {
    dict.setsize(hash + 32);
    ii.setsize(64);
  }
  np = ns = nl = ni = 0; memset(dict.p, 0, hash * sizeof(int));
}
void CTesselatorRat::addsex(int a, int b)
{
  for (int i = dict[b % hash] - 1; i != -1; i = dict[hash + i] - 1)
    if (ii[i].a == b && ii[i].b == a) { ii[i].a = -1; return; }
  if (ni >= (int)ii.n)
    ii.setsize(ni << 1);
  if (hash + ni >= (int)dict.n)
    dict.setsize(hash + ii.n);
  auto h = a % hash; dict[hash + ni] = dict[h]; dict[h] = ni + 1;
  ii[ni++] = ab(a, b); np = max(np, max(a, b) + 1);
}
void CTesselatorRat::endsex()
{
  int k = 0; for (int i = 0; i < ni; i++) if (ii[i].a != -1) ii[k++] = ii[i]; ni = k;
  _ASSERT((mode & CSG_TESS_OUTLINEPRECISE) == 0); outline();
}
void CTesselatorRat::filloutlines()
{
  BeginPolygon();
  for (int i = 0, f = 0, n = nl; i < n; i++)
  {
    if (f == 0) { BeginContour(); f = 1; }
    auto k = ll[i]; addvertex(csg.pp[k & 0x0fffffff]);
    if ((k & 0x40000000) != 0) { EndContour(); f = 0; }
  }
  EndPolygon();
}
void CTesselatorRat::outline(int* ii, int ni)
{
  beginsex();
  for (int i = 0; i < ni; i += 3)
  {
    addsex(ii[i + 0], ii[i + 1]);
    addsex(ii[i + 1], ii[i + 2]);
    addsex(ii[i + 2], ii[i + 0]);
  }
  endsex();
}
int CTesselatorRat::join(int ni, int fl)
{
  for (int swap = 0; ;)
  {
    auto ii = csg.ii.p; outline(ii, ni); if (nl == 0) break;
    for (int i = 0, n = nl, k, x; i < n; i = k)
    {
      for (k = i + 1; k < n && (ll.p[k - 1] & 0x40000000) == 0; k++);
      for (int l = k - i, t = 0, t1, t2, t3, u; t < l; t++)
      {
        auto s = Vector3R::Inline(
          csg.pp.p[t1 = ll.p[i + t] & 0x0fffffff],
          csg.pp.p[t2 = ll.p[i + (t + 1) % l] & 0x0fffffff],
          csg.pp.p[t3 = ll.p[i + (t + 2) % l] & 0x0fffffff], 2);
        if (s == 2 && fl == 1) { if (k == n) return ni; swap++; break; }
        if (s == 0 || s == 2)
        {
          //if (t == 0 && s == 2 && k - i == 3)
          //{
          //  ii = csg.ii.getptr(ni + 3);
          //  ii[ni + 0] = t1; 
          //  ii[ni + 1] = t3; 
          //  ii[ni + 2] = t2; 
          //  ni += 3; swap++;
          //  break;
          //}
          continue;
        }
        if (s < 0) { s = t1; t1 = t2; t2 = t3; t3 = s; }
        for (x = 0; x < ni && !(ii[x] == t1 && ii[x % 3 == 2 ? x - 2 : x + 1] == t2); x++);
        ii = csg.ii.getptr(ni + 3); u = x / 3 * 3;
        memcpy(ii + (u + 3), ii + u, (ni - u) * sizeof(int)); ni += 3;
        auto c = decode((UINT*)ii + u); ii[x] = ii[u + 3 + (x + 1) % 3] = t3;
        encode((UINT*)ii + u, c); encode((UINT*)ii + (u + 3), false); swap++;
        break;
      }
    }
    if (swap == 0)
      return -1;
    swap = 0;
  }
  return ni;
}

HRESULT CTesselatorRat::ConvexHull(ICSGMesh* mesh)
{
  auto& m = *static_cast<CMesh*>(mesh); m.resetee();
  UINT i1 = 1; for (; i1 < m.pp.n && m.pp[0].Equals(m.pp[i1]); i1++);
  UINT i2 = i1 + 1; for (; i2 < m.pp.n && (m.pp[0].Equals(m.pp[i2]) || m.pp[i1].Equals(m.pp[i2])); i2++);
  if (i2 >= m.pp.n) { m.clear(); return 0; }
  UINT ni = 6; auto ii = csg.ii.getptr(m.pp.n << 2); csg.ee.getptr(m.pp.n);
  ii[0] = 0; ii[1] = i1; ii[2] = i2; csg.ee[0] = 0 | Vector4R::PlaneFromPointsUnorm(m.pp[ii[0]], m.pp[ii[1]], m.pp[ii[2]]);
  ii[3] = 0; ii[4] = i2; ii[5] = i1; csg.ee[1] = -csg.ee[0]; // 0 | Vector4R::PlaneFromPoints(m.pp[ii[3]], m.pp[ii[4]], m.pp[ii[5]]);
  for (UINT i = i2 + 1, k = 0; i < m.pp.n; i++)
  {
    const auto& p = m.pp.p[i]; this->ni = 0;
    for (INT t = ni - 3; t >= 0; t -= 3)
    {
      UINT l = t / 3; auto& e = csg.ee[l]; //e = 0 | Vector4R::PlaneFromPoints(m.pp.p[ii[t + 0]], m.pp.p[ii[t + 1]], m.pp.p[ii[t + 2]]);
      auto f = 0 ^ e.DotCoord(p); if (k == 0 ? f <= 0 : f < 0) continue;
      if (!this->ni) beginsex();
      addsex(ii[t + 0], ii[t + 1]);
      addsex(ii[t + 1], ii[t + 2]);
      addsex(ii[t + 2], ii[t + 0]);
      memcpy(ii + t, ii + (t + 3), (ni - (t + 3)) * sizeof(UINT)); ni -= 3;
      for (UINT j = l, n = ni / 3; j < n; csg.ee[j] = csg.ee[j + 1], j++);
    }
    if (this->ni == 0) continue;
    ii = csg.ii.getptr(ni + (this->ni << 2)); csg.ee.getptr(ni / 3 + this->ni);
    for (int t = 0; t < this->ni; t++)
    {
      if (this->ii[t].a == -1) continue;
      ii[ni + 0] = this->ii[t].a; ii[ni + 1] = this->ii[t].b; ii[ni + 2] = i;
      if (i + 1 < m.pp.n || k == 0)
        csg.ee[ni / 3] = 0 | Vector4R::PlaneFromPointsUnorm(m.pp[ii[ni + 0]], m.pp[ii[ni + 1]], m.pp[ii[ni + 2]]);
      ni += 3;
    }
    if (k == 0) k = i = i2;
  }
  if (ni == 6) { m.clear(); return 0; } //plane
  m.pp.setsize(csg.trim(m.pp.p, m.pp.n, ni));
  m.ii.copy((UINT*)ii, ni);
  return 0;
}

/*
HRESULT CTesselatorRat::Round(ICSGMesh* mesh, CSG_TYPE t)
{
  if (!(t == CSG_TYPE_FLOAT || t == CSG_TYPE_DOUBLE || t == CSG_TYPE_DECIMAL)) return E_INVALIDARG;
  auto& m = *static_cast<CMesh*>(mesh); m.resetee(); m.flags |= MESH_FL_MODIFIED;
  for (UINT i = 0, n = m.pp.n * 3; i < n; i++)
  {
    auto& v = (&m.pp.p->x)[i];
    switch (t)
    {
    case CSG_TYPE_FLOAT: v = (float)(double)v; continue;
    case CSG_TYPE_DOUBLE: v = (double)v; continue;
    case CSG_TYPE_DECIMAL: v = (DECIMAL)v; continue;
    }
  }
  auto tt = csg.tt.getptr(m.pp.n); csg.dictpp(m.pp.n);
  for (UINT i = 0; i < m.pp.n; i++) tt[i] = csg.addpp(m.pp.p[i]);
  if (csg.np == m.pp.n)
    return 0;
  m.pp.copy(csg.pp.p, csg.np);
  for (UINT i = 0; i < m.ii.n; i++) m.ii.p[i] = tt[m.ii.p[i]];
  UINT ni = 0; auto ii = csg.ii.getptr(m.ii.n);
  for (UINT i = 0; i < m.ii.n; i += 3)
  {
    if (m.ii.p[i + 0] == m.ii.p[i + 1] || m.ii.p[i + 1] == m.ii.p[i + 2] || m.ii.p[i + 2] == m.ii.p[i + 0]) continue;
    ii[ni + 0] = m.ii.p[i + 0]; ii[ni + 1] = m.ii.p[i + 1]; ii[ni + 2] = m.ii.p[i + 2]; ni += 3;
  }
  if (ni == m.ii.n)
    return 0;
  m.pp.setsize(csg.trim(m.pp.p, m.pp.n, ni));
  m.ii.copy((UINT*)ii, ni);
  return 0;
}
*/
