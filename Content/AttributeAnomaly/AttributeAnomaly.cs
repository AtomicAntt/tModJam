using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
        public Dictionary<int, int> previousBuffCounters = new(); // Buff type : # of times debuff applied (which is the counter). Purpose: in case debuffs run out but you still want to apply the attribute anomaly.

        public Dictionary<int, float> anomalyProgress = new(); // Buff ID : progress ratio

        public override void AI(NPC npc)
        {
            for (int i = 0; i < npc.buffType.Length; i++) // Check NPC Buffs for new ones
            {
                if (npc.buffType[i] != 0 && previousBuffs[i] == 0) // New buff in town
                {
                    if (previousBuffCounters.ContainsKey(npc.buffType[i])) // If it was a buff previously applied but expired, inherit the old counter value!
                    {
                        buffCounter[i] = previousBuffCounters[npc.buffType[i]];
                        previousBuffCounters.Remove(npc.buffType[i]);
                    }

                    buffCounter[i]++;
                    //Main.NewText($"{npc.FullName} just got this buff: {Lang.GetBuffName(npc.buffType[i])}, ID: {npc.buffType[i]} Counter: {buffCounter[i]}");

                    if (buffCounter[i] >= ReturnAnomalyBuildup(npc.buffType[i]))
                    {
                        buffCounter[i] = 0;
                        Explode(npc, npc.buffTime[i], npc.buffType[i]);
                    }
                }

                if (npc.buffType[i] != 0 && previousBuffs[i] != 0 && previousBuffTimes[i] < npc.buffTime[i]) // If the debuff time increases, we can assume it is reapplied.
                {
                    buffCounter[i]++;
                    //Main.NewText($"{npc.FullName} is hit with this buff again: {Lang.GetBuffName(npc.buffType[i])}. It has been {buffCounter[i]} times so far!");
                    if (buffCounter[i] >= ReturnAnomalyBuildup(npc.buffType[i]))
                    {
                        buffCounter[i] = 0;
                        Explode(npc, npc.buffTime[i], npc.buffType[i]);
                    }
                }

                if (npc.buffType[i] == 0 && previousBuffs[i] != 0) // If a buff doesn't exist anymore, then the counter would disappear. HOWEVER, we will also store the counter for when the player wants to start anomaly buildup again on the same element.
                {
                    previousBuffCounters[previousBuffs[i]] = buffCounter[i];
                    buffCounter[i] = 0;
                }
            }
     
            // Update previous buffs & times
            npc.buffType.CopyTo(previousBuffs, 0);
            npc.buffTime.CopyTo(previousBuffTimes, 0);

            // Update the anomalyProgress dictionary which a UI will use to display progress to anomaly buildup
            anomalyProgress.Clear();
            for (int i = 0; i < npc.buffType.Length; i++)
            {
                if (npc.buffType[i] != 0)
                {
                    int buildupNeeded = ReturnAnomalyBuildup(npc.buffType[i]);
                    if (buildupNeeded > 0)
                    {
                        anomalyProgress[npc.buffType[i]] = buffCounter[i] / (float)buildupNeeded;
                    }
                }
            }
        }

        public void Explode(NPC npc, int remainingBuff, int givenBuffID)
        {
            Vector2 center = npc.Center;
            int buffDamage = ReturnBuffDPS(givenBuffID);
            int anomalyMultiplier = ReturnAnomalyMultiplier(givenBuffID);

            int totalDamage = anomalyMultiplier * buffDamage * remainingBuff / 60;

            for (int i = 0; i < 80; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(center, ReturnTorchType(givenBuffID), velocity);
                dust.scale = 4.5f;
                dust.noGravity = true;
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, velocity);
                dust.scale = 2.5f;
                dust.noGravity = true;
            }

            ActivateAnomalyEffect(npc, givenBuffID, totalDamage);

            npc.StrikeNPC(npc.CalculateHitInfo(totalDamage, 1));
            //Main.NewText($"Inflicted attribute anomaly! Did {anomalyMultiplier} * {buffDamage} * {remainingBuff/60} = {totalDamage} damage!");

            float disorderCalc = 0;
            int totalDisorder = 0;

            for (int i = 0; i < buffCounter.Length; i++) // After activating an anomaly effect, I want to see if any other anomaly buildups are tracked, and then add their potential damages as disorder!
            {
                if (buffCounter[i] > 0)
                {
                    int buffTypeToCalculate = npc.buffType[i];
                    // in short, this is the damage it would deal if we applied the attribute anomaly early, accounting for the amount of anomaly buildup so far
                    disorderCalc += (float)ReturnAnomalyMultiplier(buffTypeToCalculate) * (float)ReturnBuffDPS(buffTypeToCalculate) * ((float)npc.buffTime[i]/60) * ((float)buffCounter[i] / (float)ReturnAnomalyBuildup(buffTypeToCalculate));
                    buffCounter[i] = 0;
                }
            }
            disorderCalc *= 4f; // bonus damage (everyone likes that)
            totalDisorder = (int)disorderCalc;

            if (totalDisorder > 0)
            {
                //Main.NewText($"Inflicted disrder! Did {totalDisorder} damage!");
                npc.StrikeNPC(npc.CalculateHitInfo(totalDisorder, 1));
                CombatText.NewText(
                    npc.Hitbox,
                    Color.AntiqueWhite,
                    "Disorder!",
                    dramatic: true
                    );
                CombatText.NewText(
                    npc.Hitbox,
                    Color.AntiqueWhite,
                    totalDisorder + "!",
                    dramatic: true
                    );
            }
            else
            {
                Main.NewText($"Disorder calc = {disorderCalc}");
            }

            SoundEngine.PlaySound(SoundID.Item14, center);
        }

        public static int ReturnBuffDPS(int givenBuffID) // Manually searched these values on the terraria wiki
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
                case BuffID.Frostburn2: // frostbite
                    return 25;
                case BuffID.Venom:
                    return 30;
                case BuffID.Daybreak:
                    return 100;
                default:
                    return 0;
            }
        }

        public static int ReturnAnomalyMultiplier(int givenBuffID) // It's a damage multiplier
        {
            switch (givenBuffID)
            {
                case BuffID.Poisoned:
                    return 4;
                case BuffID.OnFire:
                    return 3;
                case BuffID.Frostburn:
                    return 3;
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return 3;
                case BuffID.ShadowFlame:
                    return 2;
                case BuffID.CursedInferno:
                    return 2;
                case BuffID.Frostburn2: // frostbite
                    return 2;
                case BuffID.Venom:
                    return 2;
                case BuffID.Daybreak:
                    return 2;
                default:
                    return 0;
            }
        }

        public static int ReturnAnomalyBuildup(int givenBuffID) // Amount of times a buff must be applied to inflict anomaly attribute dmg
        {
            switch (givenBuffID) 
            {
                case BuffID.Poisoned:
                    return 5;
                case BuffID.OnFire:
                    return 5;
                case BuffID.Frostburn:
                    return 5;
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return 10;
                case BuffID.ShadowFlame:
                    return 10;
                case BuffID.CursedInferno:
                    return 10;
                case BuffID.Frostburn2: // frostbite
                    return 10;
                case BuffID.Venom:
                    return 10;
                case BuffID.Daybreak:
                    return 10;
                default:
                    return 0;
            }
        }

        public static int ReturnTorchType(int givenBuffID)
        {
            switch (givenBuffID)
            {
                case BuffID.Poisoned:
                    return DustID.Poisoned;
                case BuffID.OnFire:
                    return DustID.Torch;
                case BuffID.Frostburn:
                    return DustID.IceTorch;
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return DustID.Torch;
                case BuffID.ShadowFlame:
                    return DustID.Shadowflame;
                case BuffID.CursedInferno:
                    return DustID.CursedTorch;
                case BuffID.Frostburn2: // frostbite
                    return DustID.IceTorch;
                case BuffID.Venom:
                    return DustID.PurpleTorch;
                case BuffID.Daybreak:
                    return DustID.IchorTorch;
                default:
                    return 0;
            }
        }

        public static Color ReturnAnomalyColor(int givenBuffID)
        {
            switch (givenBuffID)
            {
                case BuffID.Poisoned:
                    return Color.YellowGreen;
                case BuffID.OnFire:
                    return Color.Orange;
                case BuffID.Frostburn:
                    return Color.LightBlue;
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return Color.OrangeRed;
                case BuffID.ShadowFlame:
                    return Color.MediumPurple;
                case BuffID.CursedInferno:
                    return Color.GreenYellow;
                case BuffID.Frostburn2: // frostbite
                    return Color.SkyBlue;
                case BuffID.Venom:
                    return Color.Purple;
                case BuffID.Daybreak:
                    return Color.Yellow;
                default:
                    return Color.Red;
            }
        }

        public static string ReturnAnomalyString(int givenBuffID)
        {
            switch (givenBuffID)
            {
                case BuffID.Poisoned:
                    return "Poisoned";
                case BuffID.OnFire:
                    return "On Fire";
                case BuffID.Frostburn:
                    return "Frostburn";
                case BuffID.OnFire3: // hellfire is apparently onfire3, and there's no onfire2 lol
                    return "Hellfire";
                case BuffID.ShadowFlame:
                    return "ShadowFlame";
                case BuffID.CursedInferno:
                    return "Cursed Inferno";
                case BuffID.Frostburn2: // frostbite
                    return "Frostbite";
                case BuffID.Venom:
                    return "Acid Venom";
                case BuffID.Daybreak:
                    return "Daybreak";
                default:
                    return "error: developer did not account for this anomaly :(";
            }
        }

        public void ActivateAnomalyEffect(NPC npc, int givenBuffID, int totalDamage)
        {

            CombatText.NewText(
                npc.Hitbox,
                ReturnAnomalyColor(givenBuffID),
                ReturnAnomalyString(givenBuffID)+"!",
                dramatic: true
                );
            CombatText.NewText(
                npc.Hitbox,
                ReturnAnomalyColor(givenBuffID),
                totalDamage+"!",
                dramatic: true
                );

            switch(givenBuffID)
            {
                case BuffID.OnFire:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Top - new Vector2(0, 60), // unfortunately, ball of fire does not pierce enemies so im spawning them on top of the enemy
                            velocity,
                            ProjectileID.BallofFire,
                            ReturnBuffDPS(givenBuffID), // 1 second of damage dealt
                            0f, // knockback
                            Main.myPlayer
                        );
                    }
                    break;
                case BuffID.Frostburn:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Top - new Vector2(0, 60), // unfortunately, ball of frost does not pierce enemies so im spawning them on top of the enemy
                            velocity,
                            ProjectileID.BallofFrost, // funny enough, this gives frostbite
                            ReturnBuffDPS(givenBuffID), // 1 second of damage dealt
                            0f, // knockback
                            Main.myPlayer
                        );
                    }
                    break;
                case BuffID.OnFire3:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Top - new Vector2(0, 60), // unfortunately, ball of fire does not pierce enemies so im spawning them on top of the enemy
                            velocity,
                            ProjectileID.BallofFire,
                            ReturnBuffDPS(givenBuffID), // 1 second of damage dealt
                            0f, // knockback
                            Main.myPlayer
                        );
                    }
                    break;
                case BuffID.CursedInferno:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Center,
                            velocity,
                            ProjectileID.CursedFlameFriendly,
                            ReturnBuffDPS(givenBuffID), // 1 second of damage dealt
                            0f, // knockback
                            Main.myPlayer
                        );
                    }
                    break;
                case BuffID.Frostburn2:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Top - new Vector2(0, 60),
                            velocity,
                            ProjectileID.BallofFrost, // unfortunately, ball of frost does not pierce enemies so im spawning them on top of the enemy
                            ReturnBuffDPS(givenBuffID), // 1 second of damage dealt
                            0f, // knockback
                            Main.myPlayer
                        );
                    }
                    break;
            }

        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (anomalyProgress.Count > 0)
            {
                Vector2 barPosition = npc.Top - new Vector2(0, 60) - screenPos;
                foreach (var kv in anomalyProgress)
                {
                    float ratio = kv.Value;
                    int givenBuffID = kv.Key;
                    DrawProgressBar(spriteBatch, barPosition, ratio, ReturnAnomalyColor(givenBuffID));
                    barPosition.Y -= 10; // stack multiple bars
                }
            }

            return true;
        }

        private void DrawProgressBar(SpriteBatch spriteBatch, Vector2 position, float progress, Color color)
        {
            int width = 40;
            int height = 6;
            Texture2D blank = TextureAssets.MagicPixel.Value;

            Rectangle bgRect = new((int)(position.X - width / 2), (int)(position.Y - height / 2), width, height);
            spriteBatch.Draw(blank, bgRect, Color.Black * 0.5f);

            Rectangle fgRect = new(bgRect.X, bgRect.Y, (int)(width * progress), height);
            spriteBatch.Draw(blank, fgRect, color);
        }
    }
}
