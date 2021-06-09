
#pragma once
#include "resource.h"
#include "cdx_i.h"
#include "scene.h"

class ATL_NO_VTABLE CFactory :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CFactory, &CLSID_Factory>,
	public ICDXFactory, IAgileObject
{
public:
DECLARE_REGISTRY_RESOURCEID(106)
DECLARE_CLASSFACTORY_SINGLETON(CFactory)
DECLARE_NOT_AGGREGATABLE(CFactory)
BEGIN_COM_MAP(CFactory)
	COM_INTERFACE_ENTRY(ICDXFactory)
	COM_INTERFACE_ENTRY(IAgileObject)
END_COM_MAP()
	DECLARE_PROTECT_FINAL_CONSTRUCT()
	HRESULT FinalConstruct() { return S_OK; }
	void FinalRelease() { }
public:
	HRESULT __stdcall get_Version(UINT* pVal)
	{
		auto p = (BYTE*)pVal;
		p[0] = sizeof(void*);
		p[1] = Debug ? 1 : 0;
		p[2] = 0;
		p[3] = 1;
		return S_OK;
	}
	HRESULT __stdcall get_Devices(BSTR* p);
	HRESULT __stdcall get_Samples(BSTR* p);
	HRESULT __stdcall SetDevice(UINT id);
	HRESULT __stdcall SetSamples(UINT id);
	HRESULT __stdcall CreateView(HWND hwnd, ICDXSink* sink, UINT samp, ICDXView** p);
	HRESULT __stdcall CreateScene(ICDXScene** p);
	HRESULT __stdcall CreateNode(ICDXNode** p);
	HRESULT __stdcall GetFont(BSTR name, FLOAT size, UINT style, ICDXFont** p);
	HRESULT __stdcall GetInfo(CDX_INFO i, UINT* v);
	HRESULT __stdcall GetBuffer(CDX_BUFFER i, const BYTE* p, UINT n, ICDXBuffer** v);
	HRESULT __stdcall CopyCoords(const XMFLOAT3* appp, const USHORT* aiip, UINT aiin, const XMFLOAT2* attp, const XMFLOAT3* bppp, const USHORT* biip, UINT biin, XMFLOAT2* bttp, FLOAT eps);

};

OBJECT_ENTRY_AUTO(__uuidof(Factory), CFactory)
