//-------------------------------------------------------------------------------------------------
// <copyright file="PathUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace System::Text;
using namespace System::Collections::Generic;
using namespace Xunit;

namespace DutilTests
{
    public ref class PathUtil
    {
    public:
        [Fact]
        void PathPathGetHierarchyArray()
        {
            HRESULT hr = S_OK;
            LPWSTR *rgsczPaths = NULL;
            UINT cPaths = 0;

            hr = PathGetHierarchyArray(L"c:\\foo\\bar\\bas\\a.txt", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for regular file path");
            Assert::Equal<DWORD>(5, cPaths);
            Assert::Equal(gcnew String(L"c:\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"c:\\foo\\"), gcnew String(rgsczPaths[1]));
            Assert::Equal(gcnew String(L"c:\\foo\\bar\\"), gcnew String(rgsczPaths[2]));
            Assert::Equal(gcnew String(L"c:\\foo\\bar\\bas\\"), gcnew String(rgsczPaths[3]));
            Assert::Equal(gcnew String(L"c:\\foo\\bar\\bas\\a.txt"), gcnew String(rgsczPaths[4]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

            hr = PathGetHierarchyArray(L"c:\\foo\\bar\\bas\\", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for regular directory path");
            Assert::Equal<DWORD>(4, cPaths);
            Assert::Equal(gcnew String(L"c:\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"c:\\foo\\"), gcnew String(rgsczPaths[1]));
            Assert::Equal(gcnew String(L"c:\\foo\\bar\\"), gcnew String(rgsczPaths[2]));
            Assert::Equal(gcnew String(L"c:\\foo\\bar\\bas\\"), gcnew String(rgsczPaths[3]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

            hr = PathGetHierarchyArray(L"\\\\server\\share\\subdir\\file.txt", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for UNC file path");
            Assert::Equal<DWORD>(3, cPaths);
            Assert::Equal(gcnew String(L"\\\\server\\share\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"\\\\server\\share\\subdir\\"), gcnew String(rgsczPaths[1]));
            Assert::Equal(gcnew String(L"\\\\server\\share\\subdir\\file.txt"), gcnew String(rgsczPaths[2]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

            hr = PathGetHierarchyArray(L"\\\\server\\share\\subdir\\", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for UNC directory path");
            Assert::Equal<DWORD>(2, cPaths);
            Assert::Equal(gcnew String(L"\\\\server\\share\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"\\\\server\\share\\subdir\\"), gcnew String(rgsczPaths[1]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

            hr = PathGetHierarchyArray(L"Software\\Microsoft\\Windows\\ValueName", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for UNC directory path");
            Assert::Equal<DWORD>(4, cPaths);
            Assert::Equal(gcnew String(L"Software\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"Software\\Microsoft\\"), gcnew String(rgsczPaths[1]));
            Assert::Equal(gcnew String(L"Software\\Microsoft\\Windows\\"), gcnew String(rgsczPaths[2]));
            Assert::Equal(gcnew String(L"Software\\Microsoft\\Windows\\ValueName"), gcnew String(rgsczPaths[3]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

            hr = PathGetHierarchyArray(L"Software\\Microsoft\\Windows\\", &rgsczPaths, &cPaths);
            ExitOnFailure(hr, "Failed to get parent directories array for UNC directory path");
            Assert::Equal<DWORD>(3, cPaths);
            Assert::Equal(gcnew String(L"Software\\"), gcnew String(rgsczPaths[0]));
            Assert::Equal(gcnew String(L"Software\\Microsoft\\"), gcnew String(rgsczPaths[1]));
            Assert::Equal(gcnew String(L"Software\\Microsoft\\Windows\\"), gcnew String(rgsczPaths[2]));
            ReleaseNullStrArray(rgsczPaths, cPaths);

        LExit:
            ReleaseStrArray(rgsczPaths, cPaths);
        }
    };
}
