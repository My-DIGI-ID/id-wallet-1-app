namespace IDWallet.Services.Interfaces
{
    using System.Globalization;

    public interface IL10nService
    {
        CultureInfo Language { get; set; }

        string GetLocalizedText(string name);

        string GetLocalizedText(string name, CultureInfo cultureInfo);

        string GetLocalizedText(string nsKey, string tKey, string name);
    }
}