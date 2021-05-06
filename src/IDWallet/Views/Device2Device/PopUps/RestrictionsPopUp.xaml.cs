using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.PresentProof;
using System.Collections.Generic;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proofs.Device2Device.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RestrictionsPopUp : CustomPopUp
    {
        public RestrictionsPopUp(ProofAttributeInfo proofAttributeInfo, bool isPredicate = false)
        {
            RestrictionPopUpElement bindingContext = new RestrictionPopUpElement();
            if (isPredicate)
            {
                ProofPredicateInfo predicateInfo = (ProofPredicateInfo)proofAttributeInfo;
                bindingContext.Name = predicateInfo.Name + " " +
                                      predicateInfo.PredicateType + " " +
                                      predicateInfo.PredicateValue.ToString();
            }
            else
            {
                bindingContext.Name = proofAttributeInfo.Name;
            }

            bindingContext.Restrictions = new List<RestrictionSet>();
            foreach (AttributeFilter attributeFilter in proofAttributeInfo.Restrictions)
            {
                RestrictionSet newSet = new RestrictionSet { RestrictionContent = new List<SingleRestriction>() };
                if (!string.IsNullOrEmpty(attributeFilter.SchemaId))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Schema Id",
                            RestrictionValue = attributeFilter.SchemaId
                        });
                }

                if (!string.IsNullOrEmpty(attributeFilter.SchemaName))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Schema Name",
                            RestrictionValue = attributeFilter.SchemaName
                        });
                }

                if (!string.IsNullOrEmpty(attributeFilter.SchemaVersion))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Schema Version",
                            RestrictionValue = attributeFilter.SchemaVersion
                        });
                }

                if (!string.IsNullOrEmpty(attributeFilter.SchemaIssuerDid))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Schema Issuer Did",
                            RestrictionValue = attributeFilter.SchemaIssuerDid
                        });
                }

                if (!string.IsNullOrEmpty(attributeFilter.CredentialDefinitionId))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Credential Definition Id",
                            RestrictionValue = attributeFilter.CredentialDefinitionId
                        });
                }

                if (!string.IsNullOrEmpty(attributeFilter.IssuerDid))
                {
                    newSet.RestrictionContent.Add(
                        new SingleRestriction
                        {
                            RestrictionType = "Issuer Did",
                            RestrictionValue = attributeFilter.IssuerDid
                        });
                }

                bindingContext.Restrictions.Add(newSet);
            }

            BindingContext = bindingContext;
            InitializeComponent();
        }
    }
}