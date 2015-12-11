//-------------------------------------------------------------------------------------------------
// <copyright file="BalBootstrapperEngine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

// function declarations

HRESULT BalBootstrapperEngineCreate(
    __in IBootstrapperEngine* pEngine, // TODO: remove after IBootstrapperEngine is moved out of the engine.
    __in PFN_BOOTSTRAPPER_ENGINE_PROC pfnBAEngineProc,
    __in_opt LPVOID pvBAEngineProcContext,
    __out IBootstrapperEngine** ppEngineForApplication
    );

#ifdef __cplusplus
}
#endif
