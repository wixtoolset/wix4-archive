//-------------------------------------------------------------------------------------------------
// <copyright file="FileFacade.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind.Databases
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    public class FileFacade
    {
        public FileFacade(FileRow file, WixFileRow wixFile, WixDeltaPatchFileRow deltaPatchFile)
        {
            this.File = file;
            this.WixFile = wixFile;
            this.DeltaPatchFile = deltaPatchFile;
        }

        public FileFacade(bool fromModule, FileRow file, WixFileRow wixFile)
        {
            this.FromModule = fromModule;
            this.File = file;
            this.WixFile = wixFile;
        }

        public bool FromModule { get; private set; }

        public FileRow File { get; private set; }

        public WixFileRow WixFile { get; private set; }

        public WixDeltaPatchFileRow DeltaPatchFile { get; private set; }

        /// <summary>
        /// Gets the set of MsiAssemblyName rows created for this file.
        /// </summary>
        /// <value>RowCollection of MsiAssemblyName table.</value>
        public List<Row> AssemblyNames { get; set; }

        /// <summary>
        /// Gets or sets the MsiFileHash row for this file.
        /// </summary>
        public Row Hash { get; set; }
    }
}
