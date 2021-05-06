using Hyperledger.Aries.Agents;
using Newtonsoft.Json;
using System;

namespace IDWallet.Agent.Messages
{
    public class TransactionResponseMessage : AgentMessage
    {
        public TransactionResponseMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.TransactionResponse;
        }

        [JsonProperty("comment")] public string Comment { get; set; }

        [JsonProperty("transaction")] public string Transaction { get; set; }
    }
}