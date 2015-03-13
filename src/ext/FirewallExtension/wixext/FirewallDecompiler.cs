//-------------------------------------------------------------------------------------------------
// <copyright file="FirewallDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the WiX Toolset Firewall Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Firewall = WixToolset.Extensions.Serialize.Firewall;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the WiX Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Creates a decompiler for Firewall Extension.
        /// </summary>
        public FirewallDecompiler()
        {
            this.TableDefinitions = FirewallExtensionData.GetExtensionTableDefinitions();
        }

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return FirewallExtensionData.GetExtensionLibrary(tableDefinitions);
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixFirewallException":
                    this.DecompileWixFirewallExceptionTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the WixFirewallException table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixFirewallExceptionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Firewall.FirewallException fire = new Firewall.FirewallException();
                fire.Id = (string)row[0];
                fire.Name = (string)row[1];

                string[] addresses = ((string)row[2]).Split(',');
                if (1 == addresses.Length)
                {
                    // special-case the Scope attribute values
                    if ("*" == addresses[0])
                    {
                        fire.Scope = Firewall.FirewallException.ScopeType.any;
                    }
                    else if ("LocalSubnet" == addresses[0])
                    {
                        fire.Scope = Firewall.FirewallException.ScopeType.localSubnet;
                    }
                    else
                    {
                        FirewallDecompiler.AddRemoteAddress(fire, addresses[0]);
                    }
                }
                else
                {
                    foreach (string address in addresses)
                    {
                        FirewallDecompiler.AddRemoteAddress(fire, address);
                    }
                }

                if (!row.IsColumnEmpty(3))
                {
                    fire.Port = (string)row[3];
                }

                if (!row.IsColumnEmpty(4))
                {
                    switch (Convert.ToInt32(row[4]))
                    {
                        case FirewallConstants.NET_FW_IP_PROTOCOL_TCP:
                            fire.Protocol = Firewall.FirewallException.ProtocolType.tcp;
                            break;
                        case FirewallConstants.NET_FW_IP_PROTOCOL_UDP:
                            fire.Protocol = Firewall.FirewallException.ProtocolType.udp;
                            break;
                    }
                }

                if (!row.IsColumnEmpty(5))
                {
                    fire.Program = (string)row[5];
                }

                if (!row.IsColumnEmpty(6))
                {
                    int attr = Convert.ToInt32(row[6]);
                    if (0x1 == (attr & 0x1)) // feaIgnoreFailures
                    {
                        fire.IgnoreFailure = Firewall.YesNoType.yes;
                    }
                }

                if (!row.IsColumnEmpty(7))
                {
                    switch (Convert.ToInt32(row[7]))
                    {
                        case FirewallConstants.NET_FW_PROFILE2_DOMAIN:
                            fire.Profile = Firewall.FirewallException.ProfileType.domain;
                            break;
                        case FirewallConstants.NET_FW_PROFILE2_PRIVATE:
                            fire.Profile = Firewall.FirewallException.ProfileType.@private;
                            break;
                        case FirewallConstants.NET_FW_PROFILE2_PUBLIC:
                            fire.Profile = Firewall.FirewallException.ProfileType.@public;
                            break;
                        case FirewallConstants.NET_FW_PROFILE2_ALL:
                            fire.Profile = Firewall.FirewallException.ProfileType.all;
                            break;
                    }
                }

                // Description column is new in v3.6
                if (9 < row.Fields.Length && !row.IsColumnEmpty(9))
                {
                    fire.Description = (string)row[9];
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[8]);
                if (null != component)
                {
                    component.AddChild(fire);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[6], "Component"));
                }
            }
        }

        private static void AddRemoteAddress(Firewall.FirewallException fire, string address)
        {
            Firewall.RemoteAddress remote = new Firewall.RemoteAddress();
            remote.Content = address;
            fire.AddChild(remote);
        }
    }
}
