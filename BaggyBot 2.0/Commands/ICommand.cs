﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IRCSharp;

namespace BaggyBot.Commands
{
	interface ICommand
	{
		void Use(CommandArgs c);
		PermissionLevel Permissions { get; }
	}
}
