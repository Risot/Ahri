using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EloBuddy;
using EloBuddy.SDK;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Menu;
using System.Threading.Tasks;

namespace iAhri
{
    public static class SpellMenu
    {
        public static bool SpellFarm;
        public static Menu mainMenu { get; set; }
        public static Menu comboMenu { get; set; }
        public static Menu jglMenu { get; set; }
        public static Menu miscMenu { get; set; }
        public static Menu harassMenu { get; set; }
        public static Menu clearMenu { get; set; }
        public static Menu farmMenu { get; set; }
        public static Menu killStealMenu { get; set; }
        public static Menu drawMenu { get; set; }
        public static Menu fleeMenu { get; set; }

        public static void AddLine(this Menu mainMenu, string spellName)
        {
            mainMenu.AddGroupLabel(spellName + " Settings");
        }

        public static void AddText(this Menu mainMenu, string disableName)
        {
            mainMenu.AddGroupLabel(disableName);
        }

        public static void AddBool(this Menu mainMenu, string name, string disableName, bool isEnabled = true)
        {
            mainMenu.Add(name, new CheckBox(disableName, isEnabled));
        }

        public static void AddSlider(this Menu mainMenu, string name, string disableName, int defalutValue = 0, int minValue = 0, int maxValue = 100)
        {
            mainMenu.Add(name, new Slider(disableName, defalutValue, minValue, maxValue));
        }

        public static void AddList(this Menu mainMenu, string name, string disableName, string[] list, int defaultIndex = 0)
        {
            mainMenu.Add(name, new ComboBox(disableName, list, defaultIndex));
        }

        public static void AddKey(this Menu mainMenu, string name, string disableName, KeyBind.BindTypes keyBindType, uint defaultKey1 = 27, bool isEnabled = false)
        {
            mainMenu.Add(name, new KeyBind(disableName, isEnabled, keyBindType, defaultKey1));
        }

        public static bool GetBool(this Menu mainMenu, string name)
        {
            return mainMenu[name].Cast<CheckBox>().CurrentValue;
        }

        public static bool GetKey(this Menu mainMenu, string name)
        {
            return mainMenu[name].Cast<KeyBind>().CurrentValue;
        }

        public static int GetSlider(this Menu mainMenu, string name)
        {
            return mainMenu[name].Cast<Slider>().CurrentValue;
        }

        public static int GetList(this Menu mainMenu, string name)
        {
            return mainMenu[name].Cast<ComboBox>().CurrentValue;
        }
        private static readonly string Author = "iCreative and Risto";
        public static void init()
        {
            mainMenu = MainMenu.AddMenu("iAhri", "iAhri", " by " + Author + "v1.15");
            {
                mainMenu.AddGroupLabel("All Credits to iCreative for this awesome script :)");
            }

            comboMenu = mainMenu.AddSubMenu("Combo", "Combo");
            {
                comboMenu.AddLine("Q");
                comboMenu.AddBool("ComboQ", "Use Q");
                comboMenu.AddLine("W");
                comboMenu.AddBool("ComboW", "Use W");
                comboMenu.AddLine("E");
                comboMenu.AddBool("ComboE", "Use E");
                comboMenu.AddLine("R");
                comboMenu.AddBool("ComboR", "Use R");
                comboMenu.AddSlider("minR", "Min Enemies To Use R", 5);
                comboMenu.AddBool("CatchQR", "Catch the Q with R");
                comboMenu.AddBool("CatchQRPriority", "Give Priority to Catch Q with R");
            }

            harassMenu = mainMenu.AddSubMenu("Harass", "Harass");
            {
                harassMenu.AddLine("Q");
                harassMenu.AddBool("HarassQ", "Use Q");
                harassMenu.AddLine("W");
                harassMenu.AddBool("HarassW", "Use W");
                harassMenu.AddLine("E");
                harassMenu.AddBool("HarassE", "Use E");
                harassMenu.AddSlider("minHarass", "Min. Mana Percent to Harass:", 30, 0, 100);
            }

            clearMenu = mainMenu.AddSubMenu("LaneClear", "LaneClear");
            {
                clearMenu.AddLine("Q");
                clearMenu.AddBool("ClearQ", "Use Q");
                clearMenu.AddSlider("minHit", "Minimum Minions To Hit:", 3, 0, 6);
                clearMenu.AddSlider("farmTillLvl", "Only AA until level:", 4, 1, 18);
                clearMenu.AddSlider("ClearMana", "Min. Mana Percentage:", 60, 0, 100);
                clearMenu.AddLine("W");
                clearMenu.AddBool("ClearW", "Use W");
                clearMenu.AddSeparator();
                clearMenu.AddBool("SpellFarm", "Spells farm (Mwheel toggle)");
            }

            jglMenu = mainMenu.AddSubMenu("JungleClear", "JungleClear");
            {
                jglMenu.AddSlider("jglMana", "Min. Mana Percentage:", 60, 0, 100);
                jglMenu.AddLine("Q");
                jglMenu.AddBool("jglQ", "Use Q in Jungle");
                jglMenu.AddLine("W");
                jglMenu.AddBool("jglW", "Use W in Jungle");
                jglMenu.AddLine("E");
                jglMenu.AddBool("jglE", "Use E in Jungle");
            }

            fleeMenu = mainMenu.AddSubMenu("Flee", "Flee");
            {
                fleeMenu.AddLine("Q");
                fleeMenu.AddBool("FleeQ", "Use Q to Flee");
                fleeMenu.AddLine("R");
                fleeMenu.AddBool("FleeR", "Use R to Flee");
            }

            killStealMenu = mainMenu.AddSubMenu("KillSteal", "KillSteal");
            {
                killStealMenu.AddLine("Q");
                killStealMenu.AddBool("KSQ", "Use Q to Kill Steal");
                killStealMenu.AddLine("W");
                killStealMenu.AddBool("KSW", "Use W to Kill Steal");
                killStealMenu.AddLine("E");
                killStealMenu.AddBool("KSE", "Use E to Kill Steal");
                killStealMenu.AddLine("Ignite");
                killStealMenu.AddBool("IGKS", "Use Ignite to Kill Steal");
            }

            drawMenu = mainMenu.AddSubMenu("Drawings", "Drawings");
            {
                drawMenu.AddLine("Q");
                drawMenu.AddBool("DrawQ", "Draw Q range");
                drawMenu.AddLine("Line");
                drawMenu.AddBool("LineQ", "Draw line for Q orb");
            }

            miscMenu = mainMenu.AddSubMenu("Misc", "Misc");
            {
                miscMenu.AddSlider("Overkill", "Overkill & for damage prediction", 10, 0, 100);
                miscMenu.AddBool("CatchQMovement", "Catch Q with movement", false);
                miscMenu.AddBool("Gapclose", "Use E on gapclose spells");
                miscMenu.AddBool("Channeling", "Use E on channeling spells");
                miscMenu.AddBool("SkinHax", "Activate skin haxxxs", false);
                miscMenu.AddSlider("SkinID", "Choose skin ID: {0}", 4, 0, 10);
            }

            if (miscMenu.GetBool("SkinHax"))
            {
                Player.Instance.SetSkinId(miscMenu.GetSlider("SkinID"));
            }

        }

        public static void AddSpellFarm(Menu mainMenu)
        {
            farmMenu = mainMenu;

            mainMenu.AddSeparator();
            mainMenu.AddText("iAhri Farm Logic");
            mainMenu.AddBool("SpellFarm", "Use Spell Farm (MWheel Control)");

            SpellFarm = clearMenu.GetBool("ClearQ");

            Game.OnWndProc += delegate (WndEventArgs Args)
            {
                if (Args.Msg == 0x20a)
                {
                    clearMenu["ClearQ"].Cast<CheckBox>().CurrentValue = !SpellFarm;
                    SpellFarm = clearMenu.GetBool("ClearQ");
                }
            };
        }
        public static void AddDrawFarm(Menu mainMenu)
        {
            drawMenu = mainMenu;

            mainMenu.AddSeparator();
            mainMenu.AddText("Draw iAhri Farm Logic");
            mainMenu.AddBool("Draw Farm", "Draw Spell Farm Status");

            Drawing.OnDraw += delegate
            {
                if (!Player.Instance.IsDead && !MenuGUI.IsChatOpen)
                {
                    if (mainMenu.GetBool("DrawFarm"))
                    {
                        var MePos = Drawing.WorldToScreen(Player.Instance.Position);

                        Drawing.DrawText(MePos[0] - 57, MePos[1] + 48, Color.FromArgb(66, 170, 244),
                            "Spell Farm:" + (SpellFarm ? "On" : "Off"));
                    }
                }
            };
            }
    }
}




