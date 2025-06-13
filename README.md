<img src="http://i.luckyc.dev/zenith.png" width="150" height="150">

# Zenith

Download Zenith from [here](https://github.com/luckycdev/Zenith/releases/)

For support, join https://discord.gg/B3MmvMa8rD

# Flags:

⚠️REQUIRED: --hostname = server name that will show up in server list

OPTIONAL: --private = will hide from server list

⚠️REQUIRED: --maxplayers = max players (has to be atleast 1)

[⚠️WARNING: PORT MUST BE OVER 1000⚠️]
OPTIONAL: --port = port server is ran on, if not specified it is 25050

[⚠️WARNING: i havent tested without nosteam⚠️]
OPTIONAL: --nosteam = will allow pirated clients to join (might break custom pot skins) (will ban/mod by IP and not by steam account)

# Instructions:

1. In a folder, do `git clone https://github.com/Skippeh/Oxide.GettingOverIt.git`
2. Copy the GettingOverIt_Data folder from your UNMODDED getting over it files into Oxide.GettingOverIt/src/Dependencies/Patched/ (you will have to create the Dependencies and Patched folder)
3. Install [Visual Studio 2019](https://download.visualstudio.microsoft.com/download/pr/e84651e1-d13a-4bd2-a658-f47a1011ffd1/e17f0d85d70dc9f1e437a78a90dcfc527befe3dc11644e02435bdfe8fd51da27/vs_Community.exe) and [dotnet 2.1.202](https://builds.dotnet.microsoft.com/dotnet/Sdk/2.1.202/dotnet-sdk-2.1.202-win-x86.exe)
3. Open Oxide.GettingOverIt.sln in Visual Studio 2019 and go to the top and press build and then build solution, it should say 0 failed
4. In the same folder you cloned Oxide.GettingOverIt into, do `git clone https://github.com/luckycdev/Zenith.git`
5. Inside of the Zenith folder, do `git clone https://github.com/Skippeh/lidgren-network-gen3.git`
6. Install [dotnet 5.0.408](https://builds.dotnet.microsoft.com/dotnet/Sdk/5.0.408/dotnet-sdk-5.0.408-win-x86.exe)
7. Open Zenith/Oxide.GettingOverItMP.sln in Visual Studio 2019 and go to the top and press build and then build solution, it should say 0 failed

You have built Zenith! The server files will be located at Zenith/GettingOverItMP.Server/bin/Debug/net5.0/

If you would like to build for linux or other architectures, run Zenith/build-(arch).bat (example: I would run Zenith/build-zenith-linux-arm64.bat and the resulting files will be in Zenith/GettingOverItMP.Server/bin/Debug/net5.0/linux-arm64/publish - to run it I would do `chmod +x GettingOverItMP.Server` and then `./GettingOverItMP.Server --hostname "name" --maxplayers 10 --nosteam`

Folder Structure if you're confused on where to clone to:
```
A_Random_Folder/
├─ Oxide.GettingOverIt/
├─ Zenith/
│  ├─ lidgren-network-gen3/
```
---

Zenith is a fork of [Skippeh's Oxide.GettingOverItMP](https://github.com/Skippeh/Oxide.GettingOverItMP/)

Made possible by [Oxide](https://oxidemod.org/)
