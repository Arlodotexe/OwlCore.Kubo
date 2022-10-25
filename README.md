# OwlCore.Kubo [![Version](https://img.shields.io/nuget/v/OwlCore.Kubo.svg)](https://www.nuget.org/packages/OwlCore.Kubo)

An essential toolkit for Kubo, IPFS and the distributed web.

Featuring:
- [[docs]](/docs/KuboDownloader.md) Auto-download and extract a Kubo binary for the running operating system and architecture.
- [[docs]](/docs/KuboBootstrapper.md) KuboBootstrapper: A no-hassle bootstrapper for the Kubo binary.
- [[docs]](/docs/storageproviders.md) OwlCore.Storage implementations for Ipfs, Ipns and Mfs content.
- [[docs]](/docs/AesPasswordEncryptedPubSub.md) AES encrypt/decrypt pubsub messages using a pre-shared passkey.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Kubo).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Kubo
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Kubo

## Usage

See the [documentation](/docs/) for usage examples.

## Financing

We accept donations, and we do not have any active bug bounties.

If you’re looking to contract a new project, new features, improvements or bug fixes, please contact me. 

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

We’re using the MIT license for 3 reasons:
1. We're here to share useful code. You may use any part of it freely, as the MIT license allows. 
2. A library is no place for viral licensing.
3. Easy code transition to larger community-based projects, such as the .NET Community Toolkit.

