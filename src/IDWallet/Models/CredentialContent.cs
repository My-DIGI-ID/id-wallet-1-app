using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;

namespace IDWallet.Models
{
    public class CredentialContent
    {
        public List<CredentialContentAttribute> Attributes { get; set; } = new List<CredentialContentAttribute>();
        public ConnectionRecord ConnectionRecord { get; set; }
        public CredentialRecord CredentialRecord { get; set; }
        public string State { get; set; }
        public string Title { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class CredentialContentAttribute
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}