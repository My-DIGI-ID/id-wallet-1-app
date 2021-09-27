# ID Wallet

<p align="center">
      <a href="https://play.google.com/store/apps/details?id=com.digitalenabling.idw&hl=en" title="Get it on Google Play"><img src="images/Google_Play_Badge.svg" width="135"></a>
      <a href="https://apps.apple.com/de/app/id-wallet/id1564933989" title="Download on the App Store"><img src="images/App_Store_Badge.svg"></a>

## Contact
Digital Enabling GmbH  
Rheinstr. 5  
63225 Langen  
info@digital-enabling.com  

## License

The app's transparency and security are of great importance to users. For this reason, the licensor would like to disclose the source programs of the app and enable users to check and analyze the source programs. Editing, further development, distribution or other use of the source programs is not permitted.

Detailed license conditions can be found in the [LICENSE](https://github.com/My-DIGI-ID/ID-Wallet/blob/main/LICENSE) file.

## Releases

Source code for releases of the ID Wallet will be provided in a timely manner and for each major and minor version change. Patch and bug fix releases are excluded. 

## Libraries

The ID Wallet requires libraries that are not included in this repository:

### iOS
- src/libs-ios/libcrypto.a (Get it from: https://github.com/x2on/OpenSSL-for-iPhone)
- src/libs-ios/libindy.a (Get it from: https://repo.sovrin.org/ios/libindy/stable/libindy-core/1.15.0/)
- src/libs-ios/libsodium.a (Get it from: https://github.com/evernym/libsodium-ios)
- src/libs-ios/libssl.a (Get it from: https://github.com/x2on/OpenSSL-for-iPhone)
- src/libs-ios/libzmq.a (Get it from: https://github.com/evernym/libzmq-ios)

### Android

Get it from https://developer.android.com/ndk/downloads:
- src/libs-android/arm64-v8a/libc++_shared.so
- src/libs-android/arm64-v8a/libgnustl_shared.so
- src/libs-android/armeabi-v7a/libc++_shared.so
- src/libs-android/armeabi-v7a/libgnustl_shared.so
- src/libs-android/x86/libc++_shared.so
- src/libs-android/x86/libgnustl_shared.so
- src/libs-android/x86_64/libc++_shared.so
- src/libs-android/x86_64/libgnustl_shared.so

Get it from: https://repo.sovrin.org/android/libindy/stable/1.15.0/:
- src/libs-android/arm64-v8a/libindy.so
- src/libs-android/armeabi-v7a/libindy.so
- src/libs-android/x86/libindy.so
- src/libs-android/x86_64/libindy.so

## Bindings
To use the Governikus AusweisApp2 SDK it requires two binding subprojects for both iOS and Android.
- src/IDWallet.AusweisSDK.iOS/ (https://docs.microsoft.com/en-us/xamarin/ios/platform/embedded-frameworks and https://www.ausweisapp.bund.de/sdk/ios.html)
- src/IDWallet.AusweisSDK.Android/ (https://docs.microsoft.com/de-de/xamarin/android/platform/binding-java-library/binding-a-jar and https://www.ausweisapp.bund.de/sdk/android.html)

## Configuration
Some configuration needs to be set:

### Wallet Parameters
- src/IDWallet/WalletParams.cs  
      - MediatorEndpoint  
      - MediatorConnectionAliasName  
      - SafetyNetApiKey  
      - NotificationHubName  
      - ListenConnectionString  
      - MobileSecret  
      - MobileToken  
      - AusweisHost  
	  - TeamId  
	  - PushServiceName  

### Android Firebase PNS
- src/IDWallet.Android/google-services.json

### BDR API
Set public keys hashes (key pinning) and API-Key of Bundesdruckerei API
- src/IDWallet/Services/SDKMessageService.cs

## App Build
It is recommended to build the project directly from a Xamarin compatible IDE (e.g. Visual Studio or JetBrains Rider). For building and running the iOS app you will need to be on or to be connected to a Mac with the macOS operating system with all necessary Xamarin dependencies (https://docs.microsoft.com/de-de/xamarin/ios/) installed.
Further information for Android can also be found here: https://docs.microsoft.com/de-de/xamarin/android/
