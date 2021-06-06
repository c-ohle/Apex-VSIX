#pragma once

struct CVertices
{
  UINT refcount = 1, hash; CComPtr<ID3D11Buffer> p;
  static CVertices* first; CVertices* next;
  CVertices() { Critical crit; next = first; first = this; }
  ~CVertices() { auto p = &first; for (; *p != this; p = &(*p)->next); *p = next; }
  ULONG __stdcall AddRef(void) { return InterlockedIncrement(&refcount); }
  ULONG __stdcall Release(void)
  {
    auto count = InterlockedDecrement(&refcount);
    if (!count)
    {
      Critical crit;
      if (refcount != 0)
        return refcount;
      delete this;
    }
    return count;
  }
};

struct CIndices
{
  UINT refcount = 1, hash, ni; CComPtr<ID3D11Buffer> p;
  static CIndices* first; CIndices* next;
  CIndices() { Critical crit; next = first; first = this; }
  ~CIndices() { auto p = &first; for (; *p != this; p = &(*p)->next); *p = next; }
  ULONG __stdcall AddRef(void) { return InterlockedIncrement(&refcount); }
  ULONG __stdcall Release(void)
  {
    auto count = InterlockedDecrement(&refcount);
    if (!count)
    {
      Critical crit;
      if (refcount != 0)
        return refcount;
      delete this;
    }
    return count;
  }
};

template <class T>
struct CComClass : public T
{
  UINT refcount = 0;
  virtual HRESULT __stdcall QueryInterface(REFIID riid, void** p)
  {
    if (*p = T::GetInterface(riid)) { refcount++; return 0; }
    return E_NOINTERFACE;
  }
  ULONG __stdcall AddRef(void)
  {
    return InterlockedIncrement(&refcount);
  }
  ULONG __stdcall Release(void)
  {
    auto count = InterlockedDecrement(&refcount);
    if (!count)
    {
      //Critical crit;
      //if (refcount != 0)
      //  return refcount;
      delete this;
    }
    return count;
  }
};


struct __declspec(novtable) CBuffer : ICDXBuffer
{
  CDX_BUFFER id; sarray<BYTE> data;
  static CBuffer* getbuffer(CDX_BUFFER id, const BYTE* p, UINT n);
  __forceinline void* GetInterface(REFIID riid)
  {
    if (riid == __uuidof(IUnknown)) return static_cast<ICDXBuffer*>(this);
    if (riid == __uuidof(ICDXBuffer)) return static_cast<ICDXBuffer*>(this);
    if (riid == __uuidof(IAgileObject)) return static_cast<ICDXBuffer*>(this);
    return 0;
  }
  HRESULT __stdcall get_Id(CDX_BUFFER* p)
  {
    *p = id; return 0;
  }
  HRESULT __stdcall get_Name(BSTR* p) { return 0; }
  HRESULT __stdcall put_Name(BSTR p) { return E_NOTIMPL; }
  HRESULT __stdcall get_Tag(IUnknown** p)
  {
    return 0;
  }
  HRESULT __stdcall put_Tag(IUnknown* p)
  {
    return E_NOTIMPL;
  }
  HRESULT __stdcall GetBytes(BYTE* p, UINT* size)
  {
    if (p) memcpy(p, data.p, data.n);
    *size = data.n; return 0;
  }
  HRESULT __stdcall GetPtr(BYTE** p, UINT* size)
  {
    *p = data.p; *size = data.n; return 0;
  }
  HRESULT __stdcall Update(const BYTE* p, UINT n)
  {
    data.setsize(n); if (p) memcpy(data.p, p, n);
    return 0;
  }
};

//struct __declspec(novtable) CTransBuffer : CBuffer
//{
//};

struct __declspec(novtable) CCacheBuffer : CBuffer
{
  static CCacheBuffer* first; CCacheBuffer* next;
  CCacheBuffer() { next = first; first = this; }
  ~CCacheBuffer() { auto p = &first; for (; *p != this; p = &(*p)->next); *p = next; }
  void releasedx();
  HRESULT __stdcall Update(const BYTE* p, UINT n) { return E_NOTIMPL; }
};

struct __declspec(novtable) CTagBuffer : CCacheBuffer
{
  CComPtr<IUnknown> tag;
  HRESULT __stdcall get_Tag(IUnknown** p)
  {
    return tag.CopyTo(p);
  }
  HRESULT __stdcall put_Tag(IUnknown* p)
  {
    tag = p; return 0;
  }
};

struct __declspec(novtable) CTexture : CCacheBuffer
{
  CComBSTR name; UINT fl = 0; //1: error 2: A8
  CComPtr<ID3D11ShaderResourceView> srv;
  void init(struct CView* view);
  HRESULT __stdcall get_Name(BSTR* p) { return name.CopyTo(p); }
  HRESULT __stdcall put_Name(BSTR p) { name = p; return 0; }
  HRESULT __stdcall Update(const BYTE* p, UINT n);
};
/*
template <class T>
struct CComClass2 : public T
{
  UINT refcount = 0;
  virtual HRESULT __stdcall QueryInterface(REFIID riid, void** p)
  {
    if (*p = T::GetInterface(riid)) { refcount++; return 0; }
    return E_NOINTERFACE;
  }
  ULONG __stdcall AddRef(void)
  {
    return InterlockedIncrement(&refcount);
  }
  ULONG __stdcall Release(void)
  {
    auto count = InterlockedDecrement(&refcount);
    if (!count)
      delete this;
    return count;
  }
};
*/

#define NODE_FL_SELECT    0x01
#define NODE_FL_INSEL     0x02
#define NODE_FL_STATIC    0x04
#define NODE_FL_MASHOK    0x08
#define NODE_FL_LAST      0x10
#define NODE_FL_ACTIVE    0x20

struct __declspec(novtable) CNode : ICDXNode
{
  CNode()
  {
    XMStoreFloat4x3(&matrix, XMMatrixIdentity());
  }
  ~CNode()
  {
    //TRACE(L"~CNode %ws\n", name.p);
    for (UINT i = 0; i < buffer.n; i++) buffer.p[i]->Release();
    if (lastchild.p) { lastchild.p->nextnode.Release(); lastchild.Release(); }
  }
  CComBSTR name;
  UINT flags = 0, bmask = 0, subi = 0, subn = 0, color = 0xff808080;
  CComPtr<CNode> lastchild, nextnode; IUnknown* parent = 0;
  XMFLOAT4X3 matrix;
  sarray<CBuffer*> buffer;
  CComPtr<CVertices> vb;
  CComPtr<CIndices> ib;
  CComPtr<IUnknown> tag;

  XMMATRIX XM_CALLCONV getmatrix() const;
  void XM_CALLCONV setmatrix(const XMMATRIX& m);
  XMMATRIX XM_CALLCONV gettrans(IUnknown* root);
  HRESULT __stdcall get_Transform(XMFLOAT4X3* p);
  HRESULT __stdcall put_Transform(XMFLOAT4X3 p);
  HRESULT __stdcall GetTypeTransform(UINT typ, XMFLOAT4X3* p);
  HRESULT __stdcall SetTypeTransform(UINT typ, const XMFLOAT4X3* p);

  CBuffer* getbuffer(CDX_BUFFER id) const;
  void setbuffer(CDX_BUFFER id, CBuffer* p);
  void inval(CDX_BUFFER id);
  void getbox(XMVECTOR box[2], const XMMATRIX* pm, CBuffer* pp = 0);
  void update(struct CScene* scene, UINT i);
  void update(XMFLOAT3* pp, UINT np, USHORT* ii, UINT ni, float smooth = 0, void* tex = 0, UINT fl = 0);
  void save(struct Archive& ar);
  static CNode* load(Archive& ar);
  struct CScene* getscene();
  CNode* getparent();
  CNode* parentchild();
  __forceinline CNode* child() { return lastchild.p ? lastchild.p->nextnode.p : 0; }
  __forceinline CNode* next() { return !(flags & NODE_FL_LAST) ? nextnode.p : 0; }
  CNode* nextsibling(CNode* root);
  //bool ispart(CNode* main)
  //{
  //  for (auto p = this; p; p = p->getparent()) if (p == main) return true;
  //  return false;
  //}
  __forceinline UINT getscount() { return ((USHORT*)&flags)[1]; }
  __forceinline void setscount(UINT i) { ((USHORT*)&flags)[1] = i; }
  __forceinline void* GetInterface(REFIID riid)
  {
    if (riid == __uuidof(IUnknown)) return static_cast<ICDXNode*>(this);
    if (riid == __uuidof(ICDXNode)) return static_cast<ICDXNode*>(this);
    if (riid == __uuidof(ICDXRoot)) return static_cast<ICDXNode*>(this);
    if (riid == __uuidof(IAgileObject)) return static_cast<ICDXNode*>(this);
    return 0;
  }
  HRESULT __stdcall get_Count(UINT* p);
  HRESULT __stdcall get_Name(BSTR* p)
  {
    return name.CopyTo(p);
    //*p = SysAllocStringLen(name.p, name.n); return 0; 
  }
  HRESULT __stdcall put_Name(BSTR p)
  {
    name = p;
    //name.setsize2(p ? SysStringLen(p) : 0); memcpy(name.p, p, name.n << 1);  
    return 0;
  }
  HRESULT __stdcall get_Parent(ICDXRoot** p);
  HRESULT __stdcall get_Scene(ICDXScene** p);
  HRESULT __stdcall get_Index(UINT* p);
  HRESULT __stdcall put_Index(UINT p);
  HRESULT __stdcall get_IsSelect(BOOL* p);
  HRESULT __stdcall put_IsSelect(BOOL p);
  HRESULT __stdcall get_IsStatic(BOOL* p)
  {
    *p = (flags & NODE_FL_STATIC) != 0; return 0;
  }
  HRESULT __stdcall put_IsStatic(BOOL p)
  {
    if (p) flags |= NODE_FL_STATIC; else flags &= ~NODE_FL_STATIC; return 0;
  }
  HRESULT __stdcall get_IsActive(BOOL* p)
  {
    *p = (flags & NODE_FL_ACTIVE) != 0; return 0;
  }
  HRESULT __stdcall put_IsActive(BOOL p)
  {
    if (p) flags |= NODE_FL_ACTIVE; else flags &= ~NODE_FL_ACTIVE; return 0;
  }
  HRESULT __stdcall get_Color(UINT* p)
  {
    *p = color; return 0;
  }
  HRESULT __stdcall put_Color(UINT p)
  {
    color = p; return 0;
  }
  HRESULT __stdcall get_Texture(ICDXBuffer** p);
  HRESULT __stdcall put_Texture(ICDXBuffer* p);
  HRESULT __stdcall get_Range(POINT* p)
  {
    p->x = subi >> 1;
    p->y = subn >> 1;
    return 0;
  }
  HRESULT __stdcall put_Range(POINT p)
  {
    subi = p.x << 1;
    subn = p.y << 1;
    return 0;
  }
  HRESULT __stdcall GetTransform(ICDXNode* root, XMFLOAT4X3* p)
  {
    XMStoreFloat4x3(p, gettrans(root));
    return 0;
  }
  HRESULT __stdcall AddNode(BSTR name, ICDXNode** p);
  HRESULT __stdcall RemoveAt(UINT i);
  HRESULT __stdcall InsertAt(UINT i, ICDXNode* p);
  HRESULT __stdcall get_Tag(IUnknown** p)
  {
    return tag.CopyTo(p);
  }
  HRESULT __stdcall put_Tag(IUnknown* p)
  {
    tag = p; return 0;
  }
  HRESULT __stdcall HasBuffer(CDX_BUFFER id, BOOL* p)
  {
    *p = getbuffer(id) != 0; return 0;
  }
  HRESULT __stdcall GetBuffer(CDX_BUFFER id, ICDXBuffer** p);
  HRESULT __stdcall SetBuffer(ICDXBuffer* p);
  HRESULT __stdcall GetBufferPtr(CDX_BUFFER id, const BYTE** p, UINT* n);
  HRESULT __stdcall SetBufferPtr(CDX_BUFFER id, const BYTE* p, UINT n);
  HRESULT __stdcall GetBox(XMFLOAT3 box[2], const XMFLOAT4X3* pm);
  HRESULT __stdcall CopyCoords(ICDXBuffer* bpp, ICDXBuffer* bii, float eps, ICDXBuffer** btt);
  HRESULT __stdcall get_Child(ICDXNode** p);
  HRESULT __stdcall get_Next(ICDXNode** p);
  HRESULT __stdcall NextSibling(ICDXNode* r, ICDXNode** p);

};
