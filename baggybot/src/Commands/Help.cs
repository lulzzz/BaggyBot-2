﻿using System.Collections.Generic;
using System.Linq;
using BaggyBot.Formatting;

namespace BaggyBot.Commands
{
	[NonAutoGeneratedCommand]
	internal class Help : Command
	{
		public override PermissionLevel Permissions => PermissionLevel.All;
		public override string Name => "help";
		public override string Usage => "<command>";
		public override string Description => "Get help about my commands.";

		private readonly Dictionary<string, Command> commandList; 

		public Help(Dictionary<string, Command> commandList)
		{
			this.commandList = commandList;
		}

		public override void Use(CommandArgs command)
		{


			if (command.Args.Length == 0)
			{
				var availableCommands = string.Join(", ", commandList.Select(pair => pair.Key));
				command.ReturnMessage($"Use -help <command> to get help about a specific command. -- Available commands: {availableCommands}");
			}
			else if (command.Args.Length == 1)
			{
				if (commandList.ContainsKey(command.Args[0]))
				{
					var cmd = commandList[command.Args[0]];
					command.Reply($"{Frm.B}{command.Args[0]}{Frm.B}: {cmd.Description} (usable by {cmd.Permissions}) -- {Frm.B}Usage{Frm.B}: {Frm.C}{command.Args[0]} {cmd.Usage}{Frm.C}");
				}
			}
			else
			{

			}
		}
	}
}
