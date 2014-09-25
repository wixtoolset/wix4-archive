//-------------------------------------------------------------------------------------------------
// <copyright file="CreateBurnManifestCommand.cs" company="Outercurve Foundation">
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
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    internal class CreateBurnManifestCommand : ICommand
    {
        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public string ExecutableName { private get; set; }

        public WixBundleRow BundleInfo { private get; set; }

        public WixBundleUpdateRow UpdateRow { private get; set; }

        public WixUpdateRegistrationRow UpdateRegistrationInfo { private get; set; }

        public string OutputPath { private get; set; }

        public IEnumerable<RelatedBundleRow> RelatedBundles { private get; set; }

        public IEnumerable<VariableRow> Variables { private get; set; }

        public IEnumerable<WixSearchInfo> OrderedSearches { private get; set; }

        public Dictionary<string, PayloadInfoRow> Payloads { private get; set; }

        public ChainInfo Chain { private get; set; }

        public Dictionary<string, ContainerInfo> Containers { private get; set; }

        public IEnumerable<WixCatalogRow> Catalogs { private get; set; }

        public IEnumerable<Row> BundleTags { private get; set; }

        public IEnumerable<WixApprovedExeForElevationRow> ApprovedExesForElevation { private get; set; }

        public void Execute()
        {
            using (XmlTextWriter writer = new XmlTextWriter(this.OutputPath, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("BurnManifest", BurnCommon.BurnNamespace);

                // Write the condition, if there is one
                if (null != this.BundleInfo.Condition)
                {
                    writer.WriteElementString("Condition", this.BundleInfo.Condition);
                }

                // Write the log element if default logging wasn't disabled.
                if (!String.IsNullOrEmpty(this.BundleInfo.LogPrefix))
                {
                    writer.WriteStartElement("Log");
                    if (!String.IsNullOrEmpty(this.BundleInfo.LogPathVariable))
                    {
                        writer.WriteAttributeString("PathVariable", this.BundleInfo.LogPathVariable);
                    }
                    writer.WriteAttributeString("Prefix", this.BundleInfo.LogPrefix);
                    writer.WriteAttributeString("Extension", this.BundleInfo.LogExtension);
                    writer.WriteEndElement();
                }

                if (null != this.UpdateRow)
                {
                    writer.WriteStartElement("Update");
                    writer.WriteAttributeString("Location", this.UpdateRow.Location);
                    writer.WriteEndElement(); // </Update>
                }

                // Write the RelatedBundle elements
                foreach (RelatedBundleRow relatedBundle in this.RelatedBundles)
                {
                    writer.WriteStartElement("RelatedBundle");
                    writer.WriteAttributeString("Id", relatedBundle.Id);
                    writer.WriteAttributeString("Action", Convert.ToString(relatedBundle.Action, CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }

                // Write the variables
                foreach (VariableRow variable in this.Variables)
                {
                    writer.WriteStartElement("Variable");
                    writer.WriteAttributeString("Id", variable.Id);
                    if (null != variable.Type)
                    {
                        writer.WriteAttributeString("Value", variable.Value);
                        writer.WriteAttributeString("Type", variable.Type);
                    }
                    writer.WriteAttributeString("Hidden", variable.Hidden ? "yes" : "no");
                    writer.WriteAttributeString("Persisted", variable.Persisted ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the searches
                foreach (WixSearchInfo searchinfo in this.OrderedSearches)
                {
                    searchinfo.WriteXml(writer);
                }

                // write the UX element
                writer.WriteStartElement("UX");
                if (!String.IsNullOrEmpty(this.BundleInfo.SplashScreenBitmapPath))
                {
                    writer.WriteAttributeString("SplashScreen", "yes");
                }

                // write the UX allPayloads...
                List<PayloadInfoRow> uxPayloads = this.Containers[Compiler.BurnUXContainerId].Payloads;
                foreach (PayloadInfoRow payload in uxPayloads)
                {
                    writer.WriteStartElement("Payload");
                    WriteBurnManifestPayloadAttributes(writer, payload, true, this.Payloads);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                // write the catalog elements
                if (this.Catalogs.Any())
                {
                    foreach (WixCatalogRow catalog in this.Catalogs)
                    {
                        writer.WriteStartElement("Catalog");
                        writer.WriteAttributeString("Id", catalog.Id);
                        writer.WriteAttributeString("Payload", catalog.PayloadId);
                        writer.WriteEndElement();
                    }
                }

                int attachedContainerIndex = 1; // count starts at one because UX container is "0".
                foreach (ContainerInfo container in this.Containers.Values)
                {
                    if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                    {
                        writer.WriteStartElement("Container");
                        WriteBurnManifestContainerAttributes(writer, this.ExecutableName, container, attachedContainerIndex);
                        writer.WriteEndElement();
                        if ("attached" == container.Type)
                        {
                            attachedContainerIndex++;
                        }
                    }
                }

                foreach (PayloadInfoRow payload in this.Payloads.Values)
                {
                    if (PackagingType.Embedded == payload.Packaging && Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, true, this.Payloads);
                        writer.WriteEndElement();
                    }
                    else if (PackagingType.External == payload.Packaging)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, false, this.Payloads);
                        writer.WriteEndElement();
                    }
                }

                foreach (RollbackBoundaryInfo rollbackBoundary in this.Chain.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.Id);
                    writer.WriteAttributeString("Vital", YesNoType.Yes == rollbackBoundary.Vital ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the registration information...
                writer.WriteStartElement("Registration");

                writer.WriteAttributeString("Id", this.BundleInfo.BundleId.ToString("B"));
                writer.WriteAttributeString("ExecutableName", this.ExecutableName);
                writer.WriteAttributeString("PerMachine", this.BundleInfo.PerMachine ? "yes" : "no");
                writer.WriteAttributeString("Tag", this.BundleInfo.Tag);
                writer.WriteAttributeString("Version", this.BundleInfo.Version);
                writer.WriteAttributeString("ProviderKey", this.BundleInfo.ProviderKey);

                writer.WriteStartElement("Arp");
                writer.WriteAttributeString("Register", (0 < this.BundleInfo.DisableModify && this.BundleInfo.DisableRemove) ? "no" : "yes"); // do not register if disabled modify and remove.
                writer.WriteAttributeString("DisplayName", this.BundleInfo.Name);
                writer.WriteAttributeString("DisplayVersion", this.BundleInfo.Version);

                if (!String.IsNullOrEmpty(this.BundleInfo.Publisher))
                {
                    writer.WriteAttributeString("Publisher", this.BundleInfo.Publisher);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.HelpLink))
                {
                    writer.WriteAttributeString("HelpLink", this.BundleInfo.HelpLink);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.HelpTelephone))
                {
                    writer.WriteAttributeString("HelpTelephone", this.BundleInfo.HelpTelephone);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.AboutUrl))
                {
                    writer.WriteAttributeString("AboutUrl", this.BundleInfo.AboutUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.UpdateUrl))
                {
                    writer.WriteAttributeString("UpdateUrl", this.BundleInfo.UpdateUrl);
                }

                if (!String.IsNullOrEmpty(this.BundleInfo.ParentName))
                {
                    writer.WriteAttributeString("ParentDisplayName", this.BundleInfo.ParentName);
                }

                if (1 == this.BundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "yes");
                }
                else if (2 == this.BundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "button");
                }

                if (this.BundleInfo.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
                }
                writer.WriteEndElement(); // </Arp>

                if (null != this.UpdateRegistrationInfo)
                {
                    writer.WriteStartElement("Update"); // <Update>
                    writer.WriteAttributeString("Manufacturer", this.UpdateRegistrationInfo.Manufacturer);

                    if (!String.IsNullOrEmpty(this.UpdateRegistrationInfo.Department))
                    {
                        writer.WriteAttributeString("Department", this.UpdateRegistrationInfo.Department);
                    }

                    if (!String.IsNullOrEmpty(this.UpdateRegistrationInfo.ProductFamily))
                    {
                        writer.WriteAttributeString("ProductFamily", this.UpdateRegistrationInfo.ProductFamily);
                    }

                    writer.WriteAttributeString("Name", this.UpdateRegistrationInfo.Name);
                    writer.WriteAttributeString("Classification", this.UpdateRegistrationInfo.Classification);
                    writer.WriteEndElement(); // </Update>
                }

                foreach (Row row in this.BundleTags)
                {
                    writer.WriteStartElement("SoftwareTag");
                    writer.WriteAttributeString("Filename", (string)row[0]);
                    writer.WriteAttributeString("Regid", (string)row[1]);
                    writer.WriteCData((string)row[4]);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // </Register>

                // write the Chain...
                writer.WriteStartElement("Chain");
                if (this.Chain.DisableRollback)
                {
                    writer.WriteAttributeString("DisableRollback", "yes");
                }

                if (this.Chain.DisableSystemRestore)
                {
                    writer.WriteAttributeString("DisableSystemRestore", "yes");
                }

                if (this.Chain.ParallelCache)
                {
                    writer.WriteAttributeString("ParallelCache", "yes");
                }

                // Build up the list of target codes from all the MSPs in the chain.
                List<WixBundlePatchTargetCodeRow> targetCodes = new List<WixBundlePatchTargetCodeRow>();

                foreach (ChainPackageInfo package in this.Chain.Packages)
                {
                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.ChainPackageType));

                    writer.WriteAttributeString("Id", package.Id);

                    switch (package.Cache)
                    {
                        case YesNoAlwaysType.No:
                            writer.WriteAttributeString("Cache", "no");
                            break;
                        case YesNoAlwaysType.Yes:
                            writer.WriteAttributeString("Cache", "yes");
                            break;
                        case YesNoAlwaysType.Always:
                            writer.WriteAttributeString("Cache", "always");
                            break;
                    }

                    writer.WriteAttributeString("CacheId", package.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.Size));
                    writer.WriteAttributeString("PerMachine", YesNoDefaultType.Yes == package.PerMachine ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", package.Vital ? "yes" : "no");

                    if (null != package.RollbackBoundary)
                    {
                        writer.WriteAttributeString("RollbackBoundaryForward", package.RollbackBoundary.Id);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackBoundaryBackwardId))
                    {
                        writer.WriteAttributeString("RollbackBoundaryBackward", package.RollbackBoundaryBackwardId);
                    }

                    if (!String.IsNullOrEmpty(package.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", package.LogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackLogPathVariable))
                    {
                        writer.WriteAttributeString("RollbackLogPathVariable", package.RollbackLogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.InstallCondition))
                    {
                        writer.WriteAttributeString("InstallCondition", package.InstallCondition);
                    }

                    if (Compiler.ChainPackageType.Exe == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("InstallArguments", package.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", package.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", package.RepairCommand);
                        writer.WriteAttributeString("Repairable", package.Repairable ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.Protocol))
                        {
                            writer.WriteAttributeString("Protocol", package.Protocol);
                        }
                    }
                    else if (Compiler.ChainPackageType.Msi == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("ProductCode", package.ProductCode);
                        writer.WriteAttributeString("Language", package.Language);
                        writer.WriteAttributeString("Version", package.Version);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.UpgradeCode))
                        {
                            writer.WriteAttributeString("UpgradeCode", package.UpgradeCode);
                        }
                    }
                    else if (Compiler.ChainPackageType.Msp == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("PatchCode", package.PatchCode);
                        writer.WriteAttributeString("PatchXml", package.PatchXml);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");

                        // If there is still a chance that all of our patches will target a narrow set of
                        // product codes, add the patch list to the overall list.
                        if (null != targetCodes)
                        {
                            if (!package.TargetUnspecified)
                            {
                                targetCodes.AddRange(package.TargetCodes);
                            }
                            else // we have a patch that targets the world, so throw the whole list away.
                            {
                                targetCodes = null;
                            }
                        }
                    }
                    else if (Compiler.ChainPackageType.Msu == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("KB", package.MsuKB);
                    }

                    foreach (MsiFeature feature in package.MsiFeatures)
                    {
                        writer.WriteStartElement("MsiFeature");
                        writer.WriteAttributeString("Id", feature.Name);
                        writer.WriteEndElement();
                    }

                    foreach (MsiPropertyInfo msiProperty in package.MsiProperties)
                    {
                        writer.WriteStartElement("MsiProperty");
                        writer.WriteAttributeString("Id", msiProperty.Name);
                        writer.WriteAttributeString("Value", msiProperty.Value);
                        writer.WriteEndElement();
                    }

                    foreach (string slipstreamMsp in package.SlipstreamMsps)
                    {
                        writer.WriteStartElement("SlipstreamMsp");
                        writer.WriteAttributeString("Id", slipstreamMsp);
                        writer.WriteEndElement();
                    }

                    foreach (ExitCodeInfo exitCode in package.ExitCodes)
                    {
                        writer.WriteStartElement("ExitCode");
                        writer.WriteAttributeString("Type", exitCode.Type);
                        writer.WriteAttributeString("Code", exitCode.Code);
                        writer.WriteEndElement();
                    }

                    // Output the dependency information.
                    foreach (ProvidesDependency dependency in package.Provides)
                    {
                        // TODO: Add to wixpdb as an imported table, or link package wixpdbs to bundle wixpdbs.
                        dependency.WriteXml(writer);
                    }

                    foreach (RelatedPackage related in package.RelatedPackages)
                    {
                        writer.WriteStartElement("RelatedPackage");
                        writer.WriteAttributeString("Id", related.Id);
                        if (!String.IsNullOrEmpty(related.MinVersion))
                        {
                            writer.WriteAttributeString("MinVersion", related.MinVersion);
                            writer.WriteAttributeString("MinInclusive", related.MinInclusive ? "yes" : "no");
                        }
                        if (!String.IsNullOrEmpty(related.MaxVersion))
                        {
                            writer.WriteAttributeString("MaxVersion", related.MaxVersion);
                            writer.WriteAttributeString("MaxInclusive", related.MaxInclusive ? "yes" : "no");
                        }
                        writer.WriteAttributeString("OnlyDetect", related.OnlyDetect ? "yes" : "no");
                        if (0 < related.Languages.Count)
                        {
                            writer.WriteAttributeString("LangInclusive", related.LangInclusive ? "yes" : "no");
                            foreach (string language in related.Languages)
                            {
                                writer.WriteStartElement("Language");
                                writer.WriteAttributeString("Id", language);
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    // Write any contained Payloads with the PackagePayload being first
                    writer.WriteStartElement("PayloadRef");
                    writer.WriteAttributeString("Id", package.PackagePayload.Id);
                    writer.WriteEndElement();

                    foreach (PayloadInfoRow payload in package.Payloads)
                    {
                        if (payload.Id != package.PackagePayload.Id)
                        {
                            writer.WriteStartElement("PayloadRef");
                            writer.WriteAttributeString("Id", payload.Id);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement(); // </XxxPackage>
                }
                writer.WriteEndElement(); // </Chain>

                if (null != targetCodes)
                {
                    foreach (WixBundlePatchTargetCodeRow targetCode in targetCodes)
                    {
                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.TargetsProductCode ? "yes" : "no");
                        writer.WriteEndElement();
                    }
                }

                // write the ApprovedExeForElevation elements
                foreach (WixApprovedExeForElevationRow approvedExeForElevation in this.ApprovedExesForElevation)
                {
                    writer.WriteStartElement("ApprovedExeForElevation");
                    writer.WriteAttributeString("Id", approvedExeForElevation.Id);
                    writer.WriteAttributeString("Key", approvedExeForElevation.Key);

                    if (!String.IsNullOrEmpty(approvedExeForElevation.ValueName))
                    {
                        writer.WriteAttributeString("ValueName", approvedExeForElevation.ValueName);
                    }

                    if (approvedExeForElevation.Win64)
                    {
                        writer.WriteAttributeString("Win64", "yes");
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndDocument(); // </BurnManifest>
            }
        }

        private void WriteBurnManifestContainerAttributes(XmlTextWriter writer, string executableName, ContainerInfo container, int containerIndex)
        {
            writer.WriteAttributeString("Id", container.Id);
            writer.WriteAttributeString("FileSize", container.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", Common.GetFileHash(container.FileInfo));
            if (container.Type == "detached")
            {
                string resolvedUrl = this.ResolveUrl(container.DownloadUrl, null, null, container.Id, container.Name);
                if (!String.IsNullOrEmpty(resolvedUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                }
                else if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", container.DownloadUrl);
                }

                writer.WriteAttributeString("FilePath", container.Name);
            }
            else if (container.Type == "attached")
            {
                if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    Messaging.Instance.OnMessage(WixWarnings.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id));
                }

                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", containerIndex.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayloadAttributes(XmlTextWriter writer, PayloadInfoRow payload, bool embeddedOnly, Dictionary<string, PayloadInfoRow> allPayloads)
        {
            Debug.Assert(!embeddedOnly || PackagingType.Embedded == payload.Packaging);

            writer.WriteAttributeString("Id", payload.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", payload.Hash);

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            if (!String.IsNullOrEmpty(payload.PublicKey))
            {
                writer.WriteAttributeString("CertificateRootPublicKeyIdentifier", payload.PublicKey);
            }

            if (!String.IsNullOrEmpty(payload.Thumbprint))
            {
                writer.WriteAttributeString("CertificateRootThumbprint", payload.Thumbprint);
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id));
                    }

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

                    if (Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteAttributeString("Container", payload.Container);
                    }
                    break;

                case PackagingType.External:
                    string packageId = payload.ParentPackagePayload;
                    string parentUrl = payload.ParentPackagePayload == null ? null : allPayloads[payload.ParentPackagePayload].DownloadUrl;
                    string resolvedUrl = this.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id, payload.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                    }
                    else if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", payload.DownloadUrl);
                    }

                    writer.WriteAttributeString("Packaging", "external");
                    writer.WriteAttributeString("SourcePath", payload.Name);
                    break;
            }

            if (!String.IsNullOrEmpty(payload.CatalogId))
            {
                writer.WriteAttributeString("Catalog", payload.CatalogId);
            }
        }

        private string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            string resolved = null;
            foreach (IBinderFileManager fileManager in this.FileManagers)
            {
                resolved = fileManager.ResolveUrl(url, fallbackUrl, packageId, payloadId, fileName);
                if (!String.IsNullOrEmpty(resolved))
                {
                    break;
                }
            }

            return resolved;
        }
    }
}
