// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;

    /// <summary>
    /// List of supported capabilities.
    /// </summary>
    public enum CapabilityName
    {
        documentsLibrary,
        enterpriseAuthentication,
        internetClient,
        internetClientServer,
        musicLibrary,
        picturesLibrary,
        privateNetworkClientServer,
        removableStorage,
        sharedUserCertificates,
        videosLibrary,
    };

    /// <summary>
    /// Specifies a capability is required.
    /// </summary>
    public class Capability : PackageItem
    {
        public CapabilityName Name { get; set; }
    }
}
