# BattleGearUnpacker
Unpacks and repacks various Battle Gear 3 file formats.  
Battle Gear 2 support may be added later.  

For documentation on the file formats seen inside, see [BattleGearTemplates][0].

# Usage
To use this program, drag and drop files onto the exe to unpack.  
To repack, drag and drop an unpacked folder, a generated XML, or unpacked file if that is supported.  
Drag and dropping multiple files is supported.  
Paths can also be passed via command line or terminal.  

This program generates XML files for most formats when it unpacks to know what to repack.  
These are required for repacking, and may be informational for other uses.  

The main archive of Battle Gear 3 is a pair of files:  
FAT_Z.BIN and BG3ZPACK.ARC  

Drag and drop either one of these to unpack.  

# Supported File Formats
Format | Description
------ | -----------
TM2 | Unpack into PNG and repack using generated XML into TM2.
ZPACK | Unpack and repack the main archive, FAT_Z.BIN and BG3ZPACK.BIN.
ARC | Same as ZPACK.
FOZ | Decompress only currently.
GST | Decompress and recompress.

# Building
If you want to build the project you should clone it with these commands in git bash in a folder of your choosing:  
```
git clone https://github.com/WarpZephyr/BattleGearUnpacker.git  
git clone https://github.com/WarpZephyr/BinaryMemory.git  
git clone https://github.com/WarpZephyr/pngcs.git  
```

There is also a SharpZipLib dependency added via nuget.  
After that is done, build BattleGearUnpacker.  

Dependencies subject to change in the future.

[0]: https://github.com/WarpZephyr/BattleGearTemplates