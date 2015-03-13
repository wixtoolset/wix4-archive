//-------------------------------------------------------------------------------------------------
// <copyright file="FilePathTaskItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Internal implementation of MSBuild task item so the command-line can provide the same type of inputs as MSBuild.
    /// </summary>
    internal class FilePathTaskItem : ITaskItem
    {
        private Dictionary<string, string> metadata;

        public FilePathTaskItem(string path)
        {
            this.metadata = new Dictionary<string, string>();
            this.ItemSpec = path;
        }

        public IDictionary CloneCustomMetadata()
        {
            return new Dictionary<string, string>(this.metadata);
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
        }

        public string GetMetadata(string metadataName)
        {
            string value = String.Empty;
            this.metadata.TryGetValue(metadataName, out value);
            return value;
        }

        public string ItemSpec { get; set; }

        public int MetadataCount
        {
            get { return this.metadata.Count; }
        }

        public ICollection MetadataNames
        {
            get { return this.metadata.Keys; }
        }

        public void RemoveMetadata(string metadataName)
        {
            if (this.metadata.ContainsKey(metadataName))
            {
                this.metadata.Remove(metadataName);
            }
        }

        public void SetMetadata(string metadataName, string metadataValue)
        {
            this.metadata.Add(metadataName, metadataValue);
        }
    }
}
