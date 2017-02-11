﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using BaggyBot.Configuration;
using BaggyBot.Database;
using BaggyBot.EmbeddedData;
using BaggyBot.MessagingInterface.Events;
using BaggyBot.Monitoring;
using BaggyBot.Tools;

namespace BaggyBot.MessagingInterface.Handlers
{
	internal class StatsHandler : ChatClientEventHandler
	{
		private readonly Random rand;
		private readonly Regex textOnly = new Regex("[^a-z]");

		// Non-exhaustive list of shared idents that are commonly used by multiple people, often because they are standard values for their respective IRC clients.
		/*
				private string[] sharedIdents = { "webchat", "~quassel", "~AndChat12", "AndChat66", "~chatzilla", "~IceChat77", "~androirc", "Mibbit", "~PircBotX" };
		*/
		// TODO: move these to EmbeddedData
		private readonly string[] snagMessages = { "Snagged the shit outta that one!", "What a lame quote. Snagged!", "Imma stash those words for you.", "Snagged, motherfucker!", "Everything looks great out of context. Snagged!", "Yoink!", "That'll look nice on the stats page." };

		public StatsHandler()
		{
			rand = new Random();
		}
		public override void HandleMessage(MessageEvent ev)
		{
			var message = ev.Message;
			// Can't save statistics if we don't have a DB connection!
			if (StatsDatabase.ConnectionState == ConnectionState.Closed) return;

			StatsDatabase.AddMessage(message);
			var userId = message.Sender.DbUser.Id;


			if (message.Action)
				StatsDatabase.IncrementActions(userId);
			else
				StatsDatabase.IncrementLineCount(userId);

			var words = WordTools.GetWords(message.Body);
			StatsDatabase.IncrementWordCount(userId, words.Count);

			StatsDatabase.IncrementVar("global_line_count");
			StatsDatabase.IncrementVar("global_word_count", words.Count);
			GenerateRandomQuote(ev, words);
			ProcessRandomEvents(ev);
			GetEmoticons(StatsDatabase, userId, words);
			foreach (var word in words)
			{
				ProcessWord(message, word, userId);
			}
		}

		/*private void AddMessageToIrcLog(ChatMessage message, int userId)
		{
			StatsDatabase.AddIrcMessage(DateTime.Now, userId, message.Channel.Identifier, message.Sender.Nickname,
				message.Action
				? $"*{message.Sender.Nickname} {message.Body}*"
				: message.Body);
		}*/

		private void ProcessRandomEvents(MessageEvent ev)
		{
			var message = ev.Message;
			if (message.Sender.Nickname == "Ralph" && message.Body.ToLower().Contains("baggybot"))
			{
				ev.ReturnMessageCallback("Shut up you fool");
			}
			else if (message.Body.ToLower().Contains("fuck you baggybot"))
			{
				ev.ReturnMessageCallback("pls ;___;");
			}
		}

		private void ProcessWord(ChatMessage message, string word, int sender)
		{
			// In order to count word occurrence correctly, all words are transformed to
			// lowercase, and all non-latin characters are removed. This is done because
			// people commonly say "its" when they mean "it's", and it is very difficult
			// to determine which one they meant, so instead, they are considered equal.
			var lword = word.ToLower();
			var cword = textOnly.Replace(lword, string.Empty);
			if (word.StartsWith("http://") || word.StartsWith("https://"))
			{
				StatsDatabase.IncrementUrl(word, sender, message.Body);
			}
			else
			{
				var ignored = WordTools.IsIgnoredWord(cword) || cword.Length < 3;
					StatsDatabase.IncrementWord(cword);
			}
			if (WordTools.IsProfanity(lword))
			{
				StatsDatabase.IncrementProfanities(sender);
			}
		}

		private void GetEmoticons(StatsDatabaseManager statsDb, int userId, IEnumerable<string> words)
		{
			foreach (var word in words)
			{
				if (Emoticons.List.Contains(word))
				{
					statsDb.IncrementEmoticon(word, userId);
				}
			}
		}
		private void GenerateRandomQuote(MessageEvent ev, List<string> words)
		{
			var message = ev.Message;
			if (message.Action)
			{
				message = message.Edit("*" + message.Sender.Nickname + " " + message.Body + "*");
			}

			if (ControlVariables.SnagNextLine)
			{
				ControlVariables.SnagNextLine = false;
				StatsDatabase.Snag(message);
				ev.ReturnMessageCallback("Snagged line on request.");
				return;
			}
			if (ControlVariables.SnagNextLineBy != null && ControlVariables.SnagNextLineBy == message.Sender.Nickname)
			{
				ControlVariables.SnagNextLineBy = null;
				StatsDatabase.Snag(message);
				ev.ReturnMessageCallback("Snagged line on request.");
				return;
			}

			TryTakeQuote(ev, words);
		}

		private void TryTakeQuote(MessageEvent ev, List<string> words)
		{
			var last = StatsDatabase.GetLastQuotedLine(ev.Message.Sender.DbUser.Id);
			if (last.HasValue)
			{
				if ((DateTime.Now - last.Value).Hours < ConfigManager.Config.Quotes.MinDelayHours)
				{
					return;
				}
			}

			var snagChance = ConfigManager.Config.Quotes.Chance;
			var silenceChance = ConfigManager.Config.Quotes.SilentQuoteChance;

			if (words.Count > 6)
			{ // Do not snag if the amount of words to be snagged is less than 7
				if (rand.NextDouble() <= snagChance)
				{
					var allowSnagMessage = ConfigManager.Config.Quotes.AllowQuoteNotifications;
					var hideSnagMessage = rand.NextDouble() <= silenceChance;
					if (!allowSnagMessage || hideSnagMessage)
					{ // Check if snag message should be displayed
						Logger.Log(this, "Silently snagging this message");
						StatsDatabase.Snag(ev.Message);
					}
					else
					{
						var randint = rand.Next(snagMessages.Length * 2); // Determine whether to simply say "Snagged!" or use a randomized snag message.
						if (randint < snagMessages.Length)
						{
							TakeQuote(ev, snagMessages[randint]);
						}
						else
						{
							TakeQuote(ev, "Snagged!");
						}
					}
				}
			}
		}

		private void TakeQuote(MessageEvent ev, string snagMessage)
		{
			ev.ReturnMessageCallback(snagMessage);
			StatsDatabase.Snag(ev.Message);
		}
	}
}
