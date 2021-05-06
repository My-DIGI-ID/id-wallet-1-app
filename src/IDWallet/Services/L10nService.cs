using IDWallet.Services.Interfaces;
using System;
using System.Globalization;
using System.Resources;

namespace IDWallet.Services
{
    public class L10nService : IL10nService
    {
        private readonly ResourceManager _resourceManager;
        private CultureInfo _cultureInfo = CultureInfo.CurrentCulture;

        public L10nService()
        {
        }

        public L10nService(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public CultureInfo Language
        {
            get => _cultureInfo;
            set
            {
                if (!_cultureInfo.Equals(value))
                {
                    _cultureInfo = value;
                }
            }
        }
        public string GetLocalizedText(string name)
        {
            try
            {
                string resource = _resourceManager.GetString(name, Language);
                return resource.Replace("\\n", Environment.NewLine);
            }
            catch (Exception)
            {
                return name;
            }
        }

        public string GetLocalizedText(string name, CultureInfo cultureInfo)
        {
            try
            {
                string resource = _resourceManager.GetString(name, cultureInfo);
                return resource.Replace("\\n", Environment.NewLine);
            }
            catch (Exception)
            {
                return name;
            }
        }

        public string GetLocalizedText(string nsKey, string tKey, string name)
        {
            string rKey = name;

            if (!string.IsNullOrEmpty(tKey))
            {
                rKey = $"{tKey}.{rKey}";
            }

            if (!string.IsNullOrEmpty(nsKey))
            {
                rKey = $"{nsKey}.{rKey}";
            }

            try
            {
                return _resourceManager.GetString(rKey, Language).Replace("\\n", Environment.NewLine);
            }
            catch (Exception)
            {
                return name;
            }
        }
    }
}