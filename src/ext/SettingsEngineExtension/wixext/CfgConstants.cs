//-------------------------------------------------------------------------------------------------
// <copyright file="CfgConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Constants used by CfgExtension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    static class CfgConstants
    {
        // Not technically allowed to be constant, but used as a constant
        static public Guid wixCfgGuidNamespace = new Guid("{58268499-7B45-4467-B0B2-DD956724497B}");
    }
}
