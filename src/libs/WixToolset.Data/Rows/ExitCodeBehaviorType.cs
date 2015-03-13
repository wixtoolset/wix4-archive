﻿//-------------------------------------------------------------------------------------------------
// <copyright file="ExitCodeBehaviorType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    public enum ExitCodeBehaviorType
    {
        NotSet = -1,
        Success,
        Error,
        ScheduleReboot,
        ForceReboot,
    }
}
