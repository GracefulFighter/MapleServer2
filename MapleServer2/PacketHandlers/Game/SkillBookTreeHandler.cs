﻿using MaplePacketLib2.Tools;
using MapleServer2.Constants;
using MapleServer2.Database;
using MapleServer2.Packets;
using MapleServer2.Servers.Game;
using MapleServer2.Types;

namespace MapleServer2.PacketHandlers.Game;

public class SkillBookTreeHandler : GamePacketHandler<SkillBookTreeHandler>
{
    public override RecvOp OpCode => RecvOp.RequestSkillBookTree;

    private enum Mode : byte
    {
        Open = 0x00,
        Save = 0x01,
        Rename = 0x02,
        AddTab = 0x04
    }

    public override void Handle(GameSession session, PacketReader packet)
    {
        Mode mode = (Mode) packet.ReadByte();
        switch (mode)
        {
            case Mode.Open:
                HandleOpen(session);
                break;
            case Mode.Save:
                HandleSave(session, packet);
                break;
            case Mode.Rename:
                HandleRename(session, packet);
                break;
            case Mode.AddTab:
                HandleAddTab(session);
                break;
            default:
                LogUnknownMode(mode);
                break;
        }
    }

    private static void HandleOpen(GameSession session)
    {
        session.Send(SkillBookTreePacket.Open(session.Player));
    }

    private static void HandleSave(GameSession session, PacketReader packet)
    {
        Player player = session.Player;

        long activeTabId = packet.ReadLong();
        long selectedTab = packet.ReadLong(); // if 0 player used activate tab
        int unknown = packet.ReadInt();
        int tabCount = packet.ReadInt();
        for (int i = 0; i < tabCount; i++)
        {
            long tabId = packet.ReadLong();
            string tabName = packet.ReadUnicodeString();

            SkillTab skillTab = player.SkillTabs.FirstOrDefault(x => x.TabId == tabId);
            if (skillTab is null)
            {
                skillTab = new(player.CharacterId, player.Job, player.JobCode, tabId, tabName);
                player.SkillTabs.Add(skillTab);
            }
            else
            {
                skillTab = player.SkillTabs[i];
                skillTab.TabId = tabId;
                skillTab.Name = tabName;
            }

            // Count of skills that were added to the tab, doesn't show skills that were removed
            int skillCount = packet.ReadInt();
            for (int j = 0; j < skillCount; j++)
            {
                int skillId = packet.ReadInt();
                int skillLevel = packet.ReadInt();

                // not sure what to do with this
            }
        }

        session.Player.ActiveSkillTabId = activeTabId;
        session.Send(SkillBookTreePacket.Save(session.Player, selectedTab));
        foreach (SkillTab skillTab in session.Player.SkillTabs)
        {
            DatabaseManager.SkillTabs.Update(skillTab);
        }

        if (selectedTab == 0 || selectedTab == activeTabId)
        {
            session.Player.UpdatePassiveSkills();
        }
    }

    private static void HandleRename(GameSession session, PacketReader packet)
    {
        long id = packet.ReadLong();
        string newName = packet.ReadUnicodeString();

        SkillTab skillTab = session.Player.SkillTabs.FirstOrDefault(x => x.TabId == id);
        if (skillTab is null)
        {
            return;
        }

        skillTab.Name = newName;
        session.Send(SkillBookTreePacket.Rename(id, newName));
    }

    private static void HandleAddTab(GameSession session)
    {
        if (!session.Player.Account.RemoveMerets(990))
        {
            return;
        }

        // check tab max count
        session.Send(SkillBookTreePacket.AddTab(session.Player));
    }
}
