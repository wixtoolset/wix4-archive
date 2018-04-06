// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
