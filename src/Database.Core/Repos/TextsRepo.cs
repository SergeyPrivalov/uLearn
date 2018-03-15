using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Ulearn.Common.Extensions;

namespace Database.Repos
{
	public class TextsRepo
	{
		private readonly UlearnDb db;
		public const int MaxTextSize = 20000;

		public TextsRepo(UlearnDb db)
		{
			this.db = db;
		}

		public async Task<TextBlob> AddText(string text)
		{
			if (text == null)
				return new TextBlob
				{
					Hash = null,
					Text = null
				};

			if (text.Length > MaxTextSize)
				text = text.Substring(0, MaxTextSize);

			var hash = GetHash(text);
			var blob = db.Texts.Find(hash);
			if (blob != null)
				return blob;

			blob = new TextBlob
			{
				Hash = hash,
				Text = text
			};
			db.AddOrUpdate(blob, b => b.Hash == hash);

			try
			{
				await db.SaveChangesAsync();
			}
			catch (DbUpdateException)
			{
				// It's ok, just tried to insert text with hash which already exists, try to find it
				if (!db.Texts.AsNoTracking().Any(t => t.Hash == hash))
					throw;
				db.Entry(blob).State = EntityState.Unchanged;
			}
			return blob;
		}

		private static string GetHash(string text)
		{
			var byteArray = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(text));
			return BitConverter.ToString(byteArray).Replace("-", "");
		}

		public TextBlob GetText(string hash)
		{
			if (hash == null)
				return new TextBlob
				{
					Hash = null,
					Text = null
				};
			return db.Texts.Find(hash);
		}

		public Dictionary<string, string> GetTextsByHashes(IEnumerable<string> hashes)
		{
			return db.Texts.Where(t => hashes.Contains(t.Hash)).ToDictionary(t => t.Hash, t => t.Text);
		}
	}
}