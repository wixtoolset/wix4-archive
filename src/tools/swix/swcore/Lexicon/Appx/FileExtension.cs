//-------------------------------------------------------------------------------------------------
// <copyright file="FileExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    /// <summary>
    /// File extension supported by shareTarget, sendTarget and filePicker extensions.
    /// </summary>
    public class FileExtension : SupportedDataFormat
    {
        public FileExtension()
        {
            this.typeName = "FileExtension";
        }
    }
}
