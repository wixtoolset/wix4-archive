// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Msi
{
    using System.Collections.Generic;

    public enum NgenExecuteType
    {
        idle,
        immediate,
        asynchronous,
    }

    public static class Ngen
    {
        private static Dictionary<File, NgenPackageItem> ngens = new Dictionary<File, NgenPackageItem>();

        public static void SetExecute(File file, NgenExecuteType execute)
        {
            NgenPackageItem item = Ngen.CreatePackageItem(file);
            item.Execute = execute;
        }

        public static NgenExecuteType? GetExecute(File file)
        {
            NgenExecuteType? execute = null;

            //NgenItem item;
            //if (Ngen.ngens.TryGetValue(file, out item))
            //{
            //    execute = item.Execute;
            //}

            return execute;
        }

        public static void SetApplication(File file, IFileReference application)
        {
            NgenPackageItem item = Ngen.CreatePackageItem(file);
            item.Application = application;
        }

        public static File GetApplication(File file)
        {
            File app = null;

            //NgenItem item;
            //if (Ngen.ngens.TryGetValue(file, out item) && item.Application == null)
            //{
            //    DelayedItemLookup delayedItem;
            //    if (file.DelayedLookup.TryGetValue("WixToolset.Simplified.Lexicon.Ngen.SetApp()", out delayedItem))
            //    {
            //        app = item.Application = (File)delayedItem.ResolvedItem;
            //    }
            //}

            return app;
        }

        public static void SetFolder(File file, Folder folder)
        {
            NgenPackageItem item = Ngen.CreatePackageItem(file);
            item.Folder = folder;
        }

        public static Folder GetFolder(File file)
        {
            Folder folder = null;

            //NgenItem item;
            //if (Ngen.ngens.TryGetValue(file, out item) && item.Folder == null)
            //{
            //    DelayedItemLookup delayedItem;
            //    if (file.DelayedLookup.TryGetValue("WixToolset.Simplified.Lexicon.Ngen.SetFolder()", out delayedItem))
            //    {
            //        folder = item.Folder = (Folder)delayedItem.ResolvedItem;
            //    }
            //}

            return folder;
        }

        internal static NgenPackageItem CreatePackageItem(File file)
        {
            NgenPackageItem item = null;
            if (!Ngen.ngens.TryGetValue(file, out item))
            {
                item = new NgenPackageItem() { File = file, Parent = file };
                Ngen.ngens.Add(file, item);
            }

            return item;
        }

        internal static bool TryGetPackageItem(File file, out NgenPackageItem ngen)
        {
            return Ngen.ngens.TryGetValue(file, out ngen);
        }
    }
}
