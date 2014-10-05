//-------------------------------------------------------------------------------------------------
// <copyright file="CreateContainerCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WixToolset.Cab;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Creates cabinet files.
    /// </summary>
    internal class CreateContainerCommand : ICommand
    {
        public CompressionLevel DefaultCompressionLevel { private get; set; }

        public IEnumerable<PayloadRow> Payloads { private get; set; }

        public string ManifestFile { private get; set; }

        public string OutputPath { private get; set; }

        public string Hash { get; private set; }

        public long Size { get; private set; }

        public void Execute()
        {
            int payloadCount = this.Payloads.Count(); // The number of embedded payloads

            if (!String.IsNullOrEmpty(this.ManifestFile))
            {
                ++payloadCount;
            }

            using (WixCreateCab cab = new WixCreateCab(Path.GetFileName(this.OutputPath), Path.GetDirectoryName(this.OutputPath), payloadCount, 0, 0, this.DefaultCompressionLevel))
            {
                // If a manifest was provided always add it as "payload 0" to the container.
                if (!String.IsNullOrEmpty(this.ManifestFile))
                {
                    cab.AddFile(this.ManifestFile, "0");
                }

                foreach (PayloadRow payload in this.Payloads)
                {
                    Debug.Assert(PackagingType.Embedded == payload.Packaging);

                    Messaging.Instance.OnMessage(WixVerboses.LoadingPayload(payload.FullFileName));

                    cab.AddFile(payload.FullFileName, payload.EmbeddedId);
                }

                cab.Complete();
            }

            // Now that the container is created, set the outputs of the command.
            FileInfo fileInfo = new FileInfo(this.OutputPath);

            this.Hash = Common.GetFileHash(fileInfo.FullName);

            this.Size = fileInfo.Length;
        }
    }
}
