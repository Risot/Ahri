﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Notifications;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace iAhri
{
    internal class Program
    {
        private static readonly string Author = "iCreative";
        private static readonly string AddonName = "iAhri + additions";
        private static readonly float RefreshTime = 0.4f;
        private static readonly Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>();
        private static Menu menu;
        private static Spell.Skillshot Q, W, E, R;
        private static Spell.Targeted Ignite;
        private static readonly Dictionary<string, object> _Q = new Dictionary<string, object>
        {
            {"MinSpeed", 400},
            {"MaxSpeed", 1700},
            {"Acceleration", -3200},
            {"Speed1", 1400},
            {"Delay1", 250},
            {"Range1", 880},
            {"Delay2", 0},
            {"Range2", int.MaxValue},
            {"IsReturning", false},
            {"Target", null},
            {"Object", null},
            {"LastObjectVector", null},
            {"LastObjectVectorTime", null},
            {"CatchPosition", null}
        };


        private static readonly Dictionary<string, object> _E = new Dictionary<string, object>
        {
            {"LastCastTime", 0f},
            {"Object", null}
        };

        private static readonly Dictionary<string, object> _R = new Dictionary<string, object> {{"EndTime", 0f}};


        private static AIHeroClient myHero
        {
            get { return ObjectManager.Player; }
        }

        private static Vector3 mousePos
        {
            get { return Game.CursorPos; }
        }

        private static float Overkill
        {
            get { return (100f + SpellMenu.miscMenu.GetSlider("Overkill")) / 100f; }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Ahri)
            {
                return;
            }
            Chat.Print(AddonName + " made by: " + Author + "loaded, have fun!.");
            SpellMenu.init();
            Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Linear, 250, 1700, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 550, SkillShotType.Circular, 0, 1400, 300)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 975, SkillShotType.Linear, 250, 1600, 60)
            {
                AllowedCollisionCount = 0
            };
            R = new Spell.Skillshot(SpellSlot.R, 800, SkillShotType.Circular, 0, 1400, 300)
            {
                AllowedCollisionCount = int.MaxValue
            };
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDeleteObj;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnBuffGain += OnApplyBuff;
            Obj_AI_Base.OnBuffLose += OnRemoveBuff;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
        }

        public static bool IsWall(Vector3 v)
        {
            var v2 = v.To2D();
            return NavMesh.GetCollisionFlags(v2.X, v2.Y).HasFlag(CollisionFlags.Wall);
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.IsDead)
            {
                return;
            }
            if (_Q["Object"] != null)
            {
                //Q.Range = (uint)_Q["Range2"];
                Q.CastDelay = (int) _Q["Delay2"];
                Q.SourcePosition = ((GameObject) _Q["Object"]).Position;
                if (_Q["LastObjectVector"] != null)
                {
                    Q.Speed = (int) (((Vector3) Q.SourcePosition).Distance((Vector3) _Q["LastObjectVector"])
                                     / (Game.Time - (float) _Q["LastObjectVectorTime"]));
                }
                _Q["LastObjectVector"] = new Vector3(((Vector3) Q.SourcePosition).X, ((Vector3) Q.SourcePosition).Y,
                    ((Vector3) Q.SourcePosition).Z);
                _Q["LastObjectVectorTime"] = Game.Time;
            }
            else
            {
                //Q.Range = (float)_Q["Range1"];
                Q.CastDelay = (int) _Q["Delay1"];
                Q.Speed = (int) _Q["Speed1"];
                Q.SourcePosition = myHero.Position;
            }
            CatchQ();
            KillSteal();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) ||
                     Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleClear();
                }

                {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    {
                      LaneClear();
                    }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                    {
                      Flee();
                    }
                 }           
             }
        }



private static void KillSteal()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget(E.Range) && enemy.HealthPercent <= 40)
                {
                    var damageI = GetBestCombo(enemy);
                    if (damageI.Damage >= enemy.TotalShieldHealth())
                    {
                        if (SpellMenu.killStealMenu.GetBool("KSQ") &&
                            (Damage(enemy, Q.Slot) >= enemy.TotalShieldHealth() || damageI.Q))
                        {
                            CastQ(enemy);
                        }
                        if (SpellMenu.killStealMenu.GetBool("KSW") &&
                            (Damage(enemy, W.Slot) >= enemy.TotalShieldHealth() || damageI.W))
                        {
                            CastW(enemy);
                        }
                        if (SpellMenu.killStealMenu.GetBool("KSE") &&
                            (Damage(enemy, E.Slot) >= enemy.TotalShieldHealth() || damageI.E))
                        {
                            CastE(enemy);
                        }
                    }
                    if (Ignite != null && SpellMenu.killStealMenu.GetBool("IGKS") &&
                        Ignite.IsReady() &&
                        myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >=
                        enemy.TotalShieldHealth())
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target.IsValidTarget() && target != null)
            {
                if (SpellMenu.comboMenu.GetBool("ComboR") && target != null && R.IsReady())
                {
                    var RPred = R.GetPrediction(target);
                    var MinR = SpellMenu.comboMenu.GetSlider("minR");
                    {
                        if (RPred.CastPosition.CountEnemyChampionsInRange(800) >= MinR)
                        {
                            CastR(Player.Instance.Position.Extend(target.Position, R.Range + 250).To3D());
                        }
                        if (SpellMenu.comboMenu.GetBool("ComboE"))
                        {
                            if (E.IsReady())
                            {
                                CastE(target);
                            }
                            if ((Game.Time - (float) _E["LastCastTime"] <= (float) (E.CastDelay / 1000 * 1.1)) ||
                                (_E["Object"] != null &&
                                 myHero.Position.Distance(target.Position) >
                                 myHero.Position.Distance(((GameObject) _E["Object"]).Position)))
                            {
                                return;
                            }


                            if (SpellMenu.comboMenu.GetBool("ComboQ"))
                            {
                                if (Q.IsReady())
                                {
                                    CastQ(target);
                                }
                                if (SpellMenu.comboMenu.GetBool("ComboW"))
                                {
                                    if (W.IsReady())
                                    {
                                        CastW(target);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CastR(Vector3 vector3)
        {
            throw new NotImplementedException();
        }

        private static void Harass()
                    {
                        var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                        if (target.IsValidTarget() &&
                            myHero.ManaPercent >= SpellMenu.harassMenu.GetSlider("minHarass"))
                        {
                            if (SpellMenu.harassMenu.GetBool("HarassE"))
                            {
                                CastE(target);
                            }
                            if ((Game.Time - (float) _E["LastCastTime"] <= (float) (E.CastDelay / 1000f * 1.1f)) ||
                                (_E["Object"] != null &&
                                 myHero.Position.Distance(target.Position) >
                                 myHero.Position.Distance(((GameObject) _E["Object"]).Position)))
                            {
                                return;
                            }
                            if (SpellMenu.harassMenu.GetBool("HarassQ") &&
                                target.IsInRange(Player.Instance, Q.Range))
                            {
                                CastQ(target);
                            }
                            if (W.IsReady() &&
                                (SpellMenu.harassMenu.GetBool("HarassW") &&
                                 target.IsInRange(Player.Instance, W.Range)))
                            {
                                CastW(target);
                            }
                        }
                    }


                    private static void Flee()
                    {
                    if (SpellMenu.fleeMenu.GetBool("FleeR") && R.IsReady())
                    return;
                        {
                        if (Player.Instance.HealthPercent <= 15 && Player.Instance.CountEnemiesInRange(R.Range) > 1)
                        {
                        R.Cast(mousePos);
                        }
                        if (SpellMenu.fleeMenu.GetBool("FleeQ") && Q.IsReady())
                        {
                            var lastPos = Player.Instance.ServerPosition;
                            Q.Cast(lastPos);
                        }
                    }
        }

        private static void LaneClear()
        {
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions()
                        .OrderBy(o => o.Health)
                        .FirstOrDefault(c => c.IsValidTarget(Q.Range));
                if (minions != null) return;
                {

                    if (W.IsReady() && minions.IsValidTarget(W.Range) && SpellMenu.clearMenu.GetBool("HarassW") &&
                        Player.Instance.Level >=
                        SpellMenu.clearMenu.GetSlider("farmTillLvL"))
                    {
                        W.Cast();
                    }


                    if (Q.IsReady() && minions.IsValidTarget(Q.Range) && SpellMenu.clearMenu.GetBool("HarassQ") && SpellMenu.clearMenu.GetSlider("ClearMana") < Player.Instance.ManaPercent)
                    {
                        var heh =
                            EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                                Player.Instance.ServerPosition, Q.Range, false).ToArray();
                        if (heh.Length == 0) return;

                        var farmLoc = EntityManager.MinionsAndMonsters.GetLineFarmLocation(heh, Q.Width, (int)Q.Range);
                        if (farmLoc.HitNumber == SpellMenu.clearMenu.GetSlider("minHit"))
                        {
                            Q.Cast(farmLoc.CastPosition);
                        }
                    }
                }
            }
        }



        private static void JungleClear()
                    {
                        if (myHero.ManaPercent >= SpellMenu.jglMenu.GetSlider("jglMana"))
                        {
                            foreach (
                                var minion in
                                EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, 1000f)
                                    .Where(minion => minion.IsValidTarget() &&
                                                     myHero.ManaPercent >=
                                                     SpellMenu.jglMenu.GetSlider("jglMana")))
                            {
                                if (SpellMenu.jglMenu.GetBool("jglE"))
                                {
                                    CastE(minion);
                                }
                                if ((Game.Time - (float) _E["LastCastTime"] <= (float) (E.CastDelay / 1000 * 1.1)) ||
                                    (_E["Object"] != null &&
                                     myHero.Position.Distance(minion.Position) >
                                     myHero.Position.Distance(((GameObject) _E["Object"]).Position)))
                                {
                                    return;
                                }
                                if (SpellMenu.jglMenu.GetBool("jglQ"))
                                {
                                    CastQ(minion);
                                }
                                if (SpellMenu.jglMenu.GetBool("jglW"))
                                {
                                    CastW(minion);
                                }
                            }
                        }
                    }

                    private static void CastQ(Obj_AI_Base target)
                    {
                        if (Q.IsReady() && target.IsValidTarget())
                        {
                            var maxspeed = (int) _Q["MaxSpeed"];
                            var acc = (int) _Q["Acceleration"];
                            var speed = Math.Sqrt(Math.Pow(maxspeed, 2) + 2 * acc * myHero.Distance(target));
                            var tf = (speed - maxspeed) / acc;
                            Q.Speed = (int) (myHero.Distance(target) / tf);
                            var r = Q.GetPrediction(target);
                            if (r.HitChance >= HitChance.High)
                            {
                                Q.Cast(r.CastPosition);
                                _Q["Target"] = target;
                            }
                        }
                    }

                    private static void CastW(Obj_AI_Base target)
                    {
                        if (W.IsReady() && target.IsValidTarget())
                        {
                            var r = W.GetPrediction(target);
                            if (r.HitChance >= HitChance.Medium)
                            {
                                if (target.Type == myHero.Type)
                                {
                                    if (_Q["Object"] != null || Orbwalker.LastTarget.NetworkId == target.NetworkId)
                                    {
                                        myHero.Spellbook.CastSpell(W.Slot);
                                    }
                                }
                                else
                                {
                                    myHero.Spellbook.CastSpell(W.Slot);
                                }
                            }
                        }
                    }

                    private static void CastE(Obj_AI_Base target)
                    {
                        if (E.IsReady() && target.IsValidTarget())
                        {
                            var r = E.GetPrediction(target);
                            if (r.HitChance >= HitChance.High && target.IsEnemy)
                            {
                                E.Cast(r.CastPosition);
                            }
                        }
                    }

                    private static void CastR(Obj_AI_Base target)
                    {
                        if (R.IsReady() && target.IsValidTarget())
                        {
                            var damageI = GetBestCombo(target);
                            if (SpellMenu.comboMenu.GetBool("CatchQRPriority"))
                            {
                                if ((float) _R["EndTime"] > 0)
                                {
                                    //have active r
                                    if (_Q["Object"] != null)
                                    {
                                        if ((bool) _Q["IsReturning"] &&
                                            myHero.Distance((GameObject) _Q["Object"]) <
                                            myHero.Distance((Obj_AI_Base) _Q["Target"]))
                                        {
                                            R.Cast(mousePos);
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                    if (!Q.IsReady() &&
                                        (float) _R["EndTime"] - Game.Time <= myHero.Spellbook.GetSpell(R.Slot).Cooldown)
                                    {
                                        R.Cast(mousePos);
                                    }
                                }
                                if (damageI.Damage >= target.TotalShieldHealth() &&
                                    mousePos.Distance(target) < myHero.Distance(target))
                                {
                                    if (damageI.R)
                                    {
                                        if (myHero.Distance(target) > 450)
                                        {
                                            R.Cast(mousePos);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((float) _R["EndTime"] > 0)
                                {
                                    if (!Q.IsReady() &&
                                        (float) _R["EndTime"] - Game.Time <= myHero.Spellbook.GetSpell(R.Slot).Cooldown)
                                    {
                                        R.Cast(mousePos);
                                    }
                                }
                                if (damageI.Damage >= target.TotalShieldHealth() &&
                                    mousePos.Distance(target) < myHero.Distance(target))
                                {
                                    if (damageI.R)
                                    {
                                        if (myHero.Distance(target) > 450)
                                        {
                                            R.Cast(mousePos);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    private static void CatchQ()
                    {
                        if (_Q["Object"] != null)
                        {
                            _Q["CatchPosition"] = null;
                            var target = _Q["Target"] != null
                                ? (Obj_AI_Base) _Q["Target"]
                                : TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                            if (target != null && target.IsValidTarget())
                            {
                                var r = Q.GetPrediction(target);
                                if (myHero.Distance(r.CastPosition) <= myHero.Distance((GameObject) _Q["Object"]))
                                {
                                    //Chat.Print("2");
                                    var TimeLeft = myHero.Distance(target) / Q.Speed;
                                    var qObject = ((GameObject) _Q["Object"]).Position;
                                    var ExtendedPos = qObject + (r.CastPosition - qObject).Normalized() * 1500;
                                    var ClosestToTargetLine = myHero.Position.To2D()
                                        .ProjectOn(qObject.To2D(), ExtendedPos.To2D());
                                    var ClosestToHeroLine = r.CastPosition.To2D()
                                        .ProjectOn(qObject.To2D(), myHero.Position.To2D());
                                    if (ClosestToTargetLine.IsOnSegment && ClosestToHeroLine.IsOnSegment &&
                                        ClosestToTargetLine.SegmentPoint.Distance(qObject.To2D()) <
                                        r.CastPosition.To2D().Distance(qObject.To2D()))
                                    {
                                        //Chat.Print("3");
                                        if (ClosestToTargetLine.SegmentPoint.Distance(myHero.Position.To2D()) <
                                            myHero.MoveSpeed * TimeLeft)
                                        {
                                            if (SpellMenu.miscMenu.GetBool("CatchQMovement"))
                                            {
                                                //Chat.Print("4");
                                                if (ClosestToHeroLine.SegmentPoint.Distance(r.CastPosition.To2D()) >
                                                    Q.Width)
                                                {
                                                    Orbwalker.OrbwalkTo(ClosestToTargetLine.SegmentPoint.To3D());
                                                }
                                            }
                                        }
                                        else if (ClosestToTargetLine.SegmentPoint.Distance(myHero.Position.To2D()) <
                                                 450 + myHero.MoveSpeed * TimeLeft)
                                        {
                                            if (SpellMenu.comboMenu.GetBool("CatchQR") &&
                                                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                                            {
                                                if (ClosestToHeroLine.SegmentPoint.Distance(r.CastPosition.To2D()) >
                                                    Q.Width)
                                                {
                                                    var rPos = myHero.Position +
                                                               (ClosestToTargetLine.SegmentPoint.To3D() -
                                                                myHero.Position)
                                                               .Normalized() *
                                                               myHero.Distance(ClosestToTargetLine.SegmentPoint.To3D()) *
                                                               1.2f;
                                                    R.Cast(rPos);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    private static void OnCreateObj(GameObject sender, EventArgs args)
                    {
                        if (sender.Name.ToLower().Contains("missile"))
                        {
                            var missile = sender as MissileClient;
                            if (missile == null || !missile.IsValid || missile.SpellCaster == null ||
                                !missile.SpellCaster.IsValid)
                            {
                                return;
                            }
                            if (missile.SpellCaster.IsMe)
                            {
                                var name = missile.SData.Name.ToLower();
                                if (name.Contains("ahriorbmissile"))
                                {
                                    _Q["Object"] = sender;
                                    _Q["IsReturning"] = false;
                                }
                                else if (name.Contains("ahriorbreturn"))
                                {
                                    _Q["Object"] = sender;
                                    _Q["IsReturning"] = true;
                                }
                                else if (name.Contains("ahriseducemissile"))
                                {
                                    _E["Object"] = sender;
                                }
                            }
                        }
                    }

                    private static void OnDeleteObj(GameObject sender, EventArgs args)
                    {
                        if (sender.Name.ToLower().Contains("missile"))
                        {
                            var missile = sender as MissileClient;
                            if (missile == null || !missile.IsValid || missile.SpellCaster == null ||
                                !missile.SpellCaster.IsValid)
                            {
                                return;
                            }
                            if (missile.SpellCaster.IsMe)
                            {
                                var name = missile.SData.Name.ToLower();
                                if (name.Contains("ahriorbreturn"))
                                {
                                    _Q["Object"] = null;
                                    _Q["IsReturning"] = false;
                                    _Q["Target"] = null;
                                    _Q["LastObjectVector"] = null;
                                }
                                else if (name.Contains("ahriseducemissile"))
                                {
                                    _E["Object"] = null;
                                }
                            }
                        }
                    }

                    private static void OnDraw(EventArgs args)
                    {
                        if (myHero.IsDead)
                        {
                            return;
                        }
                        if (_Q["Object"] != null && SpellMenu.drawMenu.GetBool("LineQ"))
                        {
                            var asd = (GameObject) _Q["Object"];
                            var p1 = Drawing.WorldToScreen(myHero.Position);
                            var p2 = Drawing.WorldToScreen(asd.Position);
                            Drawing.DrawLine(p1, p2, Q.Width, Color.FromArgb(100, 255, 255, 255));
                        }

                        if (!Player.Instance.IsDead && SpellMenu.drawMenu.GetBool("DrawQ") && Q.IsLearned)
                        {
                            {
                                new Circle() {Color = Color.Lime, Radius = Q.Range}.Draw(ObjectManager.Player.Position);
                            }
                        }
                    }

                    private static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
                    {
                        if (SpellMenu.miscMenu.GetBool("Gapclose"))
                        {
                            CastE(args.Sender);
                        }
                    }

                    private static void OnInterruptableSpell(Obj_AI_Base sender,
                        Interrupter.InterruptableSpellEventArgs args)
                    {
                        if (SpellMenu.miscMenu.GetBool("Channeling"))
                        {
                            CastE(args.Sender);
                        }
                    }

                    private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
                    {
                        if (sender.IsMe)
                        {
                            if (Q.Slot == SpellSlot.Q)
                            {
                                _Q["IsReturning"] = false;
                                _Q["Object"] = null;
                            }
                            else if (E.Slot == SpellSlot.E)
                            {
                                _E["Object"] = null;
                                _E["LastCastTime"] = Game.Time;
                            }
                        }
                    }

                    private static void OnApplyBuff(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
                    {
                        if (sender.IsMe)
                        {
                            var buff = args.Buff;
                            if (buff.Name.ToLower() == "ahritumble")
                            {
                                _R["EndTime"] = Game.Time + buff.EndTime - buff.StartTime;
                            }
                        }
                    }

                    private static void OnRemoveBuff(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
                    {
                        if (sender.IsMe)
                        {
                            var buff = args.Buff;
                            if (buff.Name.ToLower() == "ahritumble")
                            {
                                _R["EndTime"] = 0f;
                            }
                        }
                    }

                    private static Obj_AI_Base LastHit(Spell.SpellBase spell)
                    {
                        return null;
                    }

                    private static float Damage(Obj_AI_Base target, SpellSlot slot)
                    {
                        if (target.IsValidTarget())
                        {
                            if (slot == SpellSlot.Q)
                            {
                                return
                                    myHero.CalculateDamageOnUnit(target, DamageType.Magical,
                                        25f * Q.Level + 15 + 0.35f * myHero.TotalMagicalDamage) +
                                    myHero.CalculateDamageOnUnit(target, DamageType.True,
                                        25f * Q.Level + 15 + 0.35f * myHero.TotalMagicalDamage);
                            }
                            if (slot == SpellSlot.W)
                            {
                                return 1.6f *
                                       myHero.CalculateDamageOnUnit(target, DamageType.Magical,
                                           25f * W.Level + 15 + 0.4f * myHero.TotalMagicalDamage);
                            }
                            if (slot == SpellSlot.E)
                            {
                                return myHero.CalculateDamageOnUnit(target, DamageType.Magical,
                                    35f * E.Level + 25 + 0.5f * myHero.TotalMagicalDamage);
                            }
                            if (slot == SpellSlot.R)
                            {
                                return 3 *
                                       myHero.CalculateDamageOnUnit(target, DamageType.Magical,
                                           40f * R.Level + 30 + 0.3f * myHero.TotalMagicalDamage);
                            }
                        }
                        return myHero.GetSpellDamage(target, slot);
                    }
                    private static DamageInfo GetComboDamage(Obj_AI_Base target, bool q, bool w, bool e, bool r)
                    {
                        var comboDamage = 0f;
                        var manaWasted = 0f;
                        if (target.IsValidTarget())
                        {
                            if (q)
                            {
                                comboDamage += Damage(target, Q.Slot);
                                manaWasted += myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                            }
                            if (w)
                            {
                                comboDamage += Damage(target, W.Slot);
                                manaWasted += myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                            }
                            if (e)
                            {
                                comboDamage += Damage(target, E.Slot);
                                manaWasted += myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
                            }
                            if (r)
                            {
                                comboDamage += Damage(target, R.Slot);
                                manaWasted += myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                            }
                            if (Ignite != null && Ignite.IsReady())
                            {
                                comboDamage += myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                            }
                            comboDamage += myHero.GetAutoAttackDamage(target, true);
                        }
                        comboDamage = comboDamage * Overkill;
                        return new DamageInfo(comboDamage, manaWasted);
                    }

                    private static DamageInfo GetBestCombo(Obj_AI_Base target)
                    {
                        var q = Q.IsReady() ? new[] {false, true} : new[] {false};
                        var w = W.IsReady() ? new[] {false, true} : new[] {false};
                        var e = E.IsReady() ? new[] {false, true} : new[] {false};
                        var r = R.IsReady() ? new[] {false, true} : new[] {false};
                        if (target.IsValidTarget())
                        {
                            DamageInfo damageI2;
                            if (PredictedDamage.ContainsKey(target.NetworkId))
                            {
                                var damageI = PredictedDamage[target.NetworkId];
                                if (Game.Time - damageI.Time <= RefreshTime)
                                {
                                    return damageI;
                                }
                                bool[] best =
                                {
                                    Q.IsReady(),
                                    W.IsReady(),
                                    E.IsReady(),
                                    R.IsReady()
                                };
                                var bestdmg = 0f;
                                var bestmana = 0f;
                                foreach (var q1 in q)
                                {
                                    foreach (var w1 in w)
                                    {
                                        foreach (var e1 in e)
                                        {
                                            foreach (var r1 in r)
                                            {
                                                damageI2 = GetComboDamage(target, q1, w1, e1, r1);
                                                var d = damageI2.Damage;
                                                var m = damageI2.Mana;
                                                if (myHero.Mana >= m)
                                                {
                                                    if (bestdmg >= target.TotalShieldHealth())
                                                    {
                                                        if (d >= target.TotalShieldHealth() &&
                                                            (d < bestdmg || m < bestmana))
                                                        {
                                                            bestdmg = d;
                                                            bestmana = m;
                                                            best = new[] {q1, w1, e1, r1};
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (d >= bestdmg)
                                                        {
                                                            bestdmg = d;
                                                            bestmana = m;
                                                            best = new[] {q1, w1, e1, r1};
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                PredictedDamage[target.NetworkId] = new DamageInfo(best[0], best[1], best[2], best[3],
                                    bestdmg,
                                    bestmana, Game.Time);
                                return PredictedDamage[target.NetworkId];
                            }
                            damageI2 = GetComboDamage(target, Q.IsReady(), W.IsReady(), E.IsReady(), R.IsReady());
                            PredictedDamage[target.NetworkId] = new DamageInfo(false, false, false, false,
                                damageI2.Damage,
                                damageI2.Mana, Game.Time - Game.Ping * 2);
                            return GetBestCombo(target);
                        }
                        return new DamageInfo(false, false, false, false, 0, 0, 0);
                    }
                }

        internal class DamageInfo
        {
            public float Damage;
            public bool E;
            public float Mana;
            public bool Q;
            public bool R;
            public float Time;
            public bool W;

            public DamageInfo(bool Q, bool W, bool E, bool R, float Damage, float Mana, float Time)
            {
                this.Q = Q;
                this.W = W;
                this.E = E;
                this.R = R;
                this.Damage = Damage;
                this.Mana = Mana;
                this.Time = Time;
            }

            public DamageInfo(float Damage, float Mana)
            {
                this.Damage = Damage;
                this.Mana = Mana;
            }
        }
    }