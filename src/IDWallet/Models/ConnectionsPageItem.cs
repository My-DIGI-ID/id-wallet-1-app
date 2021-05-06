using Hyperledger.Aries.Features.DidExchange;
using System;

namespace IDWallet.Models
{
    public class ConnectionsPageItem
    {
        public ConnectionsPageItem(ConnectionRecord connectionRecord, bool hasDetails)
        {
            ConnectionRecord = connectionRecord;
            HasDetails = hasDetails;
            ConnectionState = connectionRecord.State;
            Name = connectionRecord.Alias.Name;
            ImageUrl = string.IsNullOrEmpty(connectionRecord.Alias.ImageUrl)
                ? null
                : new Uri(connectionRecord.Alias.ImageUrl);
        }

        public ConnectionRecord ConnectionRecord { get; set; }
        public ConnectionState ConnectionState { get; set; }
        public bool HasDetails { get; set; }
        public Uri ImageUrl { get; set; }
        public string Name { get; set; }
    }
}