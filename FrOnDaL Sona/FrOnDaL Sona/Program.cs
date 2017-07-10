using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Spells;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using System.Collections.Generic;
using Color = System.Drawing.Color;

namespace FrOnDaL_Sona
{
    internal class Program
    {
        private static AIHeroClient Sona => Player.Instance;
        private static Spellbook _lvl;
        private static Spell.Active _q, _w,  _e;
        private static Spell.Skillshot _r;
        private static Menu _main, _combo, _harrass, _clear, _drawings, _misc;
        private static float _dikey, _yatay;
        private static float _genislik = 104;
        private static float _yukseklik = 9.82f;
        private static int SonaPassive()
        {
            return (from buff in Sona.Buffs where buff.Name == "sonapassivecount" select buff.Count).FirstOrDefault();
        }
        private static bool PassiveActive => Sona.HasBuff("sonapassiveattack");
        private static double RDamage(Obj_AI_Base d)
        {
             var damageR = Sona.CalculateDamageOnUnit(d, DamageType.Magical, (float)new double[] { 150, 250, 350 }[_r.Level - 1] + Sona.TotalMagicalDamage / 100 * 50); return damageR;
        }
        private static float TotalHealth(AttackableUnit enemy, bool magicShields = false)
        {
            return enemy.Health + enemy.AllShield + enemy.AttackShield + (magicShields ? enemy.MagicShield : 0);
        }
        private static bool SpellShield(Obj_AI_Base shield) { return shield.HasBuffOfType(BuffType.SpellShield) || shield.HasBuffOfType(BuffType.SpellImmunity); }
        private static bool SpellBuff(AIHeroClient buf)
        {
            if (buf.Buffs.Any(x => x.IsValid && (x.Name.Equals("ChronoShift", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("FioraW", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("TaricR", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("BardRStasis", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("JudicatorIntervention", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("UndyingRage", StringComparison.CurrentCultureIgnoreCase) || (x.Name.Equals("kindredrnodeathbuff", StringComparison.CurrentCultureIgnoreCase) && (buf.HealthPercent <= 10)))))
            { return true; }
            if (buf.ChampionName != "Poppy") return buf.IsInvulnerable;
            return EntityManager.Heroes.Allies.Any(y => !y.IsMe && y.Buffs.Any(z => (z.Caster.NetworkId == buf.NetworkId) && z.IsValid && z.DisplayName.Equals("PoppyDITarget", StringComparison.CurrentCultureIgnoreCase))) || buf.IsInvulnerable;
        }
        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (Sona.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Sona.Hero != Champion.Sona) return;

            _q = new Spell.Active(SpellSlot.Q, 825);
            _w = new Spell.Active(SpellSlot.W, 1000);
            _e = new Spell.Active(SpellSlot.E, 430);
            _r = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Circular, 250, 2400, 140);

            Game.OnTick += SonaActive;
            Interrupter.OnInterruptableSpell += DangerousSpellsRInterupt;
            Gapcloser.OnGapcloser += AntiGapCloser;
            Drawing.OnEndScene += HasarGostergesi;
            Drawing.OnDraw += SpellDraw;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            _lvl = Sona.Spellbook;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Sona Successfully loaded.");

            _main = MainMenu.AddMenu("FrOnDaL Sona", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Sona");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");

            /*Combo*/
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Sona");
            _combo.AddLabel("Use Combo Q (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.Add("qHit", new Slider("Q Hits Enemies", 1, 1, 2));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo W (On/Off)");
            _combo.Add("w", new CheckBox("Use W"));
            _combo.AddLabel("Allies Health Settings (On/Off)");
            foreach (var x in EntityManager.Heroes.Allies)
            {
                _combo.Add("heroW" + x.Hero, new CheckBox(x.ChampionName + " Use W (On/Off)"));
                _combo.Add("allyhpW" + x.Hero, new Slider(x.ChampionName + " Minimum Heal  Percentage %{0} Use W (On/Off)", 70, 1));
                _combo.AddSeparator(5);

            }
            _combo.AddLabel("Use Combo E (On/Off)" + "                                  " + "Combo in Use R");
            _combo.Add("e", new CheckBox("Use E"));          
            _combo.Add("comboR", new CheckBox("Combo Use R", false));
            _combo.Add("eheal", new CheckBox("Use E if enemy health is below 20%"));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Manuel R");
            _combo.Add("r", new KeyBind("Use R Key", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _combo.Add("RHitChance", new Slider("R hitchance percent : {0}"));
            _combo.AddSeparator(5);
            _combo.Add("RHit", new Slider("Manual R Hits ", 1, 1, 5));
            /*Combo*/

            /*Harass*/
            _harrass = _main.AddSubMenu("Harass");
            _harrass.AddGroupLabel("Harass mode settings for Sona");
            _harrass.AddLabel("Harass Mana Control");
            _harrass.Add("HmanaP", new Slider("Harass Mana Control Min mana percentage ({0}%) to use W", 30, 1));
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Use Harass Q (On/Off)");
            _harrass.Add("q", new CheckBox("Use Q harass"));
            _harrass.Add("qhit", new CheckBox("Q Hits Enemies 2 Use Q", false));
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Use Harass W (On/Off)");
            _harrass.Add("w", new CheckBox("Use W harass"));
            _harrass.AddLabel("Allies Health Settings (On/Off)");
            foreach (var x in EntityManager.Heroes.Allies)
            {
                _harrass.Add("heroW" + x.Hero, new CheckBox(x.ChampionName + " Use W (On/Off)"));
                _harrass.Add("allyhpW" + x.Hero, new Slider(x.ChampionName + " Minimum Heal  Percentage %{0} Use W (On/Off)", 50, 1));
                _harrass.AddSeparator(5);

            }
            _harrass.AddSeparator(5);
            /*Harass*/

            /*Clear*/
            _clear = _main.AddSubMenu("Clear(LaneJung)");
            _clear.AddGroupLabel("Lane Clear Mode Settings");
            _clear.Add("LmanaP", new Slider("LaneClear Mana Control Min mana percentage ({0}%) to use Q and W", 70, 1));
            _clear.AddLabel("Use Lane Clear Q (On/Off)");
            _clear.Add("qLane", new CheckBox("Use Q (Lane)", false));
            _clear.AddSeparator(5);
            _clear.AddGroupLabel("Jung Clear Mode Settings");
            _clear.Add("JmanaP", new Slider("JungClear Mana Control Min mana percentage ({0}%) to use Q and W", 30, 1));
            _clear.AddLabel("Use Jung Clear Q (On/Off)");
            _clear.Add("qJung", new CheckBox("Use Q (Jung)", false));
            /*Clear*/

            /*Draw*/
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q-W-E-R (On/Off)");
            _drawings.Add("q", new CheckBox("Draw Q", false));
            _drawings.Add("w", new CheckBox("Draw W", false));
            _drawings.Add("e", new CheckBox("Draw E", false));
            _drawings.Add("r", new CheckBox("Draw R", false));
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Use Draw R Damage (On/Off)");
            _drawings.Add("DamageR", new CheckBox("Damage Indicator [R Damage]"));
            /*Draw*/

            /*Misc*/
            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Anti Gap Closer W-E-R (On/Off)");
            _misc.Add("Wgap", new CheckBox("Use W Anti Gap Closer (On/Off)"));
            _misc.Add("Egap", new CheckBox("Use E Anti Gap Closer (On/Off)"));
            _misc.Add("Rgap", new CheckBox("Use R Anti Gap Closer (On/Off)", false));
            _misc.AddSeparator(5);
            _misc.AddLabel("Interrupt Dangerous Spells (On/Off)" + "                  " + "Auto R Kill Steal");
            _misc.Add("interruptR", new CheckBox("Use R Interrupt (On/Off)"));
            _misc.Add("autoR", new CheckBox("Auto R (On/Off)", false));
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
            if (_drawings["e"].Cast<CheckBox>().CurrentValue)
            {
                _e.DrawRange(Color.FromArgb(130, Color.Green));
            }
            if (_drawings["r"].Cast<CheckBox>().CurrentValue)
            {
                _r.DrawRange(Color.FromArgb(130, Color.Green));
            }
        }
        /*SpellDraw*/

        /*SonaActive*/
        private static void SonaActive(EventArgs args)
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
            if (_combo["r"].Cast<KeyBind>().CurrentValue)
            {
                ManuelR();
            }
            if (_misc["autoR"].Cast<CheckBox>().CurrentValue)
            {
                AutoKillR();
            }
            Harras();
        }
        /*SonaActive*/

        private static void LanClear()
        {
            var farmClear = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Sona.ServerPosition).Where(x => x.IsValidTarget(_q.Range)).ToList();
            if (!farmClear.Any()) return;
            if (_q.IsReady() && _clear["qLane"].Cast<CheckBox>().CurrentValue && Sona.ManaPercent >= _clear["LmanaP"].Cast<Slider>().CurrentValue && farmClear.Count >= 2)
            {
                _q.Cast();
            }
        }
        private static void JunClear()
        {
            var farmjungclear = EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => x.IsValidTarget(Sona.GetAutoAttackRange())).ToList();
            if (!farmjungclear.Any()) return;         
            if (_q.IsReady() && _clear["qJung"].Cast<CheckBox>().CurrentValue && Sona.ManaPercent >= _clear["JmanaP"].Cast<Slider>().CurrentValue)
            {
                _q.Cast();
            }         
        }
        /*Harass*/
        private static void Harras()
        {
            if (_q.IsReady() && Sona.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue && !Sona.IsUnderEnemyturret())
            {
                var harrasQtarget = TargetSelector.GetTarget(_q.Range, DamageType.Magical);
                var harrasQ = EntityManager.Heroes.Enemies.Count(x => Sona.IsInRange(x, _q.Range) && x.IsValid && !x.IsDead);
                if (harrasQtarget != null)
                {
                    if (harrasQ >= 2 && _harrass["qHit"].Cast<CheckBox>().CurrentValue && (SonaPassive() == 2 || PassiveActive))
                    {
                        _q.Cast();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, harrasQtarget);
                    }
                    else if (_harrass["q"].Cast<CheckBox>().CurrentValue && !_harrass["qHit"].Cast<CheckBox>().CurrentValue && (SonaPassive()==2 || PassiveActive))
                    {
                        _q.Cast();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, harrasQtarget);
                    }
                }
            }
            if (!_harrass["w"].Cast<CheckBox>().CurrentValue || !_w.IsReady() || !(Sona.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue)) return;
            foreach (var allies in EntityManager.Heroes.Allies.Where(y => y.HealthPercent < _harrass["allyhpW" + y.Hero].Cast<Slider>().CurrentValue && _harrass["heroW" + y.Hero].Cast<CheckBox>().CurrentValue).Where(allies => allies.IsValidTarget(_w.Range)))
            {
                _w.Cast(allies);
            }
        }
        /*Harass*/

        /*ManuelR*/
        private static void ManuelR()
        {
            if (!_r.IsReady()) return;            
            var hedefR = TargetSelector.GetTarget(_r.Range, DamageType.Magical);
            if (hedefR == null) return;
            var manuelR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput { CollisionTypes = new HashSet<CollisionType> { CollisionType.YasuoWall, CollisionType.AiHeroClient }, Delay = .25f, From = Sona.Position, Radius = _r.Width, Range = _r.Range, RangeCheckFrom = Sona.Position, Speed = _r.Speed, Target = hedefR, Type = SkillShotType.Linear });
            if ((manuelR.HitChancePercent >= _combo["RHitChance"].Cast<Slider>().CurrentValue) && (hedefR.CountEnemyChampionsInRange(_r.Width) >= _combo["RHit"].Cast<Slider>().CurrentValue) && !SpellBuff(hedefR) && !SpellShield(hedefR))
            {
                _r.Cast(manuelR.CastPosition);
            }
        }
        /*ManuelR*/

        /*Combo*/
        private static void Combo()
        {
            if (_q.IsReady() && _combo["q"].Cast<CheckBox>().CurrentValue)
            {
                var comboTargetQ = TargetSelector.GetTarget(_q.Range, DamageType.Magical);
                var comboQ = EntityManager.Heroes.Enemies.Count(x => Sona.IsInRange(x, _q.Range) && x.IsValid && !x.IsDead);
                if (comboTargetQ != null)
                {
                    if (comboQ >= _combo["qHit"].Cast<Slider>().CurrentValue)
                    {
                        _q.Cast();
                    }
                }
            }
            if (_combo["w"].Cast<CheckBox>().CurrentValue && _w.IsReady())
            {
                foreach (var allies in EntityManager.Heroes.Allies.Where(y => y.HealthPercent < _combo["allyhpW" + y.Hero].Cast<Slider>().CurrentValue && _combo["heroW" + y.Hero].Cast<CheckBox>().CurrentValue).Where(allies => allies.IsValidTarget(_w.Range)))
                {
                    _w.Cast(allies);
                }
            }
            if (_combo["comboR"].Cast<CheckBox>().CurrentValue)
            {
                ManuelR();
            }
            if (!_combo["e"].Cast<CheckBox>().CurrentValue || !_e.IsReady()) return;
            var comboTargetE = TargetSelector.GetTarget(1700, DamageType.Magical);
            if (comboTargetE == null) return;
            UseESmart(comboTargetE);
            if (comboTargetE.Health < comboTargetE.MaxHealth / 100 * 20 && _e.IsReady() && _combo["eheal"].Cast<CheckBox>().CurrentValue)
            {
                _e.Cast();
            }
        }
        /*Combo*/

        /*Thanks DETUKS*/
        private static void UseESmart(Obj_AI_Base target)
        {
            try
            {
                if (target.Path.Length == 0 || !target.IsMoving) return;
                var nextEnemPath = target.Path[0].To2D();
                var dist = Sona.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(Sona.Position.To2D());
                if (distToNext <= dist) return;
                var msDif = Sona.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !target.IsInAutoAttackRange(target))  _e.Cast();
                var reachIn = dist / msDif;
                if (reachIn > 4) _e.Cast();
            }
            catch
            {
                // ignored
            }
        }
        /*Thanks DETUKS*/

        /*AutoKillR*/
        private static void AutoKillR()
        {
            var autoKill = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_r.Range) && !SpellBuff(x) && !SpellShield(x));
            foreach (var autoKillTarget in autoKill.Where(x => TotalHealth(x) < RDamage(x) && _r.IsReady() && _r.IsInRange(x)))
            {
                _r.CastMinimumHitchance(autoKillTarget, HitChance.High);
            }
        }
        /*AutoKillR*/

        /*Damage Indicator*/
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && _r.IsReady() && Sona.Distance(x) < 2000 && x.VisibleOnScreen))
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
                if (!_drawings["DamageR"].Cast<CheckBox>().CurrentValue) continue;
                var damage = RDamage(enemy);
                var hasarX = (enemy.TotalShieldHealth() - damage > 0 ? enemy.TotalShieldHealth() - damage : 0) / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var hasarY = enemy.TotalShieldHealth() / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var go = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarX * _genislik), (int)enemy.HPBarPosition.Y + _dikey);
                var finish = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarY * _genislik) + 1, (int)enemy.HPBarPosition.Y + _dikey);
                Drawing.DrawLine(go, finish, _yukseklik, Color.FromArgb(180, Color.Green));
            }
        }
        /*Damage Indicator*/

        /*Interrupter*/
        private static void DangerousSpellsRInterupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!_misc["interruptR"].Cast<CheckBox>().CurrentValue || !sender.IsValid || sender.IsDead || !sender.IsTargetable || sender.IsStunned) return;
            if (_r.IsReady() && _r.IsInRange(sender.Position) && args.DangerLevel == DangerLevel.Medium)
            {            
                _r.Cast(sender.Position);               
            }
        }
        /*Interrupter*/

        /*AntiGapCloser*/
        private static void AntiGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gap)
        {
            if (_misc["Wgap"].Cast<CheckBox>().CurrentValue && _w.IsReady() && gap.End.Distance(Sona) <= 250)
            {
                _w.Cast();
            }

            if (_misc["Egap"].Cast<CheckBox>().CurrentValue && _e.IsReady() && gap.End.Distance(Sona) <= 250)
            {
                _e.Cast();
            }
                
            if (!_misc["Rgap"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || !sender.IsValidTarget(1000) || !(gap.End.Distance(Sona) <= 250)) return;
            if (_r.IsReady() && _r.IsInRange(sender.Position))
            {
                _r.Cast(sender.Position);
            }
        }
        /*AntiGapCloser*/
    }
}
