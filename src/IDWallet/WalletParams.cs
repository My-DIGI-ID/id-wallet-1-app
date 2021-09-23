namespace IDWallet
{
    public static class WalletParams
    {
        public const string MediatorEndpoint = "";
        public const string AllGatewaysTag = "AllGateways";
        public const string APNTemplateBody = "{\"aps\":{\"alert\":\"$(messageParam)\"}}";
        public const string AppLinkDidcommTag = "didcomm";
        public const string AppLinkIdTag = "id-wallet";
        public const string FCMTemplateBody = "{\"data\":{\"message\":\"$(messageParam)\"}}";
        public const string HistoryCredentialsTag = "HistoryCredentials";
        public const string IntroCompletedTag = "IntroCompleted";
        public const string IsRevokedOnLedgerTag = "IsRevokedOnLedger";
        public const string KeyAppBadLoginTime = "AppBadLoginTime";
        public const string KeyAppBadPwdCount = "AppBadPwdCount";
        public const string KeyAppBadPwdCountOverall = "AppBadPwdCountOverall";
        public const string KeyBiometricActivated = "IsBiometricsActivated";
        public const string KeyForceFocusActivated = "IsForceFocusActivated";
        public const string KeyLanguage = "AppLanguage";
        public const string KeyRevocationPassphrase = "RevocationPassphrase";

        public const string LogoName = "logo_splash.png";
        public const string MediatorConnectionAliasName = "";
        public const string NewPnsHandle = "NewPnsHandle";
        public const string NotificationChannelName = "XamarinNotifyChannel";

        public const int PinLength = 6;
        public const string PnsHandle = "PushToken";
        public const string PollingHandle = "PollingToken";
        public const string PollingWasActive = "PollingWasActive";
        public const string ProofCallingEndpoints = "";
        public const string PushService = "PushService";
        public const string PinRecordTag = "PinRecord";
        public const string PushServiceName = "";
        public const string RecipientKeys = "RecipientKeys";
        public const string RecommendedLedger_BCGov = "bcgov";
        public const string RecommendedLedger_Builder = "builder";
        public const string RecommendedLedger_DEVLEDGER = "devledger";
        public const string RecommendedLedger_DGCDEVLEDGER = "dgcdevledger";
        public const string RecommendedLedger_EESDI = "eesdi";
        public const string RecommendedLedger_EESDITest = "eesditest";
        public const string RecommendedLedger_Esatus = "esatus";
        public const string RecommendedLedger_IDuniontest = "iduniontest";
        public const string RecommendedLedger_Live = "live";
        public const string RecommendedLedger_Staging = "staging";
        public const string SecondIosDeviceMigration = "SecondIosDeviceMigration";
        public const string ShowMediatorConnection = "ShowMediatorConnection";
        public const string ShowSendingResponsePopUp = "ShowSendingResponsePopUp";
        public const string TransactionConnectionId = "TransactionConnectionId";
        public const string UseMediatorImages = "UseMediatorImages";
        public const string WalletKeyTag = "WalletKey";
        public const string WalletPreKeyTag = "WalletPreKey";
        public const string WalletSaltByteTag = "WalletSalt";
        public const string VacConnection = "VacConnection";

        public const string DdlSchemaId = "KqtBRiQSyWqnzaxN3u2d7G:2:Digitaler Fuehrerschein:0.3";
        public const string DdlCredentialId = "KqtBRiQSyWqnzaxN3u2d7G:3:CL:1260:Digitaler Führerschein";
        public const string DdlDemoSchemaId = "9hsRe5jdzAbbyLAStV6sPc:2:Digitaler Fuehrerschein:0.3";
        public const string DdlDemoCredentialId = "9hsRe5jdzAbbyLAStV6sPc:3:CL:429:Digitaler Führerschein Test";
        public const string DdlDemoCredentialIdOld = "9hsRe5jdzAbbyLAStV6sPc:3:CL:429:Digitaler Fuehrerschein Developer";

        //Change per Version
        public const string AppVersion = "1.6";
        public const string BuildVersion = "10617";
        public const string WalletName = "ID Wallet";
        public const string SafetyNetApiKey = "";
        public const string NotificationHubName = "";
        public const string ListenConnectionString = "";
        public const string PackageName = "com.digitalenabling.idw";
        public const string MobileSecret = "";
        public const string MobileToken = "";
		public const string TeamId = "";

        //Test-Ausweise
        public const string AusweisHost = "demo.gessine.bundesdruckerei.de/ssi";
        //Test-Ausweise
        public const string DdlHost = "";
        public const bool PublicApp = true;

        public const string BaseIDBurgeramt = "https://service.berlin.de/dienstleistung/120703/";
        public const string BaseIDPINInfo = "https://www.personalausweisportal.de/Webs/PA/DE/buergerinnen-und-buerger/online-ausweisen/pin-brief/pin-brief-node.html";
        public const string BaseIDBehoerdenFinder = "https://behoerdenfinder.de/opencms/searchjs.do";
        public const string BaseIDAusweisApp2Link = "https://www.ausweisapp.bund.de/ausweisapp2/";
        public const string BaseIDSupportMail = "mailto:support@my-digi-id.com";

        public const string DdlDataPrivacy = "https://www.kba.de/DE/Themen/ZentraleRegister/ZFER/Digitaler_Fuehrerschein/digitaler_fuehrerschein_inhalt.html";
        public const string DdlUsage = "https://www.kba.de/DE/Themen/ZentraleRegister/ZFER/Digitaler_Fuehrerschein/digitaler_fuehrerschein_inhalt.html";
        public const string DdlTerms = "https://www.kba.de/DE/Themen/ZentraleRegister/ZFER/Digitaler_Fuehrerschein/digitaler_fuehrerschein_inhalt.html";
        public const string DdlDescriptionLink = "https://www.gesetze-im-internet.de/fev_2010/anlage_9.html";

        public const string BsiUrl = "https://www.bsi.bund.de/DE/Themen/Oeffentliche-Verwaltung/Elektronische-Identitaeten/Online-Ausweisfunktion/Testinfrastruktur/eID-Karte/eID-Karte_node.html";

        //Hardware Binding
        public const string HardwareSignature = "hardwareDidProof";
        public const string HardwareSignatureDdl = "hardwareDidProofDdl";
        //Test-Ausweise
        public const string ApiKeyBaseId = "";
        //Test-Ausweise
        public const string ApiKeyDdl = "";

        public const string ApiHeader = "X-API-KEY";
        public const string BaseIdAlias = "BaseIdHWKey";
        public const string DdlAlias = "DdlHWKey";
    }
}