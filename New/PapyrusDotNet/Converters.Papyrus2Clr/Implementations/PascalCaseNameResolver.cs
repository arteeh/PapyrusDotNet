//     This file is part of PapyrusDotNet.
// 
//     PapyrusDotNet is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     PapyrusDotNet is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with PapyrusDotNet.  If not, see <http://www.gnu.org/licenses/>.
//  
//     Copyright 2016, Karl Patrik Johansson, zerratar@gmail.com

#region

using PapyrusDotNet.Common.Interfaces;

#endregion

namespace PapyrusDotNet.Converters.Papyrus2Clr.Implementations
{
	public class PascalCaseNameResolver : INameConventionResolver
	{
		private readonly string[] orderedWordList;
		private readonly Dictionary<string, string> resolvedNames;
		internal string[] WordList;

		/// <summary>
		/// Initializes a new instance of the <see cref="PascalCaseNameResolver"/> class.
		/// </summary>
		/// <param name="userInterface">The user interface.</param>
		/// <param name="settings">The settings.</param>
		public PascalCaseNameResolver(IUserInterface userInterface, PascalCaseNameResolverSettings settings)
		{
			resolvedNames = new Dictionary<string, string>();
			var ui = userInterface;
			// "wordlist.txt"
			if (string.IsNullOrEmpty(settings.WordDictionaryFile))
				return;

			if (File.Exists(settings.WordDictionaryFile))
			{
				ui.DrawInterface("Loading wordlist... This may take a few seconds.");
				WordList = File.ReadAllLines(settings.WordDictionaryFile);
				Array.Sort(WordList);
			}
			else
			{
				ui.DrawInterface("Wordlist was not found, skipping...");
				WordList = new string[0];
			}

			orderedWordList = WordList.Select(w => w.ToLower()).ToArray();
			Array.Sort(orderedWordList);
		}

		/// <summary>
		///     Resolves the inputName and returns a PascalCase class-friendly name
		/// </summary>
		/// <param name="inputName"></param>
		/// <returns></returns>
		public string Resolve(string inputName)
		{
			if (string.IsNullOrEmpty(inputName)) return null;

			var loweredName = inputName.ToLower();
			if (resolvedNames.ContainsKey(loweredName))
				return resolvedNames[loweredName];

			var outputName = inputName;
			if (WordList != null && WordList.Length > 0)
			{
				if (!char.IsUpper(inputName[0]) && !char.IsUpper(inputName[1]))
				{
					var usedWords = new HashSet<string>();

					var nameFound = false;
					for (var j = 0; j < inputName.Length; j++)
					{
						for (var i = inputName.Length - 1; i > 0; i--)
						{
							var name = string.Join("", inputName.Skip(j).Take(i + 1 - j)).ToLower();
							var index = Array.BinarySearch(orderedWordList, name);
							if (index >= 0 && !usedWords.Any(s => s.Contains(name)))
							{
								usedWords.Add(name);

								var insertionPoint = outputName.ToLower().IndexOf(name);

								outputName = outputName.Remove(insertionPoint, name.Length);
								outputName = outputName.Insert(insertionPoint, WordList[index]);

								if (name.Length == inputName.Length)
								{
									nameFound = true;
									break;
								}
							}
						}
						if (nameFound) break;
					}
				}
			}
			outputName = ToTitleCase(outputName);

			resolvedNames.Add(loweredName, outputName);

			return outputName;
		}

		private static string ToTitleCase(string text)
		{
			if (!char.IsUpper(text[0]))
			{
				text = char.ToUpper(text[0]) + text.Substring(1);
			}
			return text;
		}
	}
}