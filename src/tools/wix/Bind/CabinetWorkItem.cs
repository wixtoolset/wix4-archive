//-------------------------------------------------------------------------------------------------
// <copyright file="CabinetWorkItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// A cabinet builder work item.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// A cabinet builder work item.
    /// </summary>
    internal sealed class CabinetWorkItem
    {
        private string cabinetFile;
        private CompressionLevel compressionLevel;
        //private BinderFileManager binderFileManager;
        private int maxThreshold;

        /// <summary>
        /// Instantiate a new CabinetWorkItem.
        /// </summary>
        /// <param name="fileRows">The collection of files in this cabinet.</param>
        /// <param name="cabinetFile">The cabinet file.</param>
        /// <param name="maxThreshold">Maximum threshold for each cabinet.</param>
        /// <param name="compressionLevel">The compression level of the cabinet.</param>
        /// <param name="binderFileManager">The binder file manager.</param>
        public CabinetWorkItem(IEnumerable<FileRow> fileRows, string cabinetFile, int maxThreshold, CompressionLevel compressionLevel /*, BinderFileManager binderFileManager*/)
        {
            this.cabinetFile = cabinetFile;
            this.compressionLevel = compressionLevel;
            this.FileRows = fileRows;
            //this.binderFileManager = binderFileManager;
            this.maxThreshold = maxThreshold;
        }

        /// <summary>
        /// Gets the cabinet file.
        /// </summary>
        /// <value>The cabinet file.</value>
        public string CabinetFile
        {
            get { return this.cabinetFile; }
        }

        /// <summary>
        /// Gets the compression level of the cabinet.
        /// </summary>
        /// <value>The compression level of the cabinet.</value>
        public CompressionLevel CompressionLevel
        {
            get { return this.compressionLevel; }
        }

        /// <summary>
        /// Gets the collection of files in this cabinet.
        /// </summary>
        /// <value>The collection of files in this cabinet.</value>
        public IEnumerable<FileRow> FileRows { get; private set; }

        /// <summary>
        /// Gets the binder file manager.
        /// </summary>
        /// <value>The binder file manager.</value>
        //public BinderFileManager BinderFileManager
        //{
        //    get { return this.binderFileManager; }
        //}

        /// <summary>
        /// Gets the max threshold.
        /// </summary>
        /// <value>The maximum threshold for a folder in a cabinet.</value>
        public int MaxThreshold
        {
            get { return this.maxThreshold; }
        }
    }
}
