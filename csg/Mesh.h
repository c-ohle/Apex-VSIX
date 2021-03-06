#pragma once

#define MESH_FL_ENCODE    0x01
#define MESH_FL_SHELL     0x02
//#define MESH_FL_MODIFIED  0x10
#define MESH_FL_WRITEMASK 0x0f

struct CMesh : public ICSGMesh
{
  UINT refcount = 1, flags = 0; //MESH_FL
  sarray<UINT> ii;
  carray<Vector3R> pp;
  carray<Vector4R> ee;
  void clear()
  {
    ii.setsize(0); pp.setsize(0); ee.setsize(0);
  }
  void invert()
  {
    for (UINT i = 0; i < ii.n; i += 3) { auto t = ii.p[i + 1]; ii.p[i + 1] = ii[i + 2]; ii.p[i + 2] = t; }
  }
  void resetee()
  {
    if (flags & MESH_FL_ENCODE) { flags &= ~MESH_FL_ENCODE; ee.setsize(0); }
  }
  HRESULT __stdcall QueryInterface(REFIID riid, void** p)
  {
    if (riid == __uuidof(IUnknown) || riid == __uuidof(ICSGMesh) || riid == __uuidof(IAgileObject))
    {
      InterlockedIncrement(&refcount); *p = static_cast<ICSGMesh*>(this); return 0;
    }
    return E_NOINTERFACE;
  }
  ULONG __stdcall AddRef(void)
  {
    return InterlockedIncrement(&refcount);
  }
  ULONG __stdcall Release(void)
  {
    auto count = InterlockedDecrement(&refcount);
    if (!count) delete this;
    return count;
  }
  HRESULT __stdcall CopyBuffer(UINT ib, UINT ab, CSGVAR p)
  {
    switch (ib)
    {
    case 0:
    {
      auto d = p.count * __sizeof((CSG_TYPE)p.vt);
      for (UINT i = 0, n = min(p.length, pp.n - ab); i < n; i++, *(BYTE**)&p.p += d)
        conv(p, &pp.p[ab + i].x, 3);
      return 0;
    }
    case 1:
    {
      if (p.count != 1) return E_INVALIDARG;
      if (p.vt == CSG_TYPE_USHORT)
      {
        for (UINT i = 0, n = min(p.length, ii.n - ab); i < n; i++)
          ((USHORT*)p.p)[i] = ii[ab + i];
        return 0;
      }
      if (p.vt != CSG_TYPE_INT) return E_INVALIDARG;
      memcpy((int*)p.p, ii.p + ab, min(p.length, ii.n - ab) * sizeof(int));
      return 0;
    }
    case 2:
    {
      auto d = p.count * __sizeof((CSG_TYPE)p.vt);
      for (UINT i = 0, n = min(p.length, ee.n - ab); i < n; i++, *(BYTE**)&p.p += d)
        conv(p, &ee.p[ab + i].x, 4);
      return 0;
    }
    }
    return E_INVALIDARG;
  }
  HRESULT __stdcall get_VertexCount(UINT* p)
  {
    *p = pp.n; return 0;
  }
  HRESULT __stdcall GetVertex(UINT i, CSGVAR p)
  {
    if ((UINT)i >= (UINT)pp.n) return E_INVALIDARG;
    conv(p, &pp.p[i].x, 3); return 0;
  }
  HRESULT __stdcall SetVertex(UINT i, CSGVAR p)
  {
    if ((UINT)i >= (UINT)pp.n) return E_INVALIDARG;
    conv(&pp.p[i].x, 3, p); resetee(); return 0;
  }
  HRESULT __stdcall get_IndexCount(UINT* p)
  {
    *p = ii.n; return 0;
  }
  HRESULT __stdcall GetIndex(UINT i, UINT* p)
  {
    if ((UINT)i >= (UINT)ii.n) return E_INVALIDARG;
    *p = ii[i];  return 0;
  }
  HRESULT __stdcall SetIndex(UINT i, UINT p)
  {
    if (i >= (UINT)ii.n) return E_INVALIDARG;
    ii.p[i] = p; resetee(); return 0;
  }
  HRESULT __stdcall get_PlaneCount(UINT* p)
  {
    *p = flags & MESH_FL_ENCODE ? ee.n : -1; return 0;
  }
  HRESULT __stdcall GetPlane(UINT i, CSGVAR p)
  {
    if ((UINT)i >= (UINT)ee.n) return E_INVALIDARG;
    conv(p, &ee.p[i].x, 4); return 0;
  }
  HRESULT __stdcall Update(CSGVAR vertices, CSGVAR indices)
  {
    if (*(USHORT*)&indices.vt != CSG_TYPE_INT && (indices.count != 1 || !(indices.vt == CSG_TYPE_INT || indices.vt == CSG_TYPE_USHORT)))
      return E_INVALIDARG;
    resetee();
    if (*(USHORT*)&vertices.vt == CSG_TYPE_INT) pp.setsize(*(UINT*)&vertices.p);
    else
    {
      pp.setsize(vertices.length); if (vertices.vt == CSG_TYPE_RATIONAL) vertices.length = 0;
      auto d = vertices.count * __sizeof((CSG_TYPE)vertices.vt);
      for (UINT i = 0; i < pp.n; i++, vertices.p += d) conv(&pp.p[i].x, 3, vertices);
      Rational::compact(&pp.p->x, pp.n * 3); //todo: check optional?
    }
    if (*(USHORT*)&indices.vt == CSG_TYPE_INT) ii.setsize(*(UINT*)&indices.p);
    else
    {
      if (indices.vt == CSG_TYPE_USHORT)
      {
        ii.setsize(indices.length);
        for (UINT i = 0; i < ii.n; i++) ii.p[i] = ((USHORT*)indices.p)[i];
      }
      else ii.copy((UINT*)indices.p, indices.length);
    }
    return 0;
  }
  HRESULT __stdcall CopyTo(ICSGMesh* p)
  {
    auto& m = *static_cast<CMesh*>(p);
    m.flags = flags;
    ii.copyto(m.ii);
    pp.copyto(m.pp);
    ee.copyto(m.ee);
    return 0;
  }
  HRESULT __stdcall Transform(CSGVAR m);
  HRESULT __stdcall WriteToStream(IStream* str)
  {
    CHR(writecount(str, flags & MESH_FL_WRITEMASK));
    CHR(writecount(str, pp.n));
    CHR(Rational::write(str, &pp.p->x, pp.n * 3));
    CHR(writecount(str, ii.n / 3));
    for (UINT i = 0; i < ii.n; i++) CHR(writecount(str, ii.p[i])); return 0;
  }
  HRESULT __stdcall ReadFromStream(IStream* str)
  {
    CHR(readcount(str, flags)); if (ee.n) ee.setsize(0);
    UINT np; CHR(readcount(str, np)); pp.setsize(np);
    CHR(Rational::read(str, &pp.p->x, pp.n * 3));
    UINT ni; CHR(readcount(str, ni)); ii.setsize(ni * 3);
    for (UINT i = 0; i < ii.n; i++) CHR(readcount(str, ii.p[i])); return 0;
  }
  HRESULT __stdcall Check(CSG_MESH_CHECK check, CSG_MESH_CHECK* p);
  HRESULT __stdcall FreeExtra()
  {
    pp.freeextra();
    ii.freeextra();
    ee.freeextra();
    return 0;
  }
  //HRESULT __stdcall GetModified(BOOL* p)
  //{
  //  *p = (flags & MESH_FL_MODIFIED) != 0; flags &= ~MESH_FL_MODIFIED;
  //  return 0;
  //}
  //HRESULT __stdcall get_Indices(SAFEARRAY** p)
  //{
  //  SAFEARRAYBOUND rgsabound; rgsabound.cElements = ii.n; rgsabound.lLbound = 0;
  //  auto psa = SafeArrayCreate(VT_UI4, 1, &rgsabound);
  //  for (LONG i = 0; i < (LONG)ii.n; i++) SafeArrayPutElement(psa, &i, &ii.p[i]);
  //  *p = psa; return 0;
  //}
  //HRESULT __stdcall get_Vertices(SAFEARRAY** p)
  //{
  //  SAFEARRAYBOUND rgsabound; rgsabound.cElements = pp.n * 3; rgsabound.lLbound = 0;
  //  auto psa = SafeArrayCreate(VT_UI8, 1, &rgsabound);
  //  for (LONG i = 0; i < (LONG)rgsabound.cElements; i++) SafeArrayPutElement(psa, &i, &((UINT64*)pp.p)[i]);
  //  *p = psa; return 0;
  //}
};

