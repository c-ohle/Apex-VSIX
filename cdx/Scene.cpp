#include "pch.h"
#include "Factory.h"
#include "Scene.h"

HRESULT __stdcall CFactory::CreateScene(ICDXScene** p)
{
#if(0)
  CComPtr<ICDXScene> scene; scene.p = new CScene();// CreateScene(&scene);
  CComPtr< ICDXNode> n1; scene->AddNode(0, &n1);
  CComPtr< ICDXNode> n2; scene->AddNode(0, &n2);
  CComPtr< ICDXNode> n3; scene->AddNode(0, &n3);
  n1.Release();
  n2.Release();
  n3.Release();
  CreateNode(&n1);
  scene->InsertAt(1, n1);
  n1.Release();
  scene->RemoveAt(1);

  scene->RemoveAt(1);
  scene->RemoveAt(1);
  scene->RemoveAt(0);

  scene->AddNode(0, &n1); n1.Release();
  scene->AddNode(0, &n1); n1.Release();
  scene->AddNode(0, &n1); n1.Release();

  scene.Release();
#endif
  (*p = new CComClass<CScene>())->AddRef();
  return 0;
}
HRESULT __stdcall CFactory::CreateNode(ICDXNode** p)
{
  (*p = static_cast<ICDXNode*>(new CComClass<CNode>()))->AddRef();
  return 0;
}

static HRESULT AddNode(IUnknown* unk, CComPtr<CNode>& lastchild, BSTR name, ICDXNode** p)
{
  auto t = static_cast<CNode*>(new CComClass<CNode>()); t->parent = unk; t->put_Name(name);
  t->nextnode = lastchild.p ? lastchild.p->nextnode.p : t;
  if (lastchild.p) { lastchild.p->nextnode = t; lastchild.p->flags &= ~NODE_FL_LAST; }
  lastchild = t; t->flags |= NODE_FL_LAST; (*p = t)->AddRef(); return 0;
}
static HRESULT RemoveAt(CComPtr<CNode>& lastchild, UINT i)
{
  if (!lastchild.p) return E_INVALIDARG;
  auto p = lastchild.p; for (; i--; p = p->nextnode.p);
  auto t = p->nextnode.p; t->put_IsSelect(false);
  t->AddRef();
  if (lastchild.p == t)
  {
    t->flags &= ~NODE_FL_LAST;
    if (p != t) { lastchild = p; p->flags |= NODE_FL_LAST; }
    else lastchild = 0;
  }
  if (p != t) p->nextnode = t->nextnode.p;
  t->parent = 0;
  t->nextnode = 0;
  t->Release();
  return 0;
}

static HRESULT InsertAt(IUnknown* unk, CComPtr<CNode>& lastchild, UINT i, ICDXNode* p)
{
  auto node = static_cast<CNode*>(p);
  if (!lastchild.p)
  {
    if (i != 0) return E_INVALIDARG;
    lastchild = node; node->nextnode = node; node->parent = unk;
    node->flags |= NODE_FL_LAST; return 0;
  }
  auto t = lastchild.p; for (UINT j = i; j--; t = t->nextnode.p);
  node->nextnode = t->nextnode.p;
  t->nextnode = node; node->parent = unk;
  if (lastchild.p == t && i)
  {
    t->flags &= ~NODE_FL_LAST; node->flags |= NODE_FL_LAST;
    lastchild = node;
  }
  return 0;
}

HRESULT CNode::AddNode(BSTR name, ICDXNode** p) { return ::AddNode(this, lastchild, name, p); }
HRESULT CNode::RemoveAt(UINT i) { return ::RemoveAt(lastchild, i); }
HRESULT CNode::InsertAt(UINT i, ICDXNode* p) { return ::InsertAt(this, lastchild, i, p); }

HRESULT CScene::AddNode(BSTR name, ICDXNode** p) { return ::AddNode(this, lastchild, name, p); }
HRESULT CScene::RemoveAt(UINT i) { return ::RemoveAt(lastchild, i); }
HRESULT CScene::InsertAt(UINT i, ICDXNode* p) { return ::InsertAt(this, lastchild, i, p); }

HRESULT CScene::Clear()
{
  if (lastchild.p) { lastchild.p->nextnode.Release(); lastchild.Release(); }
  selection.clear(); camera.Release();
  return 0;
}
HRESULT CScene::get_Camera(ICDXNode** p) { if (*p = camera.p) camera.p->AddRef(); return 0; }
HRESULT CScene::put_Camera(ICDXNode* p) { camera = static_cast<CNode*>(p); return 0; }

void CNode::save(Archive& ar)
{
  UINT fl = 2 | (subn ? 8 : 0) | (this->flags & (NODE_FL_STATIC | NODE_FL_ACTIVE));
  ar.WriteCount(fl);
  ar.Serialize(name);
  ar.Write(&color);
  ar.Write(&matrix);
  ar.WriteCount(buffer.n);
  for (UINT i = 0; i < buffer.n; i++)
  {
    auto pb = buffer.p[i];
    UINT x = ar.getmap(pb); ar.WriteCount(x);
    if (x == 0)
    {
      ar.addmap(pb);
      ar.WriteCount(pb->id); ar.WriteCount(pb->data.n);
      ar.Write(pb->data.p, pb->data.n);
      if (pb->id == CDX_BUFFER_TEXTURE)
        ar.Serialize(static_cast<CTexture*>(pb)->name);
    }
  }
  if (fl & 8) { ar.WriteCount(subi >> 1); ar.WriteCount(subn >> 1); }
  for (auto p = child(); p; p = p->next())
    p->save(ar);
  ar.WriteCount(0);
}

CNode* CNode::load(Archive& ar)
{
  UINT fl = ar.ReadCount(); if (fl == 0) return 0;
  auto* p = static_cast<CNode*>(new CComClass<CNode>());
  p->flags = fl & (NODE_FL_STATIC | NODE_FL_ACTIVE);
  ar.Serialize(p->name);
  ar.Read(&p->color);
  ar.Read(&p->matrix);
  UINT n = ar.ReadCount(); p->buffer.setsize(n);
  for (UINT i = 0; i < n; i++)
  {
    UINT x = ar.ReadCount();
    if (x == 0)
    {
      CDX_BUFFER id = (CDX_BUFFER)ar.ReadCount();
      UINT size = ar.ReadCount(); ar.Read((BYTE*)stackptr, size);
      Critical crit; auto t = CCacheBuffer::getbuffer(id, (const BYTE*)stackptr, size);
      if (id == CDX_BUFFER_TEXTURE)
      {
        CComBSTR s; ar.Serialize(s);
        auto pt = static_cast<CTexture*>(t);
        if (!pt->name.m_str)
          pt->name.m_str = s.Detach();
      }
      (p->buffer.p[i] = t)->AddRef(); ar.addmap(t);
    }
    else
    {
      (p->buffer.p[i] = (CBuffer*)ar.map.p[x - 1])->AddRef();
    }
    auto id = p->buffer.p[i]->id;
    if (id < 32) p->bmask |= 1 << id;
  }
  if (fl & 8) { p->subi = ar.ReadCount() << 1; p->subn = ar.ReadCount() << 1; }
  for (UINT i = 0;;)
  {
    auto t = load(ar); if (!t) break;
    p->InsertAt(i++, t);
  }
  return p;
}
HRESULT CScene::SaveToStream(IStream* str, ICDXNode* cam)
{
  auto pc = static_cast<CNode*>(cam);
  UINT fl = (pc && camera.p ? 1 : 0) | (pc && pc->getscene() ? 2 : 0);
  Archive ar(str, true);
  ar.WriteCount(ar.version = 1);
  ar.WriteCount(fl);
  ar.WriteCount(unit);
  if (!pc)
    for (UINT i = 0; i < selection.n; i++)
      selection.p[i]->save(ar);
  else
    for (auto p = child(); p; p = p->next())
      p->save(ar);
  ar.WriteCount(0);
  if (fl & 1) camera.p->save(ar);
  if (fl & 2) ar.WriteCount(static_cast<CNode*>(cam)->getscount());
  return ar.hr;
}
HRESULT CScene::LoadFromStream(IStream* str)
{
  Archive ar(str, false);
  if ((ar.version = ar.ReadCount()) != 1) return E_FAIL; Clear();
  UINT fl = ar.ReadCount();
  unit = (CDX_UNIT)ar.ReadCount();
  for (UINT i = 0;;)
  {
    auto t = CNode::load(ar); if (!t) break;
    InsertAt(i++, t);
  }
  if (fl & 1) camera = CNode::load(ar);
  if (fl & 2) tag = findscount(ar.ReadCount());
  return 0;
}

HRESULT CScene::get_Count(UINT* pc)
{
  UINT c = 0; for (auto p = child(); p; p = p->next(), c++);
  *pc = c; return 0;
}
HRESULT CNode::get_Count(UINT* pc)
{
  UINT c = 0; for (auto p = child(); p; p = p->next(), c++);
  *pc = c; return 0;
}

//HRESULT CScene::GetNode(UINT i, ICDXNode** pn)
//{
//  auto p = child(); for (UINT c = 0; p && c < i; p = p->next(), c++);
//  if (!p) return E_INVALIDARG;
//  (*pn = p)->AddRef(); return 0;
//}
