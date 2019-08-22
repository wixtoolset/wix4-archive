// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Simplified.Lexicon;
    using WixToolset.Simplified.Lexicon.Msi;
    using Regex = System.Text.RegularExpressions;

    internal static class WixBackendCompilerServices
    {
        public static readonly XNamespace WixlibNamespace = "http://wixtoolset.org/schemas/v4/wixlib";
        public static readonly XNamespace WixobjNamespace = "http://wixtoolset.org/schemas/v4/wixobj";
        public static readonly Version WixlibVersion = new Version("3.0.2002.0");

        public const int ComplexReferenceParentTypeComponentGroup = 2;
        public const int ComplexReferenceChildTypeComponent = 1;
        public const int ComplexReferenceChildTypeComponentGroup = 3;

        public const int MsidbComponentAttributesRegistryKeyPath = 4;
        public const int MsidbComponentAttributes64bit = 256;
        public const int MsidbFileAttributesVital = 512;

        private static readonly char[] TrimBase64 = new char[] { '=' };

        private const int MaxMsiIdLength = 72;
        private const string MsiIdSeparator = "_";
        private const string MsiIdInvalidCharacterReplacement = "_";
        private static readonly Regex.Regex InvalidMsiId = new Regex.Regex(@"[^A-Za-z0-9_\.]", Regex.RegexOptions.Compiled);

        private const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\]\. ]"; // illegal: \ ? | > < : / * " + , ; = [ ] . (space)
        private static readonly Regex.Regex LegalShortFilename = new Regex.Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"), Regex.RegexOptions.Compiled);

        public static string GenerateMsiCondition(WixItem item)
        {
            HashSet<WixItem> processedItems = new HashSet<WixItem>();
            string[] conditions = WixBackendCompilerServices.GenerateMsiConditions(item, processedItems);
            return conditions.Length == 0 ? null : String.Join(" AND ", conditions);
        }

        public static string GenerateMsiFileName(bool preserveExtension, string name, params string[] args)
        {
            if (WixBackendCompilerServices.LegalShortFilename.IsMatch(name))
            {
                return name;
            }
            else
            {
                string shortName = WixBackendCompilerServices.GenerateShortFileName(preserveExtension, name, args);
                return string.Concat(shortName, "|", name);
            }
        }

        public static string GenerateId(WixBackendCompiler backend, PackageItem item, string prefix, params string[] args)
        {
            // System items do not get suffixes. All other items do unless we're are building a completely
            // neutral output.
            string suffix = String.Empty;
            if (item != null && !item.System && (backend.Architecture != PackageArchitecture.Neutral || backend.Languages != null && backend.Languages.Length > 0))
            {
                suffix = WixBackendCompilerServices.MsiIdSeparator;

                if (backend.Architecture != PackageArchitecture.Neutral)
                {
                    suffix = String.Concat(suffix, backend.Architecture.ToString().ToLowerInvariant());
                }

                if (backend.Languages != null && backend.Languages.Length > 0)
                {
                    suffix = String.Concat(suffix, backend.Languages[0].ThreeLetterWindowsLanguageName.ToLowerInvariant());
                }
            }

            // It the item has an identifier, we'll use that. Otherwise, we'll to generate an identity.
            string id = item == null ? String.Empty : item.Id;
            if (String.IsNullOrEmpty(id))
            {
                if (args.Length == 0)
                {
                    // TODO: display error message.
                    return null;
                }

                // Generate a hash of the arguments and add the result to the suffix. We'll base64
                // encode the hash since that squishes the data down pretty well and we only need
                // to trim off some equal signs and change plus signs to dots to make it a valid MSI
                // identifier.
                byte[] data = Encoding.Unicode.GetBytes(String.Join("|", args));

                // Murmur hash creates very small hashes but is not quite unqiue enough to prevent collisions
                // in large installations where file paths may differ by only a single character. So, back to
                // SHA1 hashes to be safe.
                //uint murmur = MurmurHash3.Hash(data);
                byte[] hash; // BitConverter.GetBytes(murmur)
                using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                {
                    hash = sha1.ComputeHash(data);
                }

                string stringHash = Convert.ToBase64String(hash);
                stringHash = stringHash.TrimEnd(WixBackendCompilerServices.TrimBase64).Replace('+', '.').Replace('/', '_');

                suffix = String.Concat(WixBackendCompilerServices.MsiIdSeparator, stringHash, suffix);

                // If the prefix is too long, take only the first and last half (hopefully nothing useful
                // was in the middle).
                if (!String.IsNullOrEmpty(prefix))
                {
                    int maxLength = WixBackendCompilerServices.MaxMsiIdLength - suffix.Length;
                    if (prefix.Length > maxLength)
                    {
                        int half = maxLength / 2;
                        int extra = maxLength % 2 == 0 ? 1 : 0;
                        prefix = String.Concat(prefix.Substring(0, half), WixBackendCompilerServices.MsiIdSeparator, prefix.Substring(prefix.Length - half + extra));
                        Debug.Assert(prefix.Length == maxLength);
                    }

                    id = GenerateSafeMsiId(prefix);
                }
            }

            return String.Concat(id, suffix);
        }

        public static string GenerateIdForRegKey(WixBackendCompiler backend, PackageItem item, int root, string path, string name)
        {
            string stringRoot;
            switch (root)
            {
                case -1:
                    stringRoot = "HKMU";
                    break;
                case 0:
                    stringRoot = "HKCR";
                    break;
                case 1:
                    stringRoot = "HKCU";
                    break;
                case 2:
                    stringRoot = "HKLM";
                    break;
                case 3:
                    stringRoot = "HKU";
                    break;

                default:
                    throw new ArgumentException("root");
            }

            path = String.Concat(stringRoot, "\\", path, name ?? String.Empty);
            path = GenerateSafeMsiIdFromPath(path);
            return GenerateId(backend, item, path, path);
        }

        public static string GenerateMsiId(WixBackendCompiler backend, PackageItem item, string prefix, params string[] args)
        {
            string id = item.Id;
            if (String.IsNullOrEmpty(id))
            {
                if (args.Length == 0)
                {
                    // TODO: display error message.
                    return null;
                }

                string stringData = String.Join("|", args);
                byte[] data = Encoding.Unicode.GetBytes(stringData);

                // hash the data
                byte[] hash;
                using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                {
                    hash = sha1.ComputeHash(data);
                }

                // build up the identifier
                int prefixLength = String.IsNullOrEmpty(prefix) ? 0 : prefix.Length + 1;
                StringBuilder identifier = new StringBuilder(prefixLength + hash.Length * 2, prefixLength + hash.Length * 2);

                if (!String.IsNullOrEmpty(prefix))
                {
                    identifier.Append(prefix);
                    identifier.Append(WixBackendCompilerServices.MsiIdSeparator);
                }

                for (int i = 0; i < hash.Length; i++)
                {
                    identifier.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
                }

                id = identifier.ToString();
            }

            if (!item.System)
            {
                if (backend.Architecture != PackageArchitecture.Neutral)
                {
                    id = String.Concat(id, WixBackendCompilerServices.MsiIdSeparator, backend.Architecture.ToString().ToLowerInvariant());
                }

                if (backend.Languages != null && backend.Languages.Length > 0)
                {
                    id = String.Concat(id, WixBackendCompilerServices.MsiIdSeparator, backend.Languages[0].ThreeLetterWindowsLanguageName.ToLowerInvariant());
                }
            }

            return id;
        }

        public static string GenerateSafeMsiIdFromPath(string path)
        {
            return path.Trim(new char[] { '\\' }).Replace(" ", String.Empty).Replace(":", String.Empty).Replace('\\', '.');
        }

        public static string GenerateSafeMsiId(string id)
        {
            string safeId = WixBackendCompilerServices.InvalidMsiId.Replace(id, WixBackendCompilerServices.MsiIdInvalidCharacterReplacement);
            return safeId;
        }

        public static string GenerateMsiIdForFileReference(WixBackendCompiler backend, IFileReference fs)
        {
            string id = null;

            WixItem item = backend.WixItems[fs.GetPackageItem()];
            if (item.Item is Lexicon.File)
            {
                id = String.Concat("[#", item.MsiId, "]");
            }
            else if (item.Item is Lexicon.Msi.Property)
            {
                id = String.Concat("[", item.MsiId, "]");
            }
            else if (item.Item is Lexicon.Msi.FileSearch)
            {
                id = String.Concat("[", item.MsiId, "]");
            }

            return id;
        }

        public static XElement GenerateRow(WixSection section, string tableName, FileLineNumber typeStartLineNumber, params object[] fields)
        {
            string sourceLineNumbers = GenerateSourceLineNumbers(typeStartLineNumber);

            XElement row = new XElement(WixBackendCompilerServices.WixobjNamespace + "row",
                    new XAttribute("sourceLineNumber", sourceLineNumbers));

            XElement table = section.GetTable(tableName);
            table.Add(row);

            foreach (var field in fields)
            {
                row.Add(new XElement(WixBackendCompilerServices.WixobjNamespace + "field", field));
            }

            return row;
        }

        public static XElement GenerateSimpleReference(WixSection section, string tableName, FileLineNumber typeStartLineNumber, params string[] primaryKeys)
        {
            XElement row = GenerateRow(section, "WixSimpleReference", typeStartLineNumber);
            row.Add(new XElement(WixBackendCompilerServices.WixobjNamespace + "field", tableName));
            row.Add(new XElement(WixBackendCompilerServices.WixobjNamespace + "field", String.Join("/", primaryKeys)));

            return row;
        }

        private static string[] GenerateMsiConditions(WixItem item, HashSet<WixItem> processedItems)
        {
            List<string> conditions = new List<string>();

            if (!processedItems.Contains(item))
            {
                processedItems.Add(item);

                if (item.Group != null)
                {
                    conditions.AddRange(WixBackendCompilerServices.GenerateMsiConditions(item.Group, processedItems));
                }

                if (item.Parent != null)
                {
                    conditions.AddRange(WixBackendCompilerServices.GenerateMsiConditions(item.Parent, processedItems));
                }

                string condition = Install.GetCondition(item.Item);
                if (!String.IsNullOrEmpty(condition))
                {
                    conditions.Add(condition);
                }
            }

            return conditions.ToArray();
        }

        private static string GenerateShortFileName(bool preserveExtension, string name, params string[] args)
        {
            string[] strings = new string[args.Length + 1];
            strings[0] = name.ToLowerInvariant();
            args.CopyTo(strings, 1);

            string stringData = String.Join("|", strings);
            byte[] data = Encoding.Unicode.GetBytes(stringData);

            // hash the data
            byte[] hash;
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                hash = sha1.ComputeHash(data);
            }

            // generate the short file/directory name without an extension
            StringBuilder shortName = new StringBuilder(Convert.ToBase64String(hash));
            shortName.Remove(8, shortName.Length - 8);
            shortName.Replace('/', '_');
            shortName.Replace('+', '-');

            if (preserveExtension)
            {
                string extension = Path.GetExtension(name);
                if (4 < extension.Length)
                {
                    extension = extension.Substring(0, 4);
                }

                shortName.Append(extension);
            }

            return shortName.ToString().ToLowerInvariant();
        }

        private static string GenerateSourceLineNumbers(FileLineNumber typeStartLineNumber)
        {
            return String.Concat(typeStartLineNumber.SourceFile, "*", typeStartLineNumber.LineNumber);
        }

        public static WixItem ResolveParentFolderMsiItem(Folder parentFolder, IDictionary<PackageItem, WixItem> msiItems)
        {
            WixItem item = null;
            while (parentFolder != null)
            {
                item = msiItems[parentFolder];
                if (item is WixFolderReference || item is WixFolder)
                {
                    if (!String.IsNullOrEmpty(item.MsiId))
                    {
                        break;
                    }
                }
                else
                {
                    // TODO: display error since a parent isn't what we expected?
                }

                parentFolder = parentFolder.ParentFolder;
                item = null;
            }

            return item;
        }
    }
}
