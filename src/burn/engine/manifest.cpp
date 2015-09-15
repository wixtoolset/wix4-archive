//-------------------------------------------------------------------------------------------------
// <copyright file="manifest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// internal function declarations

static HRESULT ParseContainersFromXml(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINERS* pContainers,
    __in IXMLDOMNode* pixnBundle
    );


// function definitions

extern "C" HRESULT ManifestLoadXmlFromBuffer(
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdDocument = NULL;
    IXMLDOMElement* pixeBundle = NULL;
    IXMLDOMNode* pixnLog = NULL;
    IXMLDOMNode* pixnChain = NULL;

    // load xml document
    hr = XmlLoadDocumentFromBuffer(pbBuffer, cbBuffer, &pixdDocument);
    ExitOnFailure(hr, "Failed to load manifest as XML document.");

    // get bundle element
    hr = pixdDocument->get_documentElement(&pixeBundle);
    ExitOnFailure(hr, "Failed to get bundle element.");

    // parse the log element, if present.
    hr = XmlSelectSingleNode(pixeBundle, L"Log", &pixnLog);
    ExitOnFailure(hr, "Failed to get Log element.");

    if (S_OK == hr)
    {
        hr = XmlGetAttributeEx(pixnLog, L"PathVariable", &pEngineState->log.sczPathVariable);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Log/@PathVariable.");
        }

        hr = XmlGetAttributeEx(pixnLog, L"Prefix", &pEngineState->log.sczPrefix);
        ExitOnFailure(hr, "Failed to get Log/@Prefix attribute.");

        hr = XmlGetAttributeEx(pixnLog, L"Extension", &pEngineState->log.sczExtension);
        ExitOnFailure(hr, "Failed to get Log/@Extension attribute.");
    }

    // get the chain element
    hr = XmlSelectSingleNode(pixeBundle, L"Chain", &pixnChain);
    ExitOnFailure(hr, "Failed to get chain element.");

    if (S_OK == hr)
    {
        // parse disable rollback
        hr = XmlGetYesNoAttribute(pixnChain, L"DisableRollback", &pEngineState->fDisableRollback);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@DisableRollback");
        }

        // parse disable system restore
        hr = XmlGetYesNoAttribute(pixnChain, L"DisableSystemRestore", &pEngineState->fDisableSystemRestore);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@DisableSystemRestore");
        }

        // parse parallel cache
        hr = XmlGetYesNoAttribute(pixnChain, L"ParallelCache", &pEngineState->fParallelCacheAndExecute);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@ParallelCache");
        }
    }

    // parse built-in condition 
    hr = ConditionGlobalParseFromXml(&pEngineState->condition, pixeBundle);
    ExitOnFailure(hr, "Failed to parse global condition.");

    // parse variables
    hr = VariablesParseFromXml(&pEngineState->variables, pixeBundle);
    ExitOnFailure(hr, "Failed to parse variables.");

    // parse searches
    hr = SearchesParseFromXml(&pEngineState->searches, pixeBundle); // TODO: Modularization
    ExitOnFailure(hr, "Failed to parse searches.");

    // parse user experience
    hr = UserExperienceParseFromXml(&pEngineState->userExperience, pixeBundle);
    ExitOnFailure(hr, "Failed to parse user experience.");

    // parse catalog files
    hr = CatalogsParseFromXml(&pEngineState->catalogs, pixeBundle);
    ExitOnFailure(hr, "Failed to parse catalog files.");

    // parse registration
    hr = RegistrationParseFromXml(&pEngineState->registration, pixeBundle);
    ExitOnFailure(hr, "Failed to parse registration.");

    // parse update
    hr = UpdateParseFromXml(&pEngineState->update, pixeBundle);
    ExitOnFailure(hr, "Failed to parse update.");

    // parse boxes
    hr = ParseContainersFromXml(&pEngineState->section, &pEngineState->containers, pixeBundle);
    ExitOnFailure(hr, "Failed to parse containers.");

    // parse payloads
    hr = PayloadsParseFromXml(&pEngineState->payloads, &pEngineState->containers, &pEngineState->catalogs, pixeBundle);
    ExitOnFailure(hr, "Failed to parse payloads.");

    // parse packages
    hr = PackagesParseFromXml(&pEngineState->packages, &pEngineState->payloads, pixeBundle);
    ExitOnFailure(hr, "Failed to parse packages.");

    // parse approved exes for elevation
    hr = ApprovedExesParseFromXml(&pEngineState->approvedExes, pixeBundle);
    ExitOnFailure(hr, "Failed to parse approved exes.");

LExit:
    ReleaseObject(pixnChain);
    ReleaseObject(pixnLog);
    ReleaseObject(pixeBundle);
    ReleaseObject(pixdDocument);
    return hr;
}

static HRESULT ParseContainersFromXml(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINERS* pContainers,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select container nodes
    hr = XmlSelectNodes(pixnBundle, L"Container", &pixnNodes);
    ExitOnFailure(hr, "Failed to select container nodes.");

    // get container node count
    hr = pixnNodes->get_length((long*) &cNodes);
    ExitOnFailure(hr, "Failed to get container node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for searches
    pContainers->rgBoxes = (WIX_BOX*) MemAlloc(sizeof(WIX_BOX) * cNodes, TRUE);
    ExitOnNull(pContainers->rgBoxes, hr, E_OUTOFMEMORY, "Failed to allocate memory for container structs.");

    pContainers->cBoxes = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        WIX_BOX* pBox = &pContainers->rgBoxes[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pBox->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Primary
        hr = XmlGetYesNoAttribute(pixnNode, L"Primary", &pBox->fPrimary);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Primary.");
        }

        // @Attached
        hr = XmlGetYesNoAttribute(pixnNode, L"Attached", &pBox->fAttached);
        if (E_NOTFOUND != hr || pBox->fPrimary) // if it is a primary container, it has to be attached
        {
            ExitOnFailure(hr, "Failed to get @Attached.");
        }

        // @AttachedIndex
        hr = XmlGetAttributeNumber(pixnNode, L"AttachedIndex", &pBox->dwAttachedIndex);
        if (E_NOTFOUND != hr || pBox->fAttached) // if it is an attached container it must have an index
        {
            ExitOnFailure(hr, "Failed to get @AttachedIndex.");
        }

        // @FilePath
        hr = XmlGetAttributeEx(pixnNode, L"FilePath", &pBox->sczFilePath);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @FilePath.");
        }

        // The source path starts as the file path.
        hr = StrAllocString(&pBox->sczSourcePath, pBox->sczFilePath, 0);
        ExitOnFailure(hr, "Failed to copy @FilePath");

        // @DownloadUrl
        hr = XmlGetAttributeEx(pixnNode, L"DownloadUrl", &pBox->downloadSource.sczUrl);
        if (E_NOTFOUND != hr || (!pBox->fPrimary && !pBox->sczSourcePath)) // if the package is not a primary package, it must have a source path or a download url
        {
            ExitOnFailure(hr, "Failed to get @DownloadUrl. Either @SourcePath or @DownloadUrl needs to be provided.");
        }

        // @Hash
        hr = XmlGetAttributeEx(pixnNode, L"Hash", &pBox->sczHash);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocHexDecode(pBox->sczHash, &pBox->pbHash, &pBox->cbHash);
            ExitOnFailure(hr, "Failed to hex decode the Container/@Hash.");
        }
        else if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Hash.");
        }

        // If the container is attached, make sure the information in the section matches what the
        // manifest contained and get the offset to the container.
        if (pBox->fAttached)
        {
            hr = SectionGetAttachedContainerInfo(pSection, pBox->dwAttachedIndex, &pBox->qwAttachedOffset, &pBox->qwFileSize, &pBox->fActuallyAttached);
            ExitOnFailure(hr, "Failed to get attached container information.");
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}
