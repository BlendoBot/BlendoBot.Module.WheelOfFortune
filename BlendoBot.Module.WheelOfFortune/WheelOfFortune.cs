using BlendoBot.Core.Entities;
using BlendoBot.Core.Module;
using BlendoBot.Core.Services;
using BlendoBot.Core.Utility;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BlendoBot.Module.WheelOfFortune;

[Module(Guid = "com.biendeo.blendobot.module.wheeloffortune", Name = "Wheel of Fortune", Author = "Biendeo", Version = "2.0.0", Url = "https://github.com/BlendoBot/BlendoBot.Module.WheelOfFortune")]
public class WheelOfFortune : IModule, IDisposable {
	public WheelOfFortune(IDiscordInteractor discordInteractor, IFilePathProvider filePathProvider, ILogger logger, IModuleManager moduleManager) {
		DiscordInteractor = discordInteractor;
		FilePathProvider = filePathProvider;
		Logger = logger;
		ModuleManager = moduleManager;

		WheelOfFortuneCommand = new WheelOfFortuneCommand(this);
	}

	internal ulong GuildId { get; private set; }

	internal readonly WheelOfFortuneCommand WheelOfFortuneCommand;

	internal readonly IDiscordInteractor DiscordInteractor;
	internal readonly IFilePathProvider FilePathProvider;
	internal readonly ILogger Logger;
	internal readonly IModuleManager ModuleManager;

	private static List<Puzzle> puzzles;

	private readonly Random random = new();
	internal DiscordChannel CurrentChannel;
	private List<DiscordUser> eliminatedUsers;
	private Puzzle currentPuzzle;
	private DiscordMessage lastWinningMessage;

	internal SemaphoreSlim Semaphore;

	public Task<bool> Startup(ulong guildId) {
		GuildId = guildId;

		CurrentChannel = null;
		eliminatedUsers = new List<DiscordUser>();
		Semaphore = new SemaphoreSlim(1);

		if (puzzles == null) {
			puzzles = new List<Puzzle>();
			if (File.Exists(Path.Combine(FilePathProvider.GetCommonDataDirectoryPath(this), "puzzles.txt"))) {
				using (FileStream file = File.OpenRead(Path.Combine(FilePathProvider.GetCommonDataDirectoryPath(this), "puzzles.txt"))) {
					using StreamReader reader = new(file);
					while (!reader.EndOfStream) {
						string line = reader.ReadLine();
						puzzles.Add(new Puzzle {
							Category = line.Split(";")[0],
							Phrase = line.Split(";")[1]
						});
					}
				}
				Logger.Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"Wheel Of Fortune loaded {puzzles.Count} puzzles"
				});
			} else {
				Logger.Log(this, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Wheel Of Fortune could not find {Path.Combine(FilePathProvider.GetCommonDataDirectoryPath(this), "puzzles.txt")}. Make sure that you have that file with words in the format: {"category;phrase"}."
				});
			}
		}
		return Task.FromResult(ModuleManager.RegisterCommand(this, WheelOfFortuneCommand, out _));
	}
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (disposing) {
			if (Semaphore != null) {
				Semaphore.Dispose();
			}
		}
	}

	internal async Task StartGame(DiscordChannel channel) {
		DiscordMessage message = await DiscordInteractor.Send(this, new SendEventArgs {
			Message = "Choosing a puzzle...",
			Channel = channel,
			Tag = "WheelOfFortuneGameStart"
		});

		await Semaphore.WaitAsync();
		CurrentChannel = channel;
		eliminatedUsers.Clear();
		currentPuzzle = puzzles[random.Next(0, puzzles.Count)];
		Semaphore.Release();

		for (int i = 5; i > 0; --i) {
			await message.ModifyAsync($"Game starting in {i} second{(i != 1 ? "s" : string.Empty)}...");
			await Task.Delay(1000);
		}

		WheelOfFortuneListener messageListener = new(this);
		ModuleManager.RegisterMessageListener(this, messageListener);

		string revealedPuzzle = currentPuzzle.Phrase.ToUpper();
		for (char c = 'A'; c <= 'Z'; ++c) {
			revealedPuzzle = revealedPuzzle.Replace(c, '˷');
		}

		await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());

		int timeToWait = 30000 / revealedPuzzle.Count(c => c == '˷');

		while (CurrentChannel != null && revealedPuzzle != currentPuzzle.Phrase.ToUpper()) {
			await Task.Delay(timeToWait);
			if (CurrentChannel != null) {
				bool replacedUnderscore = false;
				while (!replacedUnderscore) {
					int index = random.Next(0, revealedPuzzle.Length);
					if (revealedPuzzle[index] == '˷') {
						revealedPuzzle = revealedPuzzle[..index] + currentPuzzle.Phrase.ToUpper()[index] + revealedPuzzle[(index + 1)..];
						replacedUnderscore = true;
					}
				}
				await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());
			}
		}

		await Semaphore.WaitAsync();
		if (CurrentChannel != null) {
			await DiscordInteractor.Send(this, new SendEventArgs {
				Message = $"No one got the puzzle! The answer was {revealedPuzzle.Code()}. Thanks for playing!",
				Channel = channel,
				Tag = "WheelOfFortuneGameLose"
			});
		} else {
			await message.ModifyAsync($"{$"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock()}\n{lastWinningMessage.Author.Mention} got the correct answer {currentPuzzle.Phrase.ToUpper().Code()}");
		}

		CurrentChannel = null;
		eliminatedUsers.Clear();
		currentPuzzle = null;
		Semaphore.Release();

		ModuleManager.UnregisterMessageListener(this, messageListener);
	}

	internal async Task HandleMessageListener(MessageCreateEventArgs e) {
		await Semaphore.WaitAsync();
		if (CurrentChannel != null && e.Channel == CurrentChannel) {
			if (!eliminatedUsers.Contains(e.Author)) {
				Regex alphabetRegex = new("[^A-Z]");
				string messageText = alphabetRegex.Replace(e.Message.Content.ToUpper(), "");
				string expectedAnswer = alphabetRegex.Replace(currentPuzzle.Phrase.ToUpper(), "");
				if (messageText == expectedAnswer) {
					CurrentChannel = null;
					eliminatedUsers.Clear();
					lastWinningMessage = e.Message;
					await DiscordInteractor.Send(this, new SendEventArgs {
						Message = $"Congratulations to {e.Author.Mention} for getting the correct answer! Thanks for playing!",
						Channel = e.Channel,
						Tag = "WheelOfFortuneGameWin"
					});
				} else {
					eliminatedUsers.Add(e.Author);
					await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
				}
			}
		}
		Semaphore.Release();
	}
}
