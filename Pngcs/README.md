PNGCS
=======

A modified version of the PNGCS library for ease of access.
--------

Subject to change later on as the modifications are still a bit messy.  

Reworks most of the library's code style.  
Added options to more easily read and write System.Color objects.  
Added pixel data/color objects that may be subject for change later on.  

System.Color only supports 8-bits per channel, and not the full 16-bit extent of PNG.  
Colors will be reduced if reading 16-bit true color or grayscale pixels into System.Color.  
They are reduced by including only the most significant bits ((byte)(Channel >> 8)).  

The library has been reworked to use .NET 8.0.

LICENSING
---------

The `ICSharpCode.SharpZipLib.dll` assembly, provided with this library,  
must be referenced together with `Pngcs.dll` by client projects.  
Because SharpZipLib is released  under the GPL license with an exception  
that allows to link it with independent modules, PNGCS relies on that  
exception and is released under the Apache license. See `LICENSE.txt`.

HISTORY
-------

See changes.txt in this folder.  

Hernan J Gonzalez - hgonzalez@gmail.com -  http://stackoverflow.com/users/277304/leonbloy  
Tommy Ettinger - https://github.com/tommyettinger/  
WarpZephyr - warpzephyr@gmail.com - https://github.com/WarpZephyr/pngcs