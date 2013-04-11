﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IRCSharp;
using BaggyBot.Properties;

namespace BaggyBot
{
	class Program
	{
		private IrcClient client;
		private StatsHandler sHandler;
		private SqlConnector sqlConnector;
		private DataFunctionSet dataFunctionSet;
		private CommandHandler commandHandler;
		private IrcInterface ircInterface;
		public const string commandIdentifier = "-";

		internal const string Version = "2.0 Release Candidate 1";

		public Program()
		{
			Logger.Log("Starting BaggyBot version " + Version, LogLevel.Info);

			sqlConnector = new SqlConnector();

			Console.WriteLine("Purge the database? y/n");
			var k = Console.ReadKey();
			if (k.KeyChar == 'y') {
				string statement = 
					@"drop table `stats_bot`.`emoticons`;
					drop table `stats_bot`.`quotes`;
					drop table `stats_bot`.`urls`;
					drop table `stats_bot`.`usercreds`;
					drop table `stats_bot`.`userstats`;
					drop table `stats_bot`.`var`;
					drop table `stats_bot`.`words`;";
				sqlConnector.ExecuteStatement(statement); 
				Console.WriteLine("Database purged");
			}

			sqlConnector.InitializeDatabase();

			client = new IrcClient();
			ircInterface = new IrcInterface(client);
			dataFunctionSet = new DataFunctionSet(sqlConnector, ircInterface);
			sHandler = new StatsHandler(dataFunctionSet, sqlConnector, ircInterface);
			commandHandler = new CommandHandler(ircInterface, sqlConnector,dataFunctionSet);

			client.OnNickChanged += dataFunctionSet.HandleNickChange;
			client.OnMessageReceived += ProcessMessage;
			client.OnRawLineReceived += ProcessRawLine;
			client.OnFormattedLineReceived += ProcessFormattedLine;
		}

		private void ProcessFormattedLine(IrcLine line)
		{
		if (line.Command.Equals("NOTICE") && ircInterface.HasNickservCall && client.GetUserFromSender(line.Sender).Ident.Equals("NickServ")) {
				if (line.FinalArgument.StartsWith("Information on")) {
					string data = line.FinalArgument.Substring("Information on  ".Length);
					string nick = data.Substring(0, data.IndexOf(" ")-1);
					data = data.Substring(nick.Length + 2 + "(account  ".Length);
					string nickserv = data.Substring(0,data.Length-3);
					ircInterface.AddNickserv(nick.ToLower(),nickserv);
				} else if (line.FinalArgument.EndsWith("is not registered.")) {
					string nick = line.FinalArgument.Substring(1, line.FinalArgument.Length - 2);
					nick = nick.Substring(0, nick.IndexOf(' ')-1);
					ircInterface.AddNickserv(nick, null);
				}
			}
		}

		private void ProcessRawLine(string line)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine("[RAW]\t"+line);
			Console.ResetColor();
		}

		internal void Connect()
		{
			Logger.Log("Connecting to the IRC server");

			string server = Settings.Default.ircserver;
			int port = Settings.Default.ircserverport;
			string nick = Settings.Default.nick;
			string ident = Settings.Default.ident;
			string realname = Settings.Default.realname;
			string firschannel = Settings.Default.firstchannel;

			try {
				client.Connect(server,port, nick, ident, realname);
				client.JoinChannel(firschannel);
				Logger.Log("Connection established.");
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Log("Failed to connect to the IRC server: " + e.Message, LogLevel.Error);
			}
		}

		private void ProcessMessage(IrcMessage message)
		{
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine("[MSG]\t" + message.Sender.Nick+ ": " + message.Message);
			Console.ResetColor();
			if (message.Message.StartsWith(commandIdentifier)) {
				commandHandler.ProcessCommand(message);
			} else {
				sHandler.ProcessMessage(message);
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("Clear the log file? (y/n)");
			if (Console.ReadKey().KeyChar == 'y') {
				Logger.ClearLog();
				Console.WriteLine();
			}
			new Program().Connect();
		}
	}
}
