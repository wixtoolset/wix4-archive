//-------------------------------------------------------------------------------------------------
// <copyright file="IBinderFileManager.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    public interface IBinderFileManager
    {
        IBinderFileManagerCore Core { set; }

        ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<BindFileWithPath> files);

        string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        string ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        string ResolveMedia(MediaRow mediaRow, string mediaLayoutDirectory, string layoutDirectory);

        string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName);

        bool? CompareFiles(string targetFile, string updatedFile);

        bool CopyFile(string source, string destination, bool overwrite);

        bool MoveFile(string source, string destination, bool overwrite);
    }
}
