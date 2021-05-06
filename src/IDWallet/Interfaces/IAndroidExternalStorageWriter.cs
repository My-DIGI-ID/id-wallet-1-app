namespace IDWallet.Interfaces
{
    public interface IAndroidExternalStorageWriter
    {
        string CreateFile(string filename, byte[] bytes);
    }
}