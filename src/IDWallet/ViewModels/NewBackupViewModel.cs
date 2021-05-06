using dotnetstandard_bip39;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace IDWallet.ViewModels
{
    public class NewBackupViewModel : CustomViewModel
    {
        private static readonly BIP39 Bip39 = new BIP39();
        private string _fileTitle;
        public NewBackupViewModel()
        {
            SeedWords = new List<string>();
            DisplayedWords1 = new ObservableCollection<string>();
            DisplayedWords2 = new ObservableCollection<string>();

            string mnemonic = Bip39.GenerateMnemonic(128, BIP39Wordlist.English);
            Entropy = Bip39.MnemonicToEntropy(mnemonic, BIP39Wordlist.English);

            SeedWords = SeedWords.Concat(mnemonic.Replace("\r", "").Split(' ')).ToList();
            foreach (string word in SeedWords)
            {
                int index = (SeedWords.IndexOf(word) + 1);
                if (index < 7)
                {
                    string displayedWord = index.ToString() + ".  " + word;
                    DisplayedWords1.Add(displayedWord);
                }
                else
                {
                    if (index < 10)
                    {
                        string displayedWord = index.ToString() + ".  " + word;
                        DisplayedWords2.Add(displayedWord);
                    }
                    else
                    {
                        string displayedWord = index.ToString() + ". " + word;
                        DisplayedWords2.Add(displayedWord);
                    }
                }
            }
        }

        public ObservableCollection<string> DisplayedWords1 { get; set; }

        public ObservableCollection<string> DisplayedWords2 { get; set; }

        public string Entropy { get; set; }

        public string FileTitle
        {
            get => _fileTitle;
            set => SetProperty(ref _fileTitle, value);
        }
        public List<string> SeedWords { get; set; }
        public bool VerifySeedPhrase(string[] chosenSeeds)
        {
            int index = SeedWords.Count - 1;
            while (index >= 0)
            {
                if (chosenSeeds[index] == SeedWords[index])
                {
                    index -= 1;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}