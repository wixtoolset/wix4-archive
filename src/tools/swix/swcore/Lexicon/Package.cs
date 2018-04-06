// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// The `package` element contains the information that identifies the resulting
    /// MSI or AppX file. It also has display information for the end user to
    /// recognize the package and its manufacturer.
    /// 
    /// Parent: none
    /// 
    /// Children: none
    /// </summary>
    public class Package : PackageItem
    {
        /// <summary>
        /// `about` - URL for more information about the package, For example,
        /// `about="http://wixtoolset.org/"`
        /// </summary>
        public string About { get; set; }

        /// <summary>
        /// `copyright` - copyright information for the package, For example,
        /// `copyright=".NET Foundation and contributors"`
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// `displayName` - human readable name for the package. For example,
        /// `displayName="The WiX Toolset"`
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// `description` - human readable description for the package. For example,
        /// `description="Set of tools used to create MSI and AppX packages."`
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// `framework` - boolean value indicating whether the package is a framework.
        /// In AppX packages this creates a Framework package and in MSI packages this
        /// sets ARPSYSTEMCOMPONENT=1 so the package will not show up in Add/Remove Programs.
        /// For example, `framework=true`
        /// </summary>
        public bool Framework { get; set; }

        /// <summary>
        /// `image` - reference to a `file` that will be displayed to the end user. For
        /// example, `image=wixlogo.png`
        /// </summary>
        public QualifiedFile Image { get; set; }

        /// <summary>
        /// `license` - url to license information for the package. For example,
        /// `license="http://wixtoolset.org/about/license/"`
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// `manufacturer` - human readable company name that creates the package.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// `name` - unique name for the package. Typically namespaced by the company
        /// name. For example, `name=WixToolset.Simplified`
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// `publisher` - subject identifier of the certificate used to sign the package.
        /// This attribute is optional. If the signing certificate file is provided via the
        /// command-line, the compiler will set this attribute automatically. For example,
        /// `publisher="CN=Microsoft\O=Microsoft Corp.\L=Redmond\S=WA\C=US"`
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// * `version` - multi-part version of the package. Note that AppX and VSIX packages
        /// may use four part versions but MSI packages only support the first three parts.
        /// For example, `version=1.2.3`
        /// </summary>
        [TypeConverter(typeof(VersionTypeConverter))]
        public Version Version { get; set; }

        protected override void OnResolveEnd(FrontendCompiler context)
        {
            if (this.Image != null)
            {
                this.Image.ResolveFiles(context, this);
            }
        }
    }
}
