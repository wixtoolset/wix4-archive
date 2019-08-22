// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT PersistedEncodingFromAttribute(
    __in_z LPCWSTR wzEncoding,
    __out PERSISTED_FILE_ENCODING_TYPE *pfetEncoding
    );
static HRESULT ParseProductElement(
    __in IXMLDOMDocument *pixdDocument,
    __out LEGACY_PRODUCT *pProduct,
    __out IXMLDOMNode **ppixnProductElement
    );
static HRESULT ParseDetectArp(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDetectExe(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDetects(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    );
static HRESULT ParseDataRegistryKeyChildren(
    __inout LEGACY_REGISTRY_KEY *pRegKey,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataRegistryKeyBinary(
    __inout LEGACY_REGISTRY_KEY *pRegKey,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataRegistryKeyFlag(
    __inout LEGACY_REGISTRY_SPECIAL *plrsRegistrySPECIAL,
    __in DWORD dwRegValueType,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataRegistryKey(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataDirectory(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataFile(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataCfgFile(
    __inout LEGACY_FILE *pFile,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseDataCfgFileValue(
    __inout LEGACY_FILE_INI_INFO *pIniInfo,
    __in IXMLDOMNode *pixnElement
    );
static HRESULT ParseData(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    );
static HRESULT ParseFilter(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    );
static HRESULT ParseDisplayName(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    );
// Adds a list of registry values that should be excluded from normal handling
// because registry special handling will handle them instead
static HRESULT AddLegacyRegistrySpecialCfgValuesToDict(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __inout STRINGDICT_HANDLE *pshRegistrySpecials
    );

HRESULT ParseManifest(
    __in_z LPCWSTR wzFileContents,
    __out LEGACY_PRODUCT *pProduct
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdDocument = NULL;
    IXMLDOMNode* pixnNode = NULL;
    IXMLDOMNode* pixnProductElement = NULL;
    IXMLDOMNodeList* pixnlNodes = NULL;
    BSTR bstrElement = NULL;
    BSTR bstrText = NULL;
    LPWSTR sczInput = NULL;

    hr = XmlLoadDocument(wzFileContents, &pixdDocument);
    ExitOnFailure(hr, "Failed to load XML");

    hr = ParseProductElement(pixdDocument, pProduct, &pixnProductElement);
    ExitOnFailure(hr, "Failed to parse Product element");

    hr = ParseDetects(pProduct, pixnProductElement);
    ExitOnFailure(hr, "Failed to parse detects");

    hr = ParseData(pProduct, pixnProductElement);
    ExitOnFailure(hr, "Failed to parse data");

    hr = ParseFilter(pProduct, pixnProductElement);
    ExitOnFailure(hr, "Failed to parse filter");

    hr = XmlSelectNodes(pixnProductElement, L"*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select child elements of Product");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through list of nodes under Product");

        if (0 == lstrcmpW(bstrElement, L"DisplayName"))
        {
            hr = ParseDisplayName(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Product/DisplayName element");
        }
        else if (0 == lstrcmpW(bstrElement, L"Data") || 0 == lstrcmpW(bstrElement, L"Detect") || 0 == lstrcmpW(bstrElement, L"Filter"))
        {
            // Skip Data & Detect, they're parsed elsewhere
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Product", bstrElement);
        }

        ReleaseNullObject(pixnNode);
    }

    // Now index aspects of the manifest
    hr = DictCreateStringList(&pProduct->shRegistrySpeciallyHandled, 0, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary of registry exceptions");

    hr = DictCreateWithEmbeddedKey(&pProduct->shRegKeys, 0, reinterpret_cast<void **>(pProduct->rgRegKeys), offsetof(LEGACY_REGISTRY_KEY, sczNamespace), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary to index regkeys");

    for (DWORD i = 0; i < pProduct->cRegKeys; ++i)
    {
        for (DWORD j = 0; j < pProduct->rgRegKeys[i].cRegKeySpecials; ++j)
        {
            hr = AddLegacyRegistrySpecialCfgValuesToDict(&pProduct->rgRegKeys[i], &(pProduct->rgRegKeys[i].rgRegKeySpecials[j]), &pProduct->shRegistrySpeciallyHandled);
            ExitOnFailure(hr, "Failed to add special registry key cfg values to dictionary");
        }

        hr = DictAddValue(pProduct->shRegKeys, pProduct->rgRegKeys + i);
        ExitOnFailure(hr, "Failed to add regkey to dictionary");
    }

    hr = DictCreateWithEmbeddedKey(&pProduct->shFiles, 0, reinterpret_cast<void **>(pProduct->rgFiles), offsetof(LEGACY_FILE, sczName), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary to index files");

    for (DWORD i = 0; i < pProduct->cFiles; ++i)
    {
        hr = DictAddValue(pProduct->shFiles, pProduct->rgFiles + i);
        ExitOnFailure(hr, "Failed to add file to dictionary");
    }
    
LExit:
    ReleaseBSTR(bstrElement);
    ReleaseBSTR(bstrText);
    ReleaseStr(sczInput);
    ReleaseObject(pixdDocument);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnProductElement);
    ReleaseObject(pixnlNodes);

    return hr;
}

// Begin static functions
HRESULT ParseProductElement(
    __in IXMLDOMDocument *pixdDocument,
    __out LEGACY_PRODUCT *pProduct,
    __out IXMLDOMNode **ppixnProductElement
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczInput = NULL;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;

    hr = XmlSelectNodes(pixdDocument, L"/LegacySettings/Product", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select Product Nodes");

    hr = XmlNextElement(pixnlNodes, ppixnProductElement, NULL);
    ExitOnFailure(hr, "Failed to get first Product Node");

    // Test for a 2nd product node here - fail if found
    hr = XmlNextElement(pixnlNodes, &pixnNode, NULL);
    if (S_OK == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "More than one product node found. This is unsupported.");
    }
    ExitOnFailure(hr, "Failed to test for more than one Product node");

    hr = XmlGetAttributeEx(*ppixnProductElement, L"Id", &sczInput);
    ExitOnFailure(hr, "Failed to get product id");

    hr = ProductValidateName(sczInput);
    ExitOnFailure(hr, "Product Id doesn't match expected format");

    pProduct->sczProductId = sczInput;
    sczInput = 0;

LExit:
    ReleaseStr(sczInput);
    ReleaseObject(pixnlNodes);
    ReleaseObject(pixnNode);

    return hr;
}

HRESULT ParseDetectArp(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    BSTR bstrElement = NULL;
    BSTR bstrText = NULL;
    DWORD dwDetectIndex = pProduct->detect.cDetects;
    DWORD dwHintAllocSize = 0;
    long cNumHints = 0;
    LEGACY_DETECT *pDetect = pProduct->detect.rgDetects + dwDetectIndex;

    ++pProduct->detect.cDetects;
    pDetect->ldtType = LEGACY_DETECT_TYPE_ARP;

    hr = XmlGetAttributeEx(pixnElement, L"InstallLocation", &pDetect->arp.sczInstallLocationProperty);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get InstallLocation attribute from Detect/Arp element");

    hr = XmlGetAttributeEx(pixnElement, L"UninstallStringDir", &pDetect->arp.sczUninstallStringDirProperty);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get UninstallStringDir attribute from Detect/Arp element");

    hr = XmlGetAttributeEx(pixnElement, L"DisplayIconDir", &pDetect->arp.sczDisplayIconDirProperty);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get DisplayIconDir attribute from Detect/Arp element");

    hr = XmlGetAttributeEx(pixnElement, L"DisplayName", &pDetect->arp.sczDisplayName);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get DisplayName attribute from Detect/Arp element");

    hr = XmlGetAttributeEx(pixnElement, L"RegKeyName", &pDetect->arp.sczRegKeyName);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get RegKeyName attribute from Detect/Arp element");

    if (NULL != pDetect->arp.sczDisplayName && NULL != pDetect->arp.sczRegKeyName)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Can't specify both a DisplayName and a RegKeyName to a Detect/Arp element!");
    }

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlChildElements->get_length(reinterpret_cast<long*>(&cNumHints));
    ExitOnFailure(hr, "Failed to get number of child nodes under Detect/Arp");

    hr = ::DWordMult(static_cast<DWORD>(cNumHints), sizeof(LEGACY_DETECT_ARP_HINT), reinterpret_cast<DWORD *>(&dwHintAllocSize));
    ExitOnFailure(hr, "Maximum allocation of datatype array exceeded (pointer).");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Arp");

        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Unknown element '%ls' found under Detect/Arp", bstrElement);

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseObject(pixnlChildElements);
    ReleaseObject(pixnChildElement);
    ReleaseBSTR(bstrElement);
    ReleaseBSTR(bstrText);

    return hr;
}

HRESULT ParseDetectExe(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    BSTR bstrElement = NULL;
    BSTR bstrText = NULL;
    DWORD dwDetectIndex = pProduct->detect.cDetects;
    LEGACY_DETECT *pDetect = pProduct->detect.rgDetects + dwDetectIndex;

    ++pProduct->detect.cDetects;
    pProduct->detect.rgDetects[dwDetectIndex].ldtType = LEGACY_DETECT_TYPE_EXE;

    hr = XmlGetAttributeEx(pixnElement, L"FileDir", &pDetect->exe.sczFileDirProperty);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get FileDir attribute from Detect/Exe element");

    hr = XmlGetAttributeEx(pixnElement, L"Name", &pDetect->exe.sczFileName);
    ExitOnFailure(hr, "Failed to get Name attribute from Detect/Exe element");

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Exe");

        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Unknown element '%ls' found under Detect/Exe", bstrElement);

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseObject(pixnlChildElements);
    ReleaseObject(pixnChildElement);
    ReleaseBSTR(bstrElement);
    ReleaseBSTR(bstrText);

    return hr;
}

HRESULT ParseDetects(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    BSTR bstrElement = NULL;
    DWORD cNumDetects = 0;
    DWORD cbDetectAlloc = 0;

    hr = XmlSelectNodes(pixnProductElement, L"Detect/*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlNodes->get_length(reinterpret_cast<long*>(&cNumDetects));
    ExitOnFailure(hr, "Failed to get number of child elements under Detect");

    if (0 == cNumDetects)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = ::DWordMult(cNumDetects, sizeof(LEGACY_DETECT), &cbDetectAlloc);
    ExitOnFailure(hr, "Failed while calculating size of detects to allocate");

    pProduct->detect.rgDetects = reinterpret_cast<LEGACY_DETECT *>(MemAlloc(cbDetectAlloc, TRUE));
    ExitOnNull(pProduct->detect.rgDetects, hr, E_OUTOFMEMORY, "Failed to allocate memory for detects");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through detect list");

        if (0 == lstrcmpW(bstrElement, L"Arp"))
        {
            hr = ParseDetectArp(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Detect/Arp element");
        }
        else if (0 == lstrcmpW(bstrElement, L"Exe"))
        {
            hr = ParseDetectExe(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Detect/Exe element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Detect", bstrElement);
        }

        ReleaseNullObject(pixnNode);
    }
LExit:
    ReleaseBSTR(bstrElement);
    ReleaseObject(pixnlNodes);
    ReleaseObject(pixnNode);

    return hr;
}

HRESULT ParseDataRegistryKeyChildren(
    __inout LEGACY_REGISTRY_KEY *pRegKey,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    BSTR bstrElement = NULL;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select child elements");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through detect list");

        if (0 == lstrcmpW(bstrElement, L"Binary"))
        {
            hr = ParseDataRegistryKeyBinary(pRegKey, pixnNode);
            ExitOnFailure(hr, "Failed to parse Data/RegistryKey/Binary element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Data/RegistryKey", bstrElement);
        }

        ReleaseNullObject(pixnNode);
    }

LExit:
    ReleaseBSTR(bstrElement);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnlNodes);

    return hr;
}

HRESULT ParseDataRegistryKeyBinary(
    __inout LEGACY_REGISTRY_KEY *pRegKey,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    BSTR bstrElement = NULL;
    BOOL fHandleNonTypecasted = TRUE;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD dwInsertIndex = pRegKey->cRegKeySpecials;

    ++pRegKey->cRegKeySpecials;

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pRegKey->rgRegKeySpecials), pRegKey->cRegKeySpecials, sizeof(LEGACY_REGISTRY_SPECIAL), 0);
    ExitOnFailure(hr, "Failed to resize legacy registry specials array");

    hr = XmlGetAttributeEx(pixnElement, L"ValueName", &pRegKey->rgRegKeySpecials[dwInsertIndex].sczRegValueName);
    ExitOnFailure(hr, "Failed to get ValueName attribute from Data/RegistryKey/Binary element");

    if (pRegKey->rgRegKeySpecials[dwInsertIndex].sczRegValueName[0] == L'\\')
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "ValueName attribute cannot begin with a backslash");
    }

    pRegKey->rgRegKeySpecials[dwInsertIndex].dwRegValueType = REG_BINARY;

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select child elements under Binary element");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through detect list");

        if (0 == lstrcmpW(bstrElement, L"Flag"))
        {
            fHandleNonTypecasted = FALSE;

            hr = ParseDataRegistryKeyFlag(&pRegKey->rgRegKeySpecials[dwInsertIndex], REG_BINARY, pixnNode);
            ExitOnFailure(hr, "Failed to parse Data/RegistryKey/Binary/Flag element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Data/RegistryKey/Binary", bstrElement);
        }

        ReleaseNullObject(pixnNode);
    }

    pRegKey->rgRegKeySpecials[dwInsertIndex].fHandleNonTypecasted = fHandleNonTypecasted;

LExit:
    ReleaseBSTR(bstrElement);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnlNodes);

    return hr;
}

HRESULT ParseDataRegistryKeyFlag(
    __inout LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __in DWORD dwRegValueType,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    DWORD dwInsertIndex = pRegKeySpecial->cFlagsInfo;

    if (REG_DWORD != dwRegValueType && REG_QWORD != dwRegValueType && REG_BINARY != dwRegValueType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Flag element is only supported for dword, qword and binary registry value types!");
    }

    ++pRegKeySpecial->cFlagsInfo;

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pRegKeySpecial->rgFlagsInfo), pRegKeySpecial->cFlagsInfo, sizeof(LEGACY_FLAGS_PARSE_INFO), 0);
    ExitOnFailure(hr, "Failed to resize legacy flags parse info array");

    hr = XmlGetAttributeEx(pixnElement, L"Name", &pRegKeySpecial->rgFlagsInfo[dwInsertIndex].sczCfgValueName);
    ExitOnFailure(hr, "Failed to get Name attribute from Flag element");

    hr = XmlGetAttributeNumber(pixnElement, L"Offset", &pRegKeySpecial->rgFlagsInfo[dwInsertIndex].dwOffset);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to get Offset attribute from Flag element");

LExit:
    return hr;
}

HRESULT ParseDataRegistryKey(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRoot = NULL;
    LPWSTR sczKey = NULL;
    LPWSTR sczNamespace = NULL;

    hr = XmlGetAttributeEx(pixnElement, L"Root", &sczRoot);
    ExitOnFailure(hr, "Failed to get Root attribute from Data/RegistryKey element");

    hr = XmlGetAttributeEx(pixnElement, L"Key", &sczKey);
    ExitOnFailure(hr, "Failed to get Key attribute from Data/RegistryKey element");

    // Append backslash to key if necessary
    hr = PathBackslashTerminate(&sczKey);
    ExitOnFailure(hr, "Failed to ensure backslash is appended to key");

    hr = XmlGetAttributeEx(pixnElement, L"Namespace", &sczNamespace);
    ExitOnFailure(hr, "Failed to get Namespace attribute from Data/RegistryKey element");

    ++pProduct->cRegKeys;
    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pProduct->rgRegKeys), pProduct->cRegKeys, sizeof(LEGACY_REGISTRY_KEY), 5);
    ExitOnFailure(hr, "Failed to resize regkey array");

    if (0 == lstrcmpW(sczRoot, L"HKLM"))
    {
        pProduct->rgRegKeys[pProduct->cRegKeys-1].dwRoot = CfgLegacyDbRegistryRootLocalMachine;
    }
    else if (0 == lstrcmpW(sczRoot, L"HKCU"))
    {
        pProduct->rgRegKeys[pProduct->cRegKeys-1].dwRoot = CfgLegacyDbRegistryRootCurrentUser;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Unexpected root type encountered: %ls", sczRoot);
    }
    pProduct->rgRegKeys[pProduct->cRegKeys-1].sczKey = sczKey;
    sczKey = NULL;
    pProduct->rgRegKeys[pProduct->cRegKeys-1].sczNamespace = sczNamespace;
    sczNamespace = NULL;

    hr = ParseDataRegistryKeyChildren(&pProduct->rgRegKeys[pProduct->cRegKeys-1], pixnElement);
    ExitOnFailure(hr, "Failed to parse children of Data/RegistryKey element");

LExit:
    ReleaseStr(sczRoot);
    ReleaseStr(sczKey);
    ReleaseStr(sczNamespace);

    return hr;
}

HRESULT ParseDataDirectory(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    LPWSTR sczLocation = NULL;
    LPWSTR sczNamespace = NULL;
    DWORD cNumChildren = 0;
    BSTR bstrElement = NULL;

    hr = XmlGetAttributeEx(pixnElement, L"Location", &sczLocation);
    ExitOnFailure(hr, "Failed to get location attribute from Data/Directory element");

    hr = PathBackslashTerminate(&sczLocation);
    ExitOnFailure(hr, "Failed to ensure directory location is terminated by backslash");

    hr = XmlGetAttributeEx(pixnElement, L"Namespace", &sczNamespace);
    ExitOnFailure(hr, "Failed to get namespace attribute from Data/Directory element");

    ++pProduct->cFiles;
    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pProduct->rgFiles), pProduct->cFiles, sizeof(LEGACY_FILE), 5);
    ExitOnFailure(hr, "Failed to resize directory array");

    pProduct->rgFiles[pProduct->cFiles-1].legacyFileType = LEGACY_FILE_DIRECTORY;
    pProduct->rgFiles[pProduct->cFiles-1].sczLocation = sczLocation;
    sczLocation = NULL;
    pProduct->rgFiles[pProduct->cFiles-1].sczName = sczNamespace;
    sczNamespace = NULL;

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlChildElements->get_length(reinterpret_cast<long*>(&cNumChildren));
    ExitOnFailure(hr, "Failed to get number of child nodes under Data/File");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Arp");

        if (0 == lstrcmpW(bstrElement, L"CfgFile"))
        {
            hr = ParseDataCfgFile(pProduct->rgFiles + pProduct->cFiles-1, pixnChildElement);
            ExitOnFailure(hr, "Failed to parse Data/File/CfgFile element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, " Unknown element '%ls' found under Data/File", bstrElement);
        }

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseObject(pixnChildElement);
    ReleaseObject(pixnlChildElements);
    ReleaseStr(sczLocation);
    ReleaseStr(sczNamespace);
    ReleaseBSTR(bstrElement);

    return hr;
}

HRESULT ParseDataFile(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    LPWSTR sczLocation = NULL;
    LPWSTR sczName = NULL;
    DWORD dwLocationLen = 0;
    DWORD cNumChildren = 0;
    BSTR bstrElement = NULL;

    hr = XmlGetAttributeEx(pixnElement, L"Location", &sczLocation);
    ExitOnFailure(hr, "Failed to get location attribute from Data/File element");

    dwLocationLen = lstrlenW(sczLocation);
    if (0 == dwLocationLen)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Data/File/@Location must not be empty!");
    }
    if (L'\\' == sczLocation[dwLocationLen - 1])
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "A path ending in backslash was specified for location in a file element: %ls", sczLocation);
    }

    hr = XmlGetAttributeEx(pixnElement, L"Name", &sczName);
    ExitOnFailure(hr, "Failed to get Name attribute from Data/File element");

    ++pProduct->cFiles;
    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pProduct->rgFiles), pProduct->cFiles, sizeof(LEGACY_FILE), 5);
    ExitOnFailure(hr, "Failed to resize file array");

    pProduct->rgFiles[pProduct->cFiles-1].legacyFileType = LEGACY_FILE_PLAIN;
    pProduct->rgFiles[pProduct->cFiles-1].sczLocation = sczLocation;
    sczLocation = NULL;
    pProduct->rgFiles[pProduct->cFiles-1].sczName = sczName;
    sczName = NULL;

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlChildElements->get_length(reinterpret_cast<long*>(&cNumChildren));
    ExitOnFailure(hr, "Failed to get number of child nodes under Data/File");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Arp");

        if (0 == lstrcmpW(bstrElement, L"CfgFile"))
        {
            hr = ParseDataCfgFile(pProduct->rgFiles + pProduct->cFiles-1, pixnChildElement);
            ExitOnFailure(hr, "Failed to parse Data/File/CfgFile element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Data/File", bstrElement);
        }

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseObject(pixnChildElement);
    ReleaseObject(pixnlChildElements);
    ReleaseStr(sczLocation);
    ReleaseStr(sczName);
    ReleaseBSTR(bstrElement);

    return hr;
}

HRESULT ParseDataCfgFile(
    __inout LEGACY_FILE *pFile,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    DWORD cNumChildren = 0;
    BSTR bstrElement = NULL;
    LEGACY_FILE_SPECIAL *pFileSpecial = NULL;
    LEGACY_FILE_INI_INFO *pIniInfo = NULL;
    BOOL fExpectLocation = FALSE;
    LPWSTR sczEncoding = NULL;

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pFile->rgFileSpecials), pFile->cFileSpecials + 1, sizeof(LEGACY_FILE_SPECIAL), 5);
    ExitOnFailure(hr, "Failed to resize file specials array");
    pFileSpecial = pFile->rgFileSpecials + pFile->cFileSpecials;
    ++pFile->cFileSpecials;

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pFileSpecial->rgIniInfo), pFileSpecial->cIniInfo + 1, sizeof(LEGACY_FILE_INI_INFO), 5);
    ExitOnFailure(hr, "Failed to resize cfg file info array");
    pIniInfo = pFileSpecial->rgIniInfo + pFileSpecial->cIniInfo;
    ++pFileSpecial->cIniInfo;

    hr = XmlGetAttributeEx(pixnElement, L"Namespace", &pIniInfo->sczNamespace);
    ExitOnFailure(hr, "Failed to get Namespace attribute from CfgFile element");

    hr = XmlGetAttributeEx(pixnElement, L"Encoding", &sczEncoding);
    ExitOnFailure(hr, "Failed to get Encoding attribute from CfgFile element");

    hr = PersistedEncodingFromAttribute(sczEncoding, &pIniInfo->fetManifestEncoding);
    ExitOnFailure(hr, "Failed to parse Encoding attribute from CfgFile element");

    if (pFile->legacyFileType == LEGACY_FILE_DIRECTORY)
    {
        fExpectLocation = TRUE;
    }

    hr = XmlGetAttributeEx(pixnElement, L"Location", &pFileSpecial->sczLocation);
    if (fExpectLocation)
    {
        ExitOnFailure(hr, "Failed to get Location attribute from CfgFile element");
    }
    else
    {
        if (E_NOTFOUND != hr)
        {
            if (SUCCEEDED(hr))
            {
                hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
                ExitOnFailure(hr, "Unnecessary Location attribute specified!");
            }
            else
            {
                ExitOnFailure(hr, "Failed to get Location attribute from CfgFile element");
            }
        }
    }

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlChildElements->get_length(reinterpret_cast<long*>(&cNumChildren));
    ExitOnFailure(hr, "Failed to get number of child nodes under Data/File");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Arp");

        if (0 == lstrcmpW(bstrElement, L"SectionOpenTag"))
        {
            hr = XmlGetAttributeEx(pixnChildElement, L"Prefix", &pIniInfo->sczSectionPrefix);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get section prefix attribute from CfgFile/Section element");

            hr = XmlGetAttributeEx(pixnChildElement, L"Postfix", &pIniInfo->sczSectionPostfix);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get section postfix attribute from CfgFile element");
        }
        else if (0 == lstrcmpW(bstrElement, L"Value"))
        {
            hr = ParseDataCfgFileValue(pIniInfo, pixnChildElement);
            ExitOnFailure(hr, "Failed to parse CfgFile/Value element");
        }
        else if (0 == lstrcmpW(bstrElement, L"Comment"))
        {
            hr = XmlGetAttributeEx(pixnChildElement, L"Prefix", &pIniInfo->sczCommentPrefix);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get prefix attribute from CfgFile/Comment element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Data/File", bstrElement);
        }

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseStr(sczEncoding);
    ReleaseObject(pixnChildElement);
    ReleaseObject(pixnlChildElements);
    ReleaseBSTR(bstrElement);

    return hr;
}

HRESULT ParseDataCfgFileValue(
    __inout LEGACY_FILE_INI_INFO *pIniInfo,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    BSTR bstrElement = NULL;
    IXMLDOMNodeList* pixnlChildElements = NULL;
    IXMLDOMNode* pixnChildElement = NULL;
    DWORD cNumChildren = 0;

    hr = XmlGetAttributeEx(pixnElement, L"Separator", &pIniInfo->sczValueSeparator);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get value separator attribute from CfgFile/Value element");

    hr = XmlGetAttributeEx(pixnElement, L"Prefix", &pIniInfo->sczValuePrefix);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get value prefix attribute from CfgFile/Value element");

    hr = XmlSelectNodes(pixnElement, L"*", &pixnlChildElements);
    ExitOnFailure(hr, "Failed to select detection elements");

    hr = pixnlChildElements->get_length(reinterpret_cast<long*>(&cNumChildren));
    ExitOnFailure(hr, "Failed to get number of child nodes under Data/File");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlChildElements, &pixnChildElement, &bstrElement);
        if (S_FALSE == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next child element while going through list of elements under Detect/Arp");

        if (0 == lstrcmpW(bstrElement, L"SeparatorException"))
        {
            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pIniInfo->rgsczValueSeparatorException), pIniInfo->cValueSeparatorException + 1, sizeof(LPWSTR), 5);
            ExitOnFailure(hr, "Failed to resize ValueSeparatorException array");
        
            hr = XmlGetAttributeEx(pixnChildElement, L"Prefix", &pIniInfo->rgsczValueSeparatorException[pIniInfo->cValueSeparatorException]);
            ExitOnFailure(hr, "Failed to get section prefix attribute from CfgFile/Section element");
            ++pIniInfo->cValueSeparatorException;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under CfgFile/Value", bstrElement);
        }

        ReleaseNullObject(pixnChildElement);
    }

LExit:
    ReleaseObject(pixnChildElement);
    ReleaseObject(pixnlChildElements);
    ReleaseBSTR(bstrElement);

    return hr;
}

HRESULT ParseData(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    BSTR bstrElement = NULL;

    hr = XmlSelectNodes(pixnProductElement, L"Data/*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select data elements");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through data list");

        if (0 == lstrcmpW(bstrElement, L"RegistryKey"))
        {
            hr = ParseDataRegistryKey(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Data/RegistryKey element");
        }
        else if (0 == lstrcmpW(bstrElement, L"Directory"))
        {
            hr = ParseDataDirectory(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Data/Directory element");
        }
        else if (0 == lstrcmpW(bstrElement, L"File"))
        {
            hr = ParseDataFile(pProduct, pixnNode);
            ExitOnFailure(hr, "Failed to parse Data/File element");
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Data", bstrElement);
        }

        ReleaseNullObject(pixnNode);
    }
LExit:
    ReleaseBSTR(bstrElement);
    ReleaseObject(pixnlNodes);
    ReleaseObject(pixnNode);

    return hr;
}

HRESULT ParseFilter(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnProductElement
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    BSTR bstrElement = NULL;
    LPWSTR sczExactName = NULL;
    LPWSTR sczPrefix = NULL;
    LPWSTR sczPostfix = NULL;

    hr = XmlSelectNodes(pixnProductElement, L"Filter/*", &pixnlNodes);
    ExitOnFailure(hr, "Failed to select filter elements");

    for (;;)
    {
        ReleaseNullBSTR(bstrElement);
        hr = XmlNextElement(pixnlNodes, &pixnNode, &bstrElement);
        if (S_FALSE == hr || NULL == bstrElement)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next element while going through data list");

        if (0 != lstrcmpW(bstrElement, L"Ignore") && 0 != lstrcmpW(bstrElement, L"Normal"))
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Unknown element '%ls' found under Filter element", bstrElement);
        }

        hr = XmlGetAttributeEx(pixnNode, L"Name", &sczExactName);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get filter name");

        hr = XmlGetAttributeEx(pixnNode, L"Prefix", &sczPrefix);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get filter prefix");

        hr = XmlGetAttributeEx(pixnNode, L"Postfix", &sczPostfix);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get filter postfix");

        if (NULL != sczExactName && (NULL != sczPrefix || NULL != sczPostfix))
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            ExitOnFailure(hr, "Can't specify both a name and a (prefix or postfix) to a '%ls' element!", bstrElement);
        }

        hr = MemEnsureArraySize(reinterpret_cast<void **>(&pProduct->rgFilters), pProduct->cFilters + 1, sizeof(LEGACY_VALUE_FILTER), 5);
        ExitOnFailure(hr, "Failed to resize filter array");
        ++pProduct->cFilters;

        if (0 == lstrcmpW(bstrElement, L"Normal"))
        {
            hr = XmlGetYesNoAttribute(pixnNode, L"ShareWrite", &pProduct->rgFilters[pProduct->cFilters-1].fShareWriteOnRead);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get Normal/@ShareWrite attribute");
        }
        else if (0 == lstrcmpW(bstrElement, L"Ignore"))
        {
            pProduct->rgFilters[pProduct->cFilters-1].fIgnore = TRUE;
        }

        pProduct->rgFilters[pProduct->cFilters-1].sczExactName = sczExactName;
        sczExactName = NULL;
        pProduct->rgFilters[pProduct->cFilters-1].sczPrefix = sczPrefix;
        sczPrefix = NULL;
        pProduct->rgFilters[pProduct->cFilters-1].sczPostfix = sczPostfix;
        sczPostfix = NULL;

        ReleaseNullObject(pixnNode);
    }
LExit:
    ReleaseStr(sczExactName);
    ReleaseStr(sczPrefix);
    ReleaseStr(sczPostfix);
    ReleaseBSTR(bstrElement);
    ReleaseObject(pixnlNodes);
    ReleaseObject(pixnNode);

    return hr;
}

HRESULT ParseDisplayName(
    __inout LEGACY_PRODUCT *pProduct,
    __in IXMLDOMNode *pixnElement
    )
{
    HRESULT hr = S_OK;
    DWORD dwLCID = 0;
    BSTR bstrText = NULL;

    hr = XmlGetAttributeNumber(pixnElement, L"LCID", &dwLCID);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to get LCID attribute from DisplayName element");

    hr = XmlGetText(pixnElement, &bstrText);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to get text from DisplayName element");

    ++pProduct->cDisplayNames;
    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pProduct->rgDisplayNames), pProduct->cDisplayNames, sizeof(DISPLAY_NAME), 5);
    ExitOnFailure(hr, "Failed to resize displayname array");

    pProduct->rgDisplayNames[pProduct->cDisplayNames-1].dwLCID = dwLCID;

    hr = StrAllocString(&(pProduct->rgDisplayNames[pProduct->cDisplayNames-1].sczName), bstrText, 0);
    ExitOnFailure(hr, "Failed to allocate displayname string");

LExit:
    ReleaseBSTR(bstrText);

    return hr;
}

static HRESULT PersistedEncodingFromAttribute(
    __in_z LPCWSTR wzEncoding,
    __out PERSISTED_FILE_ENCODING_TYPE *pfetEncoding
    )
{
    HRESULT hr = S_OK;

    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzEncoding, -1, L"UTF8NoBOM", -1))
    {
        *pfetEncoding = PERSISTED_FILE_ENCODING_UTF8;
        ExitFunction1(hr = S_OK);
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzEncoding, -1, L"UTF8", -1))
    {
        *pfetEncoding = PERSISTED_FILE_ENCODING_UTF8_WITH_BOM;
        ExitFunction1(hr = S_OK);
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzEncoding, -1, L"UTF16NoBOM", -1))
    {
        *pfetEncoding = PERSISTED_FILE_ENCODING_UTF16;
        ExitFunction1(hr = S_OK);
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzEncoding, -1, L"UTF16", -1))
    {
        *pfetEncoding = PERSISTED_FILE_ENCODING_UTF16_WITH_BOM;
        ExitFunction1(hr = S_OK);
    }
    else
    {
        *pfetEncoding = PERSISTED_FILE_ENCODING_UNSPECIFIED;

        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        ExitOnFailure(hr, "Bad encoding attribute found - encodings supported are: \"UTF8NoBOM\", \"UTF8\", \"UTF16NoBOM\", \"UTF16\". To support ANSI files, specify \"UTF8NoBOM\".");
    }

LExit:
    return hr;
}

static HRESULT AddLegacyRegistrySpecialCfgValuesToDict(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __inout STRINGDICT_HANDLE *pshRegistrySpecials
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCfgValueName = NULL;

    // Add cfg values seen in flags
    for (DWORD i = 0; i < pRegKeySpecial->cFlagsInfo; ++i)
    {
        hr = StrAllocFormatted(&sczCfgValueName, L"%ls%wc%ls", pRegKey->sczNamespace, NAMESPACE_DELIMITER_CHARACTER, pRegKeySpecial->rgFlagsInfo[i].sczCfgValueName);
        ExitOnFailure(hr, "Failed to format cfg value name with namespace");

        hr = DictAddKey(*pshRegistrySpecials, sczCfgValueName);
        ExitOnFailure(hr, "Failed to add cfg value to list of specially-handled cfg values (flags): %ls", pRegKeySpecial->rgFlagsInfo[i].sczCfgValueName);
    }

    hr = MapRegSpecialToCfgValueName(pRegKey, pRegKeySpecial, &sczCfgValueName);
    ExitOnFailure(hr, "Failed to map registry value name to cfg value name");

    hr = DictAddKey(*pshRegistrySpecials, sczCfgValueName);
    ExitOnFailure(hr, "Failed to add cfg value to list of specially-handled cfg values (basic special): %ls", sczCfgValueName);

LExit:
    ReleaseStr(sczCfgValueName);

    return hr;
}
