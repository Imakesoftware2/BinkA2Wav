# BinkA2Wav
Orginaly made by angelsl and GMMan on XeNTaX

	
This program converts Bink Audio to standard PCM WAVs. You will need to supply your own copy of Miles Sound System (mss32.dll) and Bink Audio ASI Codec (binkawin.asi) in order for the program to work. Run program without arguments for options.

Drag and drop a folder on to the program to convert all files in the folder recursively. Results will be put into a folder named "__binka2wav__" under each folder from which a valid Bink Audio file is found. The folder name can be changed via commandline argument. If you specify an alternative output path, the folder structure from the original path will be recreated in the new output path. You are recommended to add a wildcard match if the folder you specify and its subfolders have files other than Bink Audio files. If a file is dragged and dropped on to the program, the output will have "_converted" appended after the file name and have a ".wav" extension.
