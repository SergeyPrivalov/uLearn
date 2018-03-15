using System;
using System.Configuration;
using System.Threading.Tasks;
using log4net;
using Telegram.Bot.Types.Enums;
using uLearn.Telegram;

namespace GiftsGranter
{
	public class GiftsTelegramBot : TelegramBot
	{
		private static ILog log = LogManager.GetLogger(typeof(GiftsTelegramBot));
		
		public GiftsTelegramBot()
		{
			channel = ConfigurationManager.AppSettings["ulearn.telegram.gifts.channel"];
		}

		public async Task PostToChannelAsync(string message, ParseMode parseMode = ParseMode.Default)
		{
			if (!IsBotEnabled)
				return;
			
			log.Info($"Отправляю в телеграм-канал {channel} сообщение об выданных призах:\n{message}");
			try
			{
				await telegramClient.SendTextMessageAsync(channel, message, parseMode: parseMode, disableWebPagePreview: true).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				log.Error($"Не могу отправить сообщение в телеграм-канал {channel}", e);
			}
		}

		public void PostToChannel(string message, ParseMode parseMode = ParseMode.Default)
		{
			PostToChannelAsync(message, parseMode).Wait();
		}
	}
}