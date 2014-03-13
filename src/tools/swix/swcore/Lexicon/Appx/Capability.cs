//-------------------------------------------------------------------------------------------------
// <copyright file="Capability.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
