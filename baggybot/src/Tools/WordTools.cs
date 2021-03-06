﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BaggyBot.Tools
{
	/// <summary>
	/// For all Tools classes goes: they must be static, and they may not change any state.
	/// </summary>
	internal static class WordTools
	{
		// Direct copy from BaggyBot 1.0 wheee
		private static readonly string[] profanities = { "fuck", "cock", "dick", "bitch", "shit", "nigger", "asshole", "faggot", "wank", "cunt", "piss" };
		private static readonly string[] conjunctions = { "and", "but", "or", "yet", "for", "nor", "so" };
		private static readonly string[] ignoredWords = { "you", "its", "not", "was", "are", "can", "now", "all", "how", "that", "this", "what", "thats", "they", "then", "there", "when", "with", "well", "from", "will", "here", "out", "dont" };
		private static readonly string[] articles = { "the", "an", "a" };

		public static List<string> GetWords(string message)
		{
			var words = message.Trim().Split(' ')
				.Select(w => w.TrimEnd(',', '.'))
				.Where(w => w != string.Empty).ToList();
			return words;
		}

		public static string[] GetProfanities()
		{
			return profanities;
		}

		public static bool IsIgnoredWord(string word)
		{
			word = word.ToLower();
			return conjunctions.Contains(word) || ignoredWords.Contains(word) || articles.Contains(word);
		}
		public static bool IsProfanity(string word)
		{
			return profanities.Select(x => word.Contains(x)).Contains(true);
		}
	}
}
