//-------------------------------------------------------------------------------------------------
// <copyright file="BindDatabaseCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections;
    using System.Globalization;
    using WixToolset.MergeMod;

    /// <summary>
    /// Callback object for configurable merge modules.
    /// </summary>
    internal sealed class ConfigurationCallback : IMsmConfigureModule
    {
        private const int SOk = 0x0;
        private const int SFalse = 0x1;
        private Hashtable configurationData;

        /// <summary>
        /// Creates a ConfigurationCallback object.
        /// </summary>
        /// <param name="configData">String to break up into name/value pairs.</param>
        public ConfigurationCallback(string configData)
        {
            if (String.IsNullOrEmpty(configData))
            {
                throw new ArgumentNullException("configData");
            }

            string[] pairs = configData.Split(',');
            this.configurationData = new Hashtable(pairs.Length);
            for (int i = 0; i < pairs.Length; ++i)
            {
                string[] nameVal = pairs[i].Split('=');
                string name = nameVal[0];
                string value = nameVal[1];

                name = name.Replace("%2C", ",");
                name = name.Replace("%3D", "=");
                name = name.Replace("%25", "%");

                value = value.Replace("%2C", ",");
                value = value.Replace("%3D", "=");
                value = value.Replace("%25", "%");

                this.configurationData[name] = value;
            }
        }

        /// <summary>
        /// Returns text data based on name.
        /// </summary>
        /// <param name="name">Name of value to return.</param>
        /// <param name="configData">Out param to put configuration data into.</param>
        /// <returns>S_OK if value provided, S_FALSE if not.</returns>
        public int ProvideTextData(string name, out string configData)
        {
            if (this.configurationData.Contains(name))
            {
                configData = (string)this.configurationData[name];
                return SOk;
            }
            else
            {
                configData = null;
                return SFalse;
            }
        }

        /// <summary>
        /// Returns integer data based on name.
        /// </summary>
        /// <param name="name">Name of value to return.</param>
        /// <param name="configData">Out param to put configuration data into.</param>
        /// <returns>S_OK if value provided, S_FALSE if not.</returns>
        public int ProvideIntegerData(string name, out int configData)
        {
            if (this.configurationData.Contains(name))
            {
                string val = (string)this.configurationData[name];
                configData = Convert.ToInt32(val, CultureInfo.InvariantCulture);
                return SOk;
            }
            else
            {
                configData = 0;
                return SFalse;
            }
        }
    }
}
