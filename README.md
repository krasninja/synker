Synker
======

**The project is not under development**

The project goal is to synchronize your applications settings across your computers. For example, you may have the same applications (FileZilla, Far, etc) installed at your home and work. It is not convenient to remember what settings you have at work and do the same at home again and again. The project allows you to export settings into shared folder and import them automatically into another environment.

## Terms

- Profile - The file in YAML format that describes specific application settings format. It contains what files to sync and specific rules for various environments.
- Target - The building blocks of profile. Contains the settings that needs to be synchronized. For example files, registry keys, etc. There are also specific targets to check special conditions.
- Bundle - The exported application settings. It contains the date and time when it was exported. Bundles are used to transfer settings across computers.

## Gettings Started

1. Define the folder that is synced across all computers. You can use [Dropbox](https://www.dropbox.com/), [Google Drive](https://www.google.com/drive/using-drive/) or [Yandex Disk](https://disk.yandex.com/client/disk).
2. Create "profiles" folder and copy all needed YAML profile files there.
3. Create "bundles" folder. It will be used to store settings.
4. Run for sync: `./synker-cli.exe sync --profiles="./synker/profiles" --bundles-directory="./synker/bundles"`

## Command Line Usage

There are following commands available:

- **export**. Export new settings into target bundles folder.
- **import**. Import new settings from target bundles folder.
- **sync**. Combines export and import.

There three commands above share the same parameters:

- `-p, --profiles`. Application profiles directories, can be several comma separated. Example: `~/sync/profiles;/opt/profiles`.
- `-b, --bundles`. Directory with settings bundles. Example: `~/sync/bundles`.
- `-pe, --profiles-exclude`. Application profiles to exclude from sync. Example: `conemu,dbeaver*`.

Other commands:

- **clean**. Clean bundles directory from outdated bundles.
    - `-md, --max-days`. Maximum days age for bundle.

## App Usage

There is UI for synker available. By default it tries to read settings from `.synker.config` file in user's home folder. For example in Windows it can be `C:\Users\ivan\AppData\Roaming`. Here is a sample configuration:

```
profiles-source: C:\YandexDisk\share\synker\profiles
bundles-directory: C:\YandexDisk\share\synker\bundles
log-file: D:\temp\synker.log
disable-export: true
disable-import: false
```
