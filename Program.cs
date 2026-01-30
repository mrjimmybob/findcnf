using EncryptorLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace findcnf 
{
    class Program
    {
		private static int versionMajor = 4;
		private static int versionMinor = 4;
		private static int versionRevision = 0;
		private static long foundCount = 0;
		private static long numSearched = 0;

        static string logFilePath = @"findcnf_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
        static string errorLogFilePath = @"findcnf_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_errorrs.log";

        static int progressLine;
        static int workingLine = 0;
        static int foundLine = 0;
        static int statsLine = 0;

        // static readonly char[] SpinnerChars = { '░', '▒', '▓', '▒' };
        // static readonly char[] SpinnerChars = { '.', 'o', 'O', 'o', '.' };
        // static readonly char[] SpinnerChars = { '.', 'o', 'O', '0', 'O', 'o' };
        static readonly char[] SpinnerChars = { '-', '\\', '|', '/'};
        // static readonly char[] SpinnerChars = { '+', '*', '+', '-' };
        static int spinnerIndex = 0;
        static volatile bool running = false;

        static readonly object consoleLock = new object();
        static string lastFoundPath = "-";
                 

        static bool fileContainsString(string filename, string strToFind, bool caseSensitive = false)
		{
			string contents = System.IO.File.ReadAllText(filename);
            if (caseSensitive) {
                if (contents.Contains(strToFind))
                {
                    return true;
                }
            } else {
                if (contents.ToUpper().Contains(strToFind.ToUpper()))
                {
                    return true;
                }
            }
			
			return false;
		}
	 

		static bool UTF16FileContainsString(string filename, string strToFind)
		{
			return false;
			string contents = System.IO.File.ReadAllText(filename);
			if (contents.Contains(strToFind))
			{
				return true;
			}
			return false;
		}


		static void debug(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("<");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(msg);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(">");
			Console.ForegroundColor = ConsoleColor.White;
		}


        public static void WriteLog(string logMessage)
        {
            try
            {
                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logMessage}");
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\'WriteLog\' ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("(Error creating or writing to Log file)");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        
		static void printTime(long elapsedMs)
		{
			string strElapsedMs;
			 
			if (elapsedMs > 1000)
			{
				strElapsedMs = Convert.ToString(elapsedMs / 1000) + " s";
			}
			else
			{
				strElapsedMs = Convert.ToString(elapsedMs) + " ms";
			}
            PrintInfo("Finished processing file in: ", strElapsedMs);
		}


        static void PrintError(string name, string error, string detail)
        {
            try
            {
                using (StreamWriter writer = File.AppendText(errorLogFilePath))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - E: {error}: '{name}' ({detail})");
                }
            }
            catch
            {
                // If error log fails, fallback to console only
            }
        }


        static void PrintProgress(string searchpattern, string fileName, bool encrypted = false)
        {
            string connector = encrypted ? "' in encrypted file '" : "' in '";
            WriteLog("P: Found: '" + searchpattern + connector + fileName + "'");

            lastFoundPath = fileName;
            RenderStatus();
        }


        static void SetupStatusLines()
        {
            // Print initial empty lines to reserve space
            Console.WriteLine(); // Working line will be here
            Console.WriteLine(); // Found line will be here
            Console.WriteLine(); // Stats will be here later

            // Set line positions
            workingLine = Console.CursorTop - 3;
            foundLine = Console.CursorTop - 2;
            statsLine = Console.CursorTop - 1;
        }

        static void RenderStatus(string workingState = null)
        {
            lock (consoleLock)
            {
                if (workingState == null)
                    workingState = SpinnerChars[spinnerIndex % SpinnerChars.Length].ToString();

                string foundText = lastFoundPath ?? "-";

                // Save current cursor position
                int currentTop = Console.CursorTop;
                int currentLeft = Console.CursorLeft;

                // ----- Update Working line -----
                Console.SetCursorPosition(0, workingLine);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Working: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(workingState);
                // Clear the rest of the line
                int written = 9 + workingState.Length; // "Working: " = 9
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - written - 1)));

                // ----- Update Found line -----
                Console.SetCursorPosition(0, foundLine);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Found: ");
                Console.ForegroundColor = ConsoleColor.Cyan;

                // Truncate found text if too long
                int maxFoundLength = Console.WindowWidth - 8; // "Found: " = 7, plus some padding
                string displayFoundText = foundText;
                if (foundText.Length > maxFoundLength)
                {
                    displayFoundText = "..." + foundText.Substring(foundText.Length - maxFoundLength + 3);
                }

                Console.Write(displayFoundText);
                // Clear the rest of the line
                written = 7 + displayFoundText.Length; // "Found: " = 7
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - written - 1)));

                // Restore cursor position
                Console.SetCursorPosition(currentLeft, currentTop);
            }
        }


        static void StartSpinner()
        {
            running = true;
            Thread t = new Thread(() =>
            {
                while (running)
                {
                    spinnerIndex++;
                    RenderStatus();
                    Thread.Sleep(150);
                }
            });

            t.IsBackground = true;
            t.Start();
        }


        static void StopSpinner()
        {
            running = false;

            lock (consoleLock)
            {
                // Clear the status lines and show final message
                int currentTop = Console.CursorTop;
                int currentLeft = Console.CursorLeft;

                // Clear working line
                Console.SetCursorPosition(0, workingLine);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Update with final message
                Console.SetCursorPosition(0, workingLine);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Working: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Done");

                // Clear the rest of the found line
                Console.SetCursorPosition(0, foundLine);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Update found line with final summary
                Console.SetCursorPosition(0, foundLine);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Found: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"see {logFilePath} for results");
                Console.ForegroundColor = ConsoleColor.White;

                // Restore cursor position to below the status area
                Console.SetCursorPosition(0, statsLine + 1);
            }

            Console.CursorVisible = true;
        }



        static void PrintInfo(string str1, string str2)
        {
            WriteLog("I: " + str1 + " " + str2);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(str1);
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(str2);
            Console.ForegroundColor = ConsoleColor.White;
        }


        static void PrintWarning(string str1, string str2)
        {
            WriteLog("W: " + str1 + " " + str2);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str1);
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(str2);
            Console.ForegroundColor = ConsoleColor.White;
        }


        static bool isEncryptedandContains(string line, string path, string strOld, bool caseSensitive = false)
		{
			Encryptor enc = new Encryptor();
			string cryptLine;
			try
			{
				cryptLine = enc.Decrypt(line, true);
                if (caseSensitive) {
                    // case sensitive
                    if (cryptLine.Contains(strOld))
                    {
                        return true;
                    }
                } else {
                    // case insensitive
                    if (cryptLine.ToUpper().Contains(strOld.ToUpper()))
                    {
                        return true;
                    }
                }
				
			}
			catch
			{
				return false;
			}
			return false;
		}


		static bool encriptedFileContainsString(string path, string strFind, bool caseSensitive = false)
		{
			string substring = "";
			try
			{
				substring = System.IO.File.ReadAllText(path);
			}
			catch (Exception ex) {
				PrintError(path, "Error reading file", ex.Message);
				return false;
			}
			
			if (substring.IndexOf("xml") < 0)
			{
				// Not an XML file, get out.
				return false;
			}
			
			int from = substring.IndexOf("connectionStrings") + "connectionStrings".Length + 1; // skip closing "
			int to = substring.IndexOf("</connectionStrings");
			int i1, i2, i3;
			string line;

			try
			{
				if (from > 0 && to > 0)
				{
					substring = substring.Substring(from, to - from); // these are the connection strings (all)
																	  // It is an xml config file with connectionStrings
					while (true)
					{
						// Find first connectionString in substring 
						i1 = substring.IndexOf("connectionString");
						var auxTest = substring.IndexOf("connectionString\"");
						if (i1 < 0) break;
						i1 += "connectionString".Length + 1; // go past the connectionString=
															 // found a connectionString, get the limits
						i2 = substring.IndexOf("\"", i1); // find first "
						if (i2 < 0) break;
						i2++; // pass the double comma
						i3 = substring.IndexOf("\"", i2); // find end of connectionString
						if (i3 < 0) break;
						i3 += 2; // pass the last " bit
						line = substring.Substring(i2, i3 - i2 - 2); // Minus string beginning and '"'
						// line = substring.Substring(i2, i3 - i2 - 2); // Minus string beginning and '"'
						
						if (isEncryptedandContains(line, path, strFind, caseSensitive))
                        {
							return true;
                        }
						if (i2 > i3) break;
						// substring = substring.Substring(i2 + (i3 - i2)); // get from after last '/>'
						substring = substring.Substring(i3 - i2); // get from after last '"'
					}
				}
			}
			catch (Exception ex)
            {
				PrintError(path, "Error searching file for encrypted data", ex.Message);
				return false;
			}
			return false;
		}

		
        static bool isDirectory(string path)
        {
			// get the file attributes for file or directory
			FileAttributes attr = File.GetAttributes(path);
			
			//detect whether its a directory or file
			if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				return true;
			else
				return false;
		}

		
        static void processFile(FileInfo file, string searchpattern, bool caseSensitive = false)
		{
			if (file is null || file.Length <= 0 || isDirectory(file.FullName)) return;
			if (!File.Exists(file.FullName)) return;
			numSearched++;
            RenderStatus();
            if (fileContainsString(file.FullName, searchpattern, caseSensitive)) {
				foundCount++;
				PrintProgress(searchpattern, file.FullName);
			}
			else
			{
				/*
				// Maybe it is Little Endian!
				if (UTF16FileContainsString(file.FullName, strFind)) {
					foundCount++;
					writeToLog("Found: '" + strFind + "' in '" + file.FullName + "'");
					printInfo("Found '" + strFind + "' in file: '", file.FullName + "'");
				} 
				else
				{*/
				if (encriptedFileContainsString(file.FullName, searchpattern, caseSensitive)) {
					foundCount++;
					PrintProgress(searchpattern, file.FullName, true);
				}
				/*}*/
			}
		}

		
        internal static void EnumerateFiles(string sFullPath, string searchpattern, bool caseSensitive = false)
		{
			DirectoryInfo di = new DirectoryInfo(sFullPath);
			
			try
			{
				FileInfo[] files = di.GetFiles();
				foreach (FileInfo file in files)
				{
					if (file.Extension.ToUpper().Equals(".CONFIG") 
						|| file.Extension.ToUpper().Equals(".JS")
                        || file.Extension.ToUpper().Equals(".UDL")
						|| file.Extension.ToUpper().Equals(".BAT"))
					{
						// writeToLog("Processing file: " + file.FullName);
						processFile(file, searchpattern, caseSensitive);
					}
				}
			}
			catch (Exception ex)
			{
				PrintError(sFullPath, "Error processing file information", ex.Message);
			}
			try
				{
				// Scan recursively
				DirectoryInfo[] dirs = di.GetDirectories();
				if (dirs == null || dirs.Length < 1)
					return;
                foreach (DirectoryInfo dir in dirs)
                {
                    if ((dir.Attributes & FileAttributes.ReparsePoint) != 0)
                        continue; // skip symlinks/junctions

                    EnumerateFiles(dir.FullName, searchpattern, caseSensitive);
                }
            }
			catch (Exception ex)
			{
                PrintError(sFullPath,  "Error processing directory information", ex.Message);
			}
		}


        static void printUsageAndExit()
        {
            CWriteLine(ConsoleColor.Cyan, "Find a string pattern in a configuration file.");
            CWriteLine();

            CWriteLine(ConsoleColor.White, "Usage: ");
            CWrite(ConsoleColor.Green, "\tfindcnf");
            CWrite(ConsoleColor.DarkGray, " -p ");
            CWrite(ConsoleColor.White, "<path>");
            CWrite(ConsoleColor.DarkGray, " -s ");
            CWrite(ConsoleColor.White, "<searchpattern> [");
            CWrite(ConsoleColor.DarkGray, "-c");
            CWrite(ConsoleColor.White, "]");
            CWriteLine();
            CWriteLine();

            CWriteLine(ConsoleColor.Cyan, "Options:");
            CWrite(ConsoleColor.DarkGray, "\t-p");
            CWrite(ConsoleColor.White, " | ");
            CWrite(ConsoleColor.DarkGray, "--path");
            CWrite(ConsoleColor.White, " <path>");
            CWriteLine(ConsoleColor.Cyan, "\t\t\tPath as from when to search.");

            CWrite(ConsoleColor.DarkGray, "\t-s");
            CWrite(ConsoleColor.White, " | ");
            CWrite(ConsoleColor.DarkGray, "--searchpattern");
            CWrite(ConsoleColor.White, " <searchpattern>");
            CWriteLine(ConsoleColor.Cyan, "\tSearch pattern to look for.");

            CWrite(ConsoleColor.DarkGray, "\t-c");
            CWrite(ConsoleColor.White, " | ");
            CWrite(ConsoleColor.DarkGray, "--casesensitive");
            CWriteLine(ConsoleColor.Cyan, "\t\t\tMake a case sensitive search.");
            CWriteLine();
            CWrite(ConsoleColor.DarkGray, "\t-h");
            CWrite(ConsoleColor.White, " | ");
            CWrite(ConsoleColor.DarkGray, "--help");
            CWriteLine(ConsoleColor.Cyan, "\t\t\t\tShow this help message.");
            CWriteLine();

            CWrite(ConsoleColor.DarkYellow, "Third ");
            CWrite(ConsoleColor.Yellow, "3");
            CWriteLine(ConsoleColor.DarkYellow, "ye Software Inc. (\u00A9) 2026");

            CWriteLine(ConsoleColor.White,
                $"Version: {versionMajor}.{versionMinor}.{versionRevision}. ");
        }


        private static void CWrite(ConsoleColor color, string message = "")
        {
            Console.ForegroundColor = color;
            if (message != "")
                Console.Write(message);
        }

        
        private static void CWriteLine(ConsoleColor color = ConsoleColor.White, string message = "")
        {
            if (message == "")
            {
                Console.WriteLine();
                return;
            }
            else
            {
                Console.ForegroundColor = color;
                message += Environment.NewLine;
                Console.Write(message);
            }
        }


        private static string NormalizeAndValidatePath(string path)
        {
            try
            {
                // Get full path to resolve relative paths and normalize separators
                string fullPath = Path.GetFullPath(path);

                // Check if path exists
                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException($"Directory does not exist: '{fullPath}'");
                }

                // Check if it's actually a directory (not a file)
                FileAttributes attr = File.GetAttributes(fullPath);
                if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    throw new IOException($"Path is not a directory: '{fullPath}'");
                }

                // Check if directory is readable/accessible
                try
                {
                    // Try to get directory info to test accessibility
                    var directoryInfo = new DirectoryInfo(fullPath);
                    var files = directoryInfo.GetFileSystemInfos("*");

                    // Check for specific access rights (this will throw if no access)
                    directoryInfo.GetAccessControl();
                }
                catch (UnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException($"Access denied to directory: '{fullPath}'");
                }
                catch (SecurityException)
                {
                    throw new SecurityException($"Security exception accessing directory: '{fullPath}'");
                }

                // Ensure path ends with directory separator
                if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                    !fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    fullPath = fullPath + Path.DirectorySeparatorChar;
                }

                return fullPath;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid path format: '{path}'", ex);
            }
            catch (PathTooLongException)
            {
                throw new PathTooLongException($"Path is too long: '{path}'");
            }
            catch (NotSupportedException)
            {
                throw new NotSupportedException($"Path format is not supported: '{path}'");
            }
        }


        static void Main(string[] args)
        {
            string path = String.Empty;
            string searchpattern = String.Empty;
            bool caseSensitive = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-p":
                    case "--path":
                        if (i + 1 < args.Length)
                            path = args[++i];
                        else
                            PrintError("Fatal Error", "Missing value for -p | --path option.", "Please read usage.");
                        break;

                    case "-s":
                    case "--searchpattern":
                        if (i + 1 < args.Length)
                            searchpattern = args[++i];
                        else
                            PrintError("Fatal Error", "Missing value for -s | --searchpattern option.", "Please read usage.");
                        break;

                    case "-c":
                    case "--casesensitive":
                        caseSensitive = true;
                        break;

                    case "-h":
                    case "--help":
                        printUsageAndExit();
                        Environment.Exit(0);
                        break;
                }
            }
            if (String.Empty == path || String.Empty == searchpattern)
            {
                printUsageAndExit();
                Environment.Exit(0);
            }
            else
            {
                try
                {
                    Console.CursorVisible = false;
                    path = NormalizeAndValidatePath(path);
                    PrintInfo("Execution: ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
                    PrintInfo("Searching directory: ", "'" + path + "'");
                    PrintInfo("Looking for string: ", "'" + searchpattern + "'");

                    SetupStatusLines();
                    StartSpinner();

                    if (Directory.Exists(path))
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();

                        EnumerateFiles(path, searchpattern, caseSensitive);

                        StopSpinner();

                        PrintInfo("Search pattern: ", $"'{searchpattern}'");
                        PrintInfo("Total occurrences found: ", $"{foundCount} {(foundCount == 1 ? "time" : "times")}");
                        PrintInfo("Number of files searched: ", numSearched.ToString());
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        printTime(elapsedMs);
                        Console.CursorVisible = true;
                    }
                    else
                    {
                        PrintError(path, "Error opening directory", "Path does not exist");
                        Console.CursorVisible = true;
                        System.Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    StopSpinner();
                    PrintError("Directory Error", $"Failed to access directory '{path}'", ex.Message);
                    Console.CursorVisible = true;
                    System.Environment.Exit(1);
                }
                finally
                {
                    Console.CursorVisible = true;
                }

            }
        }
    }
}
