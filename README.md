# HermesProxy (.NET 10)

This project enables play on existing legacy WoW emulation cores using the modern clients. It serves as a translation layer, converting all network traffic to the appropriate format each side can understand.

This GPLv3 fork is based on [WowLegacyCore commit `0a47373`](https://github.com/WowLegacyCore/HermesProxy/commit/0a47373). That commit is on a different line of development from [official v3.10 (`841a26f`)](https://github.com/WowLegacyCore/HermesProxy/releases/tag/v3.10). Changes from v3.10 should be reviewed and imported individually.

There are 4 major components to the application:
- The modern BNetServer to which the client initially logs into.
- The legacy AuthClient which will in turn login to the remote authentication server (realmd). 
- The modern WorldServer to which the game client will connect once a realm has been selected. 
- The legacy WorldClient which communicates with the remote world server (mangosd).

## Supported Versions

| Modern Versions | Legacy Versions |
|-----------------|-----------------|
| 1.14.0          | 1.12.1          |
| 1.14.2 (42597)   | 1.12.1 (5875)   |
| 2.5.2           | 2.4.3           |

## Build and test

Use a .NET 10 SDK that ships with .NET runtime 10.0.15 or newer (see [`global.json`](global.json)). On Linux, the runtime uses the system OpenSSL libraries. OpenSSL 4 support was added in [.NET 10.0.15](https://github.com/dotnet/runtime/pull/135534); a self-contained publish still needs system OpenSSL.

```sh
dotnet test --configuration Release
dotnet publish HermesProxy/HermesProxy.csproj --configuration Release \
  --runtime linux-x64 --self-contained --output artifacts/linux-x64
cd artifacts/linux-x64
./HermesProxy --no-version-check
```

To try the fork with an existing setup, make a separate publish directory and copy your own `HermesProxy.config` and `CSV/` there. For example, SoloCraft's bundle has modified CSVs that are not part of this fork. Keep account data and packet logs out of source control. Stop any existing proxy before starting the new one on the same ports.

The GitHub Actions workflow builds, tests, and packages Linux x64 and Windows x64. Old launcher/release automation is retained under `.github/legacy-workflows/` for reference only.

## Ingame Settings
Note: Keep `Optimize Network for Speed` **enabled** (it's under `System` -> `Network`), otherwise you will get kicked every now and then.

## Usage Instructions

- Edit the app's config to specify the exact versions of your game client and the remote server, along with the address.
- Go into your game folder, in the Classic or Classic Era subdirectory, and edit WTF/Config.wtf to set the portal to 127.0.0.1.
- Download [Arctium Launcher](https://github.com/Arctium/WoW-Launcher/releases/tag/latest) into the main game folder, and then run it  
with `--staticseed --version=ClassicEra` for vanilla  
or `--staticseed --version=Classic` for TBC.
- Start the proxy app and login through the game with your usual credentials.

## Chat commands
HermesProxy provides some internal chat commands:

| Command                    | Description                                                                  |
|----------------------------|------------------------------------------------------------------------------|
| `!qcomplete <questId>`     | Manually marks a quest as already completed (useful for quest helper addons) |
| `!quncomplete <questId>`   | Unmarks a quest as completed                                                 |

## Start Arguments
 - `--config <filename>` to specify a config (default `HermesProxy.config`)
 - `--set <key>=<value>` to overwrite a specific config value (example `--set ServerAddress=logon.example.com`)
 - `--no-version-check`  to disable the check for newer versions

## Acknowledgements

Parts of this poject's code are based on [CypherCore](https://github.com/CypherCore/CypherCore) and [BotFarm](https://github.com/jackpoz/BotFarm). I would like to extend my sincere thanks to these projects, as the creation of this app might have never happened without them. And I would also like to expressly thank [Modox](https://github.com/mdx7) for all his work on reverse engineering the classic clients and all the help he has personally given me. 

## Download HermesProxy
Stable Downloads: [Releases](https://github.com/WowLegacyCore/HermesProxy/releases)
