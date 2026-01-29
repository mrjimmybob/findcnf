using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EncryptorLibrary;
 
namespace findcnf 
{
    class Program
    {
		private static int versionMajor = 4;
		private static int versionMinor = 0;
		private static int versionRevision = 0;
		private static long foundCount = 0;
		private static long numSearched = 0;

        static string logFilePath = @"findcnf_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";


        static bool fileContainsString(string filename, string strToFind)
		{
			string contents = System.IO.File.ReadAllText(filename);
			if (contents.ToUpper().Contains(strToFind.ToUpper())
				)
			{
				return true;
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
            WriteLog("E: " + error + ": " + "\'" + name + "\' " + "(" + detail + ")");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(error + ": ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\'" + name + "\' ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("(" + detail + ")");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void PrintProgress(string searchpattern, string fileName)
        {
            WriteLog("P: " + "Found: '"  + searchpattern + "' in '" + fileName + "'");
			 
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Found: '");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(searchpattern);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("' in '");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(fileName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("'");
            Console.ForegroundColor = ConsoleColor.White;
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

        static bool isEncryptedandContains(string line, string path, string strOld)
        {
			Encryptor enc = new Encryptor();
			string cryptLine;
			try
			{
				cryptLine = enc.Decrypt(line, true);
				if (cryptLine.ToUpper().Contains(strOld.ToUpper()))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
			return false;
		}


		static bool encriptedFileContainsString(string path, string strFind)
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
						
						if (isEncryptedandContains(line, path, strFind))
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

		static void processFile(FileInfo file, string searchpattern)
		{
			if (file is null || file.Length <= 0 || isDirectory(file.FullName)) return;
			if (!File.Exists(file.FullName)) return;
			numSearched++;
			if (fileContainsString(file.FullName, searchpattern)) {
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
				if (encriptedFileContainsString(file.FullName, searchpattern)) {
					foundCount++;
					PrintProgress("Found '" + searchpattern + "' in encrypted file: ", "'" + file.FullName + "'");
				}
				/*}*/
			}
		}

		internal static void EnumerateFiles(string sFullPath, string searchpattern)
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
						processFile(file, searchpattern);
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
					EnumerateFiles(dir.FullName, searchpattern);
			}
			catch (Exception ex)
			{
                PrintError(sFullPath,  "Error processing directory information", ex.Message);
			}
		}

        static void printUsageAndExit()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Find a string pattern in a configuration file.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Usage: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\tfindcnf");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -p ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("path");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -s ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("searchpattern");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\tfindcnf");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" --path ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("path");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" --searchpattern ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("searchpattern");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\tfindcnf");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -h");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\tfindcnf");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" --help");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Options:");
            Console.WriteLine("\t-p | --path\t\tPath as from when to search.");
            Console.WriteLine("\t-s | --searchpattern\tSearch pattern to  look for.");
            Console.WriteLine();
            Console.WriteLine("\t-h | --help\t\tShow this help message.");

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Third ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("3");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("ye Software Inc. (\u00A9) 2024");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Version: {0}.{1}.{2}. ", versionMajor, versionMinor, versionRevision);
        }
  
		static void Main(string[] args)
        {
            string path = String.Empty;
            string searchpattern = String.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-p":
                    case "--path":
                        if (i + 1 < args.Length)
                            path = args[++i];
                        else
                            PrintError("Fatal Error", "Missing value for -s | --server option.", "Please read usage.");
                        break;

                    case "-s":
                    case "--searchpattern":
                        if (i + 1 < args.Length)
                            searchpattern = args[++i];
                        else
                            PrintError("Fatal Error", "Missing value for -d | --database option.", "Please read usage.");
                        break;

                    case "-h":
                    case "--help":
                        printUsageAndExit();
                        break;
                }
            }
            if (String.Empty == path || String.Empty == searchpattern)
			{
				printUsageAndExit();
			}
			else 
			{
				PrintInfo("Execution: ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
                PrintInfo("Searching directory: ", "'" + path + "'");
                PrintInfo("Looking for string: ", "'" + searchpattern + "'");

				if (Directory.Exists(path))
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();

					EnumerateFiles(path, searchpattern);

                    PrintInfo("Found '" + searchpattern + "': ", foundCount.ToString() + " times.");

					PrintInfo("Number of config files searched: ", numSearched.ToString());
					watch.Stop();
					var elapsedMs = watch.ElapsedMilliseconds;
					printTime(elapsedMs);
				}
				else
				{
					PrintError(path, "Error opening directory", "Path does not exist");
					System.Environment.Exit(0);
				}
			}

		}
    }
}
