namespace IDWallet.Agent.Messages
{
    public class CustomMessageTypes
    {
        public const string DeleteProofs = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/privacy/1.0/deleteproofs";
        public const string DeleteProofsHttps = "https://didcomm.org/privacy/1.0/deleteproofs";
        public const string ProofsDeleted = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/privacy/1.0/proofsdeleted";
        public const string ProofsDeletedHttps = "https://didcomm.org/privacy/1.0/proofsdeleted";
        public const string TransactionError = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/transaction/1.0/error";
        public const string TransactionErrorHttps = "https://didcomm.org/transaction/1.0/error";
        public const string TransactionOffer = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/transaction/1.0/offer";
        public const string TransactionOfferHttps = "https://didcomm.org/transaction/1.0/offer";
        public const string TransactionResponse = "did:sov:BzCbsNYhMrjHiqZDTUASHg;spec/transaction/1.0/response";
        public const string TransactionResponseHttps = "https://didcomm.org/transaction/1.0/response";
    }
}