namespace IDWallet.Interfaces
{
    public interface IAppContext
    {
        void Restore();

        void Save();
    }
}