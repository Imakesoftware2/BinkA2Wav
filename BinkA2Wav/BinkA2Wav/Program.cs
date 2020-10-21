using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BinkA2Wav
{
	internal unsafe class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine("BinkA2Wav 1.1");
			Console.WriteLine("Copyright (C) 2013 angelsl, GMMan");
			Console.WriteLine();
			
			// Parameter variables
			string inputPath = string.Empty;
			string outputPath = string.Empty;
			string outputSubdirName = "__binka2wav__";
			string glob = "*";
			bool recursive = true;
			
			if (args.Length == 0) usage(); // Ran without arguments, print usage
			
			// Argument parser
			foreach (string arg in args)
			{
				// Each switch is in the format of "/option=value"

				if (arg.StartsWith("/")) // A switch
				{
					string[] argSplit = arg.Split(new char[] { '=' }, 2);
					switch (argSplit[0].ToLower()) // Check option. New: Now works with flag options (no "=value").
					{
						case "/d": // Common option: output path
							// Check if specified already. Such a check should be present for every option.
							if (!string.IsNullOrEmpty(outputPath))
							{
								Console.WriteLine("Output directory path is specified more than once.");
								Console.WriteLine();
								usage();
							}
							else
								outputPath = argSplit[1];
							break;
						case "/n":
							if (outputSubdirName != "__binka2wav__")
							{
								Console.WriteLine("Output directory name is specified more than once.");
								Console.WriteLine();
								usage();
							}
							else
								outputSubdirName = argSplit[1];
							break;
						case "/g":
							if (glob != "*")
							{
								Console.WriteLine("Wildcard match is specified more than once.");
								Console.WriteLine();
								usage();
							}
							else
								glob = argSplit[1];
							break;
						case "/r":
							if (!recursive)
							{
								Console.WriteLine("Disable recursion option is specified more than once.");
								Console.WriteLine();
								usage();
							}
							else if (argSplit.Length > 1)
							{
								Console.WriteLine("Disable recursion option does not take arguments.");
								Console.WriteLine();
								usage();
							}
							else
								recursive = true;
							break;
						default:
							Console.WriteLine("Unknown option {0}.", argSplit[0]);
							Console.WriteLine();
							usage();
							break;
					}
				}
				else
				{
					// Checks arguments that do not begin with '/'. Typically there's only the input file path.
					if (!string.IsNullOrEmpty(inputPath))
					{
						Console.WriteLine("Input path is specified more than once.");
						Console.WriteLine();
						usage();
					}
					else
						inputPath = arg;
				}
			}

			// Input path must be specified
			if (string.IsNullOrEmpty(inputPath))
			{
				Console.WriteLine("Input path is not specified.");
				Console.WriteLine();
				usage();
			}

			AIL_set_redist_directory(".");
			if (AIL_startup() == 0)
			{
				Console.WriteLine("Unable to initialize Miles Sound System.");
				Environment.Exit(2);
			}
			
			if (File.Exists(inputPath))
			{
				// Is a file
				Console.WriteLine("Converting {0}", inputPath);
				try
				{
					byte[] inData = File.ReadAllBytes(inputPath);
					IntPtr resultPtr;
					uint resultSize = 0;
					if (AIL_decompress_ASI(inData, (uint) inData.Length, ".binka", &resultPtr, &resultSize, 0) == 0)
					{
						Console.WriteLine("Failed to convert {0}: {1}",
						                  inputPath,
						                  Marshal.PtrToStringAnsi(AIL_last_error()));
					}
					else
					{
						var result = new byte[resultSize];
						Marshal.Copy(resultPtr, result, 0, result.Length);
						AIL_mem_free_lock(resultPtr);
						string outPath = Path.GetDirectoryName(inputPath);
						Directory.CreateDirectory(outPath);
						File.WriteAllBytes(Path.Combine(outPath, Path.GetFileNameWithoutExtension(inputPath) + "_converted.wav"), result);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Error during conversion: ({0})", e.Message);
					if (System.Diagnostics.Debugger.IsAttached)
					{
						Console.WriteLine(e.ToString());
						Console.ReadKey();
					}
					AIL_shutdown();
					Environment.Exit(2);
				}
			}
			else if (Directory.Exists(inputPath))
			{
				// Is a directory
				foreach (string file in Directory.GetFiles(inputPath, glob, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
				{
					Console.WriteLine("Converting {0}", file);
					try
					{
						byte[] inData = File.ReadAllBytes(file);
						IntPtr resultPtr;
						uint resultSize = 0;
						if (AIL_decompress_ASI(inData, (uint) inData.Length, ".binka", &resultPtr, &resultSize, 0) == 0)
						{
							Console.WriteLine("Failed to convert {0}: {1}",
							                  file,
							                  Marshal.PtrToStringAnsi(AIL_last_error()));
						}
						else
						{
							var result = new byte[resultSize];
							Marshal.Copy(resultPtr, result, 0, result.Length);
							AIL_mem_free_lock(resultPtr);
							string outPath;
							if (string.IsNullOrEmpty(outputPath)) // Write to subdir
							{
								outPath = Path.Combine(Path.GetDirectoryName(file), outputSubdirName);
							}
							else // Write to different directory, keeping folder structure
							{
								outPath = Path.Combine(outputPath, Path.GetDirectoryName(file).Replace(Path.GetFullPath(inputPath), "").TrimStart('\\', '/'));
							}
							Directory.CreateDirectory(outPath);
							File.WriteAllBytes(Path.Combine(outPath, Path.GetFileNameWithoutExtension(file) + ".wav"), result);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Error converting {1}: ({0})", e.Message, file);
						if (System.Diagnostics.Debugger.IsAttached)
						{
							Console.WriteLine(e.ToString());
							Console.ReadKey();
						}
					}
				}
			}
			else
			{
				Console.WriteLine("Path is invalid.");
				Console.WriteLine();
				AIL_shutdown();
				usage();
			}
			
			Console.WriteLine("Conversion complete.");
			AIL_shutdown();
		}

		static void usage()
		{
			Console.WriteLine("{0} [/d=outputDir|/n=subdirName] [/g=glob] [/r] path", System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location));
			Console.WriteLine("\tpath\tPath to file(s) to convert. Can be either single file or a directory.");
			Console.WriteLine("\t/d=outputDir\tOutput directory path. If specified, files will be converted to this directory, retaining folder structure.");
			Console.WriteLine("\t/n=subdirName\tName of subdirectory to extract to if an alternative output path is not specified.");
			Console.WriteLine("\t/g=glob\tPattern of file names to match and convert. By default all files are attempted to be converted.");
			Console.WriteLine("\t/r\tDon't process folder recursively.");
			Environment.Exit(1);
		}

		[DllImport("mss32.dll", EntryPoint = "_AIL_decompress_ASI@24")]
		private static extern int AIL_decompress_ASI([MarshalAs(UnmanagedType.LPArray)] byte[] indata, uint insize,
		                                             [MarshalAs(UnmanagedType.LPStr)] String ext, IntPtr* result,
		                                             uint* resultsize, uint zero);

		[DllImport("mss32.dll", EntryPoint = "_AIL_last_error@0")]
		private static extern IntPtr AIL_last_error();

		[DllImport("mss32.dll", EntryPoint = "_AIL_set_redist_directory@4")]
		private static extern IntPtr AIL_set_redist_directory([MarshalAs(UnmanagedType.LPStr)] string redistDir);

		[DllImport("mss32.dll", EntryPoint = "_AIL_mem_free_lock@4")]
		private static extern void AIL_mem_free_lock(IntPtr ptr);

		[DllImport("mss32.dll", EntryPoint = "_AIL_startup@0")]
		private static extern int AIL_startup();

		[DllImport("mss32.dll", EntryPoint = "_AIL_shutdown@0")]
		private static extern int AIL_shutdown();
	}
}