namespace IDWallet.Interfaces
{
    public interface IHardwareKeyService
    {
        string GetPublicKeyAsBase64(byte[] nonce);
        string Sign(byte[] nonce);
    }
}
