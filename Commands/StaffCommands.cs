using System.Collections.Generic;
using System.Text;
using KindredCommands.Commands.Converters;
using KindredCommands.Models;
using KindredCommands.Services;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;

namespace KindredCommands.Commands;

internal class StaffCommands
{
	[Command("staff", description: "Shows online Staff members.", adminOnly: false)]
	public static void WhoIsOnline(ChatCommandContext ctx)
	{
		var users = Core.Players.GetUsersOnline();
		var staff = Database.GetStaff();

		StringBuilder builder = new();
		foreach (var user in users)
		{
			Player player = new(user);
			foreach (KeyValuePair<string, string> _kvp in staff)
			{
				if (player.SteamID.ToString() == _kvp.Key)
				{
					string _role = _kvp.Value.Replace("</color>", "");
					builder
						.Append(_role)
						.Append(player.Name)
						.Append("</color>");
					builder.Append(' ');
				}
			}
		}
		if (builder.Length == 0)
		{
			ctx.Reply("There are no staff members online.");
			return;
		}
		ctx.Reply($"Online Staff: {builder}");
	}
	[Command("reloadstaff", description: "Reloads the staff config.", adminOnly: true)]
	public static void ReloadStaff(ChatCommandContext ctx)
	{
		Database.InitConfig();
		ctx.Reply("Staff config reloaded!");
	}

	[Command("setstaff", description: "Sets someones staff rank.", adminOnly: true)]
	public static void AddStaff(ChatCommandContext ctx, FoundPlayer player, string rank)
	{
		var userEntity = player.Value.UserEntity;
		var rankname = "[" + rank + "]";

		Database.SetStaff(userEntity, rankname);
		ctx.Reply("Staff member set!");
	}

	[Command("removestaff", description: "Removes someones staff rank.", adminOnly: true)]
	public static void RemoveStaff(ChatCommandContext ctx, FoundPlayer player)
	{
		var userEntity = player.Value.UserEntity;

		if (Database.RemoveStaff(userEntity))
			ctx.Reply("Staff member removed!");
		else
			ctx.Reply("Staff member not found!");
	}

	public static AdminAuthSystem adminAuthSystem = Core.Server.GetExistingSystemManaged<AdminAuthSystem>();
	[Command("reloadadmin", description: "Reloads the admin list.", adminOnly: true)]
	public static void ReloadCommand(ChatCommandContext ctx)
	{
		adminAuthSystem._LocalAdminList.Save();
		adminAuthSystem._LocalAdminList.Refresh();
		ctx.Reply("Admin list reloaded!");
	}

	[Command("toggleadmin", description: "Adds/Removes a player to the admin list, authing and deauthing.", adminOnly: true)]
	public static void ToggleAdminCommand(ChatCommandContext ctx, FoundPlayer player)
	{
		var userEntity = player.Value.UserEntity;
		var user = userEntity.Read<User>();
		var platformId = user.PlatformId;

		if (adminAuthSystem._LocalAdminList.Contains(platformId))
		{
			ctx.Reply($"Admin deauthed for {player.Value.CharacterName}");
			adminAuthSystem._LocalAdminList.Remove(platformId);

			if (userEntity.Has<AdminUser>())
			{
				userEntity.Remove<AdminUser>();
			}

			user.IsAdmin = false;
			userEntity.Write(user);

			var archetype = Core.EntityManager.CreateArchetype(new ComponentType[]
			{
					ComponentType.ReadWrite<FromCharacter>(),
					ComponentType.ReadWrite<DeauthAdminEvent>()
			});

			var entity = Core.EntityManager.CreateEntity(archetype);
			entity.Write(new FromCharacter()
			{
				Character = player.Value.CharEntity,
				User = userEntity
			});

			FixedString512Bytes message = "You were removed as admin and deauthed";
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, ref message);

		}
		else
		{
			ctx.Reply($"Admin authed for {player.Value.CharacterName}");
			adminAuthSystem._LocalAdminList.Add(platformId);

			AdminService.AdminUser(userEntity);

			FixedString512Bytes message = "You were added as admin and authed";
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, ref message);
		}

		adminAuthSystem._LocalAdminList.Save();
	}

	[Command("autoadminauth", description: "Adds/Removes yourself from the auto AdminAuth list", adminOnly: true)]
	public static void AddAutoAuthAdmin(ChatCommandContext ctx)
	{
		if (Database.GetAutoAdmin().Contains(ctx.Event.SenderUserEntity.Read<User>().PlatformId.ToString()))
		{
			Database.RemoveAutoAdmin(ctx.Event.SenderUserEntity);
			ctx.Reply("You will no longer be automatically AdminAuth'd on login.");
			return;
		}
		else
		{
			Database.SetAutoAdmin(ctx.Event.SenderUserEntity);
			ctx.Reply("You will be automatically AdminAuth'd on login.");
		}
	}
}
