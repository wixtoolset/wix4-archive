// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using WixToolset.Data;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a payload from the file system.
    /// </summary>
    public sealed class PayloadHarvester : HarvesterExtension
    {
        private bool setUniqueIdentifiers;

        /// <summary>
        /// Instantiate a new PayloadHarvester.
        /// </summary>
        public PayloadHarvester()
        {
            this.setUniqueIdentifiers = true;
        }

        /// <summary>
        /// Gets of sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Harvest a payload.
        /// </summary>
        /// <param name="argument">The path of the payload.</param>
        /// <returns>A harvested payload.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            string fullPath = Path.GetFullPath(argument);

            Wix.RemotePayload remotePayload = this.HarvestRemotePayload(fullPath);

            Wix.Fragment fragment = new Wix.Fragment();
            fragment.AddChild(remotePayload);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a payload.
        /// </summary>
        /// <param name="path">The path of the payload.</param>
        /// <returns>A harvested payload.</returns>
        public Wix.RemotePayload HarvestRemotePayload(string path)
        {
            if (null == path)
            {
                throw new ArgumentNullException("path");
            }

            if (!File.Exists(path))
            {
                throw new WixException(UtilErrors.FileNotFound(path));
            }

            Wix.RemotePayload remotePayload = new Wix.RemotePayload();

            FileInfo fileInfo = new FileInfo(path);

            remotePayload.Size = (int)fileInfo.Length;
            remotePayload.Hash = this.GetFileHash(path);

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);

            if (null != versionInfo)
            {
                // Use the fixed version info block for the file since the resource text may not be a dotted quad.
                Version version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);

                remotePayload.Version = version.ToString();
                remotePayload.Description = versionInfo.FileDescription;
                remotePayload.ProductName = versionInfo.ProductName;
            }

            return remotePayload;
        }

        private string GetFileHash(string path)
        {
            byte[] hashBytes;
            using (SHA1Managed managed = new SHA1Managed())
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    hashBytes = managed.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(hashBytes).Replace("-", String.Empty);

            //StringBuilder sb = new StringBuilder();
            //for (int i = 0; i < hashBytes.Length; i++)
            //{
            //    sb.AppendFormat("{0:X2}", hashBytes[i]);
            //}

            //return sb.ToString();
        }
    }
}
