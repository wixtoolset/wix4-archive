//-------------------------------------------------------------------------------------------------
// <copyright file="ResxGeneration.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    internal class ResxGeneration
    {
        public byte[] GenerateResx(string codeNamespace, MessageData messages, string className)
        {
            byte[] results = null;

            MemoryStream stream = null;
            ResXResourceWriter writer = null;

            try
            {
                stream = new MemoryStream();
                writer = new ResXResourceWriter(stream);

                // Add the messages...
                foreach (Message message in messages.Messages)
                {
                    foreach (Instance instance in message.Instances)
                    {
                        ResXDataNode node = new ResXDataNode(
                            string.Concat(className, ".", instance.Name),
                            instance.Message);

                        // Add comments if there are params...
                        if (instance.ParameterList != null && instance.ParameterList.Count > 0)
                        {
                            StringBuilder builder = new StringBuilder();

                            builder.Append("params: ");

                            foreach (var param in instance.ParameterList)
                            {
                                builder.AppendFormat("[{1}]{0}, ", param.Item1, param.Item2.Name);
                            }

                            // remove the trailing ", "...
                            builder.Length -= 2;

                            node.Comment = builder.ToString();
                        }

                        writer.AddResource(node);
                    }
                }

                writer.Generate();
                results = stream.ToArray();
                stream = null;
            }
            finally
            {
                if (writer != null)
                {
                    // stream will be disposed by the writer...
                    stream = null;
                    writer.Dispose();
                    writer = null;
                }
                
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }

            return results;
        }
    }
}
