using IDWallet.Interfaces;
using IDWallet.Views.Customs.PopUps;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class AppContext
    {
        private static readonly List<IAppContext> _sessions = new List<IAppContext>();

        public static void Register(IAppContext session)
        {
            _sessions.Add(session);
        }

        public static void Restore()
        {
            foreach (IAppContext session in _sessions)
            {
                session.Restore();
            }
        }

        public static object Restore(string key, Type typeObject)
        {
            if (!Application.Current.Properties.ContainsKey(key))
            {
                return null;
            }

            string json = (string)Application.Current.Properties[key];
            return JsonConvert.DeserializeObject(json, typeObject);
        }

        public static object Restore(Type typeObject)
        {
            string key = typeObject.ToString();

            return Restore(key, typeObject);
        }

        public static void Save()
        {
            foreach (IAppContext session in _sessions)
            {
                session.Save();
            }
        }

        public static void Save(string key, object dataObject)
        {
            try
            {
                string json = JsonConvert.SerializeObject(dataObject);
                Application.Current.Properties[key] = json;
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Save_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                alertPopUp.ShowPopUp().Wait();
            }
        }

        public static void Save(object dataObject)
        {
            string key = dataObject.GetType().ToString();
            Save(key, dataObject);
        }

        public static void Unregister(IAppContext session)
        {
            _sessions.Remove(session);
        }
    }
}