# CustomAlbums

CustomAlbums is a MelonLoader mod for [Muse Dash](https://store.steampowered.com/app/774171/Muse_Dash/) that allows you to load and play custom songs, charts, and song packs (`.mdm` and `.mdp` formats).

Maintained by the [Muse Dash Modding Community (MDMC)](https://github.com/MDMods). Join the [MDMC Discord Server](https://discord.gg/mdmc) for support, chart creation tools, and community charts.

## Features

- **Custom Songs & Packs**: Supports `.mdm` chart files, `.mdp` song packs, and uncompressed chart folders.
- **In-Game Song Menu**: Adds a "Custom Albums" category to the song selection screen.
- **Hot Reloading**: Automatically detects added, updated, or deleted chart files while the game is running without needing a restart.
- **Custom Local Saves**: Tracks scores, accuracy, and clear status in a separate local save file (`CustomAlbums.json`), keeping vanilla cloud saves and official leaderboards untouched.

## Repository Structure

- `Albums/` - Data structures for archive, folder, and pack albums.
- `Data/` - BMS note definitions, chart metadata, and save models.
- `Managers/` - Core logic for album loading, audio, hot reload, song packs, and save files.
- `Patches/` - Harmony hooks into game systems (assets, UI, save data, web APIs).
- `Utilities/` - Logging, file I/O, and asset loading helpers.
- `BmsLoader.cs` - BMS chart parser and timing engine.
- `Main.cs` - Mod entry point and MelonLoader lifecycle hooks.

## Dependencies & Requirements

- [Muse Dash on Steam](https://store.steampowered.com/app/774171/Muse_Dash/)
- [Muse Plus DLC on Steam](https://store.steampowered.com/app/2593750/Muse_Dash__Muse_Plus/) (or Just as Planned DLC)
- [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)

## Installation & Usage

1. Install **MelonLoader v0.7.3** into your Muse Dash directory.
2. Download the [latest release](https://github.com/MDMods/CustomAlbums/releases/latest) of `CustomAlbums.dll` and place it in your `Muse Dash/Mods` folder.
3. Launch Muse Dash once to generate the `Custom_Albums` folder in your game directory.
4. Place `.mdm` files, `.mdp` packs, or chart folders into `Custom_Albums/`.

## Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/MDMods/CustomAlbums.git
   ```
2. Install MelonLoader v0.7.3 into Muse Dash and run the game once to generate reference assemblies.
3. Run `SetPath.cmd` as Administrator and follow the instructions to set your game path.
4. Build using Visual Studio 2026 or the .NET CLI:
   ```bash
   dotnet build -c Release
   ```

## Credits & Contributions

- [ALLMarvelous](https://github.com/ALLMarvelous) - Lead Developer
- [Mr. Talk](https://github.com/SB15-MD) - Lead Developer
- [gamrguy](https://github.com/gamrguy) - Original Maintainer (Feb 2022 - Jul 2023)
- [某10](https://github.com/mo10) - Original Developer (Feb 2021 - Dec 2021)

## Disclaimer

This repository and its contributors maintain no affiliation with Muse Dash, PeroPeroGames (peropero Ltd.), hasuhasu, or any associated entities.
