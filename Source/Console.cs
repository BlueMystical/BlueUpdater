using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LPARAM = System.IntPtr; //LPARAM, WPARAM, LRESULT, X_PTR, SIZE_T, ... (non-pointer types that have different size in 64-bit and 32-bit process)
using Wnd = System.IntPtr; //HWND (window handle)

namespace UtilConsole
{
	/// <summary>Reemplazo y Extension para la Consola en una Aplicacion de Consola.
	/// <para>Autor: Jhollman Chacon Ene/2019.</para> 
	/// No recuerdo de donde copié parte del codigo, algún lugar en StackOverflow..
	/// </summary>	
	public static class Consola
	{
		/// <summary>Guarda toda la salida x pantalla del programa.</summary>
		public static System.Text.StringBuilder LogString = new System.Text.StringBuilder();

		#region Private Stuff

		private const char PASSWORD_CHAR = '*';
		private const int PASSWORD_LENGHT = 0;
		private static int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };


		/// <summary>Constantes para los Codigos de Pagina al leer o guardar archivos de texto.</summary>
		public enum TextEncoding
		{
			/// <summary>CodePage:1252; Windows-1252; ANSI Latin 1; Western European (Windows)</summary>
			ANSI = 1252,
			/// <summary>CodePage:850; ibm850; ASCII Multilingual Latin 1; Western European (DOS)</summary>
			DOS_850 = 850,
			/// <summary>CodePage:1200; utf-16; Unicode (UTF-16), little endian byte order (BMP of ISO 10646);</summary>
			Unicode = 1200,
			/// <summary>CodePage:65001; utf-8; Unicode (UTF-8)</summary>
			UTF8 = 65001
		}

		#endregion

		#region Properties

		public class AssemblyInfo
		{
			public string AppTitle { get; set; } = string.Empty;
			public string AppExeName { get; set; } = AppDomain.CurrentDomain.FriendlyName;
			public string AppExePath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

			public string Autor { get; set; } = "JChacon";
			public string ProgramGUID { get; set; } = string.Empty;
			public Version Version { get; set; } = new Version("1.0");
			public string Company { get; set; } = "CUTCSA, Dpto. Informatico";

			public string Description { get; set; } = string.Empty;
			public string HelpInfo { get; set; } = string.Empty;
		}

		/// <summary>Useful Information about this Program.</summary>
		public static AssemblyInfo ApplicationInfo { get; set; }

		#endregion

		/// <summary>Obtiene Informacion del Ensamblado usada para identificar este programa.</summary>
		public static AssemblyInfo GetAssemblyInfo()
		{
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();

				// Obtener los atributos del Ensamblado:

				AssemblyDescriptionAttribute description = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;
				AssemblyTitleAttribute titleAttribute = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute;
				AssemblyCopyrightAttribute copyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute;
				AssemblyProductAttribute product = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0] as AssemblyProductAttribute;
				AssemblyCompanyAttribute company = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0] as AssemblyCompanyAttribute;
				GuidAttribute guid = assembly.GetCustomAttributes(typeof(GuidAttribute), false)[0] as GuidAttribute;

				ApplicationInfo = new AssemblyInfo
				{
					Version = Assembly.GetEntryAssembly().GetName().Version,
					Description = description.Description,
					AppTitle = titleAttribute.Title,
					Autor = copyright.Copyright,
					Company = company.Company,
					ProgramGUID = guid.Value					
				};
			}
			catch (Exception ex)
			{
				Consola.WriteLine(ex.Message, ConsoleColor.Red);
			}
			return ApplicationInfo;
		}

		/// <summary>Si hay Otra instancia del programa corriendo, la nueva se cierra.</summary>
		public static void ValidateInstance()
		{
			Process currentProcess = Process.GetCurrentProcess();
			Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
			if (processes != null && processes.Length > 1)
			{
				Consola.WriteLine("┌{0}┐", ConsoleColor.Red, new string('─', 62));
				Consola.WriteLine("│{0}│", ConsoleColor.Red, CenterString("ERROR: Ya hay una instancia de este porograma ejecutandose.", 62));
				Consola.WriteLine("└{0}┘", ConsoleColor.Red, new string('─', 62));
				Environment.Exit(0); // Terminate the current instance
			}
		}

		/// <summary>Muestra un Cuadro con la Información del Programa.</summary>
		public static void Initialize(bool ShowDescription = false)
		{
			try
			{
				if (ApplicationInfo is null)
				{
					GetAssemblyInfo();
				}

				if (Console.OutputEncoding != System.Text.Encoding.UTF8)
				{
					EstablecerFormatoNumerico();
				}

				Console.Title = ApplicationInfo.AppTitle;

				// Maximo Ancho es 64 Caracteres:
				Consola.WriteLine("╔{0}╗", new string('═', 62));
				Consola.WriteLine("║{0}║", CenterString(ApplicationInfo.AppTitle, 62));
				Consola.WriteLine("║{0}║", CenterString(string.Format("Version {0}, {1}", ApplicationInfo.Version, ApplicationInfo.Autor), 62));
				Consola.WriteLine("║{0}║", CenterString(ApplicationInfo.Company, 62));
				Consola.WriteLine("╚{0}╝", Console.ForegroundColor, new string('═', 62));

				if (ShowDescription && !string.IsNullOrEmpty(ApplicationInfo.Description))
				{
					var Lineas = ApplicationInfo.Description.Split(new string[] { "\r\n" }, StringSplitOptions.None);
					if (Lineas != null && Lineas.Length > 0)
					{
						Consola.WriteLine("┌{0}┐", Console.ForegroundColor, new string('─', 62));
						foreach (string linea in Lineas)
						{
							var Cortes = CutLongString(linea, 62);
							foreach (string corte in Cortes)
							{
								Consola.WriteLine("│{0}│", Console.ForegroundColor, AlignLeftString(corte, 62));
							}
						}
						Consola.WriteLine("└{0}┘", Console.ForegroundColor, new string('─', 62));
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + ex.StackTrace, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Muestra un Cuadro con la Información del Programa.</summary>
		/// <param name="Description">Qué es lo que hace este Programa</param>
		public static void Initialize(string Description = "", string Help = "")
		{
			try
			{
				if (ApplicationInfo is null)
				{
					GetAssemblyInfo();
				}

				if (Console.OutputEncoding != System.Text.Encoding.UTF8)
				{
					EstablecerFormatoNumerico();
				}

				ApplicationInfo.Description = Description;
				Console.Title = ApplicationInfo.AppTitle;

				// Maximo Ancho es 64 Caracteres:
				Consola.WriteLine("╔{0}╗", new string('═', 62));
				Consola.WriteLine("║{0}║", CenterString(ApplicationInfo.AppTitle, 62));
				Consola.WriteLine("║{0}║", CenterString(string.Format("Version {0}, {1}", ApplicationInfo.Version, ApplicationInfo.Autor), 62));
				Consola.WriteLine("║{0}║", CenterString(ApplicationInfo.Company, 62));
				Consola.WriteLine("╚{0}╝", Console.ForegroundColor, new string('═', 62));

				if (!string.IsNullOrEmpty(Description))
				{
					var Lineas = Description.Split(new string[] { "\r\n" }, StringSplitOptions.None);
					if (Lineas != null && Lineas.Length > 0)
					{
						Consola.WriteLine("┌{0}┐", Console.ForegroundColor, new string('─', 62));
						foreach (string linea in Lineas)
						{
							var Cortes = CutLongString(linea, 62);
							foreach (string corte in Cortes)
							{
								Consola.WriteLine("│{0}│", Console.ForegroundColor, AlignLeftString(corte, 62));
							}
						}
						Consola.WriteLine("└{0}┘", Console.ForegroundColor, new string('─', 62));
					}
				}
			}
			catch (Exception ex)
			{
				Consola.WriteLine(ex.Message, ConsoleColor.Red);
			}
		}

		/// <summary>Establece el uso Correcto de Puntos decimales y Separadores de Comas para la instancia actual del programa.</summary>
		public static void EstablecerFormatoNumerico()
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.InputEncoding = System.Text.Encoding.UTF8;

			//Obligar a usar los puntos y las comas;
			System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
			customCulture.NumberFormat.NumberDecimalSeparator = ",";
			customCulture.NumberFormat.NumberGroupSeparator = ".";
			customCulture.NumberFormat.CurrencyDecimalSeparator = ",";
			customCulture.NumberFormat.CurrencyGroupSeparator = ".";
			customCulture.DateTimeFormat.DateSeparator = "/";
			customCulture.DateTimeFormat.FullDateTimePattern = "dd/MM/yyyy HH:mm:ss";

			System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
		}

		#region Write

		/* Este es el metodo que realamente hace el trabajo */
		private static void WriteInternal(string value, ConsoleColor? foreColor, ConsoleColor? backColor, bool LogEntry = true)
		{
			ConsoleColor originalForeColor = Console.ForegroundColor;
			ConsoleColor originalBackColor = Console.BackgroundColor;

			if (foreColor.HasValue)
			{
				Console.ForegroundColor = foreColor.Value;
			}

			if (backColor.HasValue)
			{
				Console.BackgroundColor = backColor.Value;
			}

			Console.Write(value);
			if (LogEntry)
			{
				LogString.Append(value);
			}

			if (foreColor.HasValue)
			{
				Console.ForegroundColor = originalForeColor;
			}

			if (backColor.HasValue)
			{
				Console.BackgroundColor = originalBackColor;
			}
		}

		/// <summary>Escribe un Valor en la Consola con los Colores especificados.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		public static void Write<T>(T value, ConsoleColor foreColor, ConsoleColor backColor)
		{
			string valueString = Equals(value, null) ? string.Empty : value.ToString();
			WriteInternal(valueString, foreColor, backColor);
		}

		/// <summary>Escribe un Valor en la Consola con los Colores especificados.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		public static void Write<T>(T value, ConsoleColor foreColor)
		{
			string valueString = Equals(value, null) ? string.Empty : value.ToString();
			WriteInternal(valueString, foreColor, null);
		}

		/// <summary>Escribe un Valor en la Consola.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		public static void Write<T>(T value)
		{
			string valueString = Equals(value, null) ? string.Empty : value.ToString();
			WriteInternal(valueString, null, null);
		}

		#endregion

		#region Write (format)

		/// <summary>Escribe una Cadena de Texto Formateada con los Colores Especificados.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> or <paramref name="args"/> is null.</exception>
		public static void Write(string format, ConsoleColor foreColor, ConsoleColor backColor, params object[] args) =>
			WriteInternal(string.Format(format, args), foreColor, backColor);

		/// <summary>Escribe una Cadena de Texto Formateada con los Colores Especificados.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> or <paramref name="args"/> is null.</exception>
		public static void Write(string format, ConsoleColor foreColor, bool LogEntry, params object[] args) =>
			WriteInternal(string.Format(format, args), foreColor, null, LogEntry);

		/// <summary>Escribe una Cadena de Texto Formateada en la Consola.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> or <paramref name="args"/> is null.</exception>
		public static void Write(string format, params object[] args) =>
			WriteInternal(string.Format(format, args), null, null);

		/// <summary>Escribe una Cadena de Texto Formateada en la Consola.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="LogEntry">Determina si se Guarda el registro en el Log</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> or <paramref name="args"/> is null.</exception>
		public static void Write(string format, bool LogEntry, params object[] args) =>
			WriteInternal(string.Format(format, args), null, null, LogEntry);

		#endregion

		#region WriteLine

		/// <summary>Escribe una Linea vacia en la consola.</summary>
		public static void WriteLine() =>
			WriteInternal($"{Environment.NewLine}", null, null, true);

		/// <summary>Escribe un valor en la consola con los Colores especificados y comienza una Nueva Linea.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		public static void WriteLine<T>(T value, ConsoleColor foreColor, ConsoleColor backColor) =>
			WriteInternal($"{value}{Environment.NewLine}", foreColor, backColor, true);

		/// <summary>Escribe un valor en la consola con los Colores especificados y comienza una Nueva Linea.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		public static void WriteLine<T>(T value, ConsoleColor foreColor) =>
			WriteInternal($"{value}{Environment.NewLine}", foreColor, null, true);

		/// <summary>Escribe un valor en la consola con los Colores especificados y comienza una Nueva Linea.</summary>
		/// <typeparam name="T">Tipo de datos del Valor a escribir.</typeparam>
		/// <param name="value">El Valor a escribir.</param>
		public static void WriteLine<T>(T value, bool LogEntry = true) =>
			WriteInternal($"{value}{Environment.NewLine}", null, null, LogEntry);

		#endregion

		#region WriteLine (format)

		/// <summary>Escribe una Cadena Formateada en la Consola con los Colores Especificados y comienza una Nueva Linea.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
		public static void WriteLine(string format, ConsoleColor foreColor, ConsoleColor backColor, params object[] args) =>
			WriteInternal(string.Format(format, args) + Environment.NewLine, foreColor, backColor, true);

		/// <summary>Escribe una Cadena Formateada en la Consola con los Colores Especificados y comienza una Nueva Linea.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
		public static void WriteLine(string format, ConsoleColor foreColor, params object[] args) =>
			WriteInternal(string.Format(format, args) + Environment.NewLine, foreColor, null, true);

		/// <summary>Escribe una Cadena Formateada en la Consola y comienza una Nueva Linea.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
		public static void WriteLine(string format, params object[] args) =>
			WriteInternal(string.Format(format, args) + Environment.NewLine, null, null, true);

		/// <summary>Escribe una Cadena Formateada en la Consola y comienza una Nueva Linea.</summary>
		/// <param name="format">El Formato de la Cadena, ej: '{0:n2}'.</param>
		/// <param name="LogEntry">Determina si se Guarda el registro en el Log</param>
		/// <param name="args">Argumentos (Valores) que se agregan a la Cadena.</param>
		/// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
		public static void WriteLine(string format, bool LogEntry, params object[] args) =>
			WriteInternal(string.Format(format, args) + Environment.NewLine, null, null, LogEntry);

		#endregion

		#region Read

		/* Este es el metodo que realamente hace el trabajo */
		private static T ReadInternal<T>(Func<T> readFunc, ConsoleColor? foreColor, ConsoleColor? backColor)
		{
			ConsoleColor originalForeColor = Console.ForegroundColor;
			ConsoleColor originalBackColor = Console.BackgroundColor;

			if (foreColor.HasValue)
			{
				Console.ForegroundColor = foreColor.Value;
			}

			if (backColor.HasValue)
			{
				Console.BackgroundColor = backColor.Value;
			}

			T result = readFunc();

			if (foreColor.HasValue)
			{
				Console.ForegroundColor = originalForeColor;
			}

			if (backColor.HasValue)
			{
				Console.BackgroundColor = originalBackColor;
			}

			return result;
		}

		/// <summary>Lee el siguiente Caracter de la Entrada estandard y muestra estos caracteres usando los colores especificados.</summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		/// <returns>El siguiente caracter de la entrada, o menos uno (-1) si no hay caracteres para leer.</returns>
		public static int Read(ConsoleColor foreColor, ConsoleColor backColor) => ReadInternal(Console.Read, foreColor, backColor);

		/// <summary>Lee el siguiente Caracter de la Entrada estandard y muestra estos caracteres usando los colores especificados.</summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <returns>El siguiente caracter de la entrada, o menos uno (-1) si no hay caracteres para leer.</returns>
		public static int Read(ConsoleColor foreColor) => ReadInternal(Console.Read, foreColor, null);

		/// <summary>Lee el siguiente Caracter de la Entrada estandard.</summary>
		/// <returns>El siguiente caracter de la entrada, o menos uno (-1) si no hay caracteres para leer.</returns>
		public static int Read() => ReadInternal(Console.Read, null, null);

		#endregion

		#region ReadKey

		/// <summary>Obtiene el siguiente Caracter o Tecla de funcion presionada por el Usuario. 
		/// <para>La tecla presionada se muestra en la Consola usando los colores especificados.</para>
		/// </summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		/// <returns>A <see cref="T:System.ConsoleKeyInfo"/> object that describes the <see cref="T:System.ConsoleKey"/> constant and Unicode character, if any, that correspond to the pressed console key. The <see cref="T:System.ConsoleKeyInfo"/> object also describes, in a bitwise combination of <see cref="T:System.ConsoleModifiers"/> values, whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously with the console key.</returns>
		public static ConsoleKeyInfo ReadKey(ConsoleColor foreColor, ConsoleColor backColor) => ReadInternal(Console.ReadKey, foreColor, backColor);

		/// <summary>Obtiene el siguiente Caracter o Tecla de funcion presionada por el Usuario. 
		/// <para>La tecla presionada se muestra en la Consola usando los colores especificados.</para>
		/// </summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <returns>A <see cref="T:System.ConsoleKeyInfo"/> object that describes the <see cref="T:System.ConsoleKey"/> constant and Unicode character, if any, that correspond to the pressed console key. The <see cref="T:System.ConsoleKeyInfo"/> object also describes, in a bitwise combination of <see cref="T:System.ConsoleModifiers"/> values, whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously with the console key.</returns>
		public static ConsoleKeyInfo ReadKey(ConsoleColor foreColor) => ReadInternal(Console.ReadKey, foreColor, null);

		/// <summary>Obtiene el siguiente Caracter o Tecla de funcion presionada por el Usuario. 
		/// <para>La tecla presionada se muestra en la Consola.</para>
		/// </summary>
		/// <returns>A <see cref="T:System.ConsoleKeyInfo"/> object that describes the <see cref="T:System.ConsoleKey"/> constant and Unicode character, if any, that correspond to the pressed console key. The <see cref="T:System.ConsoleKeyInfo"/> object also describes, in a bitwise combination of <see cref="T:System.ConsoleModifiers"/> values, whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously with the console key.</returns>
		public static ConsoleKeyInfo ReadKey() => ReadInternal(Console.ReadKey, null, null);

		/// <summary>Obtiene el siguiente Caracter o Tecla de funcion presionada por el Usuario. 
		/// <para>La tecla presionada es Opcionalmente muestrada en la Consola usando los colores especificados.</para>
		/// </summary>
		/// <param name="intercept">Determina como mostrar la tecla presionada en la Consola. 'true' para no mostrar la tecla presionada; otherwise, false.</param>
		/// <returns>A <see cref="T:System.ConsoleKeyInfo"/> object that describes the <see cref="T:System.ConsoleKey"/> constant and Unicode character, if any, that correspond to the pressed console key. The <see cref="T:System.ConsoleKeyInfo"/> object also describes, in a bitwise combination of <see cref="T:System.ConsoleModifiers"/> values, whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously with the console key.</returns>
		public static ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

		#endregion

		#region ReadLine

		/// <summary>Lee la siguiente Linea de Caracteres de la Entrada estandard y muestra estos caracteres usando los colores especificados.</summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <param name="backColor">Color del Fondo. (BackColor)</param>
		/// <returns>La siguiente linea de caracteres de la Entrada Standard, o <c>null</c> si no hay mas lineas para leer.</returns>
		public static string ReadLine(ConsoleColor foreColor, ConsoleColor backColor) => ReadInternal(Console.ReadLine, foreColor, backColor);

		/// <summary>Lee la siguiente Linea de Caracteres de la Entrada estandard y muestra estos caracteres usando los colores especificados.</summary>
		/// <param name="foreColor">Color del Texto. (Forecolor)</param>
		/// <returns>La siguiente linea de caracteres de la Entrada Standard, o <c>null</c> si no hay mas lineas para leer.</returns>
		public static string ReadLine(ConsoleColor foreColor) => ReadInternal(Console.ReadLine, foreColor, null);

		/// <summary>Lee la siguiente Linea de Caracteres de la Entrada estandard y muestra estos caracteres en la Consola.</summary>
		/// <returns>La siguiente linea de caracteres de la Entrada Standard, o <c>null</c> si no hay mas lineas para leer.</returns>
		public static string ReadLine() => ReadInternal(Console.ReadLine, null, null);

		public static string ReadLineCancel()
		{
			string result = null;

			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			ConsoleKeyInfo info = Console.ReadKey(true);
			while (info.Key != ConsoleKey.Enter && info.Key != ConsoleKey.Escape)
			{
				Console.Write(info.KeyChar);
				buffer.Append(info.KeyChar);
				info = Console.ReadKey(true);
			}

			if (info.Key == ConsoleKey.Enter)
			{
				result = buffer.ToString();
				Console.WriteLine();
			}

			return result;
		}

		#endregion

		#region ReadPassword

		/* Este es el metodo que realamente hace el trabajo */
		private static TResult ReadPasswordInternal<TResult>(char outputChar, int maxLength, IPasswordBuilder<TResult> passwordBuilder)
		{
			ConsoleKeyInfo keyInfo;

			while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
			{
				int currentLength = passwordBuilder.Length;

				if (keyInfo.Key == ConsoleKey.Backspace)
				{
					if (currentLength > 0)
					{
						passwordBuilder.Backspace();
						Write("\b \b");
					}
				}
				else if (maxLength == 0 || currentLength < maxLength)
				{
					passwordBuilder.AddChar(keyInfo.KeyChar);
					Write(outputChar);
				}
			}

			return passwordBuilder.GetResult();
		}

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard. </summary>
		/// <param name="outputChar">Caracter usado para enmascarar la contraseña.</param>
		/// <param name="maxLength">Maxima longitud permitida para la contraseña.</param>
		/// <returns>La contraseña como una <see cref="string"/>.</returns>
		public static string ReadPassword(char outputChar, int maxLength)
		{
			PasswordBuilder passwordBuilder = new PasswordBuilder();
			string result = ReadPasswordInternal(outputChar, maxLength, passwordBuilder);
			WriteLine();
			return result;
		}

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard. </summary>
		/// <param name="outputChar">Caracter usado para enmascarar la contraseña.</param>
		/// <returns>La contraseña como una <see cref="string"/>.</returns>
		public static string ReadPassword(char outputChar) => ReadPassword(outputChar, PASSWORD_LENGHT);

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard. </summary>
		/// <param name="maxLength">Maxima longitud permitida para la contraseña.</param>
		/// <returns>La contraseña como una <see cref="string"/>.</returns>
		public static string ReadPassword(int maxLength) => ReadPassword(PASSWORD_CHAR, maxLength);

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard. </summary>
		/// <returns>La contraseña como una <see cref="string"/>.</returns>
		public static string ReadPassword() => ReadPassword(PASSWORD_CHAR, PASSWORD_LENGHT);

		#endregion

		#region ReadPasswordSecure

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard como una <see cref="SecureString"/>.</summary>
		/// <param name="outputChar">Caracter usado para enmascarar la contraseña.</param>
		/// <param name="maxLength">Maxima longitud permitida para la contraseña.</param>
		/// <returns>La contraseña como una <see cref="SecureString"/>.</returns>
		public static SecureString ReadPasswordSecure(char outputChar, int maxLength)
		{
			SecureString result;

			using (SecurePasswordBuilder passwordBuilder = new SecurePasswordBuilder())
			{
				result = ReadPasswordInternal(outputChar, maxLength, passwordBuilder);
				WriteLine();
			}

			return result;
		}

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard como una <see cref="SecureString"/>.</summary>
		/// <param name="outputChar">Caracter usado para enmascarar la contraseña.</param>
		/// <returns>La contraseña como una <see cref="SecureString"/>.</returns>
		public static SecureString ReadPasswordSecure(char outputChar) => ReadPasswordSecure(outputChar, PASSWORD_LENGHT);

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard como una <see cref="SecureString"/>.</summary>
		/// <param name="maxLength">Maxima longitud permitida para la contraseña.</param>
		/// <returns>La contraseña como una <see cref="SecureString"/>.</returns>
		public static SecureString ReadPasswordSecure(int maxLength) => ReadPasswordSecure(PASSWORD_CHAR, maxLength);

		/// <summary>Lee y enmascara una Contraseña de la Entrada Estandard como una <see cref="SecureString"/>.</summary>
		/// <returns>La contraseña como una <see cref="SecureString"/>.</returns>
		public static SecureString ReadPasswordSecure() => ReadPasswordSecure(PASSWORD_CHAR, PASSWORD_LENGHT);

		#endregion

		#region Saving Files

		/// <summary>Adds a new Entry to the Log</summary>
		/// <param name="value">The text to add.</param>
		public static void LogEntry(string value)
		{
			LogString.AppendLine(value);
		}
		/// <summary>Adds a new Entry to the Log</summary>
		/// <param name="Format">The text to add using 'FormatString'</param>
		/// <param name="args">Values added to the Format.</param>
		public static void LogEntry(string Format, params object[] args)
		{
			LogString.AppendLine(string.Format(Format, args));
		}

		/// <summary>Guarda toda la salida del programa en un archivo de texto 'Log.txt' ubicado en la carpeta donde se ejecuta el programa.</summary>
		/// <param name="Append">[Opcional] 'true' Añade el log al archivo ya existente.</param>
		/// <param name="Path">[Opcional] Ruta del archivo de Log.</param>
		public static void SaveLog(bool Append = false, string Path = "./Log.txt")
		{
			if (LogString != null && LogString.Length > 0)
			{
				if (Append)
				{
					using (System.IO.StreamWriter file = System.IO.File.AppendText(Path))
					{
						file.Write(LogString.ToString());
						file.Close();
						file.Dispose();
					}
				}
				else
				{
					using (System.IO.StreamWriter FILE = new System.IO.StreamWriter(Path))
					{
						FILE.Write(LogString.ToString());
						FILE.Close();
						FILE.Dispose();
					}
				}
			}
		}

		/// <summary>Guarda Datos en un Archivo de Texto usando la Codificacion especificada.</summary>
		/// <param name="FilePath">Ruta de acceso al Archivo. Si no existe, se Crea. Si existe, se Sobreescribe.</param>
		/// <param name="Data">Datos a Grabar en el Archivo.</param>
		/// <param name="CodePage">[Opcional] Pagina de Codigos con la que se guarda el archivo. Por defecto se usa Unicode(UTF-16).</param>
		public static bool SaveTextFile(string FilePath, string Data, TextEncoding CodePage = TextEncoding.Unicode)
		{
			bool _ret = false;
			try
			{
				if (FilePath != null && FilePath != string.Empty)
				{
					/* ANSI code pages, like windows-1252, can be different on different computers, 
					 * or can be changed for a single computer, leading to data corruption. 
					 * For the most consistent results, applications should use UNICODE, <-----
					 * such as UTF-8 or UTF-16, instead of a specific code page. 
					 https://docs.microsoft.com/es-es/windows/desktop/Intl/code-page-identifiers  */

					System.Text.Encoding ENCODING = System.Text.Encoding.GetEncoding((int)CodePage); //<- Unicode Garantiza Maxima compatibilidad
					using (System.IO.FileStream FILE = new System.IO.FileStream(FilePath, System.IO.FileMode.Create))
					{
						using (System.IO.StreamWriter WRITER = new System.IO.StreamWriter(FILE, ENCODING))
						{
							WRITER.Write(Data);
							WRITER.Close();
						}
					}
					if (System.IO.File.Exists(FilePath))
					{
						_ret = true;
					}
				}
			}
			catch (Exception ex) { throw ex; }
			return _ret;
		}


		/// <summary>Lee un Archivo de Texto usando la Codificacion especificada.</summary>
		/// <param name="FilePath">Ruta de acceso al Archivo. Si no existe se produce un Error.</param>
		/// <param name="CodePage">Pagina de Codigos con la que se Leerá el archivo.</param>
		public static string ReadTextFile(string FilePath, TextEncoding CodePage)
		{
			string _ret = string.Empty;
			try
			{
				if (FilePath != null && FilePath != string.Empty)
				{
					if (System.IO.File.Exists(FilePath))
					{
						System.Text.Encoding ENCODING = System.Text.Encoding.GetEncoding((int)CodePage);
						_ret = System.IO.File.ReadAllText(FilePath, ENCODING);
					}
					else { throw new Exception(string.Format("ERROR 404: Archivo '{0}' NO Encontrado!", FilePath)); }
				}
				else { throw new Exception("ERROR 425: No se ha especificado la Ruta de acceso al Archivo!"); }
			}
			catch (Exception ex) { throw ex; }
			return _ret;
		}

		/// <summary>Lee un Archivo de Texto usando la Codificacion Detectada en el Archivo.
		/// <para>La Autodeteccion sólo funciona con archivos codificados con UTF8, Unicode(UTF16) o ANSI(Windows 1252)</para>
		/// <para>Si falla la deteccion, Leerá el archivo usando 'ANSI (Windows-1252)'</para></summary>
		/// <param name="FilePath">Ruta de acceso al Archivo. Si no existe se produce un Error.</param>
		public static string ReadTextFile(string FilePath)
		{
			string _ret = string.Empty;
			try
			{
				if (FilePath != null && FilePath != string.Empty)
				{
					if (System.IO.File.Exists(FilePath))
					{
						/* Intenta Detectar la Codificacion del Archivo y lo Lee usandola. */
						System.Text.Encoding ENCODING = GetTextFileEncoding(FilePath);
						_ret = System.IO.File.ReadAllText(FilePath, ENCODING);
					}
					else { throw new Exception(string.Format("ERROR 404: Archivo '{0}' NO Encontrado!", FilePath)); }
				}
				else { throw new Exception("No se ha Especificado la Ruta de acceso al Archivo!"); }
			}
			catch (Exception ex) { throw ex; }
			return _ret;
		}


		/// <summary>Detecta la Pagina de Codigos usada por un archivo de Texto.
		/// <para>Si falla la deteccion devuelve 'ANSI(Windows1252)'</para></summary>
		/// <param name="FilePath">Ruta de acceso al Archivo</param>
		public static System.Text.Encoding GetTextFileEncoding(string FilePath)
		{
			//Si el archivo no tiene Encabezado (BOM), su codificacion puede ser ANSI (1252 Windows) o ASCII (850 DOS), 
			//Por defecto usaremos la 1252 de Windows.
			System.Text.Encoding ENCODING = System.Text.Encoding.GetEncoding(1252);
			try
			{
				using (var READER = new System.IO.StreamReader(FilePath, ENCODING, true))
				{
					READER.Peek(); //<- Lee el Encabezado (BOM) del Archivo
					ENCODING = READER.CurrentEncoding;
				}
			}
			catch (Exception ex) { throw ex; }
			return ENCODING;
		}

		// Function to detect the encoding for UTF-7, UTF-8/16/32 (bom, no bom, little
		// & big endian), and local default codepage, and potentially other codepages.
		// 'taster' = number of bytes to check of the file (to save processing). Higher
		// value is slower, but more reliable (especially UTF-8 with special characters
		// later on may appear to be ASCII initially). If taster = 0, then taster
		// becomes the length of the file (for maximum reliability). 'text' is simply
		// the string with the discovered encoding applied to the file.
		public static System.Text.Encoding detectTextEncoding(string filename, out String text, int taster = 1000)
		{
			byte[] b = System.IO.File.ReadAllBytes(filename);

			//////////////// First check the low hanging fruit by checking if a
			//////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
			if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) { text = System.Text.Encoding.GetEncoding("utf-32BE").GetString(b, 4, b.Length - 4); return System.Text.Encoding.GetEncoding("utf-32BE"); }  // UTF-32, big-endian 
			else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) { text = System.Text.Encoding.UTF32.GetString(b, 4, b.Length - 4); return System.Text.Encoding.UTF32; }    // UTF-32, little-endian
			else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { text = System.Text.Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2); return System.Text.Encoding.BigEndianUnicode; }     // UTF-16, big-endian
			else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { text = System.Text.Encoding.Unicode.GetString(b, 2, b.Length - 2); return System.Text.Encoding.Unicode; }              // UTF-16, little-endian
			else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { text = System.Text.Encoding.UTF8.GetString(b, 3, b.Length - 3); return System.Text.Encoding.UTF8; } // UTF-8
			else if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) { text = System.Text.Encoding.UTF7.GetString(b, 3, b.Length - 3); return System.Text.Encoding.UTF7; } // UTF-7


			//////////// If the code reaches here, no BOM/signature was found, so now
			//////////// we need to 'taste' the file to see if can manually discover
			//////////// the encoding. A high taster value is desired for UTF-8
			if (taster == 0 || taster > b.Length)
			{
				taster = b.Length;    // Taster size can't be bigger than the filesize obviously.
			}


			// Some text files are encoded in UTF8, but have no BOM/signature. Hence
			// the below manually checks for a UTF8 pattern. This code is based off
			// the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
			// For our purposes, an unnecessarily strict (and terser/slower)
			// implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
			// For the below, false positives should be exceedingly rare (and would
			// be either slightly malformed UTF-8 (which would suit our purposes
			// anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
			int i = 0;
			bool utf8 = false;
			while (i < taster - 4)
			{
				if (b[i] <= 0x7F) { i += 1; continue; }     // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
				if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) { i += 2; utf8 = true; continue; }
				if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) { i += 3; utf8 = true; continue; }
				if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) { i += 4; utf8 = true; continue; }
				utf8 = false; break;
			}
			if (utf8 == true)
			{
				text = System.Text.Encoding.UTF8.GetString(b);
				return System.Text.Encoding.UTF8;
			}


			// The next check is a heuristic attempt to detect UTF-16 without a BOM.
			// We simply look for zeroes in odd or even byte places, and if a certain
			// threshold is reached, the code is 'probably' UF-16.          
			double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
			int count = 0;
			for (int n = 0; n < taster; n += 2)
			{
				if (b[n] == 0)
				{
					count++;
				}
			}

			if (((double)count) / taster > threshold) { text = System.Text.Encoding.BigEndianUnicode.GetString(b); return System.Text.Encoding.BigEndianUnicode; }
			count = 0;
			for (int n = 1; n < taster; n += 2)
			{
				if (b[n] == 0)
				{
					count++;
				}
			}

			if (((double)count) / taster > threshold) { text = System.Text.Encoding.Unicode.GetString(b); return System.Text.Encoding.Unicode; } // (little-endian)


			// Finally, a long shot - let's see if we can find "charset=xyz" or
			// "encoding=xyz" to identify the encoding:
			for (int n = 0; n < taster - 9; n++)
			{
				if (
					((b[n + 0] == 'c' || b[n + 0] == 'C') && (b[n + 1] == 'h' || b[n + 1] == 'H') && (b[n + 2] == 'a' || b[n + 2] == 'A') && (b[n + 3] == 'r' || b[n + 3] == 'R') && (b[n + 4] == 's' || b[n + 4] == 'S') && (b[n + 5] == 'e' || b[n + 5] == 'E') && (b[n + 6] == 't' || b[n + 6] == 'T') && (b[n + 7] == '=')) ||
					((b[n + 0] == 'e' || b[n + 0] == 'E') && (b[n + 1] == 'n' || b[n + 1] == 'N') && (b[n + 2] == 'c' || b[n + 2] == 'C') && (b[n + 3] == 'o' || b[n + 3] == 'O') && (b[n + 4] == 'd' || b[n + 4] == 'D') && (b[n + 5] == 'i' || b[n + 5] == 'I') && (b[n + 6] == 'n' || b[n + 6] == 'N') && (b[n + 7] == 'g' || b[n + 7] == 'G') && (b[n + 8] == '='))
					)
				{
					if (b[n + 0] == 'c' || b[n + 0] == 'C')
					{
						n += 8;
					}
					else
					{
						n += 9;
					}

					if (b[n] == '"' || b[n] == '\'')
					{
						n++;
					}

					int oldn = n;
					while (n < taster && (b[n] == '_' || b[n] == '-' || (b[n] >= '0' && b[n] <= '9') || (b[n] >= 'a' && b[n] <= 'z') || (b[n] >= 'A' && b[n] <= 'Z')))
					{ n++; }
					byte[] nb = new byte[n - oldn];
					Array.Copy(b, oldn, nb, 0, n - oldn);
					try
					{
						string internalEnc = System.Text.Encoding.ASCII.GetString(nb);
						text = System.Text.Encoding.GetEncoding(internalEnc).GetString(b);
						return System.Text.Encoding.GetEncoding(internalEnc);
					}
					catch { break; }    // If C# doesn't recognize the name of the encoding, break.
				}
			}


			// If all else fails, the encoding is probably (though certainly not
			// definitely) the user's local codepage! One might present to the user a
			// list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
			// A full list can be found using Encoding.GetEncodings();
			text = System.Text.Encoding.Default.GetString(b);
			return System.Text.Encoding.Default;
		}


		/// <summary>Muestra el Cuadro de Dialogo Nativo de Windows para Seleccionar un Archivo.</summary>
		/// <param name="Filter">Tipos de Archivo a Mostrar, ej: 'Archivos de Texto|*.txt|Todos los archivos|*.*'</param>
		/// <param name="DefaultExt">Extension por defecto, ej: 'txt'</param>
		/// <param name="InitDir">Directorio Inicial</param>
		/// <param name="Title">Titulo de la Ventana</param>
		/// <returns>Devuelve la Ruta de Acceso Completa al archivo seleccionado.</returns>
		public static string OpenFileDialog(string Filter = "Todos los archivos|*.*", string DefaultExt = "", string InitDir = "", string FileName = "", string Title = "")
		{
			string _ret = string.Empty;
			try
			{
				Win32API.OpenFileName ofn = new Win32API.OpenFileName();
				ofn.structSize = Marshal.SizeOf(ofn);
				ofn.filter = Filter.Replace('|', '\0'); //"Archivos de Texto\0*.txt\0Todos los Archivos\0*.*\0"; 
				ofn.file = FileName.PadRight(256, ' '); // new String(new char[256]);
				ofn.maxFile = ofn.file.Length;
				ofn.fileTitle = new String(new char[64]);
				ofn.maxFileTitle = ofn.fileTitle.Length;
				ofn.flags = Convert.ToInt32(Win32API.OFN.OFN_FILEMUSTEXIST | Win32API.OFN.OFN_PATHMUSTEXIST);

				//Obtener el Handler de la Ventana Actual:
				var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

				ofn.initialDir = IIf(InitDir == string.Empty, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), InitDir);
				ofn.title = IIf(Title == string.Empty, "Abrir Archivo..", Title);
				ofn.defExt = DefaultExt;

				/* USA UNA FUNCION API DE WINDOWS */
				if (Win32API.GetOpenFileName(ofn))
				{
					_ret = ofn.file;
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", true, ex.Message + ex.StackTrace);
			}
			return _ret;
		}

		/// <summary>Muestra el Cuadro de Dialogo Nativo de Windows para Guadar un Archivo.</summary>
		/// <param name="Filter">Tipos de Archivo a Mostrar, ej: 'Archivos de Texto|*.txt|Todos los archivos|*.*'</param>
		/// <param name="DefaultExt">Extension por defecto, ej: 'txt'</param>
		/// <param name="InitDir">Directorio Inicial</param>
		/// <param name="Title">Titulo de la Ventana</param>
		/// <returns>Devuelve la Ruta de Acceso Completa al archivo seleccionado.</returns>
		public static string SaveFileDialog(string Filter = "Todos los archivos|*.*", string DefaultExt = "", string FileName = "", string InitDir = "", string Title = "")
		{
			string _ret = string.Empty;
			try
			{
				Win32API.OpenFileName ofn = new Win32API.OpenFileName();

				ofn.structSize = Marshal.SizeOf(ofn);
				ofn.filter = Filter.Replace('|', '\0'); //<- "Archivos de Texto\0*.txt\0Todos los Archivos\0*.*\0"; 

				ofn.file = FileName.PadRight(256, ' '); // new String(new char[256]);
				ofn.maxFile = ofn.file.Length;

				ofn.fileTitle = new String(new char[64]);
				ofn.maxFileTitle = ofn.fileTitle.Length;

				ofn.flags = Convert.ToInt32(Win32API.OFN.OFN_OVERWRITEPROMPT | Win32API.OFN.OFN_PATHMUSTEXIST);

				ofn.initialDir = IIf(InitDir == string.Empty, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), InitDir);
				ofn.title = IIf(Title == string.Empty, "Guardar Archivo..", Title);
				ofn.defExt = DefaultExt;

				/* USA UNA FUNCION API DE WINDOWS */
				if (Win32API.GetSaveFileName(ofn))
				{
					_ret = ofn.file;
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", ex.Message + ex.StackTrace);
			}
			return _ret;
		}


		#endregion

		#region Extended Methods

		/// <summary>Borra la Linea anterior de la Consola</summary>
		public static void DeletePrevConsoleLine()
		{
			if (Console.CursorTop == 0)
			{
				return;
			}

			Console.SetCursorPosition(0, Console.CursorTop - 1);
			Console.Write(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, Console.CursorTop - 1);
		}

		/// <summary>Limpia toda la Consola desde la Posicion indicada hasta el final.</summary>
		/// <param name="targetLine">Posicion del Cursor desde donde se empieza a borrar. Zero Based</param>
		public static void ClearConsole(int targetLine = 0)
		{
			// Set the cursor position to the desired line
			Console.SetCursorPosition(0, targetLine);

			// Save the current cursor position
			int cursorLeft = Console.CursorLeft;
			int cursorTop = Console.CursorTop;

			// Clear everything below the current cursor position
			for (int i = cursorTop; i < Console.WindowHeight; i++)
			{
				Console.SetCursorPosition(0, i);
				Console.Write(new string(' ', Console.WindowWidth));
			}

			// Restore the cursor position
			Console.SetCursorPosition(cursorLeft, cursorTop);
		}
		public static void ClearConsole(int left, int top)
		{
			// Set the cursor to the specified position
			Console.SetCursorPosition(left, top);

			// Get the current cursor position
			int currentLeft = Console.CursorLeft;
			int currentTop = Console.CursorTop;

			// Set the cursor to the top left and clear everything
			Console.Clear();

			// Restore the cursor position
			Console.SetCursorPosition(currentLeft, currentTop);
		}




		/// <summary>Pide Confirmacion del Usuario.
		/// Devuelve 'true' cuando el usuario presiona 'S', 'false' cuando presiona 'N' (CASE INSENSITIVE).
		/// Cualquier otra tecla ocasiona que se repita la pregunta.</summary>
		/// <param name="pTitulo">Texto de confirmacion (el '[S/N]' no es requerido).</param>
		public static bool Confirmar(string pTitulo = "Confirmar?")
		{
			bool _ret = false;
			bool Continuar = true;
			do
			{
				Write("{0} ", pTitulo); Write("[Y/N]: ", ConsoleColor.DarkYellow);

				ConsoleKey UserInput = ReadKey(true).Key;  //<- Espera la Entrada del Usuario, NO MOSTRARLA TODAVIA

				switch (UserInput)
				{
					case ConsoleKey.Escape: _ret = false; Continuar = false; break;
					case ConsoleKey.N: _ret = false; Continuar = false; break;
					case ConsoleKey.Y: _ret = true; Continuar = false; break;
					default: Continuar = true; break;
				}

				WriteLine(UserInput.ToString().ToUpper(), ConsoleColor.Yellow);
				if (Continuar)
				{
					DeletePrevConsoleLine();
				}
			} while (Continuar);
			return _ret;
		}

		/// <summary>Pide al Usuario que ingrese una fecha válida.
		/// <para>Reintenta hasta que se ingrese una Fecha válida.</para></summary>
		/// <param name="pTitulo">[Opcional] Etiqueta o titulo</param>
		/// <param name="MaxYears">[Opcional] Años hacia el Futuro, apartir de Hoy, en que se considera la Fecha como válida.</param>
		/// <param name="MinYears">[Opcional] Años hacia el Pasado, apartir de Hoy, en que se considera la Fecha como válida.</param>
		public static DateTime PedirFecha(string pTitulo = "Digite una Fecha", int MaxYears = 1, int MinYears = 100)
		{
			DateTime _ret = DateTime.MinValue;
			bool Continuar = true;
			do
			{
				Write(pTitulo); Write(" [dd/mm/aaaa]: ", ConsoleColor.Yellow);
				string Input = ReadLine(ConsoleColor.Yellow);

				if (Input != string.Empty)
				{
					try
					{
						LogString.Append(Input + "\r\n");
						_ret = DateTime.Parse(Input);

						//La Fecha debe estar dentro del Rango de Años especificado:
						if (_ret < DateTime.Today.AddYears(MaxYears))
						{
							if (_ret >= DateTime.Today.AddYears(-MinYears))
							{
								Continuar = false;
							}
							else { DeletePrevConsoleLine(); } //<- ERR: Fecha Inferior al Minimo Permitido
						}
						else { DeletePrevConsoleLine(); } //<- ERR: Fecha Posterior al Maximo permitido. 
					}
					catch { DeletePrevConsoleLine(); } //<- ERR: Formato de Fecha Incorrecto
				}
			} while (Continuar);
			return _ret;
		}

		/// <summary>Pide al Usuario que ingrese un Importe (DECIMAL) válido.
		/// <para>No usar Separador de miles, el Separador decimal puede ser '.' o ','</para></summary>
		/// <param name="MinValue">[Opcional] Valor Minimo aceptable.</param>
		/// <param name="MaxValue">[Opcional] Valor Máximo aceptable. Si 'MaxValue' = 0, entonces NO hay limites (positivos o negativos) para el numero ingresado</param>
		/// <param name="pTitulo">[Opcional] Etiqueta o titulo</param>
		public static decimal PedirImporte(int MinValue = 0, int MaxValue = 0, string pTitulo = "Digite un Importe")
		{
			decimal _ret = 0;
			bool Continuar = true;
			do
			{
				Write("{0}", Console.ForegroundColor, pTitulo);
				if (MaxValue > 0) { Write(" [{0}-{1}]: ", ConsoleColor.Gray, MinValue, MaxValue); }
				else { Write(": ", ConsoleColor.Gray); }

				string UserInput = ReadLine(ConsoleColor.Yellow);

				if (UserInput != string.Empty)
				{
					try
					{
						UserInput = UserInput.Replace('.', ',');
						LogString.Append(UserInput + "\r\n");

						_ret = decimal.Parse(UserInput);

						if (MaxValue > 0) //<- Si 'MaxValue' = 0, entonces NO hay limites (positivos o negativos) para el numero ingresado
						{
							if (_ret >= MinValue && _ret <= MaxValue)
							{
								Continuar = false;
							}
							else { DeletePrevConsoleLine(); } //<- ERR: Importe Fuera del Rango Permitido
						}
						else { Continuar = false; } //<- Sin limites de Rango
					}
					catch { DeletePrevConsoleLine(); } //<- ERR: Importe con Formato incorrecto
				}
			} while (Continuar);
			return _ret;
		}

		/// <summary>Pide al Usuario que ingrese un Numero (ENTERO) válido.</summary>
		/// <param name="MinValue">[Opcional] Valor Minimo aceptable.</param>
		/// <param name="MaxValue">[Opcional] Valor Máximo aceptable. Si 'MaxValue' = 0, entonces NO hay limites (positivos o negativos) para el numero ingresado</param>
		/// <param name="pTitulo">[Opcional] Etiqueta o titulo</param>
		public static int PedirNumero(int MinValue = 0, int MaxValue = 0, string pTitulo = "Digite un Número")
		{
			int _ret = 0;
			bool Continuar = true;
			do
			{
				Write("{0}", Console.ForegroundColor, pTitulo);
				if (MaxValue > 0) { Write(" [{0}-{1}]: ", ConsoleColor.Gray, MinValue, MaxValue); }
				else { Write(": ", ConsoleColor.Gray); }

				string UserInput = ReadLine(ConsoleColor.Yellow);

				if (UserInput != string.Empty)
				{
					try
					{
						LogString.Append(UserInput + "\r\n");
						_ret = int.Parse(UserInput);
						if (MaxValue > 0) //<- Si 'MaxValue' = 0, entonces NO hay limites (positivos o negativos) para el numero ingresado
						{
							if (_ret >= MinValue && _ret <= MaxValue)
							{
								Continuar = false;
							}
							else { DeletePrevConsoleLine(); } //<- ERR: Importe Fuera del Rango Permitido
						}
						else { Continuar = false; } //<- Sin limites de Rango
					}
					catch { DeletePrevConsoleLine(); } //<- ERR: Valor con Formato incorrecto
				}
			} while (Continuar);
			return _ret;
		}


		/// <summary>Muestra un Menu en pantalla para elejir hasta 20 opciones.</summary>
		/// <param name="elementos">Titulos de las Opciones a elejir (max. 20)</param>
		public static string Menu(params string[] elementos)
		{
			string _ret = string.Empty;
			try
			{
				if (elementos != null && elementos.Length > 0)
				{
					int Cantidad = IIf(elementos.Length > 20, 20, elementos.Length); ; //<- Numero de elementos del menu	
					int Dobles = elementos.Length - 10; //<- Cantidad de Lineas con doble Columna
					int Letra = 65; // Codigo ASCII de la primera Letra asignada a un elemento del menu

					#region *  AQUI SE DIBUJA EL MENU DE OPCIONES  *

					//WriteLine("Menu".PadLeft(27, '-').PadRight(58, '-')); //<- Linea de Encabezado 
					WriteLine("┌{0}┐", LogEntry: false, args: "Menu".PadLeft(29, '─').PadRight(62, '─'));  //<- Linea de Encabezado 
					for (int Linea = 0; Linea < 10; Linea++) //<- 2 Columnas de 10 lineas cada una
					{
						if (Linea < Cantidad)
						{
							if (Linea < Dobles)
							{
								// Lineas Dobles:	
								Write("│ {0}", ConsoleColor.Green, false, ((char)(Letra + Linea)).ToString());
								Write(". {0}", false, FixString(elementos[Linea]));

								Write("{0}", ConsoleColor.Green, false, ((char)(Letra + Linea + 10)).ToString());
								WriteLine(". {0}", FixString(elementos[Linea + 10]));
							}
							else
							{
								// Lineas Simples:
								Write(" {0}", ConsoleColor.Green, false, ((char)(Letra + Linea)).ToString());
								WriteLine(". {0}", false, FixString(elementos[Linea]));
							}
						}
					}
					WriteLine("└{0}┘", false, "".PadLeft(62, '─')); //<- Linea Final del Menu

					#endregion

					#region Aqui se Reacciona al Menu elejido

					bool Continuar = true;
					int Intentos = 0;
					do
					{
						try
						{
							Write("Seleccione una Opcion del Menú: ");
							ConsoleKeyInfo UserInput = ReadKey(true);           //<- Espera la Entrada del Usuario, no la muestra todavia
							int CharCode = Convert.ToInt32(UserInput.KeyChar);  //<- Codigo ASCII de la tecla presionada

							_ret = UserInput.KeyChar.ToString().ToUpper();      //<- Letra Elejida en MAyusculas.
							Intentos++; //<- Numero de Intentos

							int MaxValue = 0; //<- Codigo ASCII de la Letra del ultimo elemento del Menu							

							//CharCodes: 65-84  -> Letras A-T (Mayusculas)
							if (CharCode >= 65 && CharCode <= 84)
							{
								MaxValue = 65 + Cantidad - 1;
							}

							//CharCodes: 97-116 -> Letras a-t (Minusculas)
							if (CharCode >= 97 && CharCode <= 116)
							{
								MaxValue = 97 + Cantidad - 1;
							}

							// Salir del Menu si se elijio una opcion valida
							if (CharCode <= MaxValue)
							{
								Continuar = false;
							}

							//Muestra la Entrada del Usuario indicando en Color si en Correcta (Verde) o no.
							WriteLine(_ret, IIf(Continuar, ConsoleColor.Red, ConsoleColor.Green));
							if (Continuar)
							{
								DeletePrevConsoleLine();
							}
						}
						catch { DeletePrevConsoleLine(); }
					} while (Continuar);

					#endregion
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}{1}", ex.Message, ex.StackTrace);
			}
			return _ret;
		}

		/// <summary>Draws an Horizontal Line across the screen</summary>
		public static void DrawLine()
		{
			WriteLine("{0}", LogEntry: false, args: string.Empty.PadRight(64, '─'));  //<- Linea de Encabezado 
		}

		/// <summary>Dibuja una Barra de Progreso el la posicion actual del Cursor.
		///<para>Invoque nuevamente este metodo para actualizar el Progrreso.</para></summary>
		/// <param name="progress">The position of the bar</param>
		/// <param name="total">The amount it counts</param>
		/// <param name="Mensaje">Texto mostrado antes del Porcentaje completado.</param>
		/// <param name="Filler">Tipo de Barra: 0[OldStyle], 1[Pequeña], 2[Grande], 3[Segmentada]</param>
		/// <param name="Color">Color del Progreso completado</param>
		public static void ProgressBar(int progress, int total, string Mensaje = "", int Filler = 1, ConsoleColor Color = ConsoleColor.DarkYellow)
		{
			Console.CursorLeft = 1;
			ConsoleColor OriginalTextColor = Console.ForegroundColor;
			float onechunk = 30.0f / total;

			List<char[]> Fillers = new List<char[]>
			{
				new char[] { '▓', '░' }, //<- Filler[0], ASCII 178, 176				
				new char[] { '■', '■' }, //<- Filler[1], ASCII 254
				new char[] { '█', '█' }, //<- Filler[2], ASCII 219
				new char[] { '▌', '▌' }  //<- Filler[3], ASCII 179
			};

			//draw filled part
			int position = 1;
			for (int i = 0; i < onechunk * progress; i++)
			{
				Console.ForegroundColor = Color;
				Console.CursorLeft = position++;
				Console.Write(Fillers[Filler]);
			}

			//draw unfilled part
			for (int i = position; i <= 31; i++)
			{
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.CursorLeft = position++;
				Console.Write(Fillers[Filler]);
			}

			//draw totals
			Console.CursorLeft = 35;
			Console.ForegroundColor = OriginalTextColor;

			decimal Porcentage = progress * 100 / total;
			Console.Write(Mensaje + " " + Porcentage.ToString("n0") + "%" + " "); //blanks at the end remove any excess
		}


		/// <summary>Dibuja una Imagen en la Consola</summary>
		/// <param name="ImagePath">Ruta de acceso a la Imagen</param>
		public static void DrawPicture(string ImagePath)
		{
			/* NOTA: Si la pantalla se mueve o se quiere dibujar fuera del area visible, la imagen desaparece. */
			//https://stackoverflow.com/questions/33538527/display-a-image-in-a-console-application 
			try
			{
				//Obtener el Handle de la Ventana:
				using (Graphics g = Graphics.FromHwnd(Win32API.GetConsoleWindow()))
				{
					//Cargar la Imagen desde el Archivo:
					using (Image image = Image.FromFile(ImagePath))
					{
						Size WindowSize = new Size(Console.WindowWidth, Console.WindowHeight); //<- Tamaño de la Ventana (en Columnas)
						Size FontSize = Consola.GetConsoleFontSize();           //<- Obtiene el Tamaño (en Pixeles) de 1 caracter									
						Size ImageSize = new Size((image.Width / FontSize.Width), (image.Height / FontSize.Height)); //<- Tamaño de la Imagen en Columnas (Caracteres)	
						Point Location = new Point(1, Console.CursorTop + 1);   //<- Ubicacion del Cursor donde se dibujará la Imagen

						if (Console.CursorTop + ImageSize.Height > Console.WindowHeight)
						{
							Console.SetCursorPosition(1, Console.CursorTop + ImageSize.Height + 2);
						}

						//Crea un area de Dibijo en la posicion del Cursor y del tamaño de la Imagen:
						Rectangle imageRect = new Rectangle(
							Location.X * FontSize.Width,
							Location.Y * FontSize.Height,
							ImageSize.Width * FontSize.Width,
							ImageSize.Height * FontSize.Height);

						g.DrawImage(image, imageRect); //<- DIBUJA LA IMAGEN

						Console.SetCursorPosition(0, Location.Y + ImageSize.Height + 1); //<- Ubica el Cursor al Final de donde se dibujó la Imagen
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", ex.Message + ex.StackTrace);
			}
		}

		/// <summary>Muestra una Imagen en una Ventana hija de la Consola
		/// <para>Presione ESC o Doble Click para Salir</para></summary>
		/// <param name="ImagePath">Ruta de acceso a la Imagen</param>
		/// <param name="Resize">[Opcional] Permite redimensionar la imagen</param>
		/// <param name="Location">[Opcional] Permite mover la imagen dentro de la ventana de la Consola.</param>
		public static void ShowImage(string ImagePath, Size? Resize = null, Point? Location = null)
		{
			try
			{
				if (!string.IsNullOrEmpty(ImagePath) && System.IO.File.Exists(ImagePath))
				{
					Image thePicture = Image.FromFile(ImagePath);
					if (thePicture != null)
					{
						//Defaults to the Size of the Image:
						Resize = Resize ?? new Size(thePicture.Width, thePicture.Height);
						Location = Location ?? Point.Empty;

						Task.Factory.StartNew(() =>
						{
							var form = new Form
							{
								BackgroundImage = thePicture,
								BackgroundImageLayout = ImageLayout.Stretch,
								FormBorderStyle = FormBorderStyle.None,
								Width = Resize.Value.Width,
								Height = Resize.Value.Height,
							};
							form.Shown += (object sender, EventArgs e) =>
							{
								ToolTip toolTip = new ToolTip();
								toolTip.SetToolTip(form, "ESC or DobleClick to Exit");
								form.Refresh();

								//Timer timer = new Timer();
								//timer.Interval = 2500; // 3 seconds 
								//timer.Tick += (s, args) => form.Refresh();
								//timer.Start();
							};
							form.KeyPress += (object sender, KeyPressEventArgs e) =>
							{
								if (e.KeyChar == (char)Keys.Escape)
								{
									form.Close(); //<- ESC to exit
								}
							};
							form.DoubleClick += (object sender, EventArgs e) =>
							{
								form.Close(); //<- Double Click to exit
							};
							form.MouseMove += (object sender, MouseEventArgs e) =>
							{
								// Allowes to move the window by dragging the image
								if (e.Button == MouseButtons.Left)
								{
									Win32API.ReleaseCapture();
									Win32API.SendMessage(form.Handle, Win32API.WM_NCLBUTTONDOWN, Win32API.HT_CAPTION, 0);
									form.Refresh();
								}
							};
							form.Paint += (object sender, PaintEventArgs e) =>
							{
								// Draws a Golden Border around the Image:
								Graphics g = e.Graphics;
								Rectangle rect = new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height);
								using (Pen pen = new Pen(Color.Gold, 2))
								{
									g.DrawRectangle(pen, rect);
								}
							};

							Win32API.SetParent(form.Handle, Win32API.GetConsoleHandle());							

							if (Location != Point.Empty)
							{
								Win32API.MoveWindow(form.Handle, 
									Location.Value.X, Location.Value.Y, 
									Resize.Value.Width, Resize.Value.Height, 
									true
								);
							}
							Application.Run(form);
						});
					}
				}
			}
			catch (Exception ex)
			{
				Consola.WriteLine(ex.Message, ConsoleColor.Red);
			}
		}


		/// <summary>Digitaliza o Convierte una Imagen en Caracteres ASCII</summary>
		/// <param name="ImagePath">Ruta de acceso a la Imagen</param>
		public static void WriteASCIIImage(string ImagePath)
		{
			int sMax = 39;

			using (Bitmap source = new Bitmap(System.IO.Path.Combine(ImagePath)))
			{
				decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
				Size dSize = new Size((int)(source.Width * percent), (int)(source.Height * percent));
				Bitmap bmpMax = new Bitmap(source, dSize.Width * 2, dSize.Height);

				for (int i = 0; i < dSize.Height; i++)
				{
					for (int j = 0; j < dSize.Width; j++)
					{
						ConsoleWritePixel(bmpMax.GetPixel(j * 2, i));
						ConsoleWritePixel(bmpMax.GetPixel(j * 2 + 1, i));
					}
					System.Console.WriteLine();
				}
				Console.ResetColor();
			}
		}


		/// <summary>Shows the Data in a Table</summary>
		/// <param name="pDatos">The DataSource</param>
		public static void ShowData(System.Data.DataTable pDatos)
		{
			try
			{
				if (pDatos != null)
				{
					//1. Obtener los nombres de los Campos
					List<Columna> Fields = new List<Columna>();

					foreach (DataColumn column in pDatos.Columns)
					{
						// Gets the Title and Type of each Column 
						var _Col = new Columna()
						{
							Title = !string.IsNullOrEmpty(column.Caption) ? column.Caption : column.ColumnName,
							DataType = column.DataType
						};
						_Col.Width = _Col.Title.Length; // the Minimum Width is the lengh of the Column Title

						// Get the Text Align of each column based on it's data type:
						switch (_Col.DataType.ToString())
						{
							case "System.Int32": _Col.TextAlign = "Right"; break;
							case "System.Int64": _Col.TextAlign = "Right"; break;
							case "System.Decimal": _Col.TextAlign = "Right"; break;
							case "System.Double": _Col.TextAlign = "Right"; break;
							case "System.DateTime": _Col.TextAlign = "Left"; break;
							case "System.String": _Col.TextAlign = "Left"; break;
							default: _Col.TextAlign = "Center"; break;
						}
						Fields.Add(_Col);
					}

					if (Fields.Count > 0)
					{
						// Get the Max size of each column based on the data it contains
						foreach (DataRow DR in pDatos.Rows)
						{
							int colIndex = 0;
							foreach (var item in DR.ItemArray)
							{
								if (item.ToString().Length > Fields[colIndex].Width )
								{
									Fields[colIndex].Width = item.ToString().Length;
								}
								colIndex++;
							}
						}

						StringBuilder OutputLines = new StringBuilder();
						string line = string.Empty;
						int RowMaxLen = 0;

						// Build the Headers showing the Field Names:
						foreach (var field in Fields)
						{
							switch (field.TextAlign)
							{
								// Aligning the Texts:
								case "Left":	line += AlignLeftString(field.Title, field.Width) + ' '; break;  //<- Adds a Padding (1 char)
								case "Right":	line += AlignRightString(field.Title, field.Width) + ' '; break;
								case "Center":	line += CenterString(field.Title, field.Width) + ' '; break;
								default: line += CenterString(field.Title, field.Width) + ' '; break;
							}
						}
						RowMaxLen = line.Length;
						OutputLines.AppendLine(new string('─', RowMaxLen));
						OutputLines.AppendLine(line);

						// a line to split Header and Data:
						OutputLines.AppendLine(new string('─', RowMaxLen));

						Debug.WriteLine(OutputLines.ToString());						

						// Build the Data Rows:
						foreach (DataRow DR in pDatos.Rows)
						{
							int colIndex = 0;
							string linea = string.Empty;
							foreach (var Column in DR.ItemArray)
							{
								switch (Fields[colIndex].TextAlign)
								{
									case "Left":	linea += AlignLeftString(Column.ToString(), Fields[colIndex].Width) + ' '; break;  //<- Adds a Padding (1 char)
									case "Right":	linea += AlignRightString(Column.ToString(), Fields[colIndex].Width) + ' '; break;
									case "Center":	linea += CenterString(Column.ToString(), Fields[colIndex].Width) + ' '; break;
									default: break;
								}
								colIndex++;
							}
							OutputLines.AppendLine(linea);
						}
						OutputLines.AppendLine(new string('─', RowMaxLen));

						// Finnaly we show the Output:
						if (OutputLines.Length > 0)
						{
							string[] Lines = OutputLines.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
							foreach (string linea in Lines)
							{
								Consola.WriteLine(linea);
							}
						}						
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", ex.Message + ex.StackTrace);
			}
		}

		/// <summary>Shows the Data in a Table</summary>
		/// <param name="pDatos">The DataSource</param>
		public static void ShowData(dynamic pDatos)
		{
			try
			{
				if (pDatos != null && pDatos is System.Collections.IEnumerable) //<- The object is a list or collection.
				{
					List<Columna> Fields = new List<Columna>(); //<- To Store the Columns information
					int rowCount = pDatos.Count;
					int colCount = 0;
					string[,] Data;

					#region First we get the Column Headers from the first row of data:

					var FirstRow = pDatos[0];
					foreach (var property in FirstRow)
					{
						// Gets the Title and Type of each Column 
						var _Col = new Columna()
						{
							Title = property.Name,
							Width = property.Name.Length,   // the Minimum Width is the lengh of the Column Title
							DataType = GetJTokenType(property.Value)
						};
						// Get the Text Align of each column based on it's data type:
						switch (_Col.DataType.ToString())
						{
							case "System.Int32": _Col.TextAlign = "Right"; break;
							case "System.Int64": _Col.TextAlign = "Right"; break;
							case "System.Decimal": _Col.TextAlign = "Right"; break;
							case "System.Double": _Col.TextAlign = "Right"; break;
							case "System.DateTime": _Col.TextAlign = "Left"; break;
							case "System.String": _Col.TextAlign = "Left"; break;
							default: _Col.TextAlign = "Center"; break;
						}
						Fields.Add(_Col);
						colCount++;
					}

					#endregion

					#region Now we process the Rows of Data:

					Data = new string[rowCount, colCount];

					int rowIndex = 0;
					foreach (var record in pDatos)
					{
						int colIndex = 0;
						foreach (var property in record)
						{
							var propertyValue = property.Value;

							// Get the Max size of each column based on the data it contains
							if (propertyValue.ToString().Length > Fields[colIndex].Width)
							{
								Fields[colIndex].Width = propertyValue.ToString().Length;
							}
							Data[rowIndex, colIndex] = propertyValue.ToString();
							colIndex++;
						}
						rowIndex++;
					}

					#endregion

					#region Generating the Output

					StringBuilder OutputLines = new StringBuilder();
					string line = string.Empty;
					int RowMaxLen = 0;
					// Build the Headers showing the Field Names:
					foreach (var field in Fields)
					{
						switch (field.TextAlign)
						{
							// Aligning the Texts:
							case "Left": line += AlignLeftString(field.Title, field.Width) + ' '; break;  //<- Adds a Padding (1 char)
							case "Right": line += AlignRightString(field.Title, field.Width) + ' '; break;
							case "Center": line += CenterString(field.Title, field.Width) + ' '; break;
							default: line += CenterString(field.Title, field.Width) + ' '; break;
						}
					}
					RowMaxLen = line.Length;
					OutputLines.AppendLine(new string('─', RowMaxLen));
					OutputLines.AppendLine(line);

					// a line to split Header and Data:
					OutputLines.AppendLine(new string('─', RowMaxLen));

					// Build the Data Rows:
					for (int row = 0; row < rowCount; row++)
					{
						string linea = string.Empty;
						for (int col = 0; col < colCount; col++)
						{
							switch (Fields[col].TextAlign)
							{
								case "Left": linea += AlignLeftString(Data[row, col], Fields[col].Width) + ' '; break;  //<- Adds a Padding (1 char)
								case "Right": linea += AlignRightString(Data[row, col], Fields[col].Width) + ' '; break;
								case "Center": linea += CenterString(Data[row, col], Fields[col].Width) + ' '; break;
								default: break;
							}
						}
						OutputLines.AppendLine(linea);
					}

					OutputLines.AppendLine(new string('─', RowMaxLen));

					#endregion

					// Finnaly we show the Output:
					if (OutputLines.Length > 0)
					{
						string[] Lines = OutputLines.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
						foreach (string linea in Lines)
						{
							Consola.WriteLine(linea);
						}
					}
				}
				else
				{
					WriteLine("ERROR:\r\nThe Data is Empty or is not IEnumerable.");
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", ex.Message + ex.StackTrace);
			}
		}

		/// <summary>Shows the Data in a Table</summary>
		/// <typeparam name="T">The Type of the Data</typeparam>
		/// <param name="pDatos">The DataSource</param>
		public static void ShowData<T>(List<T> pDatos)
		{
			try
			{
				if (pDatos != null)
				{
					List<Columna> Fields = new List<Columna>();
					int rowCount = pDatos.Count;
					int colCount = 0;
					string[,] Data;

					#region First we get the Column Headers from the first row of data:

					PropertyInfo[] properties = typeof(T).GetProperties();
					foreach (var property in properties)
					{						
						// Gets the Title and Type of each Column 
						var _Col = new Columna()
						{
							Title = property.Name,
							Width = property.Name.Length,   // the Minimum Width is the lengh of the Column Title
							DataType = property.PropertyType
						};

						// Get the Text Align of each column based on it's data type:
						switch (_Col.DataType.ToString())
						{
							case "System.Int32":	_Col.TextAlign = "Right"; break;
							case "System.Int64":	_Col.TextAlign = "Right"; break;
							case "System.Decimal":	_Col.TextAlign = "Right"; break;
							case "System.Double":	_Col.TextAlign = "Right"; break;
							case "System.DateTime": _Col.TextAlign = "Left"; break;
							case "System.String":	_Col.TextAlign = "Left"; break;
							default: _Col.TextAlign = "Center"; break;
						}
						Fields.Add(_Col);
						colCount++;
					} //<- EndFoEach

					#endregion

					#region Now we process the Rows of Data:

					Data = new string[pDatos.Count, Fields.Count];
					if (Fields.Count > 0)
					{						
						int rowIndex = 0;						

						foreach (var record in pDatos)
						{
							int colIndex = 0;
							properties = record.GetType().GetProperties();
							foreach (var property in properties)
							{
								//Gets the Raw Data:
								string Value = property.GetValue(record, null).ToString();

								// Get the Max size of each column based on the data it contains
								if (Value.Length > Fields[colIndex].Width)
								{
									Fields[colIndex].Width = Value.Length;
								}
								
								Data[rowIndex, colIndex] = Value;
								colIndex++;
							}
							rowIndex++;
						}
					}

					#endregion

					#region Generating the Output

					StringBuilder Output = new StringBuilder();
					string line = string.Empty;
					int RowMaxLen = 0;

					// Build the Headers showing the Field Names:
					foreach (var field in Fields)
					{
						switch (field.TextAlign)
						{
							// Aligning the Texts:
							case "Left": line += AlignLeftString(field.Title, field.Width) + ' '; break;  //<- Adds a Padding (1 char)
							case "Right": line += AlignRightString(field.Title, field.Width) + ' '; break;
							case "Center": line += CenterString(field.Title, field.Width) + ' '; break;
							default: line += CenterString(field.Title, field.Width) + ' '; break;
						}
					}
					RowMaxLen = line.Length;
					Output.AppendLine(new string('─', RowMaxLen));
					Output.AppendLine(line);

					// a line to split Header and Data:
					Output.AppendLine(new string('─', RowMaxLen));

					// Build the Data Rows:
					for (int row = 0; row < rowCount; row++)
					{
						string linea = string.Empty;
						for (int col = 0; col < colCount; col++)
						{
							switch (Fields[col].TextAlign)
							{
								case "Left": linea += AlignLeftString(Data[row, col], Fields[col].Width) + ' '; break;  //<- Adds a Padding (1 char)
								case "Right": linea += AlignRightString(Data[row, col], Fields[col].Width) + ' '; break;
								case "Center": linea += CenterString(Data[row, col], Fields[col].Width) + ' '; break;
								default: break;
							}
						}
						Output.AppendLine(linea);
					}

					Output.AppendLine(new string('─', RowMaxLen));

					#endregion

					// Finnaly we show the Output:
					if (Output.Length > 0)
					{
						string[] Lines = Output.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
						foreach (string linea in Lines)
						{
							Consola.WriteLine(linea);
						}
					}
				}
				else
				{
					WriteLine("ERROR:\r\nThe Data is Empty or is not IEnumerable.");
				}
			}
			catch (Exception ex)
			{
				WriteLine("ERROR:\r\n{0}\r\n{1}", ex.Message + ex.StackTrace);
			}
		}


		//este no funca bien
		public static void WriteImage_TEST(Bitmap bmpSrc)
		{
			int sMax = 39;
			decimal percent = Math.Min(decimal.Divide(sMax, bmpSrc.Width), decimal.Divide(sMax, bmpSrc.Height));
			Size resSize = new Size((int)(bmpSrc.Width * percent), (int)(bmpSrc.Height * percent));
			Func<System.Drawing.Color, int> ToConsoleColor = c =>
			{
				int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0;
				index |= (c.R > 64) ? 4 : 0;
				index |= (c.G > 64) ? 2 : 0;
				index |= (c.B > 64) ? 1 : 0;
				return index;
			};
			Bitmap bmpMin = new Bitmap(bmpSrc, resSize.Width, resSize.Height);
			Bitmap bmpMax = new Bitmap(bmpSrc, resSize.Width * 2, resSize.Height * 2);
			for (int i = 0; i < resSize.Height; i++)
			{
				for (int j = 0; j < resSize.Width; j++)
				{
					Console.ForegroundColor = (ConsoleColor)ToConsoleColor(bmpMin.GetPixel(j, i));
					Console.Write("██");
				}

				Console.BackgroundColor = ConsoleColor.Black;
				Console.Write("    ");

				for (int j = 0; j < resSize.Width; j++)
				{
					Console.ForegroundColor = (ConsoleColor)ToConsoleColor(bmpMax.GetPixel(j * 2, i * 2));
					Console.BackgroundColor = (ConsoleColor)ToConsoleColor(bmpMax.GetPixel(j * 2, i * 2 + 1));
					Console.Write("▀");

					Console.ForegroundColor = (ConsoleColor)ToConsoleColor(bmpMax.GetPixel(j * 2 + 1, i * 2));
					Console.BackgroundColor = (ConsoleColor)ToConsoleColor(bmpMax.GetPixel(j * 2 + 1, i * 2 + 1));
					Console.Write("▀");
				}
				System.Console.WriteLine();
			}
		}

		/// <summary>Obtiene el Tamaño de la Fuente utilizada por la Ventana de la Consola.</summary>
		public static Size GetConsoleFontSize()
		{
			// getting the console out buffer handle
			IntPtr outHandle = Win32API.CreateFile("CONOUT$", Win32API.GENERIC_READ | Win32API.GENERIC_WRITE,
																Win32API.FILE_SHARE_READ | Win32API.FILE_SHARE_WRITE,
				IntPtr.Zero,
				Win32API.OPEN_EXISTING,
				0,
				IntPtr.Zero);
			int errorCode = Marshal.GetLastWin32Error();
			if (outHandle.ToInt32() == Win32API.INVALID_HANDLE_VALUE)
			{
				throw new System.IO.IOException("Unable to open CONOUT$", errorCode);
			}

			Win32API.ConsoleFontInfo cfi = new Win32API.ConsoleFontInfo();
			if (!Win32API.GetCurrentConsoleFont(outHandle, false, cfi))
			{
				throw new InvalidOperationException("Unable to get font information.");
			}

			return new Size(cfi.dwFontSize.X, cfi.dwFontSize.Y);
		}

		/// <summary>Ejecuta un Programa o un Comando y devuelve sus resultados.
		/// Para ejecutar un Comando DOS, se debe usar 'filename'= 'cmd.exe' y 'arguments'= '/C [Comando] [Argumentos]', ejem: "/C netstat -a"
		/// </summary>
		/// <param name="filename">Ruta del Archivo Ejecutable</param>
		/// <param name="arguments">Argumentos a pasar al Programa</param>
		public static string RunExternalExe(string filename, string arguments = null)
		{
			var process = new System.Diagnostics.Process();

			process.StartInfo.FileName = filename;
			if (!string.IsNullOrEmpty(arguments))
			{
				process.StartInfo.Arguments = arguments;
			}

			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			var stdOutput = new System.Text.StringBuilder();
			process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data); // Use AppendLine rather than Append since args.Data is one line of output, not including the newline character.

			string stdError = null;
			try
			{
				process.Start();
				process.BeginOutputReadLine();
				stdError = process.StandardError.ReadToEnd();
				process.WaitForExit();
			}
			catch (Exception e)
			{
				throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
			}

			if (process.ExitCode == 0)
			{
				return stdOutput.ToString();
			}
			else
			{
				var message = new System.Text.StringBuilder();

				if (!string.IsNullOrEmpty(stdError))
				{
					message.AppendLine(stdError);
				}

				if (stdOutput.Length != 0)
				{
					message.AppendLine("Std output:");
					message.AppendLine(stdOutput.ToString());
				}

				throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
			}
		}

		/// <summary>Pause the execution of the program by the indicated time.</summary>
		/// <param name="Miliseconds">1s = 1.000ms</param>
		public static void Wait(int Miliseconds = 1000)
		{
			System.Threading.Thread.Sleep(Miliseconds);
		}

		#endregion

		#region Wait Animation

		/// <summary>
		/// Create spinning console busy animation running on a background thread
		/// credits: Colin, https://www.c-sharpcorner.com/uploadfile/cbragg/console-application-waitbusy-spin-animation/
		/// </summary>
		public static class SpinAnimation
		{
			#region Private Declarations

			// Spinner background thread
			private static System.ComponentModel.BackgroundWorker spinner = InitialiseBackgroundWorkerEx();

			// Starting position of spinner changes to current position on start
			private static int spinnerPosition = 50;

			// Pause time in milliseconds between each character in the spin animation
			private static int spinWait = 80;

			// Field and property to inform client if spinner is currently running
			private static bool isRunning;

			private static string Mensaje = "Espere..";

			#endregion

			#region Public Properties

			public static string[] Animation { get; set; } = Dots2;

			public static bool IsRunning => isRunning;

			#endregion

			#region Private Methods

			/// <summary>Este Metodo es el que Dibuja el Spinner</summary>
			/// <returns>Background worker thread</returns>
			private static System.ComponentModel.BackgroundWorker InitialiseBackgroundWorkerEx()
			{
				var obj = new System.ComponentModel.BackgroundWorker();

				// Allow cancellation to be able to stop the spinner
				obj.WorkerSupportsCancellation = true;

				// Anonymous method for background thread's DoWork event
				obj.DoWork += delegate
				{
					// Set the spinner position to the current console position
					spinnerPosition = Console.CursorLeft;
					var oldTitle = Console.Title;
					if (Animation == null)
					{
						Animation = Dots;
					}

					// Run animation unless a cancellation is pending
					while (!obj.CancellationPending)
					{
						// Characters to iterate through during animation
						string[] spinChars = Animation;

						// Iterate through the animation character array
						foreach (string spinChar in spinChars)
						{
							// Reset the cursor position to the spinner position
							Console.CursorLeft = spinnerPosition;

							// Write the current character to the console
							Console.Write("  " + spinChar + "   " + Mensaje);
							Console.Title = oldTitle + "  " + spinChar;

							// Pause for smooth animation - set by the Start method
							System.Threading.Thread.Sleep(spinWait);
						}
						Console.Title = oldTitle;
					}
				};

				return obj;
			}

			#endregion

			#region Public Methods

			/// <summary>
			/// Start the animation
			/// </summary>
			/// <param name="spinWait">Wait time between spin steps in milliseconds</param>
			public static void Start(int spinWait)
			{
				// Set the running flag
				isRunning = true;

				// Process spinWait value
				SpinAnimation.spinWait = spinWait;

				// Start the animation unless already started
				if (!spinner.IsBusy)
				{
					spinner.RunWorkerAsync();
				}
				else
				{
					throw new InvalidOperationException("Cannot start spinner whilst spinner is already running");
				}
			}

			/// <summary>
			/// Overloaded Start method with default wait value
			/// </summary>
			public static void Start(string Message = "Espere..")
			{
				Mensaje = Message;
				Start(25);
			}

			/// <summary>
			/// Stop the spin animation
			/// </summary>
			public static void Stop()
			{
				// Stop the animation
				spinner.CancelAsync();

				// Wait for cancellation to complete
				while (spinner.IsBusy)
				{
					System.Threading.Thread.Sleep(100);
				}

				// Reset the cursor position
				Console.CursorLeft = spinnerPosition;

				// Set the running flag
				isRunning = false;
			}

			#endregion

			#region Spinners

			public static string[] Dots =
			{
			"⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"
		};
			public static string[] Dots2 =
			{
			"⣾","⣽","⣻","⢿","⡿","⣟","⣯","⣷"
		};
			public static string[] Dots3 =
			{
			"⠋","⠙","⠚","⠞","⠖","⠦","⠴","⠲","⠳","⠓"
		};
			public static string[] Dots4 =
			{
			"⠄","⠆","⠇","⠋","⠙","⠸","⠰","⠠","⠰","⠸","⠙","⠋","⠇","⠆"
		};
			public static string[] Dots5 =
			{
			"⠋","⠙","⠚","⠒","⠂","⠂","⠒","⠲","⠴","⠦",    "⠖","⠒","⠐","⠐","⠒","⠓","⠋"
		};
			public static string[] Dots8Bit =
			{
			"⠀","⠁","⠂","⠃","⠄","⠅","⠆","⠇",        "⡀","⡁","⡂","⡃","⡄","⡅","⡆","⡇",
			"⠈","⠉","⠊","⠋","⠌","⠍","⠎","⠏",        "⡈","⡉","⡊","⡋","⡌","⡍","⡎","⡏",
			"⠐","⠑","⠒","⠓","⠔","⠕","⠖","⠗",        "⡐","⡑","⡒","⡓","⡔","⡕","⡖","⡗",
			"⠘","⠙","⠚","⠛","⠜","⠝","⠞","⠟",        "⡘","⡙","⡚","⡛","⡜","⡝","⡞","⡟",
			"⠠","⠡","⠢","⠣","⠤","⠥","⠦","⠧",        "⡠","⡡","⡢","⡣","⡤","⡥","⡦","⡧",
			"⠨","⠩","⠪","⠫","⠬","⠭","⠮","⠯",        "⡨","⡩","⡪","⡫","⡬","⡭","⡮","⡯",
			"⠰","⠱","⠲","⠳","⠴","⠵","⠶","⠷",        "⡰","⡱","⡲","⡳","⡴","⡵","⡶","⡷",
			"⠸","⠹","⠺","⠻","⠼","⠽","⠾","⠿",        "⡸","⡹","⡺","⡻","⡼","⡽","⡾","⡿",
			"⢀","⢁","⢂","⢃","⢄","⢅","⢆","⢇",        "⣀","⣁","⣂","⣃","⣄","⣅","⣆","⣇",
			"⢈","⢉","⢊","⢋","⢌","⢍","⢎","⢏",        "⣈","⣉","⣊","⣋","⣌","⣍","⣎","⣏",
			"⢐","⢑","⢒","⢓","⢔","⢕","⢖","⢗",        "⣐","⣑","⣒","⣓","⣔","⣕","⣖","⣗",
			"⢘","⢙","⢚","⢛","⢜","⢝","⢞","⢟",        "⣘","⣙","⣚","⣛","⣜","⣝","⣞","⣟",
			"⢠","⢡","⢢","⢣","⢤","⢥","⢦","⢧",        "⣠","⣡","⣢","⣣","⣤","⣥","⣦","⣧",
			"⢨","⢩","⢪","⢫","⢬","⢭","⢮","⢯",        "⣨","⣩","⣪","⣫","⣬","⣭","⣮","⣯",
			"⢰","⢱","⢲","⢳","⢴","⢵","⢶","⢷",        "⣰","⣱","⣲","⣳","⣴","⣵","⣶","⣷",
			"⢸","⢹","⢺","⢻","⢼","⢽","⢾","⢿",        "⣸","⣹","⣺","⣻","⣼","⣽","⣾","⣿"
		};
			public static string[] Dots9 =
			{
			"⢹","⢺","⢼","⣸","⣇","⡧","⡗","⡏"
		};
			public static string[] Dots10 =
			{
			"⢄","⢂","⢁","⡁","⡈","⡐","⡠"
		};
			public static string[] Dots11 =
			{
			"⠁","⠂","⠄","⡀","⢀","⠠","⠐","⠈"
		};
			public static string[] Dots12 =
			{
			"⢀⠀","⡀⠀",  "⠄⠀","⢂⠀",  "⡂⠀","⠅⠀",
			"⢃⠀","⡃⠀",  "⠍⠀","⢋⠀",  "⡋⠀","⠍⠁",
			"⢋⠁","⡋⠁","⠍⠉","⠋⠉",    "⠋⠉","⠉⠙",
			"⠉⠙","⠉⠩",  "⠈⢙","⠈⡙",  "⢈⠩","⡀⢙",
			"⠄⡙","⢂⠩",  "⡂⢘","⠅⡘",  "⢃⠨","⡃⢐",
			"⠍⡐","⢋⠠",  "⡋⢀","⠍⡁",  "⢋⠁","⡋⠁",
			"⠍⠉","⠋⠉",  "⠋⠉","⠉⠙",  "⠉⠙","⠉⠩",
			"⠈⢙","⠈⡙",  "⠈⠩","⠀⢙","⠀⡙","⠀⠩",
			"⠀⢘","⠀⡘","⠀⠨","⠀⢐","⠀⡐",   "⠀⠠",
			"⠀⢀","⠀⡀"
		};
			public static string[] Progress7 =
			{
			"▰▱▱▱▱▱▱","▰▰▱▱▱▱▱","▰▰▰▱▱▱▱","▰▰▰▰▱▱▱","▰▰▰▰▰▱▱","▰▰▰▰▰▰▱","▰▰▰▰▰▰▰","▰▱▱▱▱▱▱"
		};
			public static string[] Progress10 =
			{
			"▰▱▱▱▱▱▱▱▱▱","▰▰▱▱▱▱▱▱▱▱","▰▰▰▱▱▱▱▱▱▱","▰▰▰▰▱▱▱▱▱▱","▰▰▰▰▰▱▱▱▱▱","▰▰▰▰▰▰▱▱▱▱","▰▰▰▰▰▰▰▱▱▱","▰▰▰▰▰▰▰▰▱▱","▰▰▰▰▰▰▰▰▰▱","▰▰▰▰▰▰▰▰▰▰","▰▱▱▱▱▱▱▱▱▱"
		};
			public static string[] Line =
			{
			"-","\\","|","/"
		};

			#endregion
		}

		#endregion

		#region Private Utilities

		/// <summary>Reemplazo de la Funcion IIF.</summary>
		/// <typeparam name="T">Tipo de Datos</typeparam>
		/// <param name="expression">Condicion a considerar</param>
		/// <param name="truePart">Valor a delvoler en caso de que la 'expression' sea 'true'</param>
		/// <param name="falsePart">Valor a delvoler en caso de que la 'expression' sea 'false'</param>
		private static T IIf<T>(bool expression, T truePart, T falsePart)
		{ return expression ? truePart : falsePart; }

		private static string FindLongestString(params string[] strings)
		{
			if (strings == null || strings.Length == 0)
			{
				return null;
			}

			string longestString = strings[0];
			int longestLength = strings[0].Length;

			for (int i = 1; i < strings.Length; i++)
			{
				if (strings[i].Length > longestLength)
				{
					longestString = strings[i];
					longestLength = strings[i].Length;
				}
			}

			return longestString;
		}

		/// <summary>Formatea el titulo para que no pase el Tamaño maximo y rellena lo que falte con espacios.</summary>
		/// <param name="pString">Cadena a Formatear</param>
		/// <param name="pMaxLen">Longitud Máxima de la Cadena.</param>
		private static string FixString(string pString, int pMaxLen = 29)
		{
			string _ret = string.Empty;
			if (pString != null && pString.Length > 0)
			{
				_ret = pString;
				if (_ret.Length > pMaxLen)
				{
					_ret = pString.Substring(0, pMaxLen - 1);
				}

				_ret = _ret.PadRight(pMaxLen, ' ');
			}
			return _ret;
		}
		private static string CenterString(string text, int maxLength = 64)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new string(' ', maxLength);
			}

			text = text.Length > maxLength ? text.Substring(0, maxLength) : text;

			int paddingLength = maxLength - text.Length;
			int leftPadding = paddingLength / 2;
			int rightPadding = paddingLength - leftPadding;

			return new string(' ', leftPadding) + text + new string(' ', rightPadding);
		}
		private static string AlignLeftString(string text, int maxLength = 64)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new string(' ', maxLength);
			}

			text = text.Length > maxLength ? text.Substring(0, maxLength) : text;

			int paddingLength = maxLength - text.Length;
			return text + new string(' ', paddingLength);
		}
		private static string AlignRightString(string text, int maxLength = 64)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new string(' ', maxLength);
			}
			text = text.Length > maxLength ? text.Substring(0, maxLength) : text;
			int paddingLength = maxLength - text.Length;
			return new string(' ', paddingLength) + text;
		}
		private static string PadBoth(this string str, int maxLength = 64)
		{
			int spaces = maxLength - str.Length;
			int padLeft = spaces / 2 + str.Length;
			return str.PadLeft(padLeft).PadRight(maxLength);
		}

		private static List<string> CutLongString(string text, int maxLength = 64)
		{
			List<string> lines = new List<string>();
			while (text.Length > maxLength)
			{
				lines.Add(text.Substring(0, maxLength));
				text = text.Substring(maxLength);
			}
			lines.Add(text);
			return lines;
		}

		private static void ConsoleWritePixel(Color cValue)
		{
			Color[] cTable = cColors.Select(x => Color.FromArgb(x)).ToArray();
			char[] rList = new char[] { (char)9617, (char)9618, (char)9619, (char)9608 }; // 1/4, 2/4, 3/4, 4/4
			int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

			for (int rChar = rList.Length; rChar > 0; rChar--)
			{
				for (int cFore = 0; cFore < cTable.Length; cFore++)
				{
					for (int cBack = 0; cBack < cTable.Length; cBack++)
					{
						int R = (cTable[cFore].R * rChar + cTable[cBack].R * (rList.Length - rChar)) / rList.Length;
						int G = (cTable[cFore].G * rChar + cTable[cBack].G * (rList.Length - rChar)) / rList.Length;
						int B = (cTable[cFore].B * rChar + cTable[cBack].B * (rList.Length - rChar)) / rList.Length;
						int iScore = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);
						if (!(rChar > 1 && rChar < 4 && iScore > 50000)) // rule out too weird combinations
						{
							if (iScore < bestHit[3])
							{
								bestHit[3] = iScore; //Score
								bestHit[0] = cFore;  //ForeColor
								bestHit[1] = cBack;  //BackColor
								bestHit[2] = rChar;  //Symbol
							}
						}
					}
				}
			}
			Console.ForegroundColor = (ConsoleColor)bestHit[0];
			Console.BackgroundColor = (ConsoleColor)bestHit[1];
			Console.Write(rList[bestHit[2] - 1]);
		}

		private static string Format(string filename, string arguments)
		{
			return "'" + filename +
				((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
				"'";
		}

		/// <summary>Reads the content of the current console line.</summary>
		/// <returns>The content of the current console line.</returns>
		private static string ReadCurrentConsoleLine()
		{
			Console.SetCursorPosition(0, Console.CursorTop); // Ensure we start from the beginning of the line
			int currentTop = Console.CursorTop;

			// Read the line content
			string lineContent = string.Empty;
			for (int i = 0; i < Console.WindowWidth; i++)
			{
				char currentChar = (char)Console.Read();
				if (currentChar == '\r' || currentChar == '\n')
				{
					break; // End of line
				}

				lineContent += currentChar;
			}

			// Restore the original cursor position
			Console.SetCursorPosition(0, currentTop);
			return lineContent;
		}

		/// <summary>Enables virtual terminal processing for the console.</summary>
		private static void EnableVirtualTerminalProcessing()
		{
			IntPtr handle = Win32API.GetStdHandle(Win32API.STD_OUTPUT_HANDLE);
			Win32API.GetConsoleMode(handle, out uint mode);
			Win32API.SetConsoleMode(handle, mode | Win32API.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
		}


		private static Type GetJTokenType(Newtonsoft.Json.Linq.JToken token)
		{
			switch (token.Type)
			{
				case Newtonsoft.Json.Linq.JTokenType.Integer:
					return typeof(int);
				case Newtonsoft.Json.Linq.JTokenType.Float:
					return typeof(double);
				case Newtonsoft.Json.Linq.JTokenType.String:
					return typeof(string);
				case Newtonsoft.Json.Linq.JTokenType.Boolean:
					return typeof(bool);
				case Newtonsoft.Json.Linq.JTokenType.Date:
					return typeof(DateTime);
				case Newtonsoft.Json.Linq.JTokenType.Null:
					return typeof(object); // or `null` to indicate null type
				default:
					return typeof(string);
			}
		}

		private class Columna
		{
			public string Title { get; set; }
			public int Width { get; set; }
			public string TextAlign { get; set; }
			public Type DataType { get; set; }
		}

		#endregion
	}

	/* FUNCIONES API DE WINDOWS PARA LOS CUADROS DE DIALOGO COMUNES */
	public static class Win32API
	{
		#region Data Structures

		public const uint WM_SETTEXT = 0xC;
		public const int VK_RETURN = 0x0D;
		public const int WM_KEYDOWN = 0x100;

		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;

		public const Int64 INVALID_HANDLE_VALUE = -1;

		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;

		public const int FILE_SHARE_READ = 0x1;
		public const int FILE_SHARE_WRITE = 0x2;

		public const int OPEN_EXISTING = 0x3;
		public const int CREATE_NEW = 0x1;

		public const int FSCTL_LOCK_VOLUME = 0x00090018;
		public const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
		public const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
		public const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

		public const int STD_OUTPUT_HANDLE = -11;
		public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

		[Flags]
		public enum OFN
		{
			OFN_FILEMUSTEXIST = 0x00001000,
			OFN_OVERWRITEPROMPT = 0x00000002,
			OFN_PATHMUSTEXIST = 0x00000800
		}

		/// <summary>CHAR_ATTRIBUTES native structure.</summary>
		[Flags]
		public enum Attr : ushort
		{
			NO_ATTRIBUTES = 0x0000,
			/// <summary>
			/// Text color contains blue.
			/// </summary>
			FOREGROUND_BLUE = 0x0001,
			/// <summary>
			/// Text color contains green.
			/// </summary>
			FOREGROUND_GREEN = 0x0002,
			/// <summary>
			/// Text color contains red.
			/// </summary>
			FOREGROUND_RED = 0x0004,
			/// <summary>
			/// Text color is intensified.
			/// </summary>
			FOREGROUND_INTENSITY = 0x0008,
			/// <summary>
			/// Background color contains blue.
			/// </summary>
			BACKGROUND_BLUE = 0x0010,
			/// <summary>
			/// Background color contains green.
			/// </summary>
			BACKGROUND_GREEN = 0x0020,
			/// <summary>
			/// Background color contains red.
			/// </summary>
			BACKGROUND_RED = 0x0040,
			/// <summary>
			/// Background color is intensified.
			/// </summary>
			BACKGROUND_INTENSITY = 0x0080,
			/// <summary>
			/// Leading byte.
			/// </summary>
			COMMON_LVB_LEADING_BYTE = 0x0100,
			/// <summary>
			/// Trailing byte.
			/// </summary>
			COMMON_LVB_TRAILING_BYTE = 0x0200,
			/// <summary>
			/// Top horizontal
			/// </summary>
			COMMON_LVB_GRID_HORIZONTAL = 0x0400,
			/// <summary>
			/// Left vertical.
			/// </summary>
			COMMON_LVB_GRID_LVERTICAL = 0x0800,
			/// <summary>
			/// Right vertical.
			/// </summary>
			COMMON_LVB_GRID_RVERTICAL = 0x1000,
			/// <summary>
			/// Reverse foreground and background attribute.
			/// </summary>
			COMMON_LVB_REVERSE_VIDEO = 0x4000,
			/// <summary>
			/// Underscore.
			/// </summary>
			COMMON_LVB_UNDERSCORE = 0x8000
		}

		/// <summary>CharSet.Unicode is required for proper marshaling.</summary>
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct CHAR_INFO
		{
			[FieldOffset(0)]
			public char UnicodeChar;
			[FieldOffset(0)]
			public char AsciiChar;
			[FieldOffset(2)] //2 bytes seems to work properly
			public Attr Attributes;

			public override string ToString()
			{
				return string.Format("CHAR_INFO : '{0}' ({1})", AsciiChar, Attributes);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct COORD
		{
			public short X;
			public short Y;

			public COORD(short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct SMALL_RECT
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;

			public SMALL_RECT(short left, short top, short right, short bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class ConsoleFontInfo
		{
			internal int nFont;
			internal COORD dwFontSize;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public class OpenFileName
		{
			public int structSize = 0;
			public IntPtr dlgOwner = IntPtr.Zero;
			public IntPtr instance = IntPtr.Zero;

			public String filter = null;
			public String customFilter = null;
			public int maxCustFilter = 0;
			public int filterIndex = 0;

			public String file = null;
			public int maxFile = 0;

			public String fileTitle = null;
			public int maxFileTitle = 0;

			public String initialDir = null;

			public String title = null;

			public int flags = 0;
			public short fileOffset = 0;
			public short fileExtension = 0;

			public String defExt = null;

			public IntPtr custData = IntPtr.Zero;
			public IntPtr hook = IntPtr.Zero;

			public String templateName = null;

			public IntPtr reservedPtr = IntPtr.Zero;
			public int reservedInt = 0;
			public int flagsEx = 0;
		}

		public delegate bool WNDENUMPROC(Wnd param1, LPARAM param2);

		#endregion

		#region Open/Save Dialogs

		[DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

		[DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		#endregion

		#region Console Window Handles

		[DllImport("user32.dll", EntryPoint = "FindWindowW")]
		public static extern Wnd FindWindow(string lpClassName, string lpWindowName);

		[DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
		public static extern IntPtr GetConsoleHandle();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		#endregion

		#region Console Specific

		[DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleOutputW")]
		public static extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO[,] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetCurrentConsoleFont(IntPtr hConsoleOutput, bool bMaximumWindow, [Out][MarshalAs(UnmanagedType.LPStruct)]ConsoleFontInfo lpConsoleCurrentFont);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		#endregion

		#region Send Message

		//this is probably the most popular Windows API function, but .NET does not have a wrapper function or class that works with any window
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern LPARAM SendMessage(Wnd hWnd, uint Msg, LPARAM wParam, LPARAM lParam);

		//overload example: lParam is string
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern LPARAM SendMessage(Wnd hWnd, uint Msg, LPARAM wParam, string lParam);

		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);


		[DllImport("User32.Dll", EntryPoint = "PostMessageA")]
		public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

		#endregion

		#region Manipulating Windows

		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		[DllImportAttribute("user32.dll")]
		public static extern bool ReleaseCapture();

		#endregion
	}

	#region PasswordBuilders

	/// <summary>
	/// Simple class for building a password.
	/// </summary>
	public class PasswordBuilder : IPasswordBuilder<string>
	{
		private readonly System.Text.StringBuilder _stringBuilder;

		/// <summary>
		/// Gets the length of the password.
		/// </summary>
		public int Length => _stringBuilder.Length;

		/// <summary>
		/// Initializes a new instance of <see cref="PasswordBuilder"/>.
		/// </summary>
		public PasswordBuilder()
		{
			_stringBuilder = new System.Text.StringBuilder();
		}

		/// <summary>
		/// Adds a character to the password.
		/// </summary>
		/// <param name="c">The character to add.</param>
		public void AddChar(char c) => _stringBuilder.Append(c);

		/// <summary>
		/// Removes the last character from the current password.
		/// </summary>
		public void Backspace()
		{
			if (_stringBuilder.Length == 0)
			{
				return;
			}

			_stringBuilder.Remove(_stringBuilder.Length - 1, 1);
		}

		/// <summary>
		/// Returns the fully built password.
		/// </summary>
		/// <returns>The result as a <see cref="string"/> object.</returns>
		public string GetResult() => _stringBuilder.ToString();
	}

	/// <summary>
	/// Interface for simple password building class.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	public interface IPasswordBuilder<out T>
	{
		/// <summary>
		/// Gets the length of the password.
		/// </summary>
		int Length { get; }
		/// <summary>
		/// Adds a character to the password.
		/// </summary>
		/// <param name="c">The character to add.</param>
		void AddChar(char c);
		/// <summary>
		/// Removes the last character from the current password.
		/// </summary>
		void Backspace();
		/// <summary>
		/// Returns the fully built password.
		/// </summary>
		/// <returns>The result as a <see cref="T"/> object.</returns>
		T GetResult();
	}

	/// <summary>
	/// Simple class for building a secure password.
	/// </summary>
	public class SecurePasswordBuilder : IPasswordBuilder<SecureString>, IDisposable
	{
		private SecureString _secureString;

		/// <summary>
		/// Gets the length of the password.
		/// </summary>
		public int Length
		{
			get
			{
				ThrowExceptionWhenDisposed();
				return _secureString.Length;
			}
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SecurePasswordBuilder"/>.
		/// </summary>
		public SecurePasswordBuilder()
		{
			_secureString = new SecureString();
		}

		/// <summary>
		/// Adds a character to the password.
		/// </summary>
		/// <param name="c">The character to add.</param>
		public void AddChar(char c)
		{
			ThrowExceptionWhenDisposed();
			_secureString.AppendChar(c);
		}

		/// <summary>
		/// Removes the last character from the current password.
		/// </summary>
		public void Backspace()
		{
			ThrowExceptionWhenDisposed();

			if (_secureString.Length == 0)
			{
				return;
			}

			_secureString.RemoveAt(_secureString.Length - 1);
		}

		/// <summary>
		/// Returns the fully built password.
		/// </summary>
		/// <returns>The result as a <see cref="SecureString"/> object.</returns>
		public SecureString GetResult()
		{
			ThrowExceptionWhenDisposed();
			return _secureString.Copy();
		}

		#region Generated Dispose members

		/// <summary>
		/// Gets whether this instance is disposed or not.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Override this method to add your own dispose logic.
		/// </summary>
		/// <param name="disposing">If the instance is being disposed after the public <see cref="Dispose()"/> method has been called.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;

			if (disposing)
			{
				_secureString.Dispose();
			}

			_secureString = null;
		}

		/// <summary>
		/// Call this method to thrown an <see cref="ObjectDisposedException"/> when this instance has been disposed.
		/// </summary>
		protected void ThrowExceptionWhenDisposed()
		{
			if (!IsDisposed)
			{
				return;
			}

			Type thisType = typeof(SecurePasswordBuilder);
			throw new ObjectDisposedException(thisType.Name);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources for this <see cref="SecurePasswordBuilder"/> instance.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		void IDisposable.Dispose()
		{
			Dispose();
		}

		/// <summary>
		/// Destroys this <see cref="SecurePasswordBuilder"/> instance.
		/// </summary>
		~SecurePasswordBuilder()
		{
			Dispose(false);
		}

		#endregion
	}

	#endregion

}
