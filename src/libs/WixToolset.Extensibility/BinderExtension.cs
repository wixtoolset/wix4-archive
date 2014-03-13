//-------------------------------------------------------------------------------------------------
// <copyright file="BinderExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating an binder extension.
    /// </summary>
    public abstract class BinderExtension : IBinderExtension
    {
        /// <summary>
        /// Gets or sets the binder core for the extension.
        /// </summary>
        /// <value>Binder core for the extension.</value>
        public IBinderCore Core { get; set; }


        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        public virtual void Initialize(Output output)
        {
        }

        /// <summary>
        /// Called after variable resolution occurs.
        /// </summary>
        public virtual void AfterResolvedFields(Output output)
        {
        }

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public virtual void Finish(Output output)
        {
        }
    }
}