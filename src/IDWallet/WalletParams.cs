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
        public const string PushServiceName = "";
        public const string RecipientKeys = "RecipientKeys";
        public const string RecommendedLedger_BCGov = "bcgov";
        public const string RecommendedLedger_Builder = "builder";
        public const string RecommendedLedger_DEVLEDGER = "devledger";
        public const string RecommendedLedger_EESDI = "eesdi";
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

        //Change per Version
        public const string AppVersion = "1.2";
        public const string BuildVersion = "10203";
        public const string WalletName = "ID Wallet";
        public const string SafetyNetApiKey = "";
        public const string NotificationHubName = "";
        public const string ListenConnectionString = "";
        public const string PackageName = "com.digitalenabling.idw";
        public const string MobileSecret = "";
        public const string MobileToken = "";
		public const string TeamId = "";

        public const string AusweisHost = "";
    }
}