//-------------------------------------------------------------------------------------------------
// <copyright file="CrypUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

namespace DutilTests
{
    using namespace System;
    using namespace Xunit;
    using namespace WixTest;

    public ref class CrypUtil
    {
    public:
        [NamedFact]
        void CrypUtilEncryptionDecryptionTest()
        {
            HRESULT hr = S_OK;
            DWORD encryptionFlags = CRYPTPROTECTMEMORY_SAME_LOGON;
            DWORD cbData = CRYP_ENCRYPT_MEMORY_SIZE * 2;
            LPVOID pData = NULL;
            LPVOID pZeroData = NULL;

            try
            {
                CrypInitialize();

                pData = MemAlloc(cbData, TRUE);
                NativeAssert::NotPointerEqual(NULL, pData);

                pZeroData = MemAlloc(cbData, TRUE);
                NativeAssert::NotPointerEqual(NULL, pZeroData);

                NativeAssert::Equal(0, memcmp(pData, pZeroData, cbData));

                hr = CrypEncryptMemory(pData, cbData, encryptionFlags);
                NativeAssert::Equal(S_OK, hr);

                NativeAssert::NotEqual(0, memcmp(pData, pZeroData, cbData));

                hr = CrypDecryptMemory(pData, cbData, encryptionFlags);
                NativeAssert::Equal(S_OK, hr);

                NativeAssert::Equal(0, memcmp(pData, pZeroData, cbData));
            }
            finally
            {
                CrypUninitialize();

                ReleaseMem(pZeroData);
                ReleaseMem(pData);
            }
        }
    };
}