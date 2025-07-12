using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace ZZZMod.Content.AttributeAnomaly
{
    public class AttributeAnomaly : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public int[] previousBuffs = new int[20]; // I know that max buffs is 20
        public int[] previousBuffTimes = new int[20];

        public override void AI(NPC npc)
        {
            for (int i = 0; i < npc.buffType.Length; i++) // Check NPC Buffs for new ones
            {
                if (npc.buffType[i] != 0 && previousBuffs[i] == 0)
                {
                    Main.NewText($"{npc.FullName} just got this buff: {Lang.GetBuffName(npc.buffType[i])}");
                }

                if (npc.buffType[i] != 0 && previousBuffs[i] != 0 && previousBuffTimes[i] < npc.buffTime[i]) // If the debuff time increases, we can assume it is reapplied.
                {
                    Main.NewText($"{npc.FullName} is hit with this buff again: {Lang.GetBuffName(npc.buffType[i])}");
                }
            }
     
            // Update previous buffs & times
            npc.buffType.CopyTo(previousBuffs, 0);
            npc.buffTime.CopyTo(previousBuffTimes, 0);
        }
    }
}
