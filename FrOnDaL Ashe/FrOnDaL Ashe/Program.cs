using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Spells;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using System.Collections.Generic;
using Color = System.Drawing.Color;

namespace FrOnDaL_Ashe
{
    internal class Program
    {
        private static AIHeroClient Ashe => Player.Instance;
        private static Spellbook _lvl;
        private static Spell.Skillshot _w, /*_e,*/ _r;
        private static Spell.Active _q;
        private static Menu _main, _combo, _laneclear, _jungleclear, _harrass, _drawings, _misc;
        internal static bool IsPreAa;
        internal static bool IsAfterAa;
        private static float _dikey, _yatay;
        private static float genislik = 104;
        private static float yukseklik = 9.82f;
        private static readonly Item AutoBotrk = new Item(ItemId.Blade_of_the_Ruined_King);
        private static readonly Item AutoCutlass = new Item(ItemId.Bilgewater_Cutlass);
        public static PredictionResult HarrasWPred(Obj_AI_Base harrasW)
        {
            var coneW = new Geometry.Polygon.Sector(Ashe.Position, Game.CursorPos, (float)(Math.PI / 180 * 40), 1250, 9).Points.ToArray();
            for (var x = 1; x < 10; x++)
            {
                var prophecyW = Prediction.Position.PredictLinearMissile(harrasW, 1250, 20, 250, 1500, 0, Ashe.Position.Extend(coneW[x], 20).To3D());
                if (prophecyW.CollisionObjects.Any() || (prophecyW.HitChance < HitChance.High)) continue;
                return prophecyW;
            }
            return null;
        }
        private static bool SpellShield(Obj_AI_Base shield) { return shield.HasBuffOfType(BuffType.SpellShield) || shield.HasBuffOfType(BuffType.SpellImmunity); }
        private static bool SpellBuff(AIHeroClient buf)
        {
            if (buf.Buffs.Any(x => x.IsValid && (x.Name.Equals("ChronoShift", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("FioraW", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("TaricR", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("BardRStasis", StringComparison.CurrentCultureIgnoreCase) ||
                                       x.Name.Equals("JudicatorIntervention", StringComparison.CurrentCultureIgnoreCase) || x.Name.Equals("UndyingRage", StringComparison.CurrentCultureIgnoreCase) || (x.Name.Equals("kindredrnodeathbuff", StringComparison.CurrentCultureIgnoreCase) && (buf.HealthPercent <= 10)))))
            { return true; }
            if (buf.ChampionName != "Poppy") return buf.IsInvulnerable;
            return EntityManager.Heroes.Allies.Any(y => !y.IsMe && y.Buffs.Any(z => (z.Caster.NetworkId == buf.NetworkId) && z.IsValid && z.DisplayName.Equals("PoppyDITarget", StringComparison.CurrentCultureIgnoreCase))) || buf.IsInvulnerable;
        }
        private static void AutoItem(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var botrkHedef = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(x => x != null && x.IsValidTarget() && x.IsInRange(Ashe, 550)), DamageType.Physical);
            if (botrkHedef != null && _misc["botrk"].Cast<CheckBox>().CurrentValue && AutoBotrk.IsOwned() && AutoBotrk.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoBotrk.Cast(botrkHedef);
            }
            if (botrkHedef != null && _misc["autoCutlass"].Cast<CheckBox>().CurrentValue && AutoCutlass.IsOwned() && AutoCutlass.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoCutlass.Cast(botrkHedef);
            }
        }
        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (Ashe.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Ashe.Hero != Champion.Ashe) return;
            _q = new Spell.Active(SpellSlot.Q);
            _w = new Spell.Skillshot(SpellSlot.W, 1250, SkillShotType.Cone)
            { AllowedCollisionCount = 0, CastDelay = 250, ConeAngleDegrees = (int)(Math.PI / 180 * 40), Speed = 1500, Range = 1250, Width = 20 };
            //_e = new Spell.Skillshot(SpellSlot.E, 30000, SkillShotType.Linear);
            _r = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 250, 1600, 130) { AllowedCollisionCount = 0 };
            Game.OnTick += AsheActive;
            Drawing.OnDraw += SpellDraw;
            Drawing.OnEndScene += HasarGostergesi;
            Orbwalker.OnPreAttack += (a, b) => IsPreAa = true;
            Orbwalker.OnPostAttack += (a, b) => { IsPreAa = false; IsAfterAa = true; };
            Game.OnPostTick += aa => IsAfterAa = false;
            Obj_AI_Base.OnProcessSpellCast += AutoItem;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Interrupter.OnInterruptableSpell += DangerousSpellsRInterupt;
            Gapcloser.OnGapcloser += RAntiGapCloser;
            _lvl = Ashe.Spellbook;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Ashe Successfully loaded.");
            _main = MainMenu.AddMenu("FrOnDaL Ashe", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Ashe");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Ashe");
            _combo.AddLabel("Use Combo Q (On/Off)"+"                                 "+ "Use Combo W (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.Add("w", new CheckBox("Use W"));
            _combo.AddSeparator(5);
            _combo.Add("WHitChance", new Slider("W hitchance percent : {0}"));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use R");
            _combo.Add("r", new KeyBind("Use R Key", false, KeyBind.BindTypes.HoldActive, 'T')); 
            _combo.Add("RHitChance", new Slider("R hitchance percent : {0}"));
            _combo.Add("RMinRange", new Slider("R minimum range to cast", 350, 100, 700));        
            _combo.Add("RMaxRange", new Slider("R maximum range to cast", 1500, 700, 2000));
            _laneclear = _main.AddSubMenu("Laneclear");
            _laneclear.AddGroupLabel("LaneClear mode settings for Ashe");
            _laneclear.AddLabel("Use Lane Clear Q and W (On/Off)");
            _laneclear.Add("q", new CheckBox("Use Q"));
            _laneclear.Add("w", new CheckBox("Use W"));
            _laneclear.Add("LmanaP", new Slider("LaneClear Mana Control Min mana percentage ({0}%) to use Q and W", 70, 1));
            _jungleclear = _main.AddSubMenu("JungleClear");
            _jungleclear.AddGroupLabel("JungleClear mode settings for Ashe");
            _jungleclear.AddLabel("Use Jung Clear Q and W (On/Off)");
            _jungleclear.Add("q", new CheckBox("Use Q"));
            _jungleclear.Add("w", new CheckBox("Use W"));
            _jungleclear.Add("JmanaP", new Slider("JungleClear Mana Control Min mana percentage ({0}%) to use Q and W", 30, 1));
            _harrass = _main.AddSubMenu("Harass");
            _harrass.AddGroupLabel("Harass mode settings for Ashe");
            _harrass.AddLabel("Use Harass W (On/Off)");
            _harrass.Add("w", new CheckBox("Use W harass"));
            _harrass.Add("HmanaP", new Slider("Harass Mana Control Min mana percentage ({0}%) to use W", 50, 1));
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q-W-E-R (On/Off)");
            _drawings.Add("w", new CheckBox("Draw W", false));
            _drawings.Add("r", new CheckBox("Draw R", false));
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Use Draw R Damage (On/Off)");
            _drawings.Add("DamageR", new CheckBox("Damage Indicator [R Damage]"));
            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Auto Blade of the Ruined King and Bilgewater Cutlass");
            _misc.Add("botrk", new CheckBox("Use BotRk (On/Off)"));
            _misc.Add("autoCutlass", new CheckBox("Use Bilgewater Cutlass (On/Off)"));
            _drawings.AddSeparator(5);
            _misc.AddLabel("Anti Gap Closer R (On/Off)");
            _misc.Add("Rgap", new CheckBox("Use R Anti Gap Closer (On/Off)", false));
            _drawings.AddSeparator(5);
            _misc.AddLabel("Interrupt Dangerous Spells (On/Off)");
            _misc.Add("interruptR", new CheckBox("Use R Interrupt (On/Off)"));

        }
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["r"].Cast<CheckBox>().CurrentValue) { Circle.Draw(SharpDX.Color.Green, _combo["RMaxRange"].Cast<Slider>().CurrentValue, Ashe); }
            if (_drawings["w"].Cast<CheckBox>().CurrentValue) { _w.DrawRange(Color.FromArgb(130, Color.Green)); }
        }
        private static void AsheActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            { Combo(); }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            { LanClear(); }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            { JunClear(); }
            ManuelR();
            HarrasW();          
        }
        private static void LanClear()
        {
            var farmClear = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Ashe.ServerPosition).Where(x => x.IsValidTarget(_w.Range - 100)).ToList();
            if (!farmClear.Any()) return;
            if ((_q.IsReady() && _laneclear["q"].Cast<CheckBox>().CurrentValue && Ashe.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue && IsPreAa && farmClear.Count > 3) || (Ashe.CountEnemyChampionsInRange(590) >= 1))
            {
                _q.Cast();
            }
            if (!_w.IsReady() || !_laneclear["w"].Cast<CheckBox>().CurrentValue || !(Ashe.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue) || farmClear.Count <= 3) return;
            var farmW = _w.GetBestLinearCastPosition(farmClear);
            if (farmW.CastPosition != Vector3.Zero)
            {
                _w.Cast(farmW.CastPosition);
            }
        }
        private static void JunClear()
        {
            var farmjungclear = EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => x.IsValidTarget(Ashe.GetAutoAttackRange())).ToList();
            if (!farmjungclear.Any()) return;
            string[] monsters = { "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab","SRU_RiftHerald", "SRU_Dragon", "SRU_Baron" };
            if (_q.IsReady() && _jungleclear["q"].Cast<CheckBox>().CurrentValue && Ashe.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue && farmjungclear.Count(x => monsters.Contains(x.BaseSkinName, StringComparer.CurrentCultureIgnoreCase)) >= 1)
            {
                _q.Cast();
            }
            if (!_w.IsReady() || !_jungleclear["w"].Cast<CheckBox>().CurrentValue || !(Ashe.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)) return;
            {
                var farmjungclearW = farmjungclear.FirstOrDefault(x => monsters.Contains(x.BaseSkinName, StringComparer.CurrentCultureIgnoreCase));
                if (farmjungclearW == null || !(farmjungclearW.Health > Ashe.GetAutoAttackDamage(farmjungclearW, true)*2)) return;
                var pred = _w.GetPrediction(farmjungclearW);
                _w.Cast(pred.CastPosition);
            }
        }
        private static void HarrasW()
        {
            if (!_w.IsReady() || !_harrass["w"].Cast<CheckBox>().CurrentValue || !(Ashe.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue) || Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo) || Ashe.IsUnderEnemyturret()) return;
            var harrasWtarget = TargetSelector.GetTarget(_w.Range, DamageType.Physical);
            if (harrasWtarget == null) return;
            var harrasWprophecy = HarrasWPred(harrasWtarget);
            if (harrasWprophecy != null && harrasWprophecy.HitChance >= HitChance.High)
            {
                _w.Cast(harrasWprophecy.CastPosition);
            }
        }
        private static void ManuelR()
        {
            if (!_r.IsReady() || !_combo["r"].Cast<KeyBind>().CurrentValue) return;
            var targetManuelR = TargetSelector.GetTarget(_combo["RMaxRange"].Cast<Slider>().CurrentValue, DamageType.Physical);
            if (targetManuelR == null || SpellShield(targetManuelR) || SpellBuff(targetManuelR) || !(targetManuelR.Distance(Ashe) > _combo["RMinRange"].Cast<Slider>().CurrentValue)) return;
            var prophecyR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput {
                CollisionTypes = new HashSet<CollisionType> {  CollisionType.AiHeroClient, CollisionType.ObjAiMinion, CollisionType.YasuoWall },
                Delay = .25f, From = Ashe.Position, Radius = 120, Range = _combo["RMaxRange"].Cast<Slider>().CurrentValue, RangeCheckFrom = Ashe.Position,
                Speed = _r.Speed, Target = targetManuelR, Type = SkillShotType.Linear });
            if (prophecyR.HitChancePercent >= _combo["RHitChance"].Cast<Slider>().CurrentValue)
            {
                _r.Cast(prophecyR.CastPosition);
            }
        }
        public static PredictionResult ComboWPred(Obj_AI_Base comW)
        {
            var comboconeW = new Geometry.Polygon.Sector(Ashe.Position, Game.CursorPos, (float)(Math.PI / 180 * 40), 1250, 9).Points.ToArray();
            for (var x = 1; x < 10; x++)
            {
                var comboprophecyW = Prediction.Position.PredictLinearMissile(comW, 1250, 20, 250, 1500, 0, Ashe.Position.Extend(comboconeW[x], 20).To3D());
                if (comboprophecyW.CollisionObjects.Any() || (comboprophecyW.HitChancePercent < _combo["WHitChance"].Cast<Slider>().CurrentValue)) continue;
                return comboprophecyW;
            }
            return null;
        }
        private static void Combo()
        {
            if (_q.IsReady() && _combo["q"].Cast<CheckBox>().CurrentValue && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(Ashe.GetAutoAttackRange() - 50) && !IsPreAa))            
            { _q.Cast(); }

            if (!_w.IsReady() || !_combo["w"].Cast<CheckBox>().CurrentValue) return;
            {
                var prophecyW = EntityManager.Heroes.Enemies.Where( x => { if (!x.IsValidTarget(_w.Range)) return false; var wPred = ComboWPred(x); if (wPred == null) return false; return !SpellShield(x) && (wPred.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue); }).ToList();
                if (!prophecyW.Any() || IsPreAa) return;
                var targetW = TargetSelector.GetTarget(prophecyW, DamageType.Physical);
                if (targetW == null) return;
                var wPred2 = ComboWPred(targetW);
                if (wPred2 != null && wPred2.HitChancePercent >= _combo["WHitChance"].Cast<Slider>().CurrentValue)
                { _w.Cast(wPred2.CastPosition); }               
            }
        }
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && _r.IsReady() && Ashe.Distance(x) < 2000 && x.VisibleOnScreen))
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
                var damage = Ashe .GetSpellDamage(enemy, SpellSlot.R);
                var hasarX = (enemy.TotalShieldHealth() - damage > 0 ? enemy.TotalShieldHealth() - damage : 0) / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var hasarY = enemy.TotalShieldHealth() / (enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield);
                var go = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarX * genislik), (int)enemy.HPBarPosition.Y + _dikey);
                var finish = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarY * genislik) + 1, (int)enemy.HPBarPosition.Y + _dikey);
                Drawing.DrawLine(go, finish, yukseklik, Color.FromArgb(180, Color.Green));
            }
        }
        private static void DangerousSpellsRInterupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!_r.IsReady() || _misc["interruptR"].Cast<CheckBox>().CurrentValue || (args.DangerLevel != DangerLevel.Medium && args.DangerLevel != DangerLevel.High) || !(Ashe.Mana > 200) || !sender.IsValidTarget(3000)) return;         
            var prophecyR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput{
                CollisionTypes = new HashSet<CollisionType> { CollisionType.AiHeroClient, CollisionType.ObjAiMinion, CollisionType.YasuoWall },
                Delay = .25f, From = Ashe.Position, Radius = 140, Range = 2500, RangeCheckFrom = Ashe.Position, Speed = _r.Speed, Target = sender, Type = SkillShotType.Linear });
            if (prophecyR.HitChance < HitChance.High) return;
            _r.Cast(prophecyR.CastPosition);
        }
        private static void RAntiGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs rGap)
        {
            if (!_misc["Rgap"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || !sender.IsValidTarget(1000) || !(Ashe.Mana > 200) || !(rGap.End.Distance(Ashe) <= 250)) return;
            var prophecyR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput {
                CollisionTypes = new HashSet<CollisionType> { CollisionType.AiHeroClient, CollisionType.ObjAiMinion},
                Delay = .25f, From = Ashe.Position, Radius = 140, Range = 2500, RangeCheckFrom = Ashe.Position, Speed = _r.Speed, Target = sender, Type = SkillShotType.Linear });
            if (prophecyR.HitChance < HitChance.High) return;
            _r.Cast(prophecyR.CastPosition);
        }
    }
}
