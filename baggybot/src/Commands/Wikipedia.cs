﻿using System;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace BaggyBot.Commands
{
	internal class Wikipedia : Command
	{
		public override PermissionLevel Permissions => PermissionLevel.All;
		public override string Name => "wiki";
		public override string Usage => "<search term>";
		public override string Description => "Searches for an article on Wikipedia.";

		public override void Use(CommandArgs command)
		{
			var uri = new Uri(
				$"http://en.wikipedia.org/w/api.php?format=json&action=query&titles={command.FullArgument}&prop=info&inprop=url");

			var rq = WebRequest.Create(uri);
			var response = rq.GetResponse();

			using (var sr = new StreamReader(response.GetResponseStream()))
			{
				var data = sr.ReadToEnd();
				dynamic jsonObj = JObject.Parse(data);

				var page = ((JObject)jsonObj.query.pages).First.First;

				var title = page["title"];


				command.ReturnMessage($"{title} ({page["canonicalurl"]}): {GetContent(page["canonicalurl"].ToString())}");
			}
		}

		private string GetContent(string url)
		{
			var rq = WebRequest.Create(url + "?action=render");
			var rs = rq.GetResponse();
			var doc = new HtmlDocument();
			doc.Load(rs.GetResponseStream());

			var firstParagraph = doc.DocumentNode.SelectSingleNode("/p[1]");

			return firstParagraph.InnerText;

		}
	}
}
