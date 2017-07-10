using System;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Spells;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using System.Collections.Generic;
using Color = System.Drawing.Color;

namespace FrOnDaL_Braum
{
    internal class Program
    {
        private static AIHeroClient Braum => Player.Instance;
        private static Spell.Skillshot _q, _e, _r;
        private static Spell.Targeted _w;
        private static Spellbook _lvl;
        private static Menu _main, _combo, _drawings, _misc;
        private static string _slot = "", _hero = "";
        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (Braum.Level > 4)
            {
                _lvl.LevelSpell(SpellSlot.R);
            }
        }
        private static bool SpellShield(Obj_AI_Base shield)
        {
            return shield.HasBuffOfType(BuffType.SpellShield) || shield.HasBuffOfType(BuffType.SpellImmunity);
        }
        private static readonly List<string> SkillShots = new List<string>
        {
            "AatroxE","AhriOrbofDeception","AhriSeduce","Pulverize","BandageToss","FlashFrostSpell","Disintegrate","Volley","EnchantedCrystalArrow","BardQ","BrandQ","BraumRWrapper",
            "BraumQ","CaitlynPiltoverPeacemaker","CaitlynEntrapment","CaitlynAceintheHole","MissileBarrage2","PhosphorusBomb","MissileBarrage","DianaArc","InfectedCleaverMissileCast",
            "DravenRCast","DravenDoubleShot","EkkoQ","EliseHumanE","EzrealMysticShot","EzrealTrueshotBarrage","gnarbigq","GnarQ","GragasR","GravesQLineSpell","GravesChargeShot",
            "HecarimUlt","IreliaTranscendentBlades","JayceQAccel","JinxR","JinxWMissile","KarmaQ","KarmaQMissileMantra","KennenShurikenHurlMissile1","KhazixW","KogMawQ","KogMawVoidOoze",
            "BlindMonkQOne","LeonaZenithBlade","LucianR","NamiR","NautilusAnchorDrag","NocturneDuskbringer","OlafAxeThrowCast","QuinnQ","rivenizunablade","RumbleGrenade","RyzeQ",
            "SejuaniArcticAssault","ShyvanaFireball","ShyvanaTransformCast","SivirQ","TaliyahQ","TahmKenchQ","WildCards","UrgotHeatseekingLineMissile","VarusQMissilee","VeigarBalefulStrike",
            "VelkozQ","XerathMageSpear","ZedQ","JhinRShot","MissFortuneBulletTime"
        };
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Braum.Hero != Champion.Braum) return;

            _q = new Spell.Skillshot(SpellSlot.Q, 1000, SkillShotType.Linear, 250, 1700, 60) { AllowedCollisionCount = 0};
            _w = new Spell.Targeted(SpellSlot.W, 650);
            _e = new Spell.Skillshot(SpellSlot.E, 1500, SkillShotType.Cone, 250, 2000, 350);
            _r = new Spell.Skillshot(SpellSlot.R, 1250, SkillShotType.Linear, (int) 0.5f, 1300, 115) {AllowedCollisionCount = int.MaxValue};
           
            Game.OnTick += BraumActive;
            Drawing.OnDraw += SpellDraw;          
            Gapcloser.OnGapcloser += AntiGapCloser;
            Interrupter.OnInterruptableSpell += DangerousSpellsInterupt;
            Obj_AI_Base.OnProcessSpellCast += AutoProtection;
            Obj_AI_Base.OnBasicAttack += AutoAttack;
            _lvl = Braum.Spellbook;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Braum Successfully loaded.");

            _main = MainMenu.AddMenu("FrOnDaL Braum", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Braum");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");

            /*Combo*/
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Braum");
            _combo.AddLabel("Use Combo Q (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.AddSeparator(5);
            _combo.AddLabel("Auto Use W (On/Off)");
            _combo.Add("autow", new CheckBox("Auto Use W", false));
            _combo.Add("Wmana", new Slider("Auto W Mana Control Min mana percentage ({0}%)", 10, 1));
            _combo.AddSeparator(5);
            _combo.AddLabel("Auto Use E (On/Off)");
            _combo.Add("autoe", new CheckBox("Auto Use E"));
            _combo.Add("Emana", new Slider("Auto E Mana Control Min mana percentage ({0}%)", 10, 1));
            _combo.AddSeparator(5);
            _combo.AddLabel("Enemy AA Use E (Shield AA below HP) (On/Off)");
            _combo.Add("AAuseE", new CheckBox("Enemy AA Use E (Combo in)"));
            _combo.Add("alliedHeal", new Slider("Shield AA below ({0}%) HP", 80, 1));
            _combo.AddSeparator(5);
            _combo.AddLabel("Auto Use W/E (On/Off)" + "                                " + "Combo in Use R (On/Off)");
            _combo.Add("autowande", new CheckBox("Auto Use W/E"));
            _combo.Add("comboR", new CheckBox("Combo Use R", false));
            _combo.Add("WEmana", new Slider("Auto W and E Mana Control Min mana percentage ({0}%)", 10, 1));
            _combo.AddSeparator(5);
            _combo.AddLabel("Enable Auto Protection for :");
            foreach (var spells in SkillShots.Where(spells => EntityManager.Heroes.Enemies.Any(enemy => enemy.Spellbook.Spells.Any(x => x.SData.Name == spells && (_slot = x.Slot.ToString()) == x.Slot.ToString() && (_hero = enemy.BaseSkinName) == enemy.BaseSkinName))))
            {
                _combo.Add(spells, new CheckBox(_hero + " " + _slot));
            }
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Manuel R");
            _combo.Add("r", new KeyBind("Use R Key", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _combo.Add("RHit", new Slider("Manual R Hits ", 1, 1, 5));
            _combo.AddSeparator(8);
            /*Combo*/

            /*Draw*/
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q-W-R (On/Off)");
            _drawings.Add("q", new CheckBox("Draw Q", false));
            _drawings.Add("w", new CheckBox("Draw W", false));
            _drawings.Add("r", new CheckBox("Draw R", false));
            /*Draw*/

            /*Misc*/
            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Anti Gap Closer Q-R (On/Off)");
            _misc.Add("Qgap", new CheckBox("Use Q Anti Gap Closer (On/Off)"));
            _misc.Add("Rgap", new CheckBox("Use R Anti Gap Closer (On/Off)"));
            _misc.AddSeparator(5);
            _misc.AddLabel("Interrupt Dangerous Spells (On/Off)");
            _misc.Add("interruptR", new CheckBox("Use R Interrupt (On/Off)"));
            /*Misc*/
        }

        /*SpellDraw*/
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["q"].Cast<CheckBox>().CurrentValue)
            {
                _q.DrawRange(Color.FromArgb(130, Color.Green));
            }
            if (_drawings["w"].Cast<CheckBox>().CurrentValue)
            {
                _w.DrawRange(Color.FromArgb(130, Color.Green));
            }        
            if (_drawings["r"].Cast<CheckBox>().CurrentValue)
            {
                _r.DrawRange(Color.FromArgb(130, Color.Green));
            }         
        }
        /*SpellDraw*/

        /*BraumActive*/
        private static void BraumActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
                if (_combo["comboR"].Cast<CheckBox>().CurrentValue)
                {
                    var hedefR = TargetSelector.GetTarget(_r.Range, DamageType.Physical);
                    if (_r.IsReady() && hedefR.IsValidTarget(1250) && hedefR.Distance(Braum.ServerPosition) > 150 && hedefR.Distance(Braum.ServerPosition) < 1250)
                    {
                        ManuelR();
                    }
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JunglePlantsClear))
            {               
                if (_w.IsReady())
                {
                    if (Braum.CountAllyChampionsInRange(_w.Range) > 1)
                    {
                        var flee = EntityManager.Heroes.Allies.FirstOrDefault(x => !x.IsMe && x.Distance(ObjectManager.Player) <= _w.Range);
                        _w.Cast(flee);
                    }
                    else
                    {
                        var fleeminion = EntityManager.MinionsAndMonsters.AlliedMinions.FirstOrDefault(x => !x.IsMe && x.Distance(ObjectManager.Player) <= _w.Range);
                        _w.Cast(fleeminion);
                    }
                }                                     
            }
            if (_combo["r"].Cast<KeyBind>().CurrentValue)
            {
                ManuelR();
            }
        }
        /*BraumActive*/

        /*Combo*/
        private static void Combo()
        {
            if (!_q.IsReady() || !_combo["q"].Cast<CheckBox>().CurrentValue) return;
            var comboTargetQ = TargetSelector.GetTarget(_q.Range, DamageType.Physical);
            if (comboTargetQ != null && comboTargetQ.IsValidTarget(_q.Range) && !SpellShield(comboTargetQ) && _q.GetPrediction(comboTargetQ).HitChance >= HitChance.High && comboTargetQ.Distance(Braum.ServerPosition) > 150 && comboTargetQ.Distance(Braum.ServerPosition) < _q.Range)
            {
                _q.Cast(comboTargetQ);
            }
        }
        /*Combo*/

        /*Enemy AutoAttack Use E*/
        private static void AutoAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo)) return;
            var hero = sender as AIHeroClient;
            var target = args.Target as AIHeroClient;
            if (!_e.IsReady() || hero == null || !hero.IsValid || target == null || !target.IsValid || target.IsMe && !hero.IsEnemy || !_combo["AAuseE"].Cast<CheckBox>().CurrentValue) return;
            var allian = EntityManager.Heroes.Allies.FirstOrDefault(x => x.Distance(x.Position) < 200 && args.IsAutoAttack() && !x.IsMe);
            if (allian == null || !(Braum.Distance(allian.Position) < 200) || !args.IsAutoAttack() || !(allian.HealthPercent <= _combo["alliedHeal"].Cast<Slider>().CurrentValue)) return;
            _e.Cast(hero.Position);
        }
        /*Enemy AutoAttack Use E*/

        /*AutoProtection*/
        private static void AutoProtection(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
           if (_e.IsReady() && sender.IsEnemy && Braum.ManaPercent >= _combo["Emana"].Cast<Slider>().CurrentValue &&
                _combo["autoe"].Cast<CheckBox>().CurrentValue &&  _combo[args.SData.Name].Cast<CheckBox>().CurrentValue || !(sender is AIHeroClient))
            {
                foreach (var protection in from spells in SkillShots.Where(spells => args.SData.Name == spells)
                                              select new Geometry.Polygon.Rectangle(args.Start, args.End, args.SData.BounceRadius + 100) into skillControl
                                              let allies = EntityManager.Heroes.Allies.Where(x => x.Distance(Braum.Position) < 240)
                                              select allies.Count(x => skillControl.IsInside(x.Position)) into protection
                                              where protection >= 1
                                              select protection)
                   {                  
                       _e.Cast(sender.Position);
                   }
            }
            if (!_w.IsReady() || !sender.IsEnemy || !_combo[args.SData.Name].Cast<CheckBox>().CurrentValue || !(sender is AIHeroClient)) return;
            if (args.SData.IsAutoAttack()) return;
            foreach (var jump in from spells in SkillShots
                                let skillControl = new Geometry.Polygon.Rectangle(args.Start, args.End, args.SData.BounceRadius + 100)
                                let allies = EntityManager.Heroes.Allies.Where(x => x.Distance(x.Position) < 240 && !x.IsMe)
                                let protection = allies.Count(x => skillControl.IsInside(x.Position))
                                let jump = EntityManager.Heroes.Allies.FirstOrDefault(x => skillControl.IsInside(x.Position) && 
                                !x.IsMe && x.Distance(ObjectManager.Player) <= _w.Range) where args.SData.Name == spells && protection >= 1 select jump)
            {
                if (!jump.IsInRange(ObjectManager.Player, _w.Range)) return;
                if (_e.IsReady() && _w.IsReady() && Braum.ManaPercent >= _combo["WEmana"].Cast<Slider>().CurrentValue && _combo["autowande"].Cast<CheckBox>().CurrentValue)
                {
                    _w.Cast(jump);
                    _e.Cast(sender.Position);
                }
                else if (_w.IsReady() && Braum.ManaPercent >= _combo["Wmana"].Cast<Slider>().CurrentValue && _combo["autow"].Cast<CheckBox>().CurrentValue)
                {
                    _w.Cast(jump);
                }
            }          
        }
        /*AutoProtection*/

        /*ManuelR*/
        private static void ManuelR()
        {
            if (!_r.IsReady()) return;
            var hedefR = TargetSelector.GetTarget(_r.Range, DamageType.Physical);
            if (hedefR == null) return;
            var manuelR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput { CollisionTypes = new HashSet<CollisionType> { CollisionType.AiHeroClient }, Delay = .25f, From = Braum.Position, Radius = _r.Width, Range = _r.Range, RangeCheckFrom = Braum.Position, Speed = _r.Speed, Target = hedefR, Type = SkillShotType.Linear });
            if (hedefR.CountEnemyChampionsInRange(_r.Width) >= _combo["RHit"].Cast<Slider>().CurrentValue)
            {
                _r.Cast(manuelR.CastPosition);
            }
        }
        /*ManuelR*/

        /*Interrupter*/
        private static void DangerousSpellsInterupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!_misc["interruptR"].Cast<CheckBox>().CurrentValue || !sender.IsValid || sender.IsDead || !sender.IsTargetable || sender.IsStunned) return;
            if (_r.IsReady() && _r.IsInRange(sender.Position) && args.DangerLevel == DangerLevel.High)
            {
                _r.Cast(sender.Position);
            }
        }
        /*Interrupter*/

        /*AntiGapCloser*/
        private static void AntiGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gap)
        {        
            if (_misc["Qgap"].Cast<CheckBox>().CurrentValue && _q.IsReady() && sender.IsEnemy && sender.IsValidTarget(_q.Range) && _q.IsInRange(sender.Position) && gap.End.Distance(Braum) <= 250)
            {
                _q.CastMinimumHitchance(gap.Sender, HitChance.Low);
       
            }
            if (Braum.HealthPercent < 20 && _misc["Rgap"].Cast<CheckBox>().CurrentValue && sender.IsEnemy && sender.IsValidTarget(_r.Range) && _r.IsInRange(sender.Position) && gap.End.Distance(Braum) <= 250)
            {
                _r.CastMinimumHitchance(gap.Sender, HitChance.High);
             
            }
        }
        /*AntiGapCloser*/

    }
}
