// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
