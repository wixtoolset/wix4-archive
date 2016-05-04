// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class MatchValues : public CfgTest
    {
    protected:
        HRESULT WriteTestFile(
            __in const BYTE *pbData,
            __in SIZE_T cbData
            )
        {
            HRESULT hr = S_OK;
            LPWSTR sczMyDocumentsDir = NULL;
            LPWSTR sczDir = NULL;
            LPWSTR sczFilePath = NULL;

            hr = PathGetKnownFolder(CSIDL_MYDOCUMENTS, &sczMyDocumentsDir);
            ExitOnFailure(hr, "Failed to get my documents folder location");

            hr = PathConcat(sczMyDocumentsDir, L"CfgFileTest", &sczDir);
            ExitOnFailure(hr, "Failed to concat CfgFileTest to my documents path");

            hr = DirEnsureExists(sczDir, NULL);
            ExitOnFailure(hr, "Failed to ensure directory exists: %ls", sczDir);

            hr = PathConcat(sczDir, L"ValueMatchFile.tst", &sczFilePath);
            ExitOnFailure(hr, "Failed to create path to file");

            hr = FileWrite(sczFilePath, 0, pbData, cbData, NULL);
            ExitOnFailure(hr, "Failed to write file with contents of array");

        LExit:
            ReleaseStr(sczMyDocumentsDir);
            ReleaseStr(sczDir);
            ReleaseStr(sczFilePath);

            return hr;
        }

        HRESULT InitRemotePaths(
            __out_z LPWSTR *psczRemote1,
            __out_z LPWSTR *psczRemote2
            )
        {
            HRESULT hr = S_OK;
            LPWSTR sczDirRemote1 = NULL;
            LPWSTR sczDirRemote2 = NULL;

            hr = PathExpand(&sczDirRemote1, L"%TEMP%\\TestRemote1\\", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to expand path to remote1 database");

            hr = PathExpand(&sczDirRemote2, L"%TEMP%\\TestRemote2\\", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to expand path to remote2 database");

            DirEnsureDelete(sczDirRemote1, TRUE, TRUE);
            DirEnsureDelete(sczDirRemote2, TRUE, TRUE);

            hr = PathConcat(sczDirRemote1, L"Remote1.sdf", psczRemote1);
            ExitOnFailure(hr, "Failed to concat path to remote1 database");

            hr = PathConcat(sczDirRemote2, L"Remote2.sdf", psczRemote2);
            ExitOnFailure(hr, "Failed to concat path to remote2 database");

        LExit:
            ReleaseStr(sczDirRemote1);
            ReleaseStr(sczDirRemote2);

            return hr;
        }

    public:
        [Fact]
        [Trait("Name", "MatchValuesTest")]
        void MatchValuesTest()
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFGDB_HANDLE cdhLocal = NULL;
            CFGDB_HANDLE cdhRemote1 = NULL;
            CFGDB_HANDLE cdhRemote2 = NULL;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;
            LPWSTR sczSampleLegacyPath = NULL;
            LPWSTR sczPathRemote1 = NULL;
            LPWSTR sczPathRemote2 = NULL;
            BYTE rgbFile[40] = { };
            rgbFile[10] = 0x11;
            rgbFile[20] = 0x12;

            // This test involves a local db and two different remotes
            // We are going to create a file on disk (that is mapped to cfg value via a UDM file), and sync it to remote 1
            // then wipe out the local db, reinitialize it, load the UDM again, and sync that to remote 2
            // then AGAIN wipe out the local db, reinitialize it, and load the UDM. Now we have an equivalent value in all 3 databases,
            // but with 3 different source guids. Cfg Db has special logic in this scenario to add a history entry to equivalent values
            // (whose source and/or timestamp differ), to ensure that future changes to either machine do not cause sync conflicts.
            // This test verifies we handle this scenario appropriately, and are not creating any more history entries than we need to.

            // From a very high level this all might sound like a silly scenario, but it's actually a very common thing for any new user
            // They might have some user settings on two different machines that are exactly the same (program defaults, or user manually copied
            // between machines). Matching is important to make sure that all new users only see conflicts if two machines disagree on a setting.
            // Avoiding creation of unnecessary history entries is important for settings expiration to be effective, since it will rely on
            // removing stale history entries

            hr = InitRemotePaths(&sczPathRemote1, &sczPathRemote2);
            ExitOnFailure(hr, "Failed to init remote paths");

            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = PathExpand(&sczSampleLegacyPath, L"samplelegacy.udm", PATH_EXPAND_FULLPATH);
            ExitOnFailure(hr, "Failed to get full path to sample legacy XML file");

            WriteTestFile(rgbFile, sizeof(rgbFile));

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczSampleLegacyPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");
            // Make sure the initial auto sync has started before proceeding
            ::Sleep(200);

            hr = CfgCreateRemoteDatabase(sczPathRemote1, &cdhRemote1);
            ExitOnFailure(hr, "Failed to create remote1 database");

            // Now trigger autosync to both databases by making them auto sync remotes
            hr = CfgRememberDatabase(cdhLocal, cdhRemote1, L"Remote1", TRUE);
            ExitOnFailure(hr, "Failed to record remote database 1 in database list");
            WaitForSyncNoResolve(cdhRemote1);

            // OK we've propagated to remote1, wipe out the local db
            hr = CfgRemoteDisconnect(cdhRemote1);
            ExitOnFailure(hr, "Failed to disconnect remote database 1");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();
            
            // Re-initialize to get a new source guid, and this time we propagate to remote2
            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczSampleLegacyPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");
            // Make sure the initial auto sync has started before proceeding
            ::Sleep(200);

            hr = CfgCreateRemoteDatabase(sczPathRemote2, &cdhRemote2);
            ExitOnFailure(hr, "Failed to create remote2 database");

            hr = CfgRememberDatabase(cdhLocal, cdhRemote2, L"Remote2", TRUE);
            ExitOnFailure(hr, "Failed to record remote database 2 in database list");
            WaitForSyncNoResolve(cdhRemote2);

            // OK we've propagated to remote2, wipe out the local db again
            hr = CfgRemoteDisconnect(cdhRemote2);
            ExitOnFailure(hr, "Failed to disconnect remote database 2");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

            TestInitialize();

            // Now let's do this one more time to get a 3rd guid, then setup autosyncing with both databases
            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczSampleLegacyPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");
            // Make sure the initial auto sync has started before proceeding
            ::Sleep(200);

            hr = CfgOpenRemoteDatabase(sczPathRemote1, &cdhRemote1);
            ExitOnFailure(hr, "Failed to open remote1 database");

            hr = CfgOpenRemoteDatabase(sczPathRemote2, &cdhRemote2);
            ExitOnFailure(hr, "Failed to open remote2 database");

            WaitForAutoSync(cdhLocal);
            hr = CfgRememberDatabase(cdhLocal, cdhRemote1, L"Remote1", TRUE);
            ExitOnFailure(hr, "Failed to record remote database 1 in database list");
            WaitForSyncNoResolve(cdhRemote1);

            hr = CfgRememberDatabase(cdhLocal, cdhRemote2, L"Remote2", TRUE);
            ExitOnFailure(hr, "Failed to record remote database 2 in database list");
            WaitForSyncNoResolve(cdhRemote2);

            WaitForSyncNoResolve(cdhRemote1);
            WaitForSyncNoResolve(cdhRemote2);

            hr = CfgSetProduct(cdhLocal, L"CfgTest", L"1.0.0.0", L"0000000000000000");
            ExitOnFailure(hr, "Failed to set product");

            // It should only be possible to have 3 source guids for this value
            hr = CfgEnumPastValues(cdhLocal, L"File:\\ValueMatchFile.tst", &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate past values of Test1");

            if (dwCount != 3)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Incorrect number of values in Test1's history - %u were found", dwCount);
            }

            // Double check matched values have the right data
            VerifyHistoryBlob(cdhLocal, cehHandle, 0, rgbFile, sizeof(rgbFile));
            VerifyHistoryBlob(cdhLocal, cehHandle, 1, rgbFile, sizeof(rgbFile));
            VerifyHistoryBlob(cdhLocal, cehHandle, 2, rgbFile, sizeof(rgbFile));
            CfgReleaseEnumeration(cehHandle);
            cehHandle = NULL;

            hr = CfgRemoteDisconnect(cdhRemote1);
            ExitOnFailure(hr, "Failed to disconnect remote database 1");

            hr = CfgRemoteDisconnect(cdhRemote2);
            ExitOnFailure(hr, "Failed to disconnect remote database 2");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            ReleaseStr(sczSampleLegacyPath);
            ReleaseStr(sczPathRemote1);
            ReleaseStr(sczPathRemote2);
            CfgReleaseEnumeration(cehHandle);
        }
    };
}
