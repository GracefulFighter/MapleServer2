﻿using Maple2Storage.Types.Metadata;
using MapleServer2.Commands.Core;
using MapleServer2.Data.Static;
using MapleServer2.Database;
using MapleServer2.Enums;
using MapleServer2.PacketHandlers.Game;
using MapleServer2.Packets;
using MapleServer2.Types;

namespace MapleServer2.Commands.Game;

public class UnlockAll : InGameCommand
{
    public UnlockAll()
    {
        Aliases = new()
        {
            "unlock"
        };
        Description = "Unlocks a bunch of emotes, stickers, and titles!";
        Usage = "/unlock";
    }

    public override void Execute(GameCommandTrigger trigger)
    {
        Player player = trigger.Session.Player;

        // Reset stats to default
        player.Stats = new(player.Job);
        player.Stats.AddBaseStats(player, 90);

        trigger.Session.Send(StatPacket.SetStats(player.FieldPlayer));
        trigger.Session.FieldManager.BroadcastPacket(StatPacket.SetStats(player.FieldPlayer), trigger.Session);

        player.Levels.SetLevel(90);
        player.Levels.SetPrestigeLevel(100);
        player.Wallet.Meso.SetAmount(10_000_000_000); // 10B
        player.Account.Meret.SetAmount(10_000_000_000); // 10B

        PremiumClubHandler.ActivatePremium(trigger.Session, 2592000); // 30 days in seconds

        // Stickers
        for (int i = 1; i < 7; i++)
        {
            if (player.ChatSticker.Any(x => x.GroupId == i))
            {
                continue;
            }

            trigger.Session.Send(ChatStickerPacket.AddSticker(21100000 + i, i, long.MaxValue));
            player.ChatSticker.Add(new((byte) i, long.MaxValue));
        }

        List<int> emotes = SkillMetadataStorage.GetEmotes();
        foreach (int emoteId in emotes)
        {
            // Broken emotes
            if (emoteId is >= 90200032 and <= 90200036)
            {
                continue;
            }

            if (player.Emotes.Contains(emoteId))
            {
                continue;
            }

            player.Emotes.Add(emoteId);

            trigger.Session.Send(EmotePacket.LearnEmote(emoteId));
        }

        List<TitleMetadata> titles = TitleMetadataStorage.GetAll();
        foreach (TitleMetadata title in titles)
        {
            int titleId = title.Id;
            if (player.Titles.Contains(titleId))
            {
                continue;
            }

            player.Titles.Add(titleId);

            trigger.Session.Send(UserEnvPacket.AddTitle(titleId));
        }

        DatabaseManager.Characters.Update(player);
        trigger.Session.Send(NoticePacket.Notice("Done!", NoticeType.Chat));
    }
}

public class UnlockTrophyCommand : InGameCommand
{
    public UnlockTrophyCommand()
    {
        Aliases = new()
        {
            "trophy"
        };
        Description = "Unlock an trophy!";
        Parameters = new()
        {
            new Parameter<int>("trophyId", "The trophy id to unlock;"),
            new Parameter<int>("amount", "The amount of trophy goals.", 1)
        };
        Usage = "/trophy [trophyId] [amount]";
    }

    public override void Execute(GameCommandTrigger trigger)
    {
        Player player = trigger.Session.Player;

        int trophyId = trigger.Get<int>("trophyId");
        int amount = trigger.Get<int>("amount");
        if (trophyId == 0)
        {
            trigger.Session.Send(NoticePacket.Notice("Type an trophy id!", NoticeType.Chat));
            return;
        }

        if (!player.TrophyData.ContainsKey(trophyId))
        {
            player.TrophyData[trophyId] = new(player.CharacterId, player.AccountId, trophyId);
        }

        player.TrophyData[trophyId].AddCounter(trigger.Session.Player, amount);

        player.TrophyData.TryGetValue(trophyId, out Trophy trophy);

        trigger.Session.Send(TrophyPacket.WriteUpdate(trophy));
        DatabaseManager.Trophies.Update(trophy);

        trigger.Session.Send(NoticePacket.Notice("Done!", NoticeType.Chat));
    }
}
