//-------------------------------------------------------------------------------------------------
// <copyright file="VrntUtilHelper.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace DutilTests
{
    using namespace System;
    using namespace System::Runtime::InteropServices;
    using namespace WixTest;

    public ref class VrntUtilHelper
    {
    public:
        delegate void StrSecureZeroFreeStringDelegate(LPWSTR scz);

        property LPVOID ActualValue;
        property LPVOID ExpectedValue;

        property PFN_STRSECUREZEROFREESTRING PfnStrSecureZeroFreeString
        {
            PFN_STRSECUREZEROFREESTRING get()
            {
                StrSecureZeroFreeStringDelegate^ sszfsDelegate = gcnew StrSecureZeroFreeStringDelegate(this, &VrntUtilHelper::CustomStrSecureZeroFreeString);
                return reinterpret_cast<PFN_STRSECUREZEROFREESTRING>((void*)Marshal::GetFunctionPointerForDelegate(sszfsDelegate));
            }
        }

        void CustomStrSecureZeroFreeString(LPWSTR scz)
        {
            this->ActualValue = scz;
            HRESULT hr = StrSecureZeroFreeString(scz);
            NativeAssert::Succeeded(hr, "StrSecureZeroFreeString failed.");
        }
    };
}