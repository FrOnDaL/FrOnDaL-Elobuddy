using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using System.Collections.Generic;
using Color = System.Drawing.Color;

using EloBuddy.SDK.Spells;


namespace FrOnDaL_Caitlyn
{
    internal class Program
    {
        private static AIHeroClient Caitlyn => Player.Instance;
        private static Spellbook _lvl;
        private static Spell.Skillshot _q, _w, _e;
        private static Spell.Targeted _r;
        private static Menu _main, _combo, _laneclear, _jungleclear, _drawings, _misc;
        private static float _dikey, _yatay;
        private static float genislik = 104;
        private static float yukseklik = 9.82f;
        private static readonly Item AutoBotrk = new Item(ItemId.Blade_of_the_Ruined_King);
        private static readonly Item AutoCutlass = new Item(ItemId.Bilgewater_Cutlass);
        protected static bool IsYordle(AIHeroClient unit) => unit.Buffs.Any(x => x.Name == "caitlynyordletrapinternal");
        protected static bool BuffChampEnemy => Caitlyn.Buffs.Any(x => x.Name == "caitlynheadshotrangecheck") && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(1350) && IsYordle(x));
        protected static bool Item => Caitlyn.InventoryItems.Any(x => x.Id == ItemId.Rapid_Firecannon);
        protected static bool ItemStackedUp => Caitlyn.Buffs.Any(x => Item && x.Name.ToLowerInvariant() == "itemstatikshankcharge" && x.Count == 100);
        protected static float Aa => ItemStackedUp ? 900 : 300;
        private static double RDamage(Obj_AI_Base d)
        {
            var damageR = Caitlyn.CalculateDamageOnUnit(d, DamageType.Physical, (float)new double[] { 250, 475, 700 }[_r.Level - 1] + Caitlyn.TotalAttackDamage / 100 * 200); return damageR;
        }
        private static bool SpellShield(Obj_AI_Base shield)
        {
            return shield.HasBuffOfType(BuffType.SpellShield) || shield.HasBuffOfType(BuffType.SpellImmunity);
        }
        private static bool SpellBuff(AIHeroClient buf)
        {
            if (buf.Buffs.Any(x => x.IsValid && (x.Name.Equals("ChronoShift", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("FioraW", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("TaricR", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("BardRStasis", StringComparison.CurrentCultureIgnoreCase) ||
                                       x.Name.Equals("JudicatorIntervention", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("UndyingRage", StringComparison.CurrentCultureIgnoreCase) || (x.Name.Equals("kindredrnodeathbuff", StringComparison.CurrentCultureIgnoreCase) && (buf.HealthPercent <= 10)))))
            { return true; }
            if (buf.ChampionName != "Poppy") return buf.IsInvulnerable;
            return EntityManager.Heroes.Allies.Any(y => !y.IsMe && y.Buffs.Any(z => (z.Caster.NetworkId == buf.NetworkId) && z.IsValid && z.DisplayName.Equals("PoppyDITarget", StringComparison.CurrentCultureIgnoreCase))) || buf.IsInvulnerable;
        }
        private static bool Yordletrap(Vector3 trapPosition, float trapMr = 200, int trapDelay = 2000) => !YordleTrapInRange(trapPosition, trapMr).Any() && (Core.GameTickCount - _trapDelayLast >= trapDelay);
        private static float _trapDelayLast;
        private static void SpellCastWe(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && args.SData.Name.Equals("SummonerFlash", StringComparison.CurrentCultureIgnoreCase) && (sender.Position.Extend(args.End, sender.Distance(args.End) >= 475 ? 475 : sender.Distance(args.End)).Distance(Caitlyn) <= 300))
            {
                _e.Cast(sender.Position.Extend(args.End, sender.Distance(args.End) >= 475 ? 475 : sender.Distance(args.End)).To3D());
            }
            if (!sender.IsMe) return;
            if (args.Slot == SpellSlot.W)
            {
                _trapDelayLast = Core.GameTickCount;
            }
        }
        private static IEnumerable<Obj_GeneralParticleEmitter> YordleTrapInRange(Vector3 trapPos, float trapR)
        {
            return ObjectManager.Get<Obj_GeneralParticleEmitter>().Where(x => x.Name.Equals("Caitlyn_Base_W_Indicator_SizeRing.troy", StringComparison.InvariantCultureIgnoreCase) && (x.Distance(trapPos) < trapR));
        }
        private static float TotalHealth(AttackableUnit enemy, bool magicShields = false)
        {
            return enemy.Health + enemy.AllShield + enemy.AttackShield + (magicShields ? enemy.MagicShield : 0);
        }
        private static void AutoItem(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {           
            var botrkHedef = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(x => x != null && x.IsValidTarget() && x.IsInRange(Caitlyn, 550)), DamageType.Physical);
            if (botrkHedef != null && _misc["botrk"].Cast<CheckBox>().CurrentValue && AutoBotrk.IsOwned() && AutoBotrk.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoBotrk.Cast(botrkHedef);
            }
            if (botrkHedef != null && _misc["autoCutlass"].Cast<CheckBox>().CurrentValue && AutoCutlass.IsOwned() && AutoCutlass.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoCutlass.Cast(botrkHedef);
            }
        }
        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (Caitlyn.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Caitlyn.Hero != Champion.Caitlyn) return;
            _q = new Spell.Skillshot(SpellSlot.Q, 1300, SkillShotType.Linear, 625, 2200, 90) { AllowedCollisionCount = -1 };
            _w = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Circular, 1600) { Width = 20 };
            _e = new Spell.Skillshot(SpellSlot.E, 800, SkillShotType.Linear, 150, 1600, 80) { AllowedCollisionCount = 0 };
            _r = new Spell.Targeted(SpellSlot.R, 2000);
            Drawing.OnEndScene += HasarGostergesi;
            Interrupter.OnInterruptableSpell += DangerousSpellsWInterupt;
            Obj_AI_Base.OnProcessSpellCast += SpellCastWe;
            Obj_AI_Base.OnProcessSpellCast += AutoItem;
            Gapcloser.OnGapcloser += EandWAntiGapCloser;
            Game.OnTick += CaitlynActive;           
            Drawing.OnDraw += SpellDraw;         
            _lvl = Caitlyn.Spellbook;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Orbwalker.OnPostAttack += QandEafterAa;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Caitlyn Successfully loaded.");
            _main = MainMenu.AddMenu("FrOnDaL Caitlyn", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Caitlyn");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Caitlyn");
            _combo.AddLabel("Use Combo Q (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.Add("AfterAaQ", new CheckBox("Use Q After AA", false));
            _combo.AddSeparator(3);
            _combo.Add("qlogic", new ComboBox("Q Logic ", 1, "FrOnDaL", "Normal"));
            _combo.AddSeparator(3);
            _combo.Add("qHitChance", new Slider("Q hitchance percent : {0}", 60));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo W (On/Off)");
            _combo.Add("w", new CheckBox("Use W"));
            _combo.AddSeparator(3);
            _combo.Add("wlogic", new ComboBox("W Logic ", 0, "FrOnDaL", "FrOnDaL2", "Normal"));
            _combo.AddSeparator(3);
            _combo.Add("maxtrap", new Slider("Maximum trap accumulation", 2, 1, 5));
            _combo.AddSeparator(3);
            _combo.Add("WHitChance", new Slider("W hitchance percent : {0}", 85));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo E (On/Off)");
            _combo.Add("e", new CheckBox("Use E"));
            _combo.Add("AfterAaE", new CheckBox("Use E After AA", false));
            _combo.Add("EHitChance", new Slider("E hitchance percent : {0}", 65));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use R");
            _combo.Add("r", new KeyBind("Use R Key", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _laneclear = _main.AddSubMenu("Laneclear");
            _laneclear.AddGroupLabel("LaneClear mode settings for Caitlyn");
            _laneclear.Add("LmanaP", new Slider("LaneClear Mana Control Min mana percentage ({0}%) to use Q", 50, 1));
            _laneclear.AddSeparator(5);
            _laneclear.Add("q", new CheckBox("Use Q", false));
            _laneclear.Add("Qhit", new Slider("Hit {0} Units Enemy and Minions", 4, 1, 6));
            _jungleclear = _main.AddSubMenu("Jungleclear");
            _jungleclear.AddGroupLabel("JungleClear mode settings for Caitlyn");
            _jungleclear.Add("JmanaP", new Slider("Jungle Clear Mana Control Min mana percentage ({0}%) to use Q and E", 30, 1));
            _jungleclear.AddLabel("Use Q Jung" + "                                                     " + "Use E Jung");
            _jungleclear.Add("q", new CheckBox("Use Q"));
            _jungleclear.Add("e", new CheckBox("Use E"));
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q-W-E-R (On/Off)");
            _drawings.Add("q", new CheckBox("Draw Q", false));
            _drawings.Add("w", new CheckBox("Draw W", false));
            _drawings.AddSeparator(5);
            _drawings.Add("e", new CheckBox("Draw E", false));
            _drawings.Add("r", new CheckBox("Draw R", false));
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Use Draw R Damage (On/Off)");
            _drawings.Add("RKillStealD", new CheckBox("Damage Indicator [R Damage]"));
            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Auto Blade of the Ruined King and Bilgewater Cutlass");
            _misc.Add("botrk", new CheckBox("Use BotRk (On/Off)"));
            _misc.Add("autoCutlass", new CheckBox("Use Bilgewater Cutlass (On/Off)"));        
            _drawings.AddSeparator(5);
            _misc.AddLabel("Anti Gap Closer E+W (On/Off)");
            _misc.Add("egap", new CheckBox("Use E Anti Gap Closer (On/Off)"));
            _misc.Add("wgap", new CheckBox("Use W Anti Gap Closer (On/Off)"));
            _drawings.AddSeparator(5);
            _misc.AddLabel("Interrupt Dangerous Spells (On/Off)");
            _misc.Add("interruptW", new CheckBox("Use W Interrupt (On/Off)"));
            _drawings.AddSeparator(5);
            _misc.AddLabel("Auto R Kill Steal");
            _misc.Add("autoR", new CheckBox("Auto R (On/Off)"));
        }
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["q"].Cast<CheckBox>().CurrentValue) { _q.DrawRange(Color.FromArgb(130, Color.Green)); }
            if (_drawings["w"].Cast<CheckBox>().CurrentValue) { _w.DrawRange(Color.FromArgb(130, Color.Green)); }
            if (_drawings["e"].Cast<CheckBox>().CurrentValue) { _e.DrawRange(Color.FromArgb(130, Color.Green)); }
            if (_drawings["r"].Cast<CheckBox>().CurrentValue) { _r.DrawRange(Color.FromArgb(130, Color.Green)); }
        }
        private static void CaitlynActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LanClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JunClear();
            }           
            if (_misc["autoR"].Cast<CheckBox>().CurrentValue)
            {
                AutoKillR();
            }
            ManuelR();          
        }
        private static void LanClear()
        {
            if (!_laneclear["q"].Cast<CheckBox>().CurrentValue || !_q.IsReady() || (Caitlyn.ManaPercent < _laneclear["LmanaP"].Cast<Slider>().CurrentValue)) return;
            var farmQ = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Caitlyn.ServerPosition).Where(x => x.IsInRange(Caitlyn, _q.Range));
            var keyhitQ = _q.GetBestLinearCastPosition(farmQ);
            if (keyhitQ.HitNumber >= _laneclear["Qhit"].Cast<Slider>().CurrentValue)
            {
                _q.CastOnBestFarmPosition(1);
            }
        }
        private static void JunClear()
        {
            if (_jungleclear["q"].Cast<CheckBox>().CurrentValue && _q.IsReady() && (Caitlyn.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue))
            {
                var farmjungQ = EntityManager.MinionsAndMonsters.GetJungleMonsters(Caitlyn.ServerPosition, Caitlyn.GetAutoAttackRange()).OrderByDescending(x => x.MaxHealth).FirstOrDefault();
                if (farmjungQ != null)
                {
                    _q.Cast(farmjungQ.ServerPosition);
                }
            }
            if (!_jungleclear["e"].Cast<CheckBox>().CurrentValue || !_e.IsReady() || (Caitlyn.ManaPercent < _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)) return;
            {
                var farmjungE = EntityManager.MinionsAndMonsters.Monsters.Where(x => x.IsValidTarget(Caitlyn.GetAutoAttackRange())).ToList();
                if (!farmjungE.Any()) return;
                string[] monsters = { "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder", "SRU_Dragon_Water", "SRU_Baron" };
                if (farmjungE.Count == 1 && farmjungE.Any(b => monsters.Any(x => x.Contains(b.BaseSkinName))))
                {
                    _e.Cast(farmjungE.First());
                }
            }
        }
        public static void QandEafterAa(AttackableUnit hedefQandE, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            if (!(hedefQandE is AIHeroClient) || hedefQandE.IsZombie || hedefQandE.IsPhysicalImmune || hedefQandE.IsInvulnerable || !hedefQandE.IsEnemy || hedefQandE.IsDead) return;
            var qandE = hedefQandE as AIHeroClient;
            if (qandE == null) return;
            if (_combo["q"].Cast<CheckBox>().CurrentValue && _combo["AfterAaQ"].Cast<CheckBox>().CurrentValue && _q.IsReady())
            { _q.Cast(qandE); }     
            if (!_combo["e"].Cast<CheckBox>().CurrentValue || !_combo["AfterAaE"].Cast<CheckBox>().CurrentValue || !_e.IsReady()) return;          
                _e.Cast(qandE);         
        }
        private static void Combo()
        {         
            if (_combo["e"].Cast<CheckBox>().CurrentValue && _e.IsReady() && !_combo["AfterAaE"].Cast<CheckBox>().CurrentValue && !BuffChampEnemy)
            {
                var prophecyE = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_e.Range) && !SpellBuff(x) && !SpellShield(x));
                var hedefE = TargetSelector.GetTarget(prophecyE, DamageType.Physical);
                if (hedefE != null)
                {
                    var predE = _e.GetPrediction(hedefE);
                    if ((predE.HitChancePercent >= _combo["EHitChance"].Cast<Slider>().CurrentValue) && hedefE.Distance(Caitlyn) < 600)
                    {                 
                            _e.Cast(_e.GetPrediction(hedefE).CastPosition);
                    }
                }
            }   
            if (_combo["w"].Cast<CheckBox>().CurrentValue && _w.IsReady() && _combo["wlogic"].Cast<ComboBox>().CurrentValue == 0)
            {
                var prophecyW = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(700) && !SpellBuff(x) && !SpellShield(x));
                var hedefW = TargetSelector.GetTarget(prophecyW, DamageType.Physical);
                if (hedefW != null)
                {
                    var predW = _w.GetPrediction(hedefW);
                    if ((predW.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue) && (predW.CastPosition.Distance(hedefW) > 50) && Yordletrap(predW.CastPosition) && (_w.Handle.Ammo >= _combo["maxtrap"].Cast<Slider>().CurrentValue))
                    {
                        _w.Cast(predW.CastPosition);
                    }
                    else
                    {
                        var hedefW2 = TargetSelector.GetTarget(_w.Range, DamageType.Physical);
                        var prophecyW2 = Prediction.Position.PredictUnitPosition(hedefW2, 500).To3D();
                        var predW2 = _w.GetPrediction(hedefW2);
                        if (hedefW2 != null && _w.IsInRange(prophecyW2) && (predW2.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue) && (_w.Handle.Ammo >= _combo["maxtrap"].Cast<Slider>().CurrentValue))
                        {
                            _w.Cast(prophecyW2); return;
                        }
                    }
                }
            }
            if (_combo["w"].Cast<CheckBox>().CurrentValue && _w.IsReady() && _combo["wlogic"].Cast<ComboBox>().CurrentValue == 1)
            {
                var prophecyW = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(700) && !SpellBuff(x) && !SpellShield(x));             
                var hedefW = TargetSelector.GetTarget(prophecyW, DamageType.Physical);
                if (hedefW != null)
                {
                    var predW = _w.GetPrediction(hedefW);
                    if ((predW.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue) && (predW.CastPosition.Distance(hedefW) > 50) && Yordletrap(predW.CastPosition) && (_w.Handle.Ammo >= _combo["maxtrap"].Cast<Slider>().CurrentValue))
                    {
                        _w.Cast(predW.CastPosition); return;
                    }
                }
            }      
            if (_combo["w"].Cast<CheckBox>().CurrentValue && _w.IsReady() && _combo["wlogic"].Cast<ComboBox>().CurrentValue == 2)
            {
                var normalW = TargetSelector.GetTarget(_w.Range, DamageType.Physical);
                var normalprophecyW = Prediction.Position.PredictUnitPosition(normalW, 500).To3D();
                var normalpredW = _w.GetPrediction(normalW);
                if (normalW != null  && _w.IsInRange(normalprophecyW) && (normalpredW.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue) && (_w.Handle.Ammo >= _combo["maxtrap"].Cast<Slider>().CurrentValue))
                {
                    _w.Cast(normalprophecyW);
                }
            }
            if (!_combo["q"].Cast<CheckBox>().CurrentValue || !_q.IsReady() || BuffChampEnemy || _combo["AfterAaQ"].Cast<CheckBox>().CurrentValue) return;
            {
                  var qProphecy = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_q.Range) && !SpellShield(x) && !SpellBuff(x)).ToList();
                var targetQ = TargetSelector.GetTarget(qProphecy, DamageType.Physical);
                if (targetQ == null) return;             
                if (_combo["qlogic"].Cast<ComboBox>().CurrentValue == 0)
                {
                    var qPrediction = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput { CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall },
                        Delay = 0, From = Caitlyn.Position, Radius = 70, Range = _q.Range, RangeCheckFrom = Caitlyn.Position, Speed = _q.Speed, Target = targetQ, Type = SkillShotType.Linear });
                    if (qPrediction.HitChancePercent >= _combo["qHitChance"].Cast<Slider>().CurrentValue)
                    { _q.Cast(qPrediction.CastPosition); }
                }
                else
                {
                    var qPrediction = _q.GetPrediction(targetQ);
                    if (qPrediction.HitChancePercent >= _combo["qHitChance"].Cast<Slider>().CurrentValue)
                    { _q.Cast(qPrediction.CastPosition); }
                }
            }
        }
        private static void ManuelR()
        {           
            if (!_combo["r"].Cast<KeyBind>().CurrentValue || !_r.IsReady()) return;
            {
                var prophecyR = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_r.Range) && !SpellBuff(x) && !SpellShield(x) && !EntityManager.Heroes.Enemies.Where(y => y.NetworkId != x.NetworkId)
                                .Any(z => z.IsValidTarget() && new Geometry.Polygon.Rectangle(Caitlyn.Position, x.Position, 400).IsInside(z.ServerPosition)));
                var hedefR = TargetSelector.GetTarget(prophecyR, DamageType.Physical);
                if (hedefR == null) return;
                if (Caitlyn.CountEnemyHeroesInRangeWithPrediction(700, 600) == 0)
                {
                    _r.Cast(hedefR);
                }
            }
        }
        private static void AutoKillR()
        {
            var autoKill = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_r.Range) && !SpellBuff(x) && !SpellShield(x));
            foreach (var autoKillTarget in autoKill.Where(x => TotalHealth(x) < RDamage(x) && !Caitlyn.IsInAutoAttackRange(x) && !(Caitlyn.CountEnemyHeroesInRangeWithPrediction(600, 350) >= 1) && _r.IsReady() && _r.IsInRange(x) && !Caitlyn.IsUnderEnemyturret()))
            {
                _r.Cast(autoKillTarget);
            }
        }
        public static void DangerousSpellsWInterupt(Obj_AI_Base w,Interrupter.InterruptableSpellEventArgs winterupt)
        {
            if (!w.IsEnemy && !_misc["interruptW"].Cast<CheckBox>().CurrentValue) return;
            if (_w.IsReady() && winterupt.DangerLevel == DangerLevel.High)
            { _w.Cast(w.Position); }
        }
        private static void EandWAntiGapCloser(AIHeroClient eAndw, Gapcloser.GapcloserEventArgs eAndwGap)
        {
            if (_misc["egap"].Cast<CheckBox>().CurrentValue && eAndw.IsEnemy && eAndw.IsValidTarget(_e.Range) && eAndwGap.End.Distance(Caitlyn) <= 250)
            { _e.Cast(eAndwGap.End); }
            if (_misc["wgap"].Cast<CheckBox>().CurrentValue && eAndw.IsEnemy && eAndw.IsValidTarget(_w.Range) && eAndwGap.End.Distance(Caitlyn) <= 250)
            { _w.Cast(eAndwGap.End); }
        }
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && _r.IsReady() && Caitlyn.Distance(x) < 2000 && x.VisibleOnScreen))
            {
                switch (enemy.Hero)
                {
                    case Champion.Annie: _dikey = -1.8f; _yatay = -9; break;
                    case Champion.Corki: _dikey = -1.8f; _yatay = -9; break;
                    case Champion.Jhin: _dikey = -4.8f; _yatay = -9; break;
                    case Champion.Darius: _dikey = 9.8f; _yatay = -2; break;
                    case Champion.XinZhao: _dikey = 10.8f; _yatay = 2; break;
                    default: _dikey = 9.8f; _yatay = 2; break;
                }
                if (!_drawings["RKillStealD"].Cast<CheckBox>().CurrentValue) continue;
                var damage = RDamage(enemy);
                var hasarX = (enemy.TotalShieldHealth() - damage > 0 ? enemy.TotalShieldHealth() - damage : 0) / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var hasarY = enemy.TotalShieldHealth() / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var go = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarX * genislik), (int)enemy.HPBarPosition.Y + _dikey);
                var finish = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarY * genislik) + 1, (int)enemy.HPBarPosition.Y + _dikey);
                Drawing.DrawLine(go, finish, yukseklik, Color.FromArgb(180, Color.Green));
            }
        }
    }
}
