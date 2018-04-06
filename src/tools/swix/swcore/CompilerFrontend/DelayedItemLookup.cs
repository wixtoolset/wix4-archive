// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Reflection;
    using WixToolset.Simplified.Lexicon;

    /// <summary>
    /// Allows string "lookup" values to be resolved to package items at a later time during the compile process.
    /// </summary>
    internal class DelayedItemLookup
    {
        private PackageItem targetItem;
        private PropertyInfo targetProperty;
        private Type targetType;

        /// <summary>
        /// Creates a delayed lookup that assigns the result to a property on the target item.
        /// </summary>
        /// <param name="lookup">String to resolve later.</param>
        /// <param name="targetItem">Target item to assign result.</param>
        /// <param name="targetProperty">Property on target item to assign result.</param>
        public DelayedItemLookup(string lookup, PackageItem targetItem, PropertyInfo targetProperty)
        {
            this.Lookup = lookup;
            this.targetItem = targetItem;
            this.targetProperty = targetProperty;
            this.targetType = targetProperty.PropertyType;
        }

        /// <summary>
        /// Creates a delayed lookup whose result can be used by an attached property.
        /// </summary>
        /// <param name="lookup">String to resolve later.</param>
        /// <param name="targetItem">Target item that is related to the result.</param>
        /// <param name="targetType">Type the result must resolve to.</param>
        public DelayedItemLookup(string lookup, PackageItem targetItem, Type targetType)
        {
            this.Lookup = lookup;
            this.targetItem = targetItem;
            this.targetType = targetType;
        }

        public string Lookup { get; set; }

        public PackageItem ResolvedItem { get; private set; }

        /// <summary>
        /// Resolves the lookup.
        /// </summary>
        /// <param name="context">Frontend compiler containing all the objects that may satisfy the resolution.</param>
        public void Resolve(FrontendCompiler context)
        {
            this.Resolve(context, false);
        }

        /// <summary>
        /// Resolves the lookup but may not fail if missing.
        /// </summary>
        /// <param name="context">Frontend compiler containing all the objects that may satisfy the resolution.</param>
        /// <param name="allowUnresolved">Flag specifying whether to allow unresolved items to be returned.</param>
        /// <returns>Item resolved.</returns>
        public PackageItem Resolve(FrontendCompiler context, bool allowUnresolved)
        {
            if (this.ResolvedItem == null)
            {
                string targetPropertyName = this.targetProperty == null ? String.Empty : this.targetProperty.Name;
                CompilerMessage message = null;

                PackageItem item = null;
                if (context.TryGetItemById(this.Lookup, out item))
                {
                    if (this.targetType.IsInstanceOfType(item))
                    {
                        if (this.targetProperty != null)
                        {
                            this.SetTarget(context, item);
                        }
                    }
                    else if (!allowUnresolved)
                    {
                        message = CompilerMessage.InvalidIdResolution(
                            targetPropertyName,
                            this.Lookup,
                            this.targetType.Name,
                            item.GetType().Name);
                    }
                }
                else if (this.targetType.IsSubclassOf(typeof(FileSystemResource)) || this.targetType == typeof(IFileReference))
                {
                    Resource resource;
                    FileSystemResourceManager manager = (FileSystemResourceManager)context.GetService(typeof(FileSystemResourceManager));
                    if (manager.TryFindResourceByPath(context, this.Lookup, out resource))
                    {
                        if (this.targetType.IsInstanceOfType(resource))
                        {
                            if (this.targetProperty != null)
                            {
                                item = this.SetTarget(context, resource);
                            }
                            else
                            {
                                item = resource;
                            }
                        }
                        else
                        {
                            message = CompilerMessage.InvalidIdResolution(
                                targetPropertyName,
                                this.Lookup,
                                this.targetType.Name,
                                resource.GetType().Name);
                        }
                    }
                    else if (!allowUnresolved)
                    {
                        message = CompilerMessage.UnknownFileSystemResolution(
                            targetPropertyName,
                            this.Lookup,
                            this.targetType.Name);
                    }
                }
                else if (!allowUnresolved)
                {
                    message = CompilerMessage.UnknownIdResolution(
                        targetPropertyName,
                        this.Lookup,
                        this.targetType.Name);
                }

                if (message != null)
                {
                    context.OnMessage(new CompilerMessageEventArgs(message, this.targetItem));
                }

                this.ResolvedItem = item;
            }

            return this.ResolvedItem;
        }

        private PackageItem SetTarget(FrontendCompiler context, PackageItem item)
        {
            if (this.targetProperty.GetValue(this.targetItem, null) != null)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.OverwritingImplicitProperty(this.targetProperty.Name, this.Lookup), this.targetItem));
            }

            this.targetProperty.SetValue(this.targetItem, item, null);
            return item;
        }
    }
}
