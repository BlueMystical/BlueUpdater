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
				Consola.WriteLine(string.Format(@"{0}\{1}", _Config["GitHub-Owner"], _Config["GitHub-Repo"]), ConsoleColor.Cyan );

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
						string url = string.Format(@"https://github.com/{0}/{1}/releases/latest/download/{2}",
							_Config["GitHub-Owner"].ToString(), _Config["GitHub-Repo"].ToString(), _Config["ReleaseFile"].ToString());

						string zipFilePath = $"./{_Config["ReleaseFile"].ToString()}";
						string extractPath = Directory.GetParent(Directory.GetParent(Consola.ApplicationInfo.AppExePath).FullName).FullName;

						// Shows a Progressbar:
						Consola.ProgressBar(0, 100, "Downloading..");

						// Setup and Start the Update Download:
						DownloadHelper.DownloadProgressChanged += OnDownloadProgressChanged;
						DownloadHelper.DownloadCompleted += OnDownloadCompleted;
						DownloadHelper.DownloadFailed += OnDownloadFailed;

						await DownloadHelper.DownloadFileAsync(url, zipFilePath);
												
						try
						{
							//Kills the Parent App:
							TerminateProgram(_Config["MainExecutable"].ToString());

							//The update should bring a new version of this program, therefore renaming the file so it can be patched.
							if (File.Exists("BlueUpdater.exe"))
							{
								System.IO.File.Move("BlueUpdater.exe", "BlueUpdater.exe.old");
							}

							// Finally lets unzip the Update contents:
							if ((bool)_Config.AutoExtract) DownloadHelper.UnzipFile(zipFilePath, extractPath);
						}
						catch { }

						//Remember the new Version:
						_Config["CurrentVersion"] = latestVersion;
						File.WriteAllText("Version.json", JsonConvert.SerializeObject(_Config, Formatting.Indented));

						Console.WriteLine("Download and extraction completed.");
						if ((bool)_Config.AutoRun) System.Diagnostics.Process.Start(_Config["MainExecutable"].ToString());
					}
				}
				else
				{
					Consola.WriteLine("The Program is Updated.", ConsoleColor.Cyan);
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
				Consola.ReadLine();
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
	}
}
