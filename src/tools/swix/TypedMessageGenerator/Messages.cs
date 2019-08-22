// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    internal class MessageData
    {
        public MessageData(string filename)
        {
            this.Filename = filename;
            this.Messages = new List<Message>();
            this.Types = new List<MessageType>();
        }

        public string Filename { get; private set; }
        public List<Message> Messages { get; private set; }
        public List<MessageType> Types { get; private set; }
    }

    internal class Message
    {
        public Message(int lineNumber, MessageType type, string name, int id = -1)
        {
            this.PragmaLine = lineNumber + 1;
            this.Type = type;
            this.Name = name;
            this.Id = id;
            this.Instances = new List<Instance>();
        }

        public int PragmaLine { get; private set; }
        public MessageType Type { get; private set; }
        public string Name { get; private set; }
        public int Id { get; private set; }
        public List<Instance> Instances { get; private set; }

        public string Error { get; set; }  // can be set externally!

        public void NameInstances()
        {
            // Create names for the instances...
            if (this.Instances.Count == 1)
            {
                this.Instances[0].SetName(this.Name);
            }
            else if (this.Instances.Count > 1)
            {
                int suffixLength = (int)Math.Ceiling(Math.Log10(this.Instances.Count));
                string suffixFormat = string.Concat("D", suffixLength.ToString());
                int instanceId = 0;

                foreach (Instance instance in this.Instances)
                {
                    instance.SetName(string.Format("{0}_{1}", this.Name, instanceId.ToString(suffixFormat)));
                    ++instanceId;
                }
            }
        }

        public void SetAutoId(int id)
        {
            if (this.Id != -1)
            {
                throw new Exception(
                    string.Format(
                        "Cannot set auto-id for {0} to {1}... it already has authored id {2}.",
                        this.Name,
                        id,
                        this.Id));
            }

            this.Id = id;
        }
    }

    internal class MessageType
    {
        public MessageType(string name, int firstId, int lastId)
        {
            this.Name = name;
            this.FirstId = firstId;
            this.LastId = lastId;
        }

        public string Name { get; private set; }
        public int FirstId { get; private set; }
        public int LastId { get; private set; }

        public string Error { get; set; }  // can be set externally!
    }

    internal class Instance
    {
        public Instance(int lineNumber, string originalMessage, string message, List<Tuple<string, Type>> parameterList)
        {
            this.PragmaLine = lineNumber + 1;
            this.OriginalMessage = originalMessage;
            this.Message = message;
            this.ParameterList = parameterList;
        }

        public int PragmaLine { get; private set; }
        public string OriginalMessage { get; private set; }
        public string Message { get; private set; }
        public List<Tuple<string, Type>> ParameterList { get; private set; }
        public string Name { get; private set; }

        public string Error { get; set; }  // can be set externally!

        public void SetName(string name)
        {
            this.Name = name;
        }
    }
}
