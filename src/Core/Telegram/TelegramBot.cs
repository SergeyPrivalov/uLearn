using Telegram.Bot;
using uLearn.Configuration;

namespace uLearn.Telegram
{
	public class TelegramBot
	{
		private readonly string token;
		protected string channel;
		protected readonly TelegramBotClient telegramClient;
		
		protected const int MaxMessageSize = 2048;

		protected TelegramBot()
		{
			token = ApplicationConfiguration.Read<UlearnConfiguration>().Telegram.BotToken;
			if (! string.IsNullOrEmpty(token))
				telegramClient = new TelegramBotClient(token);
		}

		protected bool IsBotEnabled => !string.IsNullOrWhiteSpace(token) && !string.IsNullOrEmpty(channel);
	}
}