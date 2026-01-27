using KindredCommands.Data;
using ProjectM;
using ProjectM.Shared;
using VampireCommandFramework;

namespace KindredCommands.Commands;
internal class AFKCommands
{
	[Command("afk", description: "AFK animation, locking WASD movement. Use again to remove.", adminOnly: false)]
	public static void AFKCommand(ChatCommandContext ctx)
	{
		//"AB_Bear_FallAsleep_SleepingIdle_Buff": -883762685 - gives the ZZzzz animation and locks player in place, spells still work
		if (!BuffUtility.TryGetBuff(Core.EntityManager, ctx.Event.SenderCharacterEntity, Prefabs.AB_Bear_FallAsleep_SleepingIdle_Buff, out var buffEntity))
		{
			Buffs.TryAddBuff(ctx.Event.SenderUserEntity, ctx.Event.SenderCharacterEntity, Prefabs.AB_Bear_FallAsleep_SleepingIdle_Buff, -1, false);
			ctx.Reply("You are now AFK!");
		}
		else
		{
			DestroyUtility.Destroy(Core.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
			ctx.Reply("You are no longer AFK!");
		}
	}
}
