#pragma once

#include "mesh.h"
#include "node.h"

struct __declspec(novtable) CScene : ICDXScene
{
  CDX_UNIT unit = CDX_UNIT_UNDEF;
  CComPtr<CNode> lastchild, camera;
  sarray<CNode*> selection;
  CComPtr<IUnknown> tag;
  ~CScene()
  {
    //TRACE(L"~CScene\n");
    if (lastchild.p) { lastchild.p->nextnode.Release(); lastchild.Release(); }
  }
  __forceinline CNode* child() { return lastchild.p ? lastchild.p->nextnode.p : 0; }
  CNode* findscount(UINT i)
  {
    for (auto p = child(); p; p = p->nextsibling(0)) if (!--i) return p;
    return 0;
  }
  __forceinline void* GetInterface(REFIID riid)
  {
    if (riid == __uuidof(IUnknown)) return static_cast<ICDXScene*>(this);
    if (riid == __uuidof(ICDXScene)) return static_cast<ICDXScene*>(this);
    if (riid == __uuidof(ICDXRoot)) return static_cast<ICDXScene*>(this);
    if (riid == __uuidof(IAgileObject)) return static_cast<ICDXScene*>(this);
    return 0;
  }
  HRESULT __stdcall get_Unit(CDX_UNIT* p) { *p = unit; return 0; }
  HRESULT __stdcall put_Unit(CDX_UNIT p) { unit = p; return 0; }
  HRESULT __stdcall get_Child(ICDXNode** p)
  {
    if (*p = child()) (*p)->AddRef();
    return 0;
  }
  HRESULT __stdcall get_Count(UINT* pc);
  HRESULT __stdcall AddNode(BSTR name, ICDXNode** p);
  HRESULT __stdcall RemoveAt(UINT i);
  HRESULT __stdcall InsertAt(UINT i, ICDXNode* p);
  HRESULT __stdcall Clear();
  HRESULT __stdcall SaveToStream(IStream* str, ICDXNode* cam);
  HRESULT __stdcall LoadFromStream(IStream* str);
  HRESULT __stdcall get_Camera(ICDXNode** p);
  HRESULT __stdcall put_Camera(ICDXNode* p);
  HRESULT __stdcall get_Tag(IUnknown** p)
  {
    return tag.CopyTo(p);
  }
  HRESULT __stdcall put_Tag(IUnknown* p)
  {
    tag = p; return 0;
  }
  HRESULT __stdcall get_SelectionCount(UINT* p)
  {
    *p = selection.n; return 0;
  }
  HRESULT __stdcall GetSelection(UINT i, ICDXNode** p)
  {
    if (i >= (UINT)selection.n) return E_INVALIDARG;
    (*p = selection.p[i])->AddRef(); return 0;
  }
};

struct Archive
{
  IStream* str; bool storing; HRESULT hr = 0; UINT version;
  Archive(IStream* str, bool storing)
  {
    this->str = str;
    this->storing = storing;
  }
  void WriteCount(UINT c)
  {
    BYTE bb[8]; int e = 0;
    for (; c >= 0x80; bb[e++] = c | 0x80, c >>= 7); bb[e++] = c;
    str->Write(bb, e, 0);
  }
  UINT ReadCount()
  {
    UINT c = 0;
    for (UINT shift = 0; ; shift += 7)
    {
      UINT b = 0; str->Read(&b, 1, 0);
      c |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break;
    }
    return c;
  }
  template<class T> void Write(const T* p, UINT n = 1)
  {
    auto hr = str->Write(p, n * sizeof(T), 0);
  }
  template<class T> void Read(T* p, UINT n = 1)
  {
    str->Read(p, n * sizeof(T), 0);
  }
  void SerialCount(UINT& c)
  {
    if (storing) WriteCount(c); else c = ReadCount();
  }
  template<class T> void Serialize(T* p, UINT n = 1)
  {
    if (storing) Write(p, n); else Read(p, n);
  }
  template<class T> void Serialize(sarray<T>& p)
  {
    if (storing) { WriteCount(p.n); Write(p.p, p.n); }
    else { p.setsize2(ReadCount()); Read(p.p, p.n); }
  }
  void Serialize(CComBSTR& s)
  {
    if (storing)
    {
      UINT n = SysStringLen(s.m_str); //SysStringByteLen(s.m_str);
      WriteCount(s.m_str ? n + 1 : 0);
      for (UINT i = 0; i < n; i++) WriteCount(s.m_str[i]);
    }
    else
    {
      UINT n = ReadCount(); if (n-- == 0) return; XMASSERT(!s.m_str);
      s.m_str = SysAllocStringLen(0, n);
      for (UINT i = 0; i < n; i++) s.m_str[i] = (OLECHAR)ReadCount();
    }
  }
  sarray<void*> map; UINT mapcount = 0;
  UINT getmap(void* p) { for (UINT i = 0; i < mapcount; i++) if (map.p[i] == p) return i + 1; return 0; }
  void addmap(void* p) { map.getptr(mapcount + 1)[mapcount++] = p; }
};
