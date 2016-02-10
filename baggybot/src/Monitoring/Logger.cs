﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BaggyBot.Configuration;
using BaggyBot.Tools;

namespace BaggyBot.Monitoring
{
	public enum LogLevel
	{
		Debug,
		Info,
		Message,
		Irc,
		Warning,
		Error
	}

	public enum Colours
	{
		Windows,
		Ansi,
		Disabled
	}

	public delegate void LogEvent(string message, LogLevel level);

	public static class Logger
	{
		public static Colours UseColouredOutput { get; set; }
		public const string LogFileName = "baggybot.log";
		private static bool disposed;
		private static string prefix = string.Empty;
		private const int prefixLength = 52;

		public static event LogEvent OnLogEvent;

		static Logger()
		{
			LoadLogFile();
		}
		private static void LoadLogFile()
		{
			textWriter = new StreamWriter(LogFileName, true);
		}

		private static TextWriter textWriter;

		public static void SetPrefix(string newPrefix)
		{
			prefix = newPrefix;
		}

		public static void ClearPrefix()
		{
			prefix = string.Empty;
		}

		private enum Colour
		{
			Normal,
			Red,
			Green,
			Yellow,
			Blue,
			Magenta,
			White,
			Reset
		}

		private const string KNRM = "\x1B[0m";
		private const string KRED = "\x1B[31m";
		private const string KGRN = "\x1B[32m";
		private const string KYEL = "\x1B[33m";
		private const string KBLU = "\x1B[34m";
		private const string KMAG = "\x1B[35m";
		private const string KCYN = "\x1B[36m";
		private const string KWHT = "\x1B[37m";
		private const string RESET = "\x33[0m";

		public static void Log(object sender, string message, LogLevel level = LogLevel.Debug, bool writeLine = true)
		{
			var lineBuilder = new StringBuilder();
			var lineColour = Colour.Normal;

			switch (level)
			{
				case LogLevel.Debug:
					lineBuilder.Append("[DEB] ");
					lineColour = Colour.White;
					break;
				case LogLevel.Info:
					lineBuilder.Append("[INF] ");
					lineColour = Colour.Green;
					break;
				case LogLevel.Message:
					lineBuilder.Append("[MSG] ");
					lineColour = Colour.Blue;
					break;
				case LogLevel.Irc:
					lineBuilder.Append("[IRC] ");
					lineColour = Colour.Normal;
					break;
				case LogLevel.Warning:
					lineBuilder.Append("[WRN] ");
					lineColour = Colour.Yellow;
					break;
				case LogLevel.Error:
					lineBuilder.Append("[ERR] ");
					lineColour = Colour.Red;
					break;
			}
			lineBuilder.Append(prefix);

			var time = DateTime.Now.ToString("[MMM dd - HH:mm:ss.fff] ");
			if (sender != null)
			{
				var location = $"[{sender.GetType().Name.Truncate(16)}-{sender.GetHashCode():X4}] ";
				lineBuilder.Insert(0, (time + location).PadRight(prefixLength));
			}
			else
			{
				lineBuilder.Insert(0, time.PadRight(prefixLength));
			}
			lineBuilder.Append(message);

			WriteToConsole(lineColour, level, lineBuilder);

			if ((level == LogLevel.Error || level == LogLevel.Warning))
			{
				OnLogEvent?.Invoke(lineBuilder.ToString(), level);
			}
			WriteToLogFile(lineBuilder, writeLine);
		}

		private static void WriteToLogFile(StringBuilder lineBuilder, bool writeLine)
		{
			if (disposed) return;

			if (writeLine)
				textWriter.WriteLine(lineBuilder.ToString());
			else
				textWriter.Write(lineBuilder.ToString());
			textWriter.Flush();
		}

		private static ConsoleColor GetForegroundColour(Colour colour)
		{
			switch (colour)
			{
				case Colour.Blue:
					return ConsoleColor.Blue;
				case Colour.Green:
					return ConsoleColor.Green;
				case Colour.Red:
					return ConsoleColor.Red;
				case Colour.Magenta:
					return ConsoleColor.Cyan;
				case Colour.Yellow:
					return ConsoleColor.Yellow;
				case Colour.White:
					return ConsoleColor.White;
				case Colour.Reset:
				case Colour.Normal:
				default:
					return ConsoleColor.Gray;
			}
		}

		private static string GetAnsiColour(Colour colour)
		{
			switch (colour)
			{
				case Colour.Blue:
					return KBLU;
				case Colour.Green:
					return KGRN;
				case Colour.Red:
					return KRED;
				case Colour.Magenta:
					return KMAG;
				case Colour.Yellow:
					return KYEL;
				case Colour.White:
					return KWHT;
				case Colour.Reset:
					return RESET;
				case Colour.Normal:
				default:
					return KNRM;
			}
		}

		private static void WriteToConsole(Colour lineColour, LogLevel level, StringBuilder lineBuilder)
		{
			if (UseColouredOutput == Colours.Windows)
			{
				Console.ForegroundColor = GetForegroundColour(lineColour);
			}
			else if (UseColouredOutput == Colours.Ansi)
			{
				Console.Write(GetAnsiColour(lineColour));
			}
			if (level == LogLevel.Debug)
			{
				if (ConfigManager.Config.Logging.ShowDebug)
				{
					Console.WriteLine(lineBuilder.ToString());
				}
				else
				{
					Log(null, "Unable to parse settings value for show_debug_log into a boolean.", LogLevel.Warning);
					Console.WriteLine(lineBuilder.ToString());
				}
			}
			else
			{
				Console.WriteLine(lineBuilder.ToString());
			}
			//Console.Write(RESET);
			//Console.ForegroundColor = prevColor;
		}

		public static void ClearLog()
		{
			textWriter.Close();
			File.Delete(LogFileName);
			LoadLogFile();
		}
		public static void Dispose()
		{
			Log(null, "Shutting down logger", LogLevel.Info);
			textWriter.Close();
			textWriter.Dispose();
			disposed = true;
		}

		internal static void LogException(object sender, Exception e, string currentAction)
		{
			var stackTrace = new StackTrace(e, true).GetFrame(0);
			Log(sender, $"An unhandled exception (type: {e.GetType()}) occurred while {currentAction}. Exception message: \"{e.Message}\"; in file:{stackTrace.GetFileName()}:{stackTrace.GetFileLineNumber()}", LogLevel.Error);
		}
	}
}