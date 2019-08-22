// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Markup;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// File association for one or more file types.
    /// </summary>
    [DefaultCollectionProperty("SupportedFileTypes")]
    public class FileAssociation : PackageItemTargetsFile
    {
        public FileAssociation()
        {
            this.SupportedFileTypes = new List<FileType>();
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string Implementation { get; set; }

        public bool OpenIsSafe { get; set; }

        public bool AlwaysUnsafe { get; set; }

        [TypeConverter(typeof(IdTypeConverter))]
        public File Image { get; set; }

        public List<FileType> SupportedFileTypes { get; private set; }
    }
}
