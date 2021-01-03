﻿using System;

namespace MapleServer2.Types
{
    public class Hotbar
    {
        public const int MAX_SLOTS = 25;
        public QuickSlot[] Slots { get; private set; }

        public Hotbar()
        {
            Slots = new QuickSlot[MAX_SLOTS];

            for (int i = 0; i < MAX_SLOTS; i++)
            {
                Slots[i] = new QuickSlot();
            }
        }

        public void MoveQuickSlot(int targetSlotIndex, QuickSlot quickSlot)
        {
            if (targetSlotIndex < 0 || targetSlotIndex >= MAX_SLOTS)
            {
                // This should never occur
                throw new ArgumentException($"Invalid target slot {targetSlotIndex}");
            }

            int sourceSlotIndex = FindQuickSlotIndex(quickSlot.SkillId, quickSlot.ItemUid);
            if (sourceSlotIndex != -1)
            {
                // Swapping with an existing slot on the hotbar
                QuickSlot sourceQuickSlot = Slots[targetSlotIndex];
                Slots[sourceSlotIndex] = QuickSlot.From(
                    sourceQuickSlot.SkillId,
                    sourceQuickSlot.ItemId,
                    sourceQuickSlot.ItemUid
                );
            }

            Slots[targetSlotIndex] = quickSlot;
        }

        private int FindQuickSlotIndex(int skillId, long itemUid = 0)
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                QuickSlot currentSlot = Slots[i];
                if (currentSlot.SkillId == skillId && currentSlot.ItemUid == itemUid)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool RemoveQuickSlot(int skillId, long itemUid)
        {
            int targetSlotIndex = FindQuickSlotIndex(skillId, itemUid);
            if (targetSlotIndex < 0 || targetSlotIndex >= MAX_SLOTS)
            {
                // TODO - There is either a) hotbar desync or b) something unintended occuring
                return false;
            }

            Slots[targetSlotIndex] = new QuickSlot(); // Clear
            return true;
        }
    }
}
