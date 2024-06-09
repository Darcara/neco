namespace Neco.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

internal static class OneTimeHelper {
	// Splits the HTML "The Complete Works of William Shakespeare by William Shakespeare" from Project Gutenberg into separate txt files
	// https://www.gutenberg.org/ebooks/100
	public static void SplitShakespeareByGutenberg() {
		HtmlDocument doc = new();
		doc.Load("./TestData/pg100-images.html");

		StringBuilder sb = new();
		String currentChapterTitle = String.Empty;
		String currentSubTitle = String.Empty;

		Directory.CreateDirectory("./shakespeare/");

		String GetFilename(String n1, String n2) {
			return $"./shakespeare/{n1.Replace(".", String.Empty)}{(String.IsNullOrWhiteSpace(n2) ? String.Empty : $".{n2}")}.txt";
		}

		List<HtmlNode> list = doc.DocumentNode.SelectNodes("/html/body/div[@class='chapter']").Skip(1).ToList();
		for (Int32 chapterIdx = 0; chapterIdx < list.Count; chapterIdx++) {
			HtmlNode chapterNode = list[chapterIdx];

			// chapters
			HtmlNode? chapterAnchor = chapterNode.SelectNodes(".//a[contains(@id, 'chap')]")?.FirstOrDefault();
			if (chapterAnchor != null) {
				// save current chapter
				if (sb.Length > 0) {
					File.WriteAllText(GetFilename(currentChapterTitle, currentSubTitle), sb.ToString().Trim());
					sb.Clear();
				}

				// new chapter / work
				currentChapterTitle = chapterAnchor.ParentNode.InnerText.Trim();
				Console.WriteLine(chapterAnchor.Attributes["id"].Value + ": " + currentChapterTitle);
				currentSubTitle = String.Empty;
			}

			// scenes
			HtmlNode? sceneAnchor = chapterNode.SelectNodes(".//a[@id and not(contains(@id, 'chap'))]")?.FirstOrDefault();
			if (sceneAnchor != null) {
				// save current
				if (sb.Length > 0) {
					File.WriteAllText(GetFilename(currentChapterTitle, currentSubTitle), sb.ToString().Trim());
					sb.Clear();
				}

				// new part
				currentSubTitle = String.Join(String.Empty, sceneAnchor.ParentNode.InnerText.Trim().TakeWhile(c => c != '\n')).Trim();
				Console.WriteLine($"    {sceneAnchor.Attributes["id"].Value}: {currentSubTitle}");
			}

			// sonnets
			if (currentChapterTitle.Contains("sonnets", StringComparison.OrdinalIgnoreCase)) {
				foreach (HtmlNode sonnetNode in chapterNode.SelectNodes("./p")) {
					currentSubTitle = sonnetNode.SelectSingleNode("./b").InnerText;
					File.WriteAllText(GetFilename(currentChapterTitle, currentSubTitle), sonnetNode.InnerText);
				}

				continue;
			}

			String txt = chapterNode.InnerText.Trim();
			if (!String.IsNullOrWhiteSpace(txt))
				sb.AppendLine(txt);
		}

		if (sb.Length > 0) {
			File.WriteAllText(GetFilename(currentChapterTitle, currentSubTitle), sb.ToString().Trim());
			sb.Clear();
		}
	}
}