using Hyperledger.Aries.Agents;
using System.Collections.Concurrent;

namespace IDWallet.Agent.Models
{
    public class CustomAgentContext : DefaultAgentContext
    {
        private readonly ConcurrentQueue<MessageContext> _queue = new ConcurrentQueue<MessageContext>();

        public void AddNext(MessageContext message)
        {
            _queue.Enqueue(message);
        }

        public bool TryGetNext(out MessageContext message)
        {
            return _queue.TryDequeue(out message);
        }
    }
}