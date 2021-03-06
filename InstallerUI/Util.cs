using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace InstallerUI
{
	public static class Util
	{
        public const string SettingsPath = "Gta5EyeTracking";
		public static string GetGtaInstallPathFromRegistry()
		{
			var regKey = new ApplicationUninstallRegistryKey("Grand Theft Auto V");
			if (regKey.IsPresent)
			{
				return regKey.InstallLocation;
			}
			return null;
		}
		public static bool IsValidGtaFolder(string gtaPath)
		{
			if (gtaPath == null) return false;
			return File.Exists(Path.Combine(gtaPath, "GTA5.exe"));
		}

		public static bool TryGetFileVersion(string filePath, ref Version fileVersion)
		{
			if (!File.Exists(filePath))
			{
				//Invalid gta path
				return false;
			}

			try
			{
				var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
				if (!Version.TryParse(fileVersionInfo.FileVersion, out fileVersion))
				{
					//Can't parse gta version
					return false;
				}
			}
			catch
			{
				//Can't get gta version
				return false;
			}
			return true;
		}

		public static string ReadWebPageContent(string urlAddress)
		{
			var request = (HttpWebRequest)WebRequest.Create(urlAddress);
			request.Timeout = 5000;
			var response = (HttpWebResponse)request.GetResponse();

			if (response.StatusCode != HttpStatusCode.OK) return null;

			var receiveStream = response.GetResponseStream();
			StreamReader readStream = null;

			if (response.CharacterSet == null)
			{
				readStream = new StreamReader(receiveStream);
			}
			else
			{
				readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
			}

			var data = readStream.ReadToEnd();

			response.Close();
			readStream.Close();

			return data;
		}

		public static string GetDownloadsPath()
		{
			var downloadsFolderName = "Downloads";
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsPath,
				downloadsFolderName);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			return path;
		}

		public static void Log(string message)
		{
			var now = DateTime.Now;
			var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingsPath);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}


			var logpath = Path.Combine(folderPath, "installer.log." + Process.GetCurrentProcess().Id + ".txt");

			try
			{
				var fs = new FileStream(logpath, FileMode.Append, FileAccess.Write, FileShare.Read);
				var sw = new StreamWriter(fs);

				try
				{
					sw.Write("[" + now.ToString("dd.MM.yyyy HH:mm:ss") + "] ");

					sw.Write(message);

					sw.WriteLine();
				}
				finally
				{
					sw.Close();
					fs.Close();
				}
			}
			catch
			{
				return;
			}
		}


		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite, bool backup, IEnumerable<string> skipFiles)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				if ((skipFiles != null) &&
					(skipFiles.Any(fileName => fileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))) continue;
				if (backup)
				{
					if (File.Exists(temppath))
					{
						if (File.Exists(temppath + ".bak"))
						{
							File.Delete(temppath + ".bak");
						}
						File.Move(temppath, temppath + ".bak");
					}
				}
				file.CopyTo(temppath, overwrite);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs, overwrite, backup, skipFiles);
				}
			}
		}
	}
}