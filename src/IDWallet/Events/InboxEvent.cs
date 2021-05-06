using System.Collections.Generic;

namespace IDWallet.Events
{
    public class InboxEvent
    {
        public IDictionary<string, string> CustomData;
        public string Message;
        public string Title;
        public override string ToString()
        {
            string summary = $"Push notification received:" +
                             $"\n\tNotification title: {Title}" +
                             $"\n\tMessage: {Message}";

            if (CustomData != null)
            {
                summary += "\n\tCustom data:\n";
                foreach (string key in CustomData.Keys)
                {
                    summary += $"\t\t{key} : {CustomData[key]}\n";
                }
            }

            return summary;
        }
    }
}