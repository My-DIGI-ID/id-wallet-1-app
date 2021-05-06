using Hyperledger.Aries.Storage;

namespace IDWallet.Agent.Models
{
    public class PinRecord : RecordBase
    {
        public override string TypeName => "PinRecord";

        public byte[] WalletPinSaltByte { get; set; }

        public byte[] WalletPinPBKDF2 { get; set; }
    }
}
