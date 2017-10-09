// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


class CBalBootstrapperEngine : public IBootstrapperEngine, public IMarshal
{
public: // IUnknown
    virtual STDMETHODIMP QueryInterface(
        __in REFIID riid,
        __out LPVOID *ppvObject
        )
    {
        if (!ppvObject)
        {
            return E_INVALIDARG;
        }

        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBootstrapperEngine), riid))
        {
            *ppvObject = static_cast<IBootstrapperEngine*>(this);
        }
        else if (::IsEqualIID(IID_IMarshal, riid))
        {
            *ppvObject = static_cast<IMarshal*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = reinterpret_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    virtual STDMETHODIMP_(ULONG) AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }

    virtual STDMETHODIMP_(ULONG) Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }

public: // IBootstrapperEngine
    virtual STDMETHODIMP GetPackageCount(
        __out DWORD* pcPackages
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETPACKAGECOUNT_ARGS args = { };
        BAENGINE_GETPACKAGECOUNT_RESULTS results = { };

        ExitOnNull(pcPackages, hr, E_INVALIDARG, "pcPackages is required");

        args.cbSize = sizeof(args);

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT, &args, &results, m_pvBAEngineProcContext);

        *pcPackages = results.cPackages;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __out LONGLONG* pllValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLENUMERIC_ARGS args = { };
        BAENGINE_GETVARIABLENUMERIC_RESULTS results = { };

        ExitOnNull(pllValue, hr, E_INVALIDARG, "pllValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC, &args, &results, m_pvBAEngineProcContext);

        *pllValue = results.llValue;

    LExit:
        SecureZeroMemory(&results, sizeof(results));
        return hr;
    }

    virtual STDMETHODIMP GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout DWORD* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLESTRING_ARGS args = { };
        BAENGINE_GETVARIABLESTRING_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING, &args, &results, m_pvBAEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __out DWORD64* pqwValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLEVERSION_ARGS args = { };
        BAENGINE_GETVARIABLEVERSION_RESULTS results = { };

        ExitOnNull(pqwValue, hr, E_INVALIDARG, "pqwValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION, &args, &results, m_pvBAEngineProcContext);

        *pqwValue = results.qwValue;

    LExit:
        SecureZeroMemory(&results, sizeof(results));
        return hr;
    }

    virtual STDMETHODIMP FormatString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        return m_pEngine->FormatString(wzIn, wzOut, pcchOut);
    }

    virtual STDMETHODIMP EscapeString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        return m_pEngine->EscapeString(wzIn, wzOut, pcchOut);
    }

    virtual STDMETHODIMP EvaluateCondition(
        __in_z LPCWSTR wzCondition,
        __out BOOL* pf
        )
    {
        return m_pEngine->EvaluateCondition(wzCondition, pf);
    }

    virtual STDMETHODIMP Log(
        __in BOOTSTRAPPER_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        )
    {
        return m_pEngine->Log(level, wzMessage);
    }

    virtual STDMETHODIMP SendEmbeddedError(
        __in DWORD dwErrorCode,
        __in_z_opt LPCWSTR wzMessage,
        __in DWORD dwUIHint,
        __out int* pnResult
        )
    {
        return m_pEngine->SendEmbeddedError(dwErrorCode, wzMessage, dwUIHint, pnResult);
    }

    virtual STDMETHODIMP SendEmbeddedProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __out int* pnResult
        )
    {
        return m_pEngine->SendEmbeddedProgress(dwProgressPercentage, dwOverallProgressPercentage, pnResult);
    }

    virtual STDMETHODIMP SetUpdate(
        __in_z_opt LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource,
        __in DWORD64 qwSize,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
        __in_bcount_opt(cbHash) BYTE* rgbHash,
        __in DWORD cbHash
        )
    {
        return m_pEngine->SetUpdate(wzLocalSource, wzDownloadSource, qwSize, hashType, rgbHash, cbHash);
    }

    virtual STDMETHODIMP SetLocalSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzPath
        )
    {
        return m_pEngine->SetLocalSource(wzPackageOrContainerId, wzPayloadId, wzPath);
    }

    virtual STDMETHODIMP SetDownloadSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzUrl,
        __in_z_opt LPCWSTR wzUser,
        __in_z_opt LPCWSTR wzPassword
        )
    {
        return m_pEngine->SetDownloadSource(wzPackageOrContainerId, wzPayloadId, wzUrl, wzUser, wzPassword);
    }

    virtual STDMETHODIMP SetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        )
    {
        return m_pEngine->SetVariableNumeric(wzVariable, llValue);
    }

    virtual STDMETHODIMP SetVariableString(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        )
    {
        return m_pEngine->SetVariableString(wzVariable, wzValue);
    }

    virtual STDMETHODIMP SetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __in DWORD64 qwValue
        )
    {
        return m_pEngine->SetVariableVersion(wzVariable, qwValue);
    }

    virtual STDMETHODIMP CloseSplashScreen()
    {
        return m_pEngine->CloseSplashScreen();
    }

    virtual STDMETHODIMP Detect(
        __in_opt HWND hwndParent
        )
    {
        BAENGINE_DETECT_ARGS args = { };
        BAENGINE_DETECT_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.hwndParent = hwndParent;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_DETECT, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Plan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        return m_pEngine->Plan(action);
    }

    virtual STDMETHODIMP Elevate(
        __in_opt HWND hwndParent
        )
    {
        return m_pEngine->Elevate(hwndParent);
    }

    virtual STDMETHODIMP Apply(
        __in_opt HWND hwndParent
        )
    {
        return m_pEngine->Apply(hwndParent);
    }

    virtual STDMETHODIMP Quit(
        __in DWORD dwExitCode
        )
    {
        return m_pEngine->Quit(dwExitCode);
    }

    virtual STDMETHODIMP LaunchApprovedExe(
        __in_opt HWND hwndParent,
        __in_z LPCWSTR wzApprovedExeForElevationId,
        __in_z_opt LPCWSTR wzArguments,
        __in DWORD dwWaitForInputIdleTimeout
        )
    {
        return m_pEngine->LaunchApprovedExe(hwndParent, wzApprovedExeForElevationId, wzArguments, dwWaitForInputIdleTimeout);
    }

public: // IMarshal
    virtual STDMETHODIMP GetUnmarshalClass(
        __in REFIID /*riid*/,
        __in_opt LPVOID /*pv*/,
        __in DWORD /*dwDestContext*/,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out LPCLSID /*pCid*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP GetMarshalSizeMax(
        __in REFIID riid,
        __in_opt LPVOID /*pv*/,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out DWORD *pSize
        )
    {
        HRESULT hr = S_OK;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pSize, hr, E_FAIL, "Invalid size output parameter is NULL.");

        // Specify enough size to marshal just the interface pointer across threads.
        *pSize = sizeof(LPVOID);

    LExit:
        return hr;
    }

    virtual STDMETHODIMP MarshalInterface(
        __in IStream* pStm,
        __in REFIID riid,
        __in_opt LPVOID pv,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/
        )
    {
        HRESULT hr = S_OK;
        IBootstrapperEngine *pThis = NULL;
        ULONG ulWritten = 0;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // "pv" may not be set, so we should us "this" otherwise.
        if (pv)
        {
            pThis = reinterpret_cast<IBootstrapperEngine*>(pv);
        }
        else
        {
            pThis = static_cast<IBootstrapperEngine*>(this);
        }

        // E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");

        // Marshal the interface pointer in-proc as is.
        hr = pStm->Write(pThis, sizeof(pThis), &ulWritten);
        if (STG_E_MEDIUMFULL == hr)
        {
            ExitOnFailure(hr, "Failed to write the stream because the stream is full.");
        }
        else if (FAILED(hr))
        {
            // All other STG error must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to write the IBootstrapperEngine interface pointer to the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP UnmarshalInterface(
        __in IStream* pStm,
        __in REFIID riid,
        __deref_out LPVOID* ppv
        )
    {
        HRESULT hr = S_OK;
        ULONG ulRead = 0;

        // We only support marshaling the engine in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");
        ExitOnNull(ppv, hr, E_FAIL, "The interface output parameter is NULL.");

        // Unmarshal the interface pointer in-proc as is.
        hr = pStm->Read(*ppv, sizeof(LPVOID), &ulRead);
        if (FAILED(hr))
        {
            // All STG errors must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to read the IBootstrapperEngine interface pointer from the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP ReleaseMarshalData(
        __in IStream* /*pStm*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP DisconnectObject(
        __in DWORD /*dwReserved*/
        )
    {
        return E_NOTIMPL;
    }

public:
    CBalBootstrapperEngine(
        __in IBootstrapperEngine* pEngine, // TODO: remove after IBootstrapperEngine is moved out of the engine.
        __in PFN_BOOTSTRAPPER_ENGINE_PROC pfnBAEngineProc,
        __in_opt LPVOID pvBAEngineProcContext
        )
    {
        m_cReferences = 1;
        m_pfnBAEngineProc = pfnBAEngineProc;
        m_pvBAEngineProcContext = pvBAEngineProcContext;

        pEngine->AddRef();
        m_pEngine = pEngine;
    }

    virtual ~CBalBootstrapperEngine()
    {
        ReleaseNullObject(m_pEngine);
    }

private:
    long m_cReferences;
    IBootstrapperEngine* m_pEngine;
    PFN_BOOTSTRAPPER_ENGINE_PROC m_pfnBAEngineProc;
    LPVOID m_pvBAEngineProcContext;
};

HRESULT BalBootstrapperEngineCreate(
    __in IBootstrapperEngine* pEngine, // TODO: remove after IBootstrapperEngine is moved out of the engine.
    __in PFN_BOOTSTRAPPER_ENGINE_PROC pfnBAEngineProc,
    __in_opt LPVOID pvBAEngineProcContext,
    __out IBootstrapperEngine** ppBootstrapperEngine
    )
{
    HRESULT hr = S_OK;
    CBalBootstrapperEngine* pBootstrapperEngine = NULL;

    pBootstrapperEngine = new CBalBootstrapperEngine(pEngine, pfnBAEngineProc, pvBAEngineProcContext);
    ExitOnNull(pBootstrapperEngine, hr, E_OUTOFMEMORY, "Failed to allocate new BalBootstrapperEngine object.");

    hr = pBootstrapperEngine->QueryInterface(IID_PPV_ARGS(ppBootstrapperEngine));
    ExitOnFailure(hr, "Failed to QI for IBootstrapperEngine from BalBootstrapperEngine object.");

LExit:
    ReleaseObject(pBootstrapperEngine);
    return hr;
}
