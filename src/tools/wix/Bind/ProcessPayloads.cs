//-------------------------------------------------------------------------------------------------
// <copyright file="ProcessPayloads.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class ProcessPayloads : ICommand
    {
        private static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

        public IEnumerable<PayloadRow> Payloads { private get; set; }

        public PackagingType DefaultPackaging { private get; set; }

        public string LayoutDirectory { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; private set; }

        public void Execute()
        {
            List<FileTransfer> fileTransfers = new List<FileTransfer>();

            foreach (PayloadRow payload in this.Payloads)
            {
                // Embedded files (aka: files from binary .wixlibs) are not content files (because they are hidden
                // in the .wixlib).
                ObjectField field = (ObjectField)payload.Fields[2];
                payload.ContentFile = !field.EmbeddedFileIndex.HasValue;

                this.UpdatePayloadPackagingType(payload);

                if (String.IsNullOrEmpty(payload.SourceFile))
                {
                    // Remote payloads obviously cannot be embedded.
                    Debug.Assert(PackagingType.Embedded != payload.Packaging);
                }
                else // not a remote payload so we have a lot more to update.
                {
                    this.UpdatePayloadFileInformation(payload);

                    this.UpdatePayloadVersionInformation(payload);

                    // External payloads need to be transfered.
                    if (PackagingType.External == payload.Packaging)
                    {
                        FileTransfer transfer;
                        if (FileTransfer.TryCreate(payload.FullFileName, Path.Combine(this.LayoutDirectory, payload.Name), false, "Payload", payload.SourceLineNumbers, out transfer))
                        {
                            fileTransfers.Add(transfer);
                        }
                    }
                }
            }

            this.FileTransfers = fileTransfers;
        }

        private void UpdatePayloadPackagingType(PayloadRow payload)
        {
            if (PackagingType.Unknown == payload.Packaging)
            {
                if (YesNoDefaultType.Yes == payload.Compressed)
                {
                    payload.Packaging = PackagingType.Embedded;
                }
                else if (YesNoDefaultType.No == payload.Compressed)
                {
                    payload.Packaging = PackagingType.External;
                }
                else
                {
                    payload.Packaging = this.DefaultPackaging;
                }
            }

            // Embedded payloads that are not assigned a container already are placed in the default attached
            // container.
            if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.Container))
            {
                payload.Container = Compiler.BurnDefaultAttachedContainerId;
            }
        }

        private void UpdatePayloadFileInformation(PayloadRow payload)
        {
            FileInfo fileInfo = new FileInfo(payload.SourceFile);

            if (null != fileInfo)
            {
                payload.FileSize = (int)fileInfo.Length;

                payload.Hash = Common.GetFileHash(fileInfo.FullName);

                // Try to get the certificate if the payload is a signed file and we're not suppressing signature validation.
                if (payload.EnableSignatureValidation)
                {
                    X509Certificate2 certificate = null;
                    try
                    {
                        certificate = new X509Certificate2(fileInfo.FullName);
                    }
                    catch (CryptographicException) // we don't care about non-signed files.
                    {
                    }

                    // If there is a certificate, remember its hashed public key identifier and thumbprint.
                    if (null != certificate)
                    {
                        byte[] publicKeyIdentifierHash = new byte[128];
                        uint publicKeyIdentifierHashSize = (uint)publicKeyIdentifierHash.Length;

                        WixToolset.Cab.Interop.NativeMethods.HashPublicKeyInfo(certificate.Handle, publicKeyIdentifierHash, ref publicKeyIdentifierHashSize);
                        StringBuilder sb = new StringBuilder(((int)publicKeyIdentifierHashSize + 1) * 2);
                        for (int i = 0; i < publicKeyIdentifierHashSize; ++i)
                        {
                            sb.AppendFormat("{0:X2}", publicKeyIdentifierHash[i]);
                        }

                        payload.PublicKey = sb.ToString();
                        payload.Thumbprint = certificate.Thumbprint;
                    }
                }
            }
        }

        private void UpdatePayloadVersionInformation(PayloadRow payload)
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(payload.SourceFile);

            if (null != versionInfo)
            {
                // Use the fixed version info block for the file since the resource text may not be a dotted quad.
                Version version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);

                if (ProcessPayloads.EmptyVersion != version)
                {
                    payload.Version = version.ToString();
                }

                payload.Description = versionInfo.FileDescription;
                payload.DisplayName = versionInfo.ProductName;
            }
        }
    }
}
