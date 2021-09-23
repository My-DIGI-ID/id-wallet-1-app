namespace IDWallet.Interfaces
{
    public interface IHardwareKeyService
    {
        string GetPublicKeyAsBase64(byte[] nonce, string alias);
        string Sign(byte[] nonce, string alias);
    }
}
