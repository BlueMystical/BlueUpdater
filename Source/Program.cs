using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UtilConsole;

namespace BlueUpdater
{
	class Program
	{
		[STAThread]
		static async Task Main(string[] args)
		{
			dynamic _Config = GetConfiguration("./Version.json");
			try
			{
				Consola.ValidateInstance();
				Consola.Initialize(true);

				Consola.Write("Checking Version of:       ");
				Consola.WriteLine(string.Format(@"{0}\{1}", _Config["GitHub-Owner"], _Config["GitHub-Repo"]), ConsoleColor.Cyan);

				string latestVersion = await GitHubVersionChecker.GetLatestReleaseVersion(
					_Config["GitHub-Owner"].ToString(), _Config["GitHub-Repo"].ToString());
				latestVersion = latestVersion.TrimStart('v');

				Consola.WriteLine($"Latest Release Version:    {latestVersion}");
				Consola.WriteLine($"Current Installed Version: {_Config["CurrentVersion"]}");
				Consola.DrawLine();

				Version version1 = new Version(latestVersion);
				Version version2 = new Version(_Config["CurrentVersion"].ToString());
				int comparisonResult = version1.CompareTo(version2);

				if (comparisonResult > 0)
				{
					if (Consola.Confirmar("There is a new Version available!  Download?"))
					{
						// Shows a Progressbar:
						Consola.ProgressBar(0, 100, "Downloading..");

						// Build the Download Link:
						string url = string.Format(@"https://github.com/{0}/{1}/releases/latest/download/{2}",
							_Config["GitHub-Owner"].ToString(), _Config["GitHub-Repo"].ToString(), _Config["ReleaseFile"].ToString());

						string zipFilePath = $"./{_Config.ReleaseFile}";
						string extractPath = Directory.GetParent(Consola.ApplicationInfo.AppExePath).FullName;


						// Setup and Start the Download:
						DownloadHelper.DownloadProgressChanged += OnDownloadProgressChanged;
						DownloadHelper.DownloadCompleted += OnDownloadCompleted;
						DownloadHelper.DownloadFailed += OnDownloadFailed;

						await DownloadHelper.DownloadFileAsync(url, zipFilePath);

						//Kills the Parent App:
						TerminateProgram(_Config.MainExecutable.ToString());

						try
						{
							//The update should bring a new version of this program, therefore renaming the file so it can be patched.
							DeleteOldFiles(extractPath);
							System.IO.File.Move("./BlueUpdater.exe", "./BlueUpdater.exe.old");
						}
						catch { }

						// Finally lets unzip the Update contents:
						DownloadHelper.UnzipFile(zipFilePath, extractPath, (bool)_Config.FolderContained);

						//Remember the new Version:
						_Config["CurrentVersion"] = latestVersion;
						File.WriteAllText("Version.json", JsonConvert.SerializeObject(_Config, Formatting.Indented));

						Console.WriteLine("Download and extraction completed.");
						string _MainExecutable = $"{_Config.MainExecutable}";
						Consola.WriteLine($"Starting '{_MainExecutable}'..");

						// Starts the newly updated Program:
						if (File.Exists(_MainExecutable) && (bool)_Config.AutoRun)
						{
							var startInfo = new ProcessStartInfo
							{
								FileName = _MainExecutable,
								UseShellExecute = false,
								CreateNoWindow = true
							};

							Process.Start(startInfo);
						}
					}
				}
				else
				{
					Consola.WriteLine("The Program is Updated.", ConsoleColor.Green);
				}
			}
			catch (Exception ex)
			{
				Consola.WriteLine(ex.Message, ConsoleColor.Red);
				Consola.WriteLine("Press ENTER to continue... ", ConsoleColor.Green);
				Consola.ReadLine();
			}
			finally
			{
				//Consola.ReadLine();
			}
		}

		private static void OnDownloadProgressChanged(object sender, DownloadProgressEventArgs e)
		{
			string message = (e.ProgressPercentage >= 100) ? "Done." : "Downloading..";
			Consola.ProgressBar((int)e.ProgressPercentage, 100, message); //<- Shows the Progressbar
		}
		private static void OnDownloadCompleted(object sender, EventArgs e)
		{
			Consola.WriteLine("Download Completed.");
		}
		private static void OnDownloadFailed(object sender, Exception e)
		{
			Console.WriteLine($"Download Failed: {e.Message}");
		}


		public static dynamic GetConfiguration(string pFile)
		{
			dynamic _ret = null;
			try
			{
				if (File.Exists(pFile))
				{
					_ret = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(pFile));
				}
			}
			catch (Exception ex)
			{
				Consola.WriteLine(ex.Message, ConsoleColor.Red);
			}
			return _ret;
		}

		public static void TerminateProgram(string fileName)
		{
			try
			{
				string processName = Path.GetFileNameWithoutExtension(fileName);
				var processes = Process.GetProcessesByName(processName);
				bool foundProcess = false;

				foreach (var process in processes)
				{
					foundProcess = true;
					process.Kill();
					process.WaitForExit();
					Console.WriteLine($"Terminated: {process.ProcessName}, PID: {process.Id}");
				}

				if (!foundProcess)
				{
					Console.WriteLine($"No running processes found for {fileName}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error terminating process: {ex.Message}");
			}
		}

		private static void DeleteOldFiles(string directoryPath)
		{
			var oldFiles = Directory.GetFiles(directoryPath, "*.old", SearchOption.AllDirectories);
			foreach (var oldFile in oldFiles)
			{
				try
				{
					File.Delete(oldFile);
				}
				catch { }
			}
		}
	}
}
