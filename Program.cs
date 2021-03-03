using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EncryptorLibrary;

namespace findcnf3
{
    class Program
    {
		static int versionMajor = 3;
		static int versionMinor = 0;
		static int versionRevision = 1;
		static string strFind = "";

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

		static void printError(string name, string error, string detail)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("" + error + ": ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("\'" + name + "\' ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("(" + detail + ")");
			Console.ForegroundColor = ConsoleColor.White;
		}

		static void printInfo(string title, string data)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(data);
			Console.ForegroundColor = ConsoleColor.White;
		}

		static bool isEncrypted(string line, string path, string strOld)
        {
			Encryptor enc = new Encryptor();
			string cryptLine;
			try
			{
				cryptLine = enc.Encrypt(line, true);
				if (cryptLine.Contains("atalog") || cryptLine.Contains("atabase") || cryptLine.Contains("DSN"))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				return false;
			}
			return false;
		}


		static bool fileHasEncriptedConnectionString(string path, string strOld)
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
			int i1 = 0, i2 = 0, i3 = 0;
			string line = "";

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
						if (isEncrypted(line, path, strOld))
                        {
							return true;
                        }
						if (i2 > i3) break;
						substring = substring.Substring(i2 + (i3 - i2)); // get from after last '/>'
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
				writeToLog("Found: '" + strFind + "' in '" + file.FullName + "'");
				printInfo("Found '" + strFind + "' in file: '", file.FullName);
			}
			else
			{
				/*
				// Maybe it is Little Endian!
				if (UTF16FileContainsString(file.FullName, strFind)) {
					writeToLog("Found: '" + strFind + "' in '" + file.FullName + "'");
					printInfo("Found '" + strFind + "' in file: '", file.FullName);
				} 
				else
				{*/
				if (fileHasEncriptedConnectionString(file.FullName, strFind)) {
					writeToLog("File: '" + file.FullName + "' has encrypted connectionsStrings!");
					printInfo("Found encrypted connection string in file: '", file.FullName);
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
					if (file.Extension.ToUpper().Equals(".CONFIG") || file.Extension.ToUpper().Equals(".UDL"))
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
			string logFileName = "findcnf3.log";
			using (StreamWriter logFile = File.AppendText(logFileName))
			{
				logFile.WriteLine(logMessage);
				// Console.WriteLine(logMessage);
			}
		}

		static void writeLogHeader()
		{
			writeToLog("Execution: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
		}

		static void printUsageAndExit()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Usage: ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("findcnf3.exe");
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
			Console.WriteLine("As from <path>, find STR1 in files.");
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

				writeToLog("Searching directory '" + path + "' for '" + strFind + "'");
				writeLogHeader();
				if (Directory.Exists(path))
				{
					EnumerateFiles(path);
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
