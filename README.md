# OwlCore.Kubo [![Version](https://img.shields.io/nuget/v/OwlCore.Kubo.svg)](https://www.nuget.org/packages/OwlCore.Kubo)

An essential toolkit for Kubo, IPFS and the distributed web.

Featuring:
- [[docs]](/docs/KuboDownloader.md) KuboDownloader: Automatically download / extract the correct Kubo binary for the running OS and architecture.
- [[docs]](/docs/KuboBootstrapper.md) KuboBootstrapper: A no-hassle bootstrapper for the Kubo binary.
- [[docs]](/docs/storageproviders.md) OwlCore.Storage implementations for Ipfs, Ipns and Mfs content.
- [[docs]](/docs/AesPasswordEncryptedPubSub.md) AES encrypt/decrypt pubsub messages using a pre-shared passkey.
- [[docs]](/docs/PeerRoom.md) PeerRoom: Watch a pubsub topic for other nodes that join the room.


## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Kubo).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Kubo
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Kubo

## Usage

See the [documentation](/docs/) for usage examples.

## Financing

We accept donations [here](https://github.com/sponsors/Arlodotexe) and [here](https://www.patreon.com/arlodotexe), and we do not have any active bug bounties.

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

All OwlCore code is licensed under the MIT License. OwlCore is licensed under the MIT License. See the [LICENSE](./src/LICENSE.txt) file for more details.
