using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using Database.DataContexts;
using Database.Models;
using Kontur.Spam.Client;
using log4net;
using Metrics;
using Microsoft.AspNet.Identity;
using Ulearn.Common.Extensions;

namespace uLearn.Web.Controllers
{
	public class BaseUserController : BaseController
	{
		protected static readonly ILog log = LogManager.GetLogger(typeof(BaseUserController));

		protected readonly ULearnDb db;
		protected UserManager<ApplicationUser> userManager;
		protected readonly UsersRepo usersRepo;

		protected readonly GraphiteMetricSender metricSender;

		protected readonly string secretForHashes;

		protected string spamChannelId;
		protected SpamClient spamClient;
		protected string spamTemplateId;

		protected BaseUserController(ULearnDb db)
		{
			this.db = db;
			userManager = new ULearnUserManager(db);
			usersRepo = new UsersRepo(db);
			metricSender = new GraphiteMetricSender("web");

			secretForHashes = WebConfigurationManager.AppSettings["ulearn.secretForHashes"] ?? "";

			var spamEndpoint = WebConfigurationManager.AppSettings["ulearn.spam.endpoint"] ?? "";
			var spamLogin = WebConfigurationManager.AppSettings["ulearn.spam.login"] ?? "ulearn";
			var spamPassword = WebConfigurationManager.AppSettings["ulearn.spam.password"] ?? "";
			spamChannelId = WebConfigurationManager.AppSettings["ulearn.spam.channels.emailConfirmations"] ?? "";
			spamTemplateId = WebConfigurationManager.AppSettings["ulearn.spam.templates.withButton"] ?? "";

			try
			{
				spamClient = new SpamClient(new Uri(spamEndpoint), spamLogin, spamPassword);
			}
			catch (Exception e)
			{
				log.Error($"Can\'t initialize Spam.API client to {spamEndpoint}, login {spamLogin}, password {spamPassword.MaskAsSecret()}", e);
				throw;
			}
		}

		protected BaseUserController()
			: this(new ULearnDb())
		{
		}

		protected string GetEmailConfirmationSignature(string email)
		{
			return $"{secretForHashes}email={email}{secretForHashes}".CalculateMd5();
		}

		protected async Task<bool> SendConfirmationEmail(ApplicationUser user)
		{
			metricSender.SendCount("email_confirmation.send_confirmation_email.try");
			var confirmationUrl = Url.Action("ConfirmEmail", "Account", new { email = user.Email, signature = GetEmailConfirmationSignature(user.Email) }, "https");
			var subject = "Подтверждение адреса";

			var messageInfo = new MessageSentInfo
			{
				RecipientAddress = user.Email,
				RecipientName = user.VisibleName,
				Subject = subject,
				TemplateId = spamTemplateId,
				Variables = new Dictionary<string, object>
				{
					{ "title", subject },
					{ "content", $"<h2>Привет, {user.VisibleName}!</h2><p>Подтверди адрес электронной почты, нажав на кнопку:</p>" },
					{ "text_content", $"Привет, {user.VisibleName}!\nПодтверди адрес электронной почты, нажав на кнопку:" },
					{ "button", true },
					{ "button_link", confirmationUrl },
					{ "button_text", "Подтвердить адрес" },
					{
						"content_after_button",
						"<p>Подтвердив адрес, ты сможешь восстановить доступ к своему аккаунту " +
						"в любой момент, а также получать уведомления об ответах на свои комментарии и других важных событиях</p>" +
						"<p>Мы не подпиcываем ни на какую периодическую рассылку, " +
						"а все уведомления можно выключить в профиле.</p><p>" +
						"Если ссылка для подтверждения почты не работает, просто скопируй адрес " +
						$"и вставь его в адресную строку браузера: <a href=\"{confirmationUrl}\">{confirmationUrl}</a></p>"
					}
				}
			};

			try
			{
				await spamClient.SentMessageAsync(spamChannelId, messageInfo);
			}
			catch (Exception e)
			{
				log.Error($"Не могу отправить письмо для подтверждения адреса на {user.Email}", e);
				return false;
			}
			metricSender.SendCount("email_confirmation.send_confirmation_email.success");

			await usersRepo.UpdateLastConfirmationEmailTime(user);
			return true;
		}
	}
}