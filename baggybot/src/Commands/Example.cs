﻿// NOTE: This is not an actual command, just a template for quickly adding a new command

namespace BaggyBot.Commands
{
	class Example : Command
	{
		public override PermissionLevel Permissions { get { return PermissionLevel.All; } }
		public override string Usage => "";
		public override string Description => "";

		public void Use(CommandArgs command)
		{

		}
	}
}