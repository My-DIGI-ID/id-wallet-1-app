using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDWallet.Agent.Interface
{
    public interface ICustomAgentProvider : IAgentProvider
    {
        Task AddDevice(IAgentContext agentContext);

        bool AgentExists();

        bool AgentExists(AgentOptions agentOptions = null);

        Task<bool> CreateAgentAsync(AgentOptions agentOptions, string poolFile = null, string pin = "");

        Task<bool> OpenWallet(AgentOptions agentOptions, string pin, string oldPin = "");

        Task SwitchLedger(AgentOptions agentOptions, string poolFile = null);

        Task CreateInboxAtMediator();

        AgentOptions GetActiveAgentOptions();

        AgentOptions GetAgentOptionsRecommendedLedger(string recommendedLedgerName);

        List<AgentOptions> GetAllAgentOptions();

        new Task<IAgentContext> GetContextAsync(params object[] args);

        string GetPoolName(AgentOptions agentOptions);

        Task ImportAgentAsync(string importConfig);

        Task StoreAgentConfigs(List<AgentOptions> agentOptionsList);

        Task<AgentOptions> GetActiveAgent();
    }
}