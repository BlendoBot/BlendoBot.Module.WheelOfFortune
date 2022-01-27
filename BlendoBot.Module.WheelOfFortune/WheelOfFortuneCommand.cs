using BlendoBot.Core.Command;
using BlendoBot.Core.Entities;
using BlendoBot.Core.Module;
using BlendoBot.Core.Utility;
using DSharpPlus.EventArgs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlendoBot.Module.WheelOfFortune;

internal class WheelOfFortuneCommand : ICommand {
	public WheelOfFortuneCommand(WheelOfFortune module) {
		this.module = module;
	}

	private readonly WheelOfFortune module;
	public IModule Module => module;

	public string Guid => "wheeloffortune.command";
	public string DesiredTerm => "wof";
	public string Description => "Play a round of the Second Guess puzzle.";
	public Dictionary<string, string> Usage => new() {
		{ string.Empty, $"Triggers a new puzzle. Only one puzzle can be active at a time on the server. Afterwards, the next message each person types in the channel will be interpreted as an answer for the game. Type the correct answer to win! Games last 30 seconds. You only get one chance. Answers can be case insensitive and can lack punctuation (e.g. {"MY LEFT-LEG".Code()} can be matched by typing {"myleftleg".Code()})." }
	};
		
	public async Task OnMessage(MessageCreateEventArgs e, string[] tokenizedInput) {
		await module.Semaphore.WaitAsync();
		if (module.CurrentChannel != null) {
			await module.DiscordInteractor.Send(this, new SendEventArgs {
				Message = $"A game is already in session in {module.CurrentChannel.Mention}, please wait until it has finished!",
				Channel = e.Channel,
				Tag = "WheelOfFortuneGameInProgress"
			});
		} else {
			module.CurrentChannel = e.Channel;
			await Task.Factory.StartNew(() => module.StartGame(e.Channel));
		}
		module.Semaphore.Release();
	}
}
