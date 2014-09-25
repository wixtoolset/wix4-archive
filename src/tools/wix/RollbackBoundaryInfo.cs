//-------------------------------------------------------------------------------------------------
// <copyright file="RollbackBoundaryInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using WixToolset.Data;

    /// <summary>
    /// Rollback boundary info for binding Bundles.
    /// </summary>
    internal class RollbackBoundaryInfo
    {
        public RollbackBoundaryInfo(Row row)
        {
            this.Id = row[0].ToString();

            this.Vital = (null == row[10] || 1 == (int)row[10]) ? YesNoType.Yes : YesNoType.No;
            this.SourceLineNumbers = row.SourceLineNumbers;
        }

        public string Id { get; private set; }

        public YesNoType Vital { get; private set; }

        public SourceLineNumber SourceLineNumbers { get; private set; }
    }
}
