namespace IDWallet.Interfaces
{
    public interface ISecurityChecks
    {
        public void SafetyCheck(byte[] nonce);
    }
}
