using Autofac;
using IDWallet.Agent.Interface;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.IssueCredential;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace IDWallet.Services
{
    public class CheckRevocationService
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ILedgerService _ledgerService = App.Container.Resolve<ILedgerService>();

        public async Task<bool> NonRevoked(CredentialInfo credentialInfo, CredentialRecord credentialRecord)
        {
            NetworkAccess connectivity = Connectivity.NetworkAccess;
            if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
            {
                return true;
            }

            try
            {
                if (credentialInfo.RevocationRegistryId != null)
                {
                    Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();
                    long currentDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    Hyperledger.Indy.LedgerApi.ParseRegistryResponseResult state =
                        await _ledgerService.LookupRevocationRegistryDeltaAsync(agentContext,
                            credentialInfo.RevocationRegistryId, 0, currentDate);
                    JToken revoked = JArray.Parse("[" + state.ObjectJson + "]")[0]["value"]["revoked"];
                    JToken issued = JArray.Parse("[" + state.ObjectJson + "]")[0]["value"]["issued"];
                    if (revoked == null && issued == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (revoked != null && issued == null)
                        {
                            if (revoked.Where(x => x.ToString().Equals(credentialInfo.CredentialRevocationId)).Any())
                            {
                                return false;
                            }
                        }
                        else if (revoked == null && issued != null)
                        {
                            if (!issued.Where(x => x.ToString().Equals(credentialInfo.CredentialRevocationId)).Any())
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (revoked.Where(x => x.ToString().Equals(credentialInfo.CredentialRevocationId)).Any() &&
                                !issued.Where(x => x.ToString().Equals(credentialInfo.CredentialRevocationId)).Any())
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}