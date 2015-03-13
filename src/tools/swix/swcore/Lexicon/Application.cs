//-------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public class Application : Group
    {
        public string DisplayName { get; set; }

        public string Description { get; set; }

        [TypeConverter(typeof(IdTypeConverter))]
        public File File { get; set; }

        public string Name { get; set; }

        public string Implementation { get; set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            if (String.IsNullOrEmpty(this.Name))
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ApplicationNameRequired(), this));

                if (String.IsNullOrEmpty(this.Id))
                {
                    this.Id = "$invalid_application_id$";
                }
            }
            else if (String.IsNullOrEmpty(this.Id))
            {
                this.Id = this.Name.Replace('.', '_');
            }

            base.OnResolveBegin(context);
        }
    }
}
