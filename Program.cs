using System.Diagnostics;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace QuizBot;

internal class Program
{
	static List<string> questions = new List<string>();
	static List<string[]> answers = new List<string[]>();
	static List<int> correctAnswers = new List<int>();
	static int counter = 0, answerCounter = 0;
	static bool isStarted = true;
	static string name = "", subjectName = "";
	static List<TelegramUser> users = new List<TelegramUser>();
	static long messagePollId, userId, milliseconds;
	static void Main(string[] args)
	{
		const string token = "6803135707:AAEK-AGwRZq4AvoSOZmKR9xI_hZj1b8HMoI";

		TelegramBotClient client = new TelegramBotClient(token);

		client.StartReceiving(UpdateHandlerAsync, ErrorHandlerAsync);
		
		Console.ReadKey();

		Task ErrorHandlerAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
		{
			var ErrorMessage = exception switch
			{
				ApiRequestException apiRequestException
					=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => exception.ToString()
			};

			Console.WriteLine(ErrorMessage);
			
			return Task.CompletedTask;
		}

		async Task UpdateHandlerAsync(ITelegramBotClient client, Update update, CancellationToken token)
		{
			//return;
			if (update is null) return;
			if (update.Message is not null)
			{
				if (update.Message.Type is MessageType.Text)
					await OnSendMessageAsync(client, update);
				else if (update.Message.Type is MessageType.Contact)
					await ChoosingSubjectAsync(client, update, false);
			}
			else if (update.CallbackQuery is not null || CheckingIsAnswered(update.Poll.Options) || update.Poll.IsClosed)
			{
				if (counter < 10)
					await StartingTestAsync(client, update, (isStarted) ? update.CallbackQuery.Data : null, token) ;
				else
				{
					await SendingResultOfTestAsync(client, update);

					TelegramUser telegramUser = users.Find(user => user.Id == userId);
					
					users.Remove(telegramUser);

					telegramUser.SavingResult(subjectName, answerCounter);

					users.Add(telegramUser);

					WritingToUsers(users);

					answerCounter = 0;
				}
			}
		}

		void WritingToUsers(List<TelegramUser> userss)
		{
			JsonSerializerOptions options = new JsonSerializerOptions()
			{
				WriteIndented = true,
			};

			using (StreamWriter writer = new StreamWriter("C:\\My works\\PDP_c#\\QuizBot\\Users.json"))
			{
				string jsonData = JsonSerializer.Serialize(userss, options);

				writer.Write(jsonData);
			}
		}

		List<TelegramUser> ReadingFromUsers()
		{	
			List<TelegramUser> list = new List<TelegramUser>();

			using (StreamReader reader = new StreamReader("C:\\My works\\PDP_c#\\QuizBot\\Users.json"))
			{
				string jsonData = reader.ReadToEnd();

				list = JsonSerializer.Deserialize<List<TelegramUser>>(jsonData);
			}

			return list;
		}

		async Task SendingResultOfTestAsync(ITelegramBotClient client, Update update)
		{
			await client.SendTextMessageAsync(
				chatId: messagePollId,
				text: "🏁 Test yakunlandi!\n\n" +
				"Siz " + counter + " ta savolga javob berdingiz:\n\n" +
				"✅ To'g'ri - " + answerCounter +
				"\n❌ Xato - " + (counter - answerCounter) +
				"\n\n<b>Bu testda yana qatnashib o'z bilimingizni yanada mustahkamlashingiz mumkin</b>😊",
				parseMode: ParseMode.Html);
		}

		bool CheckingIsAnswered(PollOption[] options)
		{
			for (int i = 0; i < options.Length; i++)
				if (options[i].VoterCount > 0)
				{
					if (correctAnswers[counter - 1] == i)
						answerCounter++;

					return true;
				}
			return false;
		}

		TelegramUser FindUser(Update update)
		{
			List<TelegramUser> list = ReadingFromUsers();

			userId = update.Message.From.Id;

			return list.Find(x => x.Id == userId);
		}

		async Task AskingToStart(ITelegramBotClient client, Update update)
		{
			await client.SendTextMessageAsync(
				chatId: update.Message.Chat.Id,
				text: "Quiz botga xush kelibsiz 😊\nFoydalanishni boshlashdan avval iltimos ro'yxatdan o'ting 😊");

			await client.SendTextMessageAsync(
				chatId: update.Message.Chat.Id,
				text: "Ismingizni kiriting 😊");

			isStarted = true;
		}

		async Task OnSendMessageAsync(ITelegramBotClient client, Update update)
		{
			questions = new List<string>();
			answers = new List<string[]>();
			correctAnswers = new List<int>();
			users = ReadingFromUsers();
			counter = 0;
			name = "";

			if (update.Message.Text is "/start" || update.Message.Text is "/restart")
			{
				TelegramUser tempUser = FindUser(update);
				
				if (tempUser is null)
					 await AskingToStart(client, update);
				else
					await ChoosingSubjectAsync(client, update, true);

				isStarted = true;
			}
			else if(update.Message.Text is "/my_profile")
			{
				TelegramUser telegramUser = FindUser(update);

				if (telegramUser is null)
				{
					await AskingToStart(client, update);
				}
				else
				{
					await client.SendTextMessageAsync(
						chatId: update.Message.Chat.Id,
						text: "<b>😊 Ismingiz</b> : " + telegramUser.FirstName +
						"\n📞 <b>Telefon raqamingiz</b> : " + telegramUser.TellNumber +
						"\n\n<b>Eng yaxshi natijalaringiz</b>😊\n" +
						"\n💻 <b>Informatika</b> : " + telegramUser.InformaticsAnswers +
						"\n📕 <b>Ona tili</b> : " + telegramUser.MothTongAnswers +
						"\n🛕 <b>Tarix</b> : " + telegramUser.HistoryAnswers +
						"\n👨‍💻 <b>Dasturlash</b> : " + telegramUser.ProgramingAnswers,
						parseMode: ParseMode.Html);
				}
			}
			else
			{
				name = update.Message.Text;

				await client.SendTextMessageAsync(
					chatId: update.Message.Chat.Id,
					text: "Iltimos, telefon raqamingizni kiriting 😊",
					replyMarkup: new ReplyKeyboardMarkup(KeyboardButton.WithRequestContact("📞 Mening telefon raqamim"))
					{
						ResizeKeyboard = true,
						OneTimeKeyboard = true
					});
			}
		}

		async Task ChoosingSubjectAsync(ITelegramBotClient client, Update update, bool isFound)
		{
			if (!isFound)
			{
				TelegramUser user = new TelegramUser()
				{
					FirstName = name,
					UserName = update.Message.From.Username,
					Id = update.Message.From.Id,
					TellNumber = update.Message.Contact.PhoneNumber
				};

				users = ReadingFromUsers();

				users.Add(user);

				WritingToUsers(users);
			}

				await client.SendTextMessageAsync(
					chatId: update.Message.Chat.Id,
					text: "Bilimingizni sinashni qaysi fandan boshlaysiz 😊",
					replyMarkup: new InlineKeyboardMarkup(
					new InlineKeyboardButton[][]
					{
					new InlineKeyboardButton[]
					{
						InlineKeyboardButton.WithCallbackData("Informatika 💻", "Informatics")
					},
					//new InlineKeyboardButton[]
					//{
					//	InlineKeyboardButton.WithCallbackData("Dasturlash 👨‍💻", "Programming")
					//},
					new InlineKeyboardButton[]
					{
						InlineKeyboardButton.WithCallbackData("Tarix 🛕", "History")

					},
					new InlineKeyboardButton[]
					{
						InlineKeyboardButton.WithCallbackData("Ona tili 📕", "MothTong")
					}
					}));
		}

		async Task StartingTestAsync(ITelegramBotClient client, Update update, string subject, CancellationToken token)
		{
			if (isStarted)
			{
				await Notification(client, update);
				
				subjectName = $"{subject}Answers";

				using (StreamReader reader = new StreamReader($"C:\\My works\\PDP_c#\\QuizBot\\{subject}Questions.json"))
				{
					string jsonData = reader.ReadToEnd();

					questions = JsonSerializer.Deserialize<List<string>>(jsonData);
				}

				using (StreamReader reader = new StreamReader($"C:\\My works\\PDP_c#\\QuizBot\\{subject}Answers.json"))
				{
					string jsonData = reader.ReadToEnd();

					answers = JsonSerializer.Deserialize<List<string[]>>(jsonData);
				}

				using (StreamReader reader = new StreamReader($"C:\\My works\\PDP_c#\\QuizBot\\{subject}CorrectAnswers.json"))
				{
					string jsonData = reader.ReadToEnd();

					correctAnswers = JsonSerializer.Deserialize<List<int>>(jsonData);
				}
			}

			Message message =  await client.SendPollAsync(
				chatId: (update.CallbackQuery is not null) ? update.CallbackQuery.Message.Chat.Id : messagePollId,
				question: questions[counter],
				options: answers[counter],
				type: PollType.Quiz,
				correctOptionId: correctAnswers[counter]);

			messagePollId = message.Chat.Id;
			counter++;
		}

		async Task Notification(ITelegramBotClient client, Update update)
		{
			isStarted = false;

			Message message = await client.SendTextMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				text: "3️⃣");
			Thread.Sleep(1000);

			await client.DeleteMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				messageId: message.MessageId);

			message = await client.SendTextMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				text: "2️⃣ Tayyormisiz");
			Thread.Sleep(1000);

			await client.DeleteMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				messageId: message.MessageId);

			message = await client.SendTextMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				text: "1️⃣ <b>Boshlanmoqda</b>",
				parseMode: ParseMode.Html);
			Thread.Sleep(1000);

			await client.DeleteMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				messageId: message.MessageId);

			message = await client.SendTextMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				text: "<b> Ketdik 🚀</b>",
				parseMode: ParseMode.Html);
			Thread.Sleep(1000);

			await client.DeleteMessageAsync(
				chatId: update.CallbackQuery.Message.Chat.Id,
				messageId: message.MessageId);
		}
	}
}