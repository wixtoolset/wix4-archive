//-------------------------------------------------------------------------------------------------
// <copyright file="CreateBootstrapperApplicationManifestCommand.cs" company="Outercurve Foundation">
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
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class CreateBootstrapperApplicationManifestCommand : ICommand
    {
        public WixBundleRow BundleRow { private get; set; }

        public IEnumerable<ChainPackageFacade> ChainPackages { private get; set; }

        public int LastUXPayloadIndex { private get; set; }

        public IEnumerable<WixBundleMsiFeatureRow> MsiFeatures { private get; set; }

        public Output Output { private get; set; }

        public RowDictionary<PayloadRow> Payloads { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string TempFilesLocation { private get; set; }

        public PayloadRow BootstrapperApplicationManifestPayloadRow { get; private set; }

        public void Execute()
        {
            this.GenerateBAManifestBundleTables();

            this.GenerateBAManifestMsiFeatureTables();

            this.GenerateBAManifestPackageTables();

            this.GenerateBAManifestPayloadTables();

            string baManifestPath = Path.Combine(this.TempFilesLocation, "wix-badata.xml");

            this.CreateBootstrapperApplicationManifest(baManifestPath);

            this.BootstrapperApplicationManifestPayloadRow = this.CreateBootstrapperApplicationManifestPayloadRow(baManifestPath);
        }

        private void GenerateBAManifestBundleTables()
        {
            Table wixBundlePropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixBundleProperties"]);

            Row row = wixBundlePropertiesTable.CreateRow(this.BundleRow.SourceLineNumbers);
            row[0] = this.BundleRow.Name;
            row[1] = this.BundleRow.LogPathVariable;
            row[2] = (YesNoDefaultType.Yes == this.BundleRow.Compressed) ? "yes" : "no";
            row[3] = this.BundleRow.BundleId.ToString("B");
            row[4] = this.BundleRow.UpgradeCode;
            row[5] = this.BundleRow.PerMachine ? "yes" : "no";
        }

        private void GenerateBAManifestPackageTables()
        {
            Table wixPackagePropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixPackageProperties"]);

            foreach (ChainPackageFacade package in this.ChainPackages)
            {
                PayloadRow packagePayload = this.Payloads[package.ChainPackage.PackagePayloadId];

                Row row = wixPackagePropertiesTable.CreateRow(package.ChainPackage.SourceLineNumbers);
                row[0] = package.ChainPackage.WixChainItemId;
                row[1] = (YesNoType.Yes == package.ChainPackage.Vital) ? "yes" : "no";
                row[2] = package.ChainPackage.DisplayName;
                row[3] = package.ChainPackage.Description;
                row[4] = package.ChainPackage.Size.ToString(CultureInfo.InvariantCulture); // TODO: DownloadSize (compressed) (what does this mean when it's embedded?)
                row[5] = package.ChainPackage.Size.ToString(CultureInfo.InvariantCulture); // Package.Size (uncompressed)
                row[6] = package.ChainPackage.InstallSize.Value.ToString(CultureInfo.InvariantCulture); // InstallSize (required disk space)
                row[7] = package.ChainPackage.Type.ToString(CultureInfo.InvariantCulture);
                row[8] = package.ChainPackage.Permanent ? "yes" : "no";
                row[9] = package.ChainPackage.LogPathVariable;
                row[10] = package.ChainPackage.RollbackLogPathVariable;
                row[11] = (PackagingType.Embedded == packagePayload.Packaging) ? "yes" : "no";

                if (ChainPackageType.Msi == package.ChainPackage.Type)
                {
                    row[12] = package.MsiPackage.DisplayInternalUI ? "yes" : "no";

                    if (!String.IsNullOrEmpty(package.MsiPackage.ProductCode))
                    {
                        row[13] = package.MsiPackage.ProductCode;
                    }

                    if (!String.IsNullOrEmpty(package.MsiPackage.UpgradeCode))
                    {
                        row[14] = package.MsiPackage.UpgradeCode;
                    }
                }
                else if (ChainPackageType.Msp == package.ChainPackage.Type)
                {
                    row[12] = package.MspPackage.DisplayInternalUI ? "yes" : "no";

                    if (!String.IsNullOrEmpty(package.MspPackage.PatchCode))
                    {
                        row[13] = package.MspPackage.PatchCode;
                    }
                }

                if (!String.IsNullOrEmpty(package.ChainPackage.Version))
                {
                    row[15] = package.ChainPackage.Version;
                }

                if (!String.IsNullOrEmpty(package.ChainPackage.InstallCondition))
                {
                    row[16] = package.ChainPackage.InstallCondition;
                }

                switch (package.ChainPackage.Cache)
                {
                    case YesNoAlwaysType.No:
                        row[17] = "no";
                        break;
                    case YesNoAlwaysType.Yes:
                        row[17] = "yes";
                        break;
                    case YesNoAlwaysType.Always:
                        row[17] = "always";
                        break;
                }
            }
        }

        private void GenerateBAManifestMsiFeatureTables()
        {
            Table wixPackageFeatureInfoTable = this.Output.EnsureTable(this.TableDefinitions["WixPackageFeatureInfo"]);

            foreach (WixBundleMsiFeatureRow feature in this.MsiFeatures)
            {
                Row row = wixPackageFeatureInfoTable.CreateRow(feature.SourceLineNumbers);
                row[0] = feature.ChainPackageId;
                row[1] = feature.Name;
                row[2] = Convert.ToString(feature.Size, CultureInfo.InvariantCulture);
                row[3] = feature.Parent;
                row[4] = feature.Title;
                row[5] = feature.Description;
                row[6] = Convert.ToString(feature.Display, CultureInfo.InvariantCulture);
                row[7] = Convert.ToString(feature.Level, CultureInfo.InvariantCulture);
                row[8] = feature.Directory;
                row[9] = Convert.ToString(feature.Attributes, CultureInfo.InvariantCulture);
            }

        }

        private void GenerateBAManifestPayloadTables()
        {
            Table wixPayloadPropertiesTable = this.Output.EnsureTable(this.TableDefinitions["WixPayloadProperties"]);

            foreach (PayloadRow payload in this.Payloads.Values)
            {
                Row row = wixPayloadPropertiesTable.CreateRow(payload.SourceLineNumbers);
                row[0] = payload.Id;
                row[1] = payload.Package;
                row[2] = payload.Container;
                row[3] = payload.Name;
                row[4] = payload.FileSize.ToString();
                row[5] = payload.DownloadUrl;
                row[6] = payload.LayoutOnly ? "yes" : "no";
            }
        }

        private void CreateBootstrapperApplicationManifest(string path)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperApplicationData", "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData");

                foreach (Table table in this.Output.Tables)
                {
                    if (table.Definition.BootstrapperApplicationData)
                    {
                        // We simply assert that the table (and field) name is valid, because
                        // this is up to the extension developer to get right. An author will
                        // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                        Debug.Assert(Common.IsIdentifier(table.Name));
                        foreach (ColumnDefinition column in table.Definition.Columns)
                        {
                            Debug.Assert(Common.IsIdentifier(column.Name));
                        }
#endif // DEBUG

                        foreach (Row row in table.Rows)
                        {
                            writer.WriteStartElement(table.Name);

                            foreach (Field field in row.Fields)
                            {
                                if (null != field.Data)
                                {
                                    writer.WriteAttributeString(field.Column.Name, field.Data.ToString());
                                }
                            }

                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private PayloadRow CreateBootstrapperApplicationManifestPayloadRow(string baManifestPath)
        {
            Table payloadTable = this.Output.EnsureTable(this.TableDefinitions["Payload"]);
            PayloadRow row = (PayloadRow)payloadTable.CreateRow(this.BundleRow.SourceLineNumbers);
            row.Id = Common.GenerateIdentifier("ux", "BootstrapperApplicationData.xml");
            row.Name = "BootstrapperApplicationData.xml";
            row.SourceFile = baManifestPath;
            row.Compressed = YesNoDefaultType.Yes;
            row.UnresolvedSourceFile = baManifestPath;
            row.Container = Compiler.BurnUXContainerId;
            row.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex);
            row.Packaging = PackagingType.Embedded;

            FileInfo fileInfo = new FileInfo(row.SourceFile);

            row.FileSize = (int)fileInfo.Length;

            row.Hash = Common.GetFileHash(fileInfo.FullName);

            return row;
        }
    }
}
