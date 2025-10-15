# Spinda Pattern Plugin for PKHeX

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/hexbyt3/SpindaPatternPlugin/total?style=flat-square&logoColor=Red&color=red)  ![GitHub Release](https://img.shields.io/github/v/release/hexbyt3/SpindaPatternPlugin)



A PKHeX plugin that provides an advanced editor for Spinda spot patterns with proper support for BDSP endianness handling.

## About

The different spot patterns are generated based on Spinda's personality value, a hidden statistic in the Pokémon games. This value is a 32-bit number, which results in the possibility of 4,294,967,296 unique patterns (doubled if you consider also its shiny variant). So, every time you encounter a Spinda, you're meeting a truly unique Pokémon!

## Features

- Visual Spinda pattern editor with real-time preview
- Support for all Pokémon generations (Gen 3+)
- **BDSP Endianness Fix**: Correctly handles the byte-order bug in Brilliant Diamond/Shining Pearl where Encryption Constants are read as big-endian instead of little-endian
- Shiny pattern preview
- Randomize patterns with shiny support

## Screenshots
<img width="710" height="512" alt="image" src="https://github.com/user-attachments/assets/ce0dd729-bd8e-49ff-a6d5-d0389c435aef" />

## Installation

1. Download the latest `SpindaPatternPlugin.dll` from the [Releases](https://github.com/hexbyt3/SpindaPatternPlugin/releases) page
2. Place the DLL file in your PKHeX `plugins` folder
5. Restart PKHeX

## Usage

1. Load a save file in PKHeX
2. Select a Pokémon (or an empty slot)
3. Go to Tools → Spinda Pattern Editor (or press Ctrl+Shift+S)
4. Edit the pattern using the hex input or randomize button
5. Click Apply to save changes

## BDSP Bug Information

In Pokémon Brilliant Diamond and Shining Pearl, there's a bug where the game reads Spinda's Encryption Constant in reverse byte order (big-endian instead of little-endian). This plugin automatically detects BDSP Pokémon and applies the correct byte swapping to ensure patterns display correctly.

For example:
- Normal EC: `0x12345678` displays pattern based on bytes `12 34 56 78`
- BDSP EC: `0x12345678` displays pattern based on bytes `78 56 34 12`

## Building from Source

Requirements:
- .NET 9.0 SDK
- Windows Forms support
- PKHeX.Core.dll reference

```bash
git clone https://github.com/yourusername/SpindaPatternPlugin.git
cd SpindaPatternPlugin
dotnet build -c Release
```

## Version Compatibility

This plugin includes automatic version detection and will warn if there's a mismatch between the plugin version and your PKHeX version. The plugin is built against the PKHeX.Core.dll included in the repository.

## Credits

- [Pokeos](https://www.pokeos.com/tools/spinda-generator) for the images and positioning logic
- PKHeX developers for the plugin API
- Pokémon community (@Atrius97) for [documenting the BDSP Spinda bug](https://x.com/Atrius97/status/1500557458623778819)

## License

This project is licensed under the MIT License - see the LICENSE file for details.
