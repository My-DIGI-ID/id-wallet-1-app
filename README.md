# ID Wallet

## Contact
Digital Enabling GmbH  
Bahnhofstr. 29  
94424 Arnstorf  
info@digital-enabling.com  

## License

_TODO_

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

### PNS Android
- src/IDWallet.Android/google-services.json

## App Build
It is recommended to build the project directly from a Xamarin compatible IDE (e.g. Visual Studio or JetBrains Rider). For building and running the iOS app you will need to be on or to be connected to a Mac with the macOS operating system with all necessary Xamarin dependencies (https://docs.microsoft.com/de-de/xamarin/ios/) installed.
Further information for Android can also be found here: https://docs.microsoft.com/de-de/xamarin/android/