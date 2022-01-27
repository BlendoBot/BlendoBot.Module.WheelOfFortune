using BlendoBot.Core.Messages;
using BlendoBot.Core.Module;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace BlendoBot.Module.WheelOfFortune;

internal class WheelOfFortuneListener : IMessageListener {
	private readonly WheelOfFortune module;

	public WheelOfFortuneListener(WheelOfFortune module) {
		this.module = module;
	}

	public IModule Module => module;

	public async Task OnMessage(MessageCreateEventArgs e) {
		await module.HandleMessageListener(e);
	}
}
