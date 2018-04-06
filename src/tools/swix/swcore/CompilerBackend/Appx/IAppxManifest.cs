// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Schema;

    /// <summary>
    /// Temporary interface to allow backend compiler to swap out manifest
    /// objects from different milestones.
    /// </summary>
    interface IAppxManifest
    {
        List<PackageFile> Files { get; }

        void ProcessIntermediates(IEnumerable<Intermediate> intermediates);

        void Validate(ValidationEventHandler eventHandler);

        Stream GetStream();
    }
}
