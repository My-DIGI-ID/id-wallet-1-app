using Autofac;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class GatewaysViewModel : CustomViewModel
    {
        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        public GatewaysViewModel()
        {
            Gateways = new ObservableCollection<Gateway>();
            try
            {
                Gateways = _secureStorageService.GetAsync<ObservableCollection<Gateway>>(WalletParams.AllGatewaysTag)
                    .GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                Gateways = new ObservableCollection<Gateway>();

                _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, Gateways);
            }
        }

        public ObservableCollection<Gateway> Gateways { get; set; }

        public async Task AddGateway(string name, string address, string key)
        {
            try
            {
                Gateway newGateway = new Gateway { Name = name, Address = address, Key = key };
                Gateways.Add(newGateway);
                ObservableCollection<Gateway> currentGateways =
                    await _secureStorageService.GetAsync<ObservableCollection<Gateway>>(WalletParams.AllGatewaysTag);

                if (currentGateways == null)
                {
                    currentGateways = new ObservableCollection<Gateway>();
                }

                currentGateways.Add(newGateway);
                await _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, Gateways);

                MessagingCenter.Send(this, WalletEvents.NewGateway);

                while (!App.ProofsLoaded)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public async Task DeleteGateway(Gateway gateway)
        {
            try
            {
                Gateways.Remove(gateway);
                await _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, Gateways);

                App.ProofsLoaded = false;
                MessagingCenter.Send(this, WalletEvents.NewGateway);

                while (!App.ProofsLoaded)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public async Task EditGateway(string name, string address, string key,
                    string oldName, string oldAddress, string oldKey)
        {
            try
            {
                System.Collections.Generic.List<Gateway> gatewayList = Gateways.ToList();
                int index = gatewayList.FindIndex(x => x.Name == oldName && x.Address == oldAddress && x.Key == oldKey);
                Gateway newGateway = new Gateway { Name = name, Address = address, Key = key };
                gatewayList[index] = newGateway;
                Gateways.Clear();
                foreach (Gateway gateway in gatewayList)
                {
                    Gateways.Add(gateway);
                }

                await _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, Gateways);

                App.ProofsLoaded = false;
                MessagingCenter.Send(this, WalletEvents.NewGateway);

                while (!App.ProofsLoaded)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}