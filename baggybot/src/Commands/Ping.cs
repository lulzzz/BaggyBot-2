﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace BaggyBot.Commands
{
	internal class Ping : Command
	{
		public override PermissionLevel Permissions => PermissionLevel.All;
		public override string Name => "ping";
		public override string Usage => "[server] [count]";
		public override string Description => "Returns \"Pong!\" when no arguments are given. When [server] is specified, tries to ping that server once, or as many times as specified in [count].";

		private string Colour(int? code)
		{
			//string control = "\\";
			const string control = "\x03";
			const string bold = "\x02";
			if (code.HasValue)
			{
				return control + code.Value + bold;
			}
			return bold + control;
		}

		private string Colourise(double ping)
		{
			if (ping < 10) return Colour(6);
			if (ping < 25) return Colour(9);
			if (ping <= 50) return Colour(3);
			if (ping <= 150) return Colour(8);
			if (ping <= 300) return Colour(7);
			return Colour(4);
		}

		public override void Use(CommandArgs command)
		{
			if (command.Args.Length == 1)
			{
				var target = command.Args[0];

				PingReply reply;
				try
				{
					reply = new System.Net.NetworkInformation.Ping().Send(target);
				}
				catch (PingException e)
				{
					command.ReturnMessage($"Unable to ping {target}: {e.InnerException.Message}");
					return;
				}

				if (reply.Status == IPStatus.Success)
				{
					command.ReturnMessage($"Reply from {reply.Address} in {Colourise(reply.RoundtripTime)}ms{reply.RoundtripTime}{Colour(null)}");
				}
				else
				{
					command.ReturnMessage($"Ping failed ({reply.Status})");
				}
			}
			else if (command.Args.Length == 2)
			{
				var target = command.Args[0];
				var attempts = int.Parse(command.Args[1]);

				var pings = new List<PingReply>();
				long total = 0;
				var successCount = 0;

				for (var i = 0; i < attempts; i++)
				{
					pings.Add(new System.Net.NetworkInformation.Ping().Send(target));
					if (pings[i].Status == IPStatus.Success)
					{
						successCount++;
						total += pings[i].RoundtripTime;
						if (pings[i].RoundtripTime < 500)
						{
							Thread.Sleep(500 - (int)pings[i].RoundtripTime);
						}
					}
				}

				var average = Math.Round(total / (double)successCount, 2);

				var raw = string.Join(", ", pings.Select(reply => (reply.Status == IPStatus.Success ? Colourise(reply.RoundtripTime) + reply.RoundtripTime + "ms" + Colour(null) : Colour(4) + "failed" + Colour(null))));
				var word = successCount == 1 ? "reply" : "replies";
				var address = pings[0].Address == null ? "Unknown IP Address" : pings[0].Address.ToString();
				var number = double.IsNaN(average) ? "NaN " : average.ToString();
				command.ReturnMessage($"{successCount} {word} from {address}, averaging {Colourise(average) + number + "ms" + Colour(null)} ({raw})");
			}
			else
			{
				command.ReturnMessage("Pong!");
			}
		}
	}
}
