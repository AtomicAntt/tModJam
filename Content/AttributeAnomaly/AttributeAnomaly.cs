using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZZZMod.Content.AttributeAnomaly
{
    public class AttributeAnomaly : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public int[] previousBuffs = new int[20]; // I know that max buffs is 20
        public int[] previousBuffTimes = new int[20];

        public int[] buffCounter = new int[20]; // This signifies the amount of times the same buff is applied on each buff slot.

        public override void AI(NPC npc)
        {
            for (int i = 0; i < npc.buffType.Length; i++) // Check NPC Buffs for new ones
            {
                if (npc.buffType[i] != 0 && previousBuffs[i] == 0)
                {
                    Main.NewText($"{npc.FullName} just got this buff: {Lang.GetBuffName(npc.buffType[i])}, ID: {npc.buffType[i]}");
                    if (npc.buffType[i] == BuffID.OnFire)
                    {
                        Main.NewText($"On fire detected");
                    }
                }

                if (npc.buffType[i] != 0 && previousBuffs[i] != 0 && previousBuffTimes[i] < npc.buffTime[i]) // If the debuff time increases, we can assume it is reapplied.
                {
                    buffCounter[i]++;
                    Main.NewText($"{npc.FullName} is hit with this buff again: {Lang.GetBuffName(npc.buffType[i])}. It has been {1 + buffCounter[i]} times so far!");
                    if (buffCounter[i] >= 9)
                    {
                        buffCounter[i] = 0;
                        Explode(npc, npc.buffTime[i], npc.buffType[i]);
                    }
                }

                if (npc.buffType[i] == 0) // If a buff doesn't exist anymore, then the counter would disappear.
                {
                    buffCounter[i] = 0;
                }
            }
     
            // Update previous buffs & times
            npc.buffType.CopyTo(previousBuffs, 0);
            npc.buffTime.CopyTo(previousBuffTimes, 0);
        }

        public void Explode(NPC npc, int remainingBuff, int givenBuffID)
        {
            Vector2 center = npc.Center;
            int buffDamage = ReturnBuffDPS(givenBuffID);

            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(center, DustID.CursedTorch, velocity);
                dust.scale = 3.5f;
                dust.noGravity = true;
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, velocity);
                dust.scale = 2.5f;
                dust.noGravity = true;
            }

            npc.StrikeNPC(npc.CalculateHitInfo(4 * remainingBuff/60, 1));
            Main.NewText($"Inflicted attribute anomaly! Did {buffDamage} * {remainingBuff/60} = {buffDamage * remainingBuff/60} damage!");

            SoundEngine.PlaySound(SoundID.Item14, center);
        }

        public int ReturnBuffDPS(int givenBuffID)
        {
            switch (givenBuffID)
            {
                case BuffID.Poisoned:
                    return 2;
                case BuffID.OnFire:
                    return 4;
                case BuffID.Frostburn:
                    return 6;
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return 15;
                case BuffID.ShadowFlame:
                    return 15;
                case BuffID.CursedInferno:
                    return 24;
                case BuffID.Frostburn2:
                    return 25;
                default:
                    return 0;
            }
        }
    }
}
