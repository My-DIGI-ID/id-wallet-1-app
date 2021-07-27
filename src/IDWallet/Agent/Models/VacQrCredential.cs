using Hyperledger.Aries.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDWallet.Agent.Models
{
    public class VacQrCredential : RecordBase
    {
        public string QrContent { get; set; }

        public string Name { get; set; }

        public override string TypeName => "IDW.VacQrCredential";
    }
}
