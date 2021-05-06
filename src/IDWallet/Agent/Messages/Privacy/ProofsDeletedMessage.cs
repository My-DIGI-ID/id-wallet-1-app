using Hyperledger.Aries.Agents;
using Newtonsoft.Json;
using System;

namespace IDWallet.Agent.Messages.Privacy
{
    public class ProofsDeletedMessage : AgentMessage
    {
        public ProofsDeletedMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.ProofsDeleted;
        }

        [JsonProperty("comment")] public string Comment { get; set; }

        [JsonProperty("deleted_proofs")] public int DeletedProofs { get; set; }
    }
}