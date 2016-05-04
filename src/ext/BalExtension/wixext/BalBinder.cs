// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    public class BalBinder : BinderExtension
    {
        public override void Finish(Output output)
        {
            // Only process Bundles.
            if (OutputType.Bundle != output.Type)
            {
                return;
            }

            Table baTable = output.Tables["WixBootstrapperApplication"];
            Row baRow = baTable.Rows[0];
            string baId = (string)baRow[0];

            if (null == baId || !baId.StartsWith("ManagedBootstrapperApplicationHost"))
            {
                return;
            }

            Table prereqInfoTable = output.Tables["WixMbaPrereqInformation"];
            if (null == prereqInfoTable || prereqInfoTable.Rows.Count == 0)
            {
                this.Core.OnMessage(BalErrors.MissingPrereq());
                return;
            }

            bool foundLicenseFile = false;
            bool foundLicenseUrl = false;

            foreach (Row prereqInfoRow in prereqInfoTable.Rows)
            {
                if (null != prereqInfoRow[1])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Core.OnMessage(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseFile = true;
                }

                if (null != prereqInfoRow[2])
                {
                    if (foundLicenseFile || foundLicenseUrl)
                    {
                        this.Core.OnMessage(BalErrors.MultiplePrereqLicenses(prereqInfoRow.SourceLineNumbers));
                        return;
                    }

                    foundLicenseUrl = true;
                }
            }
        }
    }
}
