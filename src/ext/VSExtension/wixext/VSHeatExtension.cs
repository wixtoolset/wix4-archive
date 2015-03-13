//-------------------------------------------------------------------------------------------------
// <copyright file="VSHeatExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// VS-related extensions for the WiX Toolset Harvester application.
// </summary>
//-------------------------------------------------------------------------------------------------

using System.Net.Mime;
using System.Text.RegularExpressions;

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Tools;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// Defines generated element types.
    /// </summary>
    public enum GenerateType
    {
        /// <summary>Generate Components.</summary>
        Components,

        /// <summary>Generate a Conatiner with Payloads.</summary>
        Container,

        /// <summary>Generate a Bundle Layout.</summary>
        Layout,

        /// <summary>Generate a Bundle PackageGroups.</summary>
        PackageGroup,

        /// <summary>Generate a PayloadGroup with Payloads.</summary>
        PayloadGroup
    }

    /// <summary>
    /// VS-related extensions for the WiX Toolset Harvester application.
    /// </summary>
    public sealed class VSHeatExtension : HeatExtension
    {
        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        public override HeatCommandLineOption[] CommandLineTypes
        {
            get
            {
                return new HeatCommandLineOption[]
                {
                    new HeatCommandLineOption("project", "harvest outputs of a VS project"),
                    new HeatCommandLineOption("-configuration", "configuration to set when harvesting the project"),
                    new HeatCommandLineOption("-directoryid", "overridden directory id for generated directory elements"),
                    new HeatCommandLineOption("-generate", Environment.NewLine +
                        "            specify what elements to generate, one of:" + Environment.NewLine + 
                        "                components, container, payloadgroup, layout, packagegroup" + Environment.NewLine +
                        "                (default is components)"),
                    new HeatCommandLineOption("-platform", "platform to set when harvesting the project"),
                    new HeatCommandLineOption("-pog", Environment.NewLine +
                        "            specify output group of VS project, one of:" + Environment.NewLine +
                        "                " + String.Join(",", VSProjectHarvester.GetOutputGroupNames()) + Environment.NewLine +
                        "              This option may be repeated for multiple output groups."),
                    new HeatCommandLineOption("-projectname", "overridden project name to use in variables"),
                    new HeatCommandLineOption("-wixvar", "generate binder variables instead of preprocessor variables"),
                };
            }
        }

        /// <summary>
        /// Parse the command line options for this extension.
        /// </summary>
        /// <param name="type">The active harvester type.</param>
        /// <param name="args">The option arguments.</param>
        public override void ParseOptions(string type, string[] args)
        {
            if ("project" == type)
            {
                string[] allOutputGroups = VSProjectHarvester.GetOutputGroupNames();
                bool suppressUniqueId = false;
                bool generateWixVars = false;
                GenerateType generateType = GenerateType.Components;
                string directoryIds = null;
                string projectName = null;
                string configuration = null;
                string platform = null;
                ArrayList outputGroups = new ArrayList();

                for (int i = 0; i < args.Length; i++)
                {
                    if ("-configuration" == args[i])
                    {
                        configuration = args[++i];
                    }
                    else if ("-directoryid" == args[i])
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            throw new WixException(VSErrors.InvalidDirectoryId(args[i]));
                        }
                        
                        directoryIds = args[i];
                    }
                    else if ("-generate" == args[i])
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            throw new WixException(VSErrors.InvalidOutputType(args[i]));
                        }

                        string genType = args[i].ToUpperInvariant();
                        switch(genType)
                        {
                            case "LAYOUT":
                                generateType = GenerateType.Layout;
                                break;
                            case "CONTAINER":
                                generateType = GenerateType.Container;
                                break;
                            case "COMPONENTS":
                                generateType = GenerateType.Components;
                                break;
                            case "PACKAGEGROUP":
                                generateType = GenerateType.PackageGroup;
                                break;
                            case "PAYLOADGROUP":
                                generateType = GenerateType.PayloadGroup;
                                break;
                            default:
                                throw new WixException(VSErrors.InvalidOutputType(genType));
                        }
                    }
                    else if ("-platform" == args[i])
                    {
                        platform = args[++i];
                    }
                    else if ("-pog" == args[i])
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            throw new WixException(VSErrors.InvalidOutputGroup(args[i]));
                        }

                        string pogName = args[i];
                        bool found = false;
                        foreach (string availableOutputGroup in allOutputGroups)
                        {
                            // Allow for specifying the name along with a Metadata filter that can be applied to include/exclude items
                            // The syntax for that will be "{pogName}:{Metadata Filter Expression}"
                            // eg.  References:CopyLocal=true   or References:CopyLocal!=true
                            Match expressive = Regex.Match(pogName, "([^:]+):(.*)", RegexOptions.IgnoreCase);
                            if (expressive.Success)
                            {
                                pogName = expressive.Groups[1].Value;
                                VSProjectHarvester.SetMetadataFilter(pogName, expressive.Groups[2].Value);
                            }
                            if (String.Equals(pogName, availableOutputGroup, StringComparison.Ordinal))
                            {
                                outputGroups.Add(availableOutputGroup);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new WixException(VSErrors.InvalidOutputGroup(pogName));
                        }
                    }
                    else if (args[i].StartsWith("-pog:", StringComparison.Ordinal))
                    {
                        this.Core.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("pog:", "pog"));

                        string pogName = args[i].Substring(5);
                        bool found = false;
                        foreach (string availableOutputGroup in allOutputGroups)
                        {
                            if (String.Equals(pogName, availableOutputGroup, StringComparison.Ordinal))
                            {
                                outputGroups.Add(availableOutputGroup);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new WixException(VSErrors.InvalidOutputGroup(pogName));
                        }
                    }
                    else if ("-projectname" == args[i])
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            throw new WixException(VSErrors.InvalidProjectName(args[i]));
                        }
                        
                        projectName = args[i];
                    }
                    else if ("-suid" == args[i])
                    {
                        suppressUniqueId = true;
                    }
                    else if ("-wixvar" == args[i])
                    {
                        generateWixVars = true;
                    }
                }

                if (outputGroups.Count == 0)
                {
                    throw new WixException(VSErrors.NoOutputGroupSpecified());
                }

                VSProjectHarvester harvester = new VSProjectHarvester(
                    (string[]) outputGroups.ToArray(typeof(string)));

                harvester.SetUniqueIdentifiers = !suppressUniqueId;
                harvester.GenerateWixVars = generateWixVars;
                harvester.GenerateType = generateType;
                harvester.DirectoryIds = directoryIds;
                harvester.ProjectName = projectName;
                harvester.Configuration = configuration;
                harvester.Platform = platform;

                this.Core.Harvester.Extension = harvester;
            }
        }
    }
}
