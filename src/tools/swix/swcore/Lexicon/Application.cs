// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
