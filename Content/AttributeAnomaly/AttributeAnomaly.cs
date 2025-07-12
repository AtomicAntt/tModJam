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

        public override void AI(NPC npc)
        {
            for (int i = 0; i < npc.buffType.Length; i++) // Check NPC Buffs for new ones
            {
                if (npc.buffType[i] != 0 && previousBuffs[i] == 0)
                {
                    Main.NewText($"{npc.FullName} just got this buff: {Lang.GetBuffName(npc.buffType[i])}");
                }
            }
     
            // Update previous buffs
            npc.buffType.CopyTo(previousBuffs, 0);
        }
    }
}
