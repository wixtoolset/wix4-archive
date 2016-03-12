//-------------------------------------------------------------------------------------------------
// <copyright file="WixSampleBAFunctions.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------


#include "precomp.h"

class CWixSampleBAFunctions : IBAFunctions
{
public:
    STDMETHODIMP OnDetect()
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running detect BA function");

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }


    STDMETHODIMP OnDetectComplete() { return S_OK; }
    STDMETHODIMP OnPlan() { return S_OK; }
    STDMETHODIMP OnPlanComplete() { return S_OK; }

/*
    STDMETHODIMP OnDetectComplete()
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running detect complete BA function");

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }


    STDMETHODIMP OnPlan()
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running plan BA function");

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }

    
    STDMETHODIMP OnPlanComplete()
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running plan complete BA function");

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }
*/


private:
    HMODULE m_hModule;
    IBootstrapperEngine* m_pEngine;


public:
    //
    // Constructor - initialize member variables.
    //
	CWixSampleBAFunctions(
        __in IBootstrapperEngine* pEngine,
        __in HMODULE hModule
        )
    {
        m_hModule = hModule;
        m_pEngine = pEngine;
    }

    //
    // Destructor - release member variables.
    //
    ~CWixSampleBAFunctions()
    {
    }
};


extern "C" HRESULT WINAPI CreateBAFunctions(
    __in IBootstrapperEngine* pEngine,
    __in HMODULE hModule,
    __out IBAFunctions** ppBAFunctions
    )
{
    HRESULT hr = S_OK;
	CWixSampleBAFunctions* pBAFunctions = NULL;

    // This is required to enable logging functions.
    BalInitialize(pEngine);

    pBAFunctions = new CWixSampleBAFunctions(pEngine, hModule);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CWixSampleBAFunctions object.");

	*ppBAFunctions = reinterpret_cast<IBAFunctions*>(pBAFunctions);
	pBAFunctions = NULL;

LExit:
    delete pBAFunctions;
    return hr;
}
