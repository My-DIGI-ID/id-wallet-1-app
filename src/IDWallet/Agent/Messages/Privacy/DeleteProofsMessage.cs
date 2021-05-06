using Hyperledger.Aries.Agents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IDWallet.Agent.Messages.Privacy
{
    public class DeleteProofsMessage : AgentMessage
    {
        public DeleteProofsMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.DeleteProofs;
        }

        [JsonProperty("comment")] public string Comment { get; set; }

        [JsonProperty("delete_ids")] public List<string> DeleteIds { get; set; }
    }
}