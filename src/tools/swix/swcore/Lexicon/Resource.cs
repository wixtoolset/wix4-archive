// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for all resource package items.
    /// </summary>
    public abstract class Resource : PackageItem
    {
        public string Path { get; private set; }

        internal void SetPath(string path)
        {
            if (String.IsNullOrEmpty(this.Path))
            {
                this.Path = path;
            }
            else
            {
                CompilerException.ThrowInternalError("Path was already set on the Resource. Resource.SetPath() cannot be called when the Path has already been set. Attempting to overwrite path: '{0}' with path: '{1}'", this.Path, path);
            }
        }

        // REVIEW: Should we have an implementation here at all?
        public override IEnumerable<string> GetReferenceNames()
        {
            if (!String.IsNullOrEmpty(this.Id))
            {
                yield return String.Concat(this.Id, ":");
            }

            // REVIEW: Do we need to prepend the reference names from the parent?
            if (!String.IsNullOrEmpty(this.Path))
            {
                yield return this.Path;
            }
        }
    }
}
