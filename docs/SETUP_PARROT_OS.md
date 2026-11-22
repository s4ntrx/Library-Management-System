# Setting up on Parrot OS

Parrot OS is based on Debian. Microsoft does not publish an official .NET package feed for Parrot, so these steps use Microsoft's installer script, which works on any Linux distribution and installs into your home folder without touching system files. [Likely] to work on current Parrot releases; if a step fails, check the troubleshooting section.

## 1. Update the system and install prerequisites

```bash
sudo apt update
sudo apt install -y git curl ca-certificates libicu-dev
```

## 2. Install the .NET 8 SDK

Download the installer and read it before running it. You are about to execute a script from the internet, so looking first is good practice.

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
less dotnet-install.sh        # press q to leave
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

The SDK is installed in `~/.dotnet`.

## 3. Add .NET to your PATH

Find out which shell you use:

```bash
echo $SHELL
```

If it ends in `zsh`, use `~/.zshrc`. If it ends in `bash`, use `~/.bashrc`. The example below uses zsh.

```bash
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.zshrc
echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> ~/.zshrc
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> ~/.zshrc
source ~/.zshrc
```

The last line stops the .NET command-line tools from sending usage data to Microsoft. It is optional.

## 4. Check the installation

```bash
dotnet --version
```

You should see a version starting with `8.0`.

## 5. Get the project

From GitHub (after you have followed `GITHUB_HOSTING.md`):

```bash
git clone git@github.com:YOUR-USERNAME/library-management-system.git
cd library-management-system
```

Or, if you have the zip file:

```bash
unzip library-management-system.zip
cd library-management-system
```

## 6. Build, test and run

```bash
dotnet build
dotnet test
dotnet run --project src/LibraryManagement
```

The first build downloads the test libraries, so it needs an internet connection. After that, everything works offline.

## 7. Find and back up your data

By default the library is stored here:

```bash
cat ~/.local/share/library-management/library.json
```

It is plain text, so back it up by copying it:

```bash
cp ~/.local/share/library-management/library.json ~/library-backup-$(date +%F).json
```

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `dotnet: command not found` | PATH was not loaded. Run `source ~/.zshrc`, or open a new terminal. Check that `~/.dotnet/dotnet` exists. |
| `Couldn't find a valid ICU package` | Run `sudo apt install libicu-dev`, or temporarily run `export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. |
| `Storage error: ... Permission denied` | The data folder is not writable. Choose another file with `--data ~/library.json`. |
| `Storage error: The data file ... is corrupt` | The file was edited by hand or damaged. Fix the JSON, or move it aside with `mv library.json library.json.bad` and start fresh. |
| `dotnet test` cannot download packages | No internet access, or a proxy is blocking `api.nuget.org`. |
| Terminal looks stuck after a crash | Type `reset` and press Enter. |
