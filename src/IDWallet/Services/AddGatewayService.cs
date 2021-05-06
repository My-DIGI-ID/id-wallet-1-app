using Autofac;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using Hyperledger.Aries.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class AddGatewayService
    {
        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        public async Task AddGateway(GatewayQR gatewayQR)
        {
            App.ProofsLoaded = false;

            Gateway gateway = new Gateway()
            { Name = gatewayQR.GwName, Address = gatewayQR.GwAddress, Key = gatewayQR.GwKey };

            List<Gateway> currentGateways;
            try
            {
                currentGateways = await _secureStorageService.GetAsync<List<Gateway>>(WalletParams.AllGatewaysTag);
            }
            catch (Exception)
            {
                currentGateways = new List<Gateway>();
            }

            currentGateways.Add(gateway);
            await _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, currentGateways);

            MessagingCenter.Send(this, WalletEvents.NewGateway);

            while (!App.ProofsLoaded)
            {
                await Task.Delay(100);
            }
        }

        public GatewayQR ReadGatewayJson(string gatewayJson)
        {
            GatewayQR gatewayQR = null;
            try
            {
                gatewayQR = gatewayJson.ToObject<GatewayQR>();
            }
            catch (Exception)
            {
                try
                {
                    gatewayQR = gatewayJson.FromBase64().ToObject<GatewayQR>();
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            return gatewayQR;
        }
    }
}