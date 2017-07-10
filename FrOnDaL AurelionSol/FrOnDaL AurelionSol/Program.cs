using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using Color = System.Drawing.Color;

namespace FrOnDaL_AurelionSol
{
    internal class Program
    {
        private static AIHeroClient AurelionSol => Player.Instance;
        private static Spell.Skillshot _q, _r, _w, _w1, _q1/*, _e*/;
        private static Menu _main, _combo, _laneclear, _jungleclear, _drawings, _misc;
        private static float _dikey, _yatay;
        private static float _genislik = 104;
        private static float _yukseklik = 9.82f;
        private static Spellbook _lvl;
        private static bool IsWActive => AurelionSol.HasBuff("AurelionSolWActive");
        private static double RDamage(Obj_AI_Base d)
        {
            var expiryDamage = AurelionSol.CalculateDamageOnUnit(d, DamageType.Magical, (float)new double[] { 200, 400, 600 }[_r.Level - 1] + 0.70f * AurelionSol.TotalMagicalDamage);
            return expiryDamage;
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
        private static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (AurelionSol.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (AurelionSol.Hero != Champion.AurelionSol) return;
            _q = new Spell.Skillshot(SpellSlot.Q, 600, SkillShotType.Cone, 250, 850, 180);
            _q1 = new Spell.Skillshot(SpellSlot.Q, 670, SkillShotType.Circular, 250, 850, 150);          
            _w = new Spell.Skillshot(SpellSlot.W, 675, SkillShotType.Circular, 250, 850, 300);
            _w1 = new Spell.Skillshot(SpellSlot.W, 375, SkillShotType.Circular, 250, 850, 150);
            //_e = new Spell.Skillshot(SpellSlot.E, 5000, SkillShotType.Linear, 250, 1500, 240);
            _r = new Spell.Skillshot(SpellSlot.R, 1250, SkillShotType.Linear, 250, 1950, 120) { AllowedCollisionCount = -1 };

            Game.OnTick += AurelionSolActive;
            Drawing.OnDraw += SpellDraw;
            Drawing.OnEndScene += HasarGostergesi;
            Gapcloser.OnGapcloser += AntiGapCloser;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            _lvl = AurelionSol.Spellbook;

            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Aurelion Sol Successfully loaded.");
            _main = MainMenu.AddMenu("FrOnDaL AurelionSol", "index");
            _main.AddGroupLabel("Welcome FrOnDaL AurelionSol");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(10);
            _main.AddLabel("My good deeds -> FrOnDaL");

             /*Combo Menu*/
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for AurelionSol");
            _combo.AddLabel("Use Combo Q (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.AddSeparator(5);
            _combo.Add("qHit", new Slider("Q Hit {0} Use Q", 1, 1, 5));
            _combo.AddSeparator(5);
            _combo.Add("QHitChance", new Slider("Q hitchance percent : {0}", 60));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo W (On/Off)");
            _combo.Add("w", new CheckBox("Use W"));
            _combo.AddSeparator(5);
            _combo.Add("wHit", new Slider("W Hit {0} Use Q", 1, 1, 3));
            _combo.AddSeparator(5);
            _combo.Add("WHitChance", new Slider("W hitchance percent : {0}", 60));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Manual R Key Setting");
            _combo.Add("RKey", new KeyBind("Manual R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _combo.Add("RHit", new Slider("Manual R Hits {0}", 1, 1, 5));
            _combo.AddSeparator(5);
            _combo.Add("RHitChance", new Slider("R hitchance percent : {0}", 60));
            /*Combo Menu*/

            /*LaneClear Menu*/
            _laneclear = _main.AddSubMenu("LaneClear");
            _laneclear.AddGroupLabel("LaneClear mode settings for AurelionSol");
            _laneclear.Add("LmanaP", new Slider("Lane Clear Mana Control Min mana percentage ({0}%) to use Q and W", 70, 1));
            _laneclear.AddSeparator(5);
            _laneclear.Add("q", new CheckBox("Use Q (On/Off)"));
            _laneclear.Add("qHit", new Slider("Hit {0} Units Minions Use Q", 3, 1, 6));
            _laneclear.AddSeparator(5);
            _laneclear.Add("W", new CheckBox("Use w (On/Off)"));
            /*LaneClear Menu*/

            /*JungClear Menu*/
            _jungleclear = _main.AddSubMenu("JungClear");
            _jungleclear.AddGroupLabel("JungClear mode settings for AurelionSol");
            _jungleclear.Add("JmanaP", new Slider("Jung Clear Mana Control Min mana percentage ({0}%) to use Q", 30, 1));
            _jungleclear.AddSeparator(5);
            _jungleclear.AddLabel("Jung Clear Use Q an W (On/Off)");
            _jungleclear.Add("q", new CheckBox("Use Q"));
            /*JungClear Menu*/

            /*Drawing Menu*/
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddGroupLabel("Drawings mode settings for AurelionSol");
            _drawings.AddLabel("Use Drawings Q-E-R (On/Off)");
            _drawings.Add("drawQ", new CheckBox("Draw Q", false));
            _drawings.Add("drawW", new CheckBox("Draw W", false));
            _drawings.Add("drawR", new CheckBox("Draw R", false));
            _drawings.AddLabel("Use Draw Damage (On/Off)");
            _drawings.Add("damageR", new CheckBox("Draw damage indicator R"));
            /*Drawing Menu*/

            /*Misc Menu*/
            _misc = _main.AddSubMenu("Misc");
            _drawings.AddSeparator(5);
            _misc.AddLabel("Anti Gap Closer Q an R (On/Off)");
            _misc.Add("Rqgap", new CheckBox("Use Q and R Anti Gap Closer (On/Off)", false));
            _drawings.AddSeparator(5);
            _misc.AddLabel("Auto R Kill Steal");
            _misc.Add("autoR", new CheckBox("Auto R (On/Off)"));
            /*Misc Menu*/
        }

        /*SpellDraw*/
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["drawQ"].Cast<CheckBox>().CurrentValue)
            {
                _q.DrawRange(Color.FromArgb(130, Color.Green));
            }
            if (_drawings["drawW"].Cast<CheckBox>().CurrentValue)
            {
                if (IsWActive)
                {
                    _w.DrawRange(Color.FromArgb(130, Color.Green));
                }
                else if (!IsWActive)
                {
                    _w1.DrawRange(Color.FromArgb(130, Color.Green));
                }             
            }
            if (_drawings["drawR"].Cast<CheckBox>().CurrentValue)
            {
                _r.DrawRange(Color.FromArgb(130, Color.Green));
            }
        }
        /*SpellDraw*/

        /*AurelionSol Active*/
        private static void AurelionSolActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                 Combo();
                if (IsWActive && ((AurelionSol.CountEnemyChampionsInRange(350) >= 1) || (AurelionSol.CountEnemyChampionsInRange(1000) == 0)))
                {
                    _w1.Cast(AurelionSol);
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
                if (IsWActive && ((AurelionSol.CountEnemyMinionsInRange(400) >= 3) || (AurelionSol.CountEnemyMinionsInRange(1000) == 0) || (AurelionSol.ManaPercent < _laneclear["LmanaP"].Cast<Slider>().CurrentValue -8)))
                {
                    _w1.Cast(AurelionSol);                  
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungClear();
            }
            if (_misc["autoR"].Cast<CheckBox>().CurrentValue)
            {
                AutoKillR();
            }
            ManuelR();
            

           
        }
        /*AurelionSol Active*/
       
        /*LaneClear*/
        private static void LaneClear()
        {
            if (_laneclear["q"].Cast<CheckBox>().CurrentValue && AurelionSol.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)
            {
                var farm = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, AurelionSol.ServerPosition).Where(x => x.IsInRange(AurelionSol, 650)).ToList();
                if (!farm.Any()) return;
                if (farm.Count >= _laneclear["qHit"].Cast<Slider>().CurrentValue && _q.IsReady())
                {
                    _q1.CastOnBestFarmPosition();
                }
            }
            if (!_laneclear["w"].Cast<CheckBox>().CurrentValue) return;
            {            
               var farmw = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, AurelionSol.ServerPosition).Where(x => x.IsValidTarget(_w.Range)).ToList();              
               if (!farmw.Any()) return;
               if (!IsWActive && (farmw.Count >= 3) && _w.IsReady() && AurelionSol.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)
                    {
                        _w.CastOnBestFarmPosition();
                    }                      
            }
        }
        /*LaneClear*/

        /*JungClear*/
        private static void JungClear()
        {
            if (!_jungleclear["q"].Cast<CheckBox>().CurrentValue || !(AurelionSol.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)) return;
            var farmjungQ = EntityManager.MinionsAndMonsters.GetJungleMonsters(AurelionSol.ServerPosition).FirstOrDefault(x => x.IsInRange(AurelionSol, 650));
            if (farmjungQ != null && _q.IsReady())
            {
                _q1.Cast(farmjungQ.ServerPosition);
            }
        }
        /*JungClear*/

        /*Combo*/
        private static void Combo()
        {
            if (_combo["q"].Cast<CheckBox>().CurrentValue && _q.IsReady())
            {
                var qProphecy = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(650) && !SpellShield(x) && !SpellBuff(x)).ToList();                
                var targetQ = TargetSelector.GetTarget(qProphecy, DamageType.Magical);
            
                if (targetQ != null && qProphecy.Count >= _combo["qHit"].Cast<Slider>().CurrentValue)
                {
                  _q.CastMinimumHitchance(targetQ, _combo["QHitChance"].Cast<Slider>().CurrentValue); 
                }   
            }

            if (!_combo["w"].Cast<CheckBox>().CurrentValue && _w.IsReady()) return;
            {               
                var wProphecy = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_w.Range)).ToList();
                var targetW = TargetSelector.GetTarget(wProphecy, DamageType.Magical);
                if (targetW == null) return;
                if (IsWActive || wProphecy.Count < _combo["wHit"].Cast<Slider>().CurrentValue) return;
                if (targetW.IsValidTarget(500)) return;                       
                _w.CastMinimumHitchance(targetW, _combo["WHitChance"].Cast<Slider>().CurrentValue);
            }           
        }
        /*Combo*/


        /*ManuelR*/
        private static void ManuelR()
        {
            if (!_r.IsReady() || !_combo["RKey"].Cast<KeyBind>().CurrentValue) return;
            var rProphecy = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(_r.Range) && !SpellShield(x) && !SpellBuff(x)).ToList();
            var targetR = TargetSelector.GetTarget(rProphecy, DamageType.Magical);
            if (targetR == null) return;
            if (rProphecy.Count >= _combo["RHit"].Cast<Slider>().CurrentValue)
            {
                _r.CastMinimumHitchance(targetR, _combo["RHitChance"].Cast<Slider>().CurrentValue);
            }
        }
        /*ManuelR*/

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
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && _r.IsReady() && AurelionSol.Distance(x) < 2000 && x.VisibleOnScreen))
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

        /*AntiGapCloser*/
        private static void AntiGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs rGap)
        {
            if (_q.IsReady() && _misc["Rqgap"].Cast<CheckBox>().CurrentValue)
            {
                var qPred = _q.GetPrediction(sender);
                if (sender.IsValidTarget(_q.Range))
                {
                    if (qPred.HitChance >= HitChance.High)  _q.Cast(qPred.CastPosition);
                }
            }
            if (!_r.IsReady() || !_misc["Rqgap"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || !sender.IsValidTarget(1000) || !(rGap.End.Distance(AurelionSol) <= 250)) return;
            var rProphecy = TargetSelector.GetTarget(_r.Range, DamageType.Magical);
            var rpred = _w.GetPrediction(rProphecy);
            if (rpred.HitChance < HitChance.High) return;
            _r.Cast(rpred.CastPosition);
        }
        /*AntiGapCloser*/
    }
}
