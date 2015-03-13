//-------------------------------------------------------------------------------------------------
// <copyright file="UtilConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    /// <summary>
    /// Constants used by Utility Extension.
    /// </summary>
    internal static class UtilConstants
    {
        internal static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", null, "ReadAttributes", "WriteAttributes" };
        internal static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };
        internal static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };
        internal static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };
        internal static readonly string[] ServicePermissions = { "ServiceQueryConfig", "ServiceChangeConfig", "ServiceQueryStatus", "ServiceEnumerateDependents", "ServiceStart", "ServiceStop", "ServicePauseContinue", "ServiceInterrogate", "ServiceUserDefinedControl" };
        internal static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };
    }
}
