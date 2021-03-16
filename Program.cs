using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EncryptorLibrary;

namespace findcnf 
{
    class Program
    {
		private static int versionMajor = 3;
		private static int versionMinor = 3;
		private static int versionRevision = 0;
        private static string strFind = ""; 
		private static string logFileName = "";
		private static long foundCount = 0;

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
			printInfo("Finished processing file in: ", strElapsedMs);
		}

		static void printError(string name, string error, string detail)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(error + ": ");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("\'" + name + "\' ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("(" + detail + ")");
			Console.ForegroundColor = ConsoleColor.White;
		}

		static void printProgress(string title, string data)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(data);
			Console.ForegroundColor = ConsoleColor.White;
		}

		static void printInfo(string title, string data)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(data);
			Console.ForegroundColor = ConsoleColor.White;
		}

		static bool isEncryptedandContains(string line, string path, string strOld)
        {
			Encryptor enc = new Encryptor();
			string cryptLine;
			try
			{
				cryptLine = enc.Decrypt(line, true);
				if (cryptLine.ToUpper().Contains(strFind.ToUpper()))
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
				printError(path, "Error reading file", ex.Message);
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
				printError(path, "Error searching file for encrypted data", ex.Message);
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

		static void processFile(FileInfo file)
		{
			if (file is null || file.Length <= 0 || isDirectory(file.FullName)) return;
			if (!File.Exists(file.FullName)) return;
			if (fileContainsString(file.FullName, strFind)) {
				foundCount++;
				writeToLog("Found: '" + strFind + "' in '" + file.FullName + "'");
				printProgress("Found '" + strFind + "' in file: ", "'" + file.FullName + "'");
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
				if (encriptedFileContainsString(file.FullName, strFind)) {
					foundCount++;
					writeToLog("Found '" + strFind + "' in encrypted file: '" + file.FullName + "'");
					printProgress("Found '" + strFind + "' in encrypted file: ", "'" + file.FullName + "'");
				}
				/*}*/
			}
		}

		internal static void EnumerateFiles(string sFullPath)
		{
			DirectoryInfo di = new DirectoryInfo(sFullPath);
			
			try
			{
				FileInfo[] files = di.GetFiles();
				foreach (FileInfo file in files)
				{
					if (file.Extension.ToUpper().Equals(".CONFIG") 
						|| file.Extension.ToUpper().Equals(".UDL")
						|| file.Extension.ToUpper().Equals(".BAT"))
					{
						// writeToLog("Processing file: " + file.FullName);
						processFile(file);
					}
				}
			}
			catch (Exception ex)
			{
				printError(sFullPath, "Error processing file information", ex.Message);
			}
			try
				{
				// Scan recursively
				DirectoryInfo[] dirs = di.GetDirectories();
				if (dirs == null || dirs.Length < 1)
					return;
				foreach (DirectoryInfo dir in dirs)
					EnumerateFiles(dir.FullName);
			}
			catch (Exception ex)
			{
				printError(sFullPath,  "Error processing directory information", ex.Message);
			}
		}
 
		static void writeToLog(string logMessage)
		{
			if (logFileName.Length <= 0)
            {
				logFileName = "findcnf_" + strFind + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmssffff") + ".log";

				printInfo("Created log file: ", logFileName);
			}
			try
			{
				using (StreamWriter logFile = File.AppendText(logFileName))
				{
					logFile.WriteLine(logMessage);
					// Console.WriteLine(logMessage);
				}
			}
			catch (Exception ex)
            {
				printError(logFileName, "Error creating log file", ex.Message);
            }
		}

		static void writeLogHeader(string args)
		{
			writeToLog("Execution: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
			writeToLog(args);
		}

		static void printUsageAndExit()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Usage: ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("findcnf.exe");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("path");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(">");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("STR1");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(">");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("As from <path>, find STR1 in configuration (.config & .udl) files.");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("Third ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("3");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("ye Software Inc. (c) 2021");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Version: {0}.{1}.{2}. ", versionMajor, versionMinor, versionRevision);
			System.Environment.Exit(0);
		}

		static void Main(string[] args)
        {
			if (args.Length != 2)
			{
				printUsageAndExit();
			}
			else 
			{
				string path = args[0];
				strFind = args[1];

				printInfo("Execution: ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
				printInfo("Searching directory: ", "'" + path + "'");
				printInfo("Looking for string: ", "'" + strFind + "'");

				writeLogHeader("Searching directory '" + path + "' for '" + strFind + "'");

				if (Directory.Exists(path))
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();

					EnumerateFiles(path);

					printInfo("Found '" + strFind + "': ", foundCount.ToString() + " times.");
					
					watch.Stop();
					var elapsedMs = watch.ElapsedMilliseconds;
					printTime(elapsedMs);
				}
				else
				{
					printError(path, "Error opening directory", "Path does not exist");
					System.Environment.Exit(0);
				}
			}

		}
    }
}
