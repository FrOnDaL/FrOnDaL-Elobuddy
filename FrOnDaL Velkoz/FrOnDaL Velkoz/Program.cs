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
 
namespace FrOnDaL_Velkoz
{
    internal class Program
    {
        private static AIHeroClient Velkoz => Player.Instance;
        private static Spellbook _lvl;
        private static Spell.Skillshot _q, _qSplit, _qDummy, _w, _e, _r;
        private static float _dikey, _yatay;
        private static float _genislik = 104;
        private static float _yukseklik = 9.82f;
        private static Menu _main, _combo, _laneclear, _jungleclear, _harrass, _drawings, _misc;
        private static MissileClient _qMissile;
        private static double RDamage(Obj_AI_Base d)
        {
            var damageR = Velkoz.CalculateDamageOnUnit(d, DamageType.Magical, (float)new double[] { 450, 625, 800 }[_r.Level - 1] + Velkoz.TotalMagicalDamage / 100 * 125); return damageR;
        }
        private static float TotalHealth(AttackableUnit enemy, bool magicShields = false)
        {
            return enemy.Health + enemy.AllShield + enemy.AttackShield + (magicShields ? enemy.MagicShield : 0);
        }
        private static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args) { if (Velkoz.Level > 4) { _lvl.LevelSpell(SpellSlot.R); } }
        /*UltiLogicFollowEnemy*/
        private static Vector3 _rCastPos; // Store last R Cast Position
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.R)
                _rCastPos = sender.ServerPosition.Extend(args.End, _r.Range).To3DWorld(); //Extend the casted position to max R Rnage
        }     
        /*UltiLogicFollowEnemy*/
        private static void Main() { Loading.OnLoadingComplete += OnLoadingComplete; }

        private static void OnLoadingComplete(EventArgs args)
        {
            if (Velkoz.Hero != Champion.Velkoz) return;
         
            _q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear) { AllowedCollisionCount = 0, Range = 1200, CastDelay = 250, Speed = 1300, Width = 50 };
            _qSplit = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear) { AllowedCollisionCount = 0, Range = 1100, CastDelay = 250, Speed = 2100, Width = 55 };
            _qDummy = new Spell.Skillshot(SpellSlot.Q, (uint)Math.Sqrt(Math.Pow(_q.Range, 2) + Math.Pow(_qSplit.Range, 2)), SkillShotType.Linear)
            { AllowedCollisionCount = 0, Range = (uint) Math.Sqrt(Math.Pow(_q.Range, 2) + Math.Pow(_qSplit.Range, 2)), CastDelay = 250, Speed = int.MaxValue, Width = 55 };
            _w = new Spell.Skillshot(SpellSlot.W, 1050, SkillShotType.Linear) { AllowedCollisionCount = int.MaxValue, Range = 1050, CastDelay = 250, Speed = 1700, Width = 85 };
            _e = new Spell.Skillshot(SpellSlot.E, 800, SkillShotType.Linear)  { AllowedCollisionCount = int.MaxValue, Range = 800, CastDelay = 50, Speed = 1500, Width = 150 };
            _r = new Spell.Skillshot(SpellSlot.R, 1550, SkillShotType.Linear) { AllowedCollisionCount = int.MaxValue, Range = 1550, CastDelay = 30, Speed = int.MaxValue, Width = 85 };

            GameObject.OnCreate += Obj_SpellMissile_OnCreate;
            Interrupter.OnInterruptableSpell += DangerousSpellsEInterupt;
            Gapcloser.OnGapcloser += AntiGapCloser;
            Game.OnTick += VelkozActive;
            Drawing.OnEndScene += HasarGostergesi;
            Drawing.OnDraw += SpellDraw;
            _lvl = Velkoz.Spellbook;
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Game.OnUpdate += Game_OnUpdate; // This to override game input
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast; // use the event to get R Casted position
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Velkoz Successfully loaded.");

            _main = MainMenu.AddMenu("FrOnDaL Velkoz", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Velkoz");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");

            /*Combo*/
            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Velkoz");
            _combo.AddLabel("Use Combo Q (On/Off)");
            _combo.Add("q", new CheckBox("Use Q"));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo W (On/Off)");          
            _combo.Add("w", new CheckBox("Use W"));
            _combo.Add("wHit", new Slider("W Hits Enemies", 1, 1, 3));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Combo E (On/Off)" + "                                  " + "Combo in Use R");
            _combo.Add("e", new CheckBox("Use E"));
            _combo.Add("comboR", new CheckBox("Combo Use R"));
            _combo.AddSeparator(5);
            _combo.AddLabel("Use Manuel R");
            _combo.Add("r", new KeyBind("Use R Key", false, KeyBind.BindTypes.HoldActive, 'T'));
            _combo.AddSeparator(5);
            _combo.Add("RHitChance", new Slider("R hitchance percent : {0}", 20));         
            /*Combo*/

            /*Harass*/
            _harrass = _main.AddSubMenu("Harass");
            _harrass.AddGroupLabel("Harass mode settings for Velkoz");
            _harrass.AddLabel("Harass Mana Control");
            _harrass.Add("HmanaP", new Slider("Harass Mana Control Min mana percentage ({0}%)", 40, 1));
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Use Harass Q (On/Off)");
            _harrass.Add("q", new CheckBox("Use Q harass"));
            _harrass.Add("autoq", new CheckBox("Use Q harass Auto", false));
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Use Harass W (On/Off)");
            _harrass.Add("w", new CheckBox("Use W harass", false));           
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Use Harass E (On/Off)");
            _harrass.Add("e", new CheckBox("Use E harass", false));
            _harrass.AddSeparator(5);
            _harrass.AddLabel("Harass Key C");
            _harrass.Add("harC", new KeyBind("Use Harass Key C", false, KeyBind.BindTypes.HoldActive, 'C'));

            /*Harass*/

            /*LaneClear*/
            _laneclear = _main.AddSubMenu("Laneclear");
            _laneclear.AddGroupLabel("LaneClear mode settings for Velkoz");
            _laneclear.AddLabel("LaneClear Mana Control");
            _laneclear.Add("LmanaP", new Slider("LaneClear Mana Control Min mana percentage ({0}%) to use Q - W - E", 70, 1));
            _laneclear.AddSeparator(5);
            _laneclear.AddLabel("Use Lane Clear Q (On/Off)");
            _laneclear.Add("q", new CheckBox("Use Q", false));
            _laneclear.AddSeparator(5);
            _laneclear.AddLabel("Use Lane Clear W (On/Off)");
            _laneclear.Add("w", new CheckBox("Use W"));
            _laneclear.Add("wHit", new Slider("W Hits Minions", 3, 1, 6));
            _laneclear.AddSeparator(5);
            _laneclear.AddLabel("Use Lane Clear E (On/Off)");
            _laneclear.Add("e", new CheckBox("Use E"));
            _laneclear.Add("eHit", new Slider("E Hits Minions", 3, 1, 3));
            _laneclear.AddSeparator(5);
            
            /*LaneClear*/

            /*JungClear*/
            _jungleclear = _main.AddSubMenu("JungleClear");
            _jungleclear.AddGroupLabel("JungleClear mode settings for Velkoz");
            _jungleclear.AddLabel("JungClear Mana Control");
            _jungleclear.Add("JmanaP", new Slider("JungleClear Mana Control Min mana percentage ({0}%) to use Q - W - E", 30, 1));
            _jungleclear.AddSeparator(5);
            _jungleclear.AddLabel("Use Jung Clear Q (On/Off)");
            _jungleclear.Add("q", new CheckBox("Use Q"));
            _jungleclear.AddSeparator(5);
            _jungleclear.AddLabel("Use Jung Clear W (On/Off)");
            _jungleclear.Add("w", new CheckBox("Use W"));
            _jungleclear.AddSeparator(5);
            _jungleclear.AddLabel("Use Jung Clear E (On/Off)");
            _jungleclear.Add("E", new CheckBox("Use E"));

            /*JungClear*/

            /*Draw*/
            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q-W-E-R (On/Off)");
            _drawings.Add("q", new CheckBox("Draw Q", false));
            _drawings.Add("w", new CheckBox("Draw W"));
            _drawings.Add("e", new CheckBox("Draw E", false));
            _drawings.Add("r", new CheckBox("Draw R", false));
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Use Draw R Damage (On/Off)");
            _drawings.Add("DamageR", new CheckBox("Damage Indicator [R Damage]"));
            /*Draw*/

            /*Misc*/
            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Anti Gap Closer Q - E (On/Off)");
            _misc.Add("Qgap", new CheckBox("Use Q Anti Gap Closer (On/Off)"));
            _misc.Add("Egap", new CheckBox("Use E Anti Gap Closer (On/Off)"));
            _misc.AddSeparator(5);
            _misc.AddLabel("Interrupt Dangerous Spells (On/Off)");
            _misc.Add("interruptE", new CheckBox("Use E Interrupt (On/Off)"));      
            /*Misc*/
        }
        /*Q Split Q Misslie*/
        private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;

            if (missile?.SpellCaster != null && missile.SpellCaster.IsValid && missile.SpellCaster.IsMe &&
                missile.SData.Name.Equals("VelkozQMissile", StringComparison.InvariantCultureIgnoreCase))
            {
                _qMissile = missile;
            }
        }
        /*Q Split Q Misslie*/

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

        /*VelkozActive*/
        private static void VelkozActive(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {                            
                Combo();
                if (_combo["comboR"].Cast<CheckBox>().CurrentValue)
                {
                    var hedefR = TargetSelector.GetTarget(_r.Range, DamageType.Magical);
                    if (TotalHealth(hedefR) < RDamage(hedefR) && _r.IsReady() && hedefR.IsValidTarget(1000) && hedefR.Distance(Velkoz.ServerPosition) > 150 && hedefR.Distance(Velkoz.ServerPosition) < 1000)
                    {
                        ManuelR();
                    }
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LanClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JunClear();
            }
            if(Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harras();
            }
            if (_combo["r"].Cast<KeyBind>().CurrentValue)
            {
                ManuelR();
            }
         AutoQ();
        }
        /*VelkozActive*/

        /*LaneClear*/
        private static void LanClear()
        {
            if (_q.IsReady() && _q.ToggleState == 0 && !IsQActive && _laneclear["q"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)
            {
            var farmQ = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Velkoz.ServerPosition).Where(x => x.IsValidTarget(_q.Range)).ToList();
            if (farmQ.Any())
            {               
                    var qpred = _w.GetBestLinearCastPosition(farmQ);
                    if (qpred.HitNumber >= 2)
                        _q.Cast(qpred.CastPosition);
                }
            }
            if (_w.IsReady() && _laneclear["w"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)
            {
            var farmW = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Velkoz.ServerPosition).Where(x => x.IsValidTarget(_w.Range)).ToList();
            if (farmW.Any())
            {               
                    var wpred = _w.GetBestLinearCastPosition(farmW);
                    if (wpred.HitNumber >= _laneclear["wHit"].Cast<Slider>().CurrentValue)
                        _w.Cast(wpred.CastPosition);
                }
            }
            if (!_e.IsReady() || !_laneclear["e"].Cast<CheckBox>().CurrentValue || !(Velkoz.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)) return;
            var farmE = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Velkoz.ServerPosition,_e.Range + _e.Width).Where(x => x.IsValidTarget(_e.Range)).ToList();
            if (!farmE.Any()) return;            
            var epred = _e.GetBestCircularCastPosition(farmE);
            if (epred.HitNumber >= _laneclear["eHit"].Cast<Slider>().CurrentValue)
                _e.Cast(epred.CastPosition);
        }
        /*LaneClear*/
        
        /*JungClear*/
        private static void JunClear()
        {                    
            var farmjung = EntityManager.MinionsAndMonsters.GetJungleMonsters(Velkoz.ServerPosition, 900).OrderByDescending(x => x.MaxHealth).FirstOrDefault();
            if (farmjung == null) return;
            if (_q.IsReady() && _q.ToggleState == 0 && !IsQActive && _jungleclear["q"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)
            {
                _q.Cast(farmjung.ServerPosition);
            }
            if (_w.IsReady() && _jungleclear["w"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)
            {
                _w.Cast(farmjung.ServerPosition);
            }
            if (_e.IsReady() && _jungleclear["e"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)
            {
                _e.Cast(farmjung.ServerPosition);
            }
        }
        /*JungClear*/       

        /*Harass*/
        private static void Harras()
        {
            if (_w.IsReady() && _harrass["w"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue)
            {
                var harassTargetW = TargetSelector.GetTarget(_w.Range, DamageType.Magical);
                var harassW = EntityManager.Heroes.Enemies.Where(x => Velkoz.IsInRange(x, _w.Range) && x.IsValid && !x.IsDead).ToList();
                var wpred = _w.GetBestLinearCastPosition(harassW);
                if (harassTargetW != null)
                {
                    
                        _w.Cast(wpred.CastPosition);
                    
                }
            }
            if (_e.IsReady() && _harrass["e"].Cast<CheckBox>().CurrentValue && (Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue))
            {
                var harassTargetE = TargetSelector.GetTarget(_e.Range, DamageType.Magical);
                var harassE = EntityManager.Heroes.Enemies.Where(x => Velkoz.IsInRange(x, _e.Range) && x.IsValid && !x.IsDead).ToList();
                var epred = _e.GetBestCircularCastPosition(harassE);
                if (harassTargetE != null)
                {
                    _e.Cast(epred.CastPosition);
                }
            }
            /*Q Split*/
            var qTarget = TargetSelector.GetTarget(_q.Range, DamageType.Magical);
            var qDummyTarget = TargetSelector.GetTarget(_qDummy.Range, DamageType.Magical);
            if (_harrass["q"].Cast<CheckBox>().CurrentValue && Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue && qTarget != null && _q.IsReady() && !IsQActive)
            {
                if (QCast(qTarget))
                    return;
            }

            if (qDummyTarget == null || !_harrass["q"].Cast<CheckBox>().CurrentValue || !(Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue) || !_q.IsReady() || IsQActive)
                return;
            if (qTarget != null) qDummyTarget = qTarget;
            _qDummy.CastDelay = (int)(.25f + _q.Range / _q.Speed * 1000 + _qSplit.Range / _qSplit.Speed * 1000) * 1000;

            var predictedPos = _qDummy.GetPrediction(qDummyTarget);

            if (predictedPos.HitChance < HitChance.High) return;
            for (var i = -1; i < 1; i = i + 2)
            {
                const float alpha = 28 * (float)Math.PI / 180;
                var cp = ObjectManager.Player.ServerPosition.To2D() +
                         (predictedPos.CastPosition.To2D() - ObjectManager.Player.ServerPosition.To2D()).Rotated
                             (i * alpha);

                if (GetCollision(Velkoz.ServerPosition.To2D(), new List<Vector2> { cp },
                    _q.Width, _q.Speed, _q.CastDelay / 1000f).Count != 0 ||
                    GetCollision(cp, new List<Vector2> { predictedPos.CastPosition.To2D() },
                        _qSplit.Width, _qSplit.Speed, _qSplit.CastDelay / 1000f).Count != 0)
                    continue;
                QCast(cp.To3DWorld());
                return;
            }
            /*Q Split*/
        }

        private static void AutoQ()
        {
            if (!_harrass["autoq"].Cast<CheckBox>().CurrentValue) return;
            var qTarget = TargetSelector.GetTarget(_q.Range, DamageType.Magical);
            var qDummyTarget = TargetSelector.GetTarget(_qDummy.Range, DamageType.Magical);
            if (Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue && qTarget != null && _q.IsReady() && !IsQActive)
            {
                if (QCast(qTarget)) return;
            }
            if (qDummyTarget == null || !(Velkoz.ManaPercent >= _harrass["HmanaP"].Cast<Slider>().CurrentValue) || !_q.IsReady() || IsQActive) return;
            if (qTarget != null) qDummyTarget = qTarget; _qDummy.CastDelay = (int)(.25f + _q.Range / _q.Speed * 1000 + _qSplit.Range / _qSplit.Speed * 1000) * 1000;
            var predictedPos = _qDummy.GetPrediction(qDummyTarget);
            if (predictedPos.HitChance < HitChance.High) return;
            for (var i = -1; i < 1; i = i + 2)
            {
                const float alpha = 28 * (float)Math.PI / 180;
                var cp = ObjectManager.Player.ServerPosition.To2D() + (predictedPos.CastPosition.To2D() - ObjectManager.Player.ServerPosition.To2D()).Rotated (i * alpha);
                if (GetCollision(Velkoz.ServerPosition.To2D(), new List<Vector2> { cp }, _q.Width, _q.Speed, _q.CastDelay / 1000f).Count != 0 || GetCollision(cp, new List<Vector2> { predictedPos.CastPosition.To2D() }, _qSplit.Width, _qSplit.Speed, _qSplit.CastDelay / 1000f).Count != 0) continue;
                QCast(cp.To3DWorld()); return;
            }
        }
        /*Harass*/

        /*Combo*/
        private static void Combo()
        {
            if (_w.IsReady() && _combo["w"].Cast<CheckBox>().CurrentValue)
            {
                var comboTargetW = TargetSelector.GetTarget(_w.Range, DamageType.Magical);
                var comboW = EntityManager.Heroes.Enemies.Where(x => Velkoz.IsInRange(x, _w.Range) && x.IsValid && !x.IsDead).ToList();
                var wpred = _w.GetBestLinearCastPosition(comboW, (int)HitChance.High);
                if (comboTargetW != null)
                {
                    if (wpred.HitNumber >= _combo["wHit"].Cast<Slider>().CurrentValue)
                    {
                        _w.Cast(wpred.CastPosition);
                    }
                }
            }
            if (_e.IsReady() && _combo["e"].Cast<CheckBox>().CurrentValue)
            {
                var comboTargetE = TargetSelector.GetTarget(_e.Range, DamageType.Magical);
                var comboE = EntityManager.Heroes.Enemies.Where(x => Velkoz.IsInRange(x, _e.Range) && x.IsValid && !x.IsDead).ToList();
                var epred = _e.GetBestCircularCastPosition(comboE);
                if (comboTargetE != null)
                {                    
                    _e.Cast(epred.CastPosition);                  
                }
            }
            /*Q Split*/
            var qTarget = TargetSelector.GetTarget(_q.Range, DamageType.Magical);
            var qDummyTarget = TargetSelector.GetTarget(_qDummy.Range, DamageType.Magical);      
            if (_combo["q"].Cast<CheckBox>().CurrentValue && qTarget != null && _q.IsReady() && !IsQActive)
            {
                if (QCast(qTarget)) return;
            }
            if (qDummyTarget == null || !_combo["q"].Cast<CheckBox>().CurrentValue || !_q.IsReady() || IsQActive) return;
            if (qTarget != null) qDummyTarget = qTarget; _qDummy.CastDelay = (int)(.25f + _q.Range / _q.Speed * 1000 + _qSplit.Range / _qSplit.Speed * 1000) * 1000;
            var predictedPos = _qDummy.GetPrediction(qDummyTarget);
            if (predictedPos.HitChance < HitChance.High) return;
            for (var i = -1; i < 1; i = i + 2)
            {
                const float alpha = 28 * (float)Math.PI / 180; var cp = ObjectManager.Player.ServerPosition.To2D() + (predictedPos.CastPosition.To2D() - ObjectManager.Player.ServerPosition.To2D()).Rotated (i * alpha);
                if (GetCollision(Velkoz.ServerPosition.To2D(), new List<Vector2> {cp}, _q.Width, _q.Speed, _q.CastDelay/1000f).Count != 0 || GetCollision(cp, new List<Vector2> {predictedPos.CastPosition.To2D()}, _qSplit.Width, _qSplit.Speed, _qSplit.CastDelay/1000f).Count != 0) continue;
                QCast(cp.To3DWorld()); return;
            }
            /*Q Split*/
        }
        /*Combo*/

        /*Q Split GetCollision*/
        private static List<Obj_AI_Base> GetCollision(Vector2 from, IEnumerable<Vector2> to, float width, float speed, float delay, float delayOverride = -1)
        {
            return Pradiction.Collision.GetCollision( to.Select(h => h.To3D()).ToList(), new Pradiction.PredictionInput { From = from.To3D(), Type = Pradiction.SkillshotType.SkillshotLine, Radius = width, Delay = delayOverride > 0 ? delayOverride : delay, Speed = speed });
        }
        /*Q Split GetCollision*/

        /*Q Split QCast*/
        public static float QLastCastAttemptT { get; private set; }
        private static void QCast()
        {
            QLastCastAttemptT = Core.GameTickCount; Velkoz.Spellbook.CastSpell(SpellSlot.Q);
        }
        private static void QCast(Vector3 position)
        {
            QLastCastAttemptT = Core.GameTickCount; _q.Cast(position);
        }
        private static bool QCast(Obj_AI_Base target)
        {
            QLastCastAttemptT = Core.GameTickCount; return _q.Cast(target);
        }
        /*Q Split QCast*/

        private static void Game_OnUpdate(EventArgs args)
        {
            var target = EntityManager.Heroes.Enemies.OrderBy(t => t.Distance(_rCastPos)).FirstOrDefault(t => t.IsValidTarget(_r.Range)); // Get target near R Casted Position
            if (Velkoz.Spellbook.IsChanneling && target != null) // Detect player is Channeling
            {
                Velkoz.Spellbook.UpdateChargeableSpell(_r.Slot, target.ServerPosition, false, false); // Change cast Position to target position
            }
            /*Q Split -> THANK YOU Esk0r*/
            if (_qMissile == null || !_qMissile.IsValid || !IsQActive) return;
            {
                var qMissilePosition = _qMissile.Position.To2D();
                var perpendicular = (_qMissile.EndPosition - _qMissile.StartPosition).To2D().Normalized().Perpendicular();
                var lineSegment1End = qMissilePosition + perpendicular * _qSplit.Range;
                var lineSegment2End = qMissilePosition - perpendicular * _qSplit.Range;
                var potentialTargets = EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget() && h.ServerPosition.To2D().Distance(qMissilePosition, _qMissile.EndPosition.To2D(), true, false) < 700).Cast<Obj_AI_Base>().ToList();
                _qSplit.RangeCheckSource = qMissilePosition.To3DWorld();
                _qSplit.SourcePosition = qMissilePosition.To3D();
                foreach (var enemy in from enemy in EntityManager.Heroes.Enemies.Where( h => h.IsValidTarget() && (potentialTargets.Count == 0 || h.NetworkId == potentialTargets.OrderBy(t => t.Health / Velkoz.GetSpellDamage(t, SpellSlot.Q)).ToList()[0].NetworkId) && (h.ServerPosition.To2D().Distance(qMissilePosition, _qMissile.EndPosition.To2D(), true, false) > _q.Width + h.BoundingRadius))
                                      let prediction = _qSplit.GetPrediction(enemy)
                                      let d1 = prediction.UnitPosition.To2D().Distance(qMissilePosition, lineSegment1End, true, false)
                                      let d2 = prediction.UnitPosition.To2D().Distance(qMissilePosition, lineSegment2End, true, false)
                                      where prediction.HitChance >= HitChance.Medium && (d1 < _qSplit.Width + enemy.BoundingRadius || d2 < _qSplit.Width + enemy.BoundingRadius)
                                      select enemy)
                {
                    QCast();
                }
            }
            /*Q Split -> THANK YOU Esk0r*/
        }

        /*ManuelR*/
        private static void ManuelR()
        {        
            if (!_r.IsReady()) return;
            var hedefR = TargetSelector.GetTarget(_r.Range, DamageType.Magical);
            if (hedefR == null) return;
            var manuelR = Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput { CollisionTypes = new HashSet<CollisionType> { CollisionType.AiHeroClient }, Delay = .25f, From = Velkoz.Position, Radius = _r.Width, Range = _r.Range, RangeCheckFrom = Velkoz.Position, Speed = _r.Speed, Target = hedefR, Type = SkillShotType.Linear });
            if (manuelR.HitChancePercent >= _combo["RHitChance"].Cast<Slider>().CurrentValue)
            {
                _r.Cast(manuelR.CastPosition);
            }        
        }
        /*ManuelR*/

        /*Interrupter*/
        private static void DangerousSpellsEInterupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!_misc["interruptE"].Cast<CheckBox>().CurrentValue || !sender.IsValid || sender.IsDead || !sender.IsTargetable || sender.IsStunned) return;
            if (_e.IsReady() && _e.IsInRange(sender.Position) && args.DangerLevel == DangerLevel.Medium)
            {
                _e.Cast(sender.Position);
            }
        }
        /*Interrupter*/

        /*AntiGapCloser*/
        private static void AntiGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gap)
        {
            if (_misc["Qgap"].Cast<CheckBox>().CurrentValue && _q.IsReady() && sender.IsEnemy && sender.IsValidTarget(1000) && gap.End.Distance(Velkoz) <= 250)
            {
                _q.Cast(sender.Position);
            }
            if (_misc["Egap"].Cast<CheckBox>().CurrentValue && _e.IsReady() && sender.IsEnemy && sender.IsValidTarget(1000) && gap.End.Distance(Velkoz) <= 250)
            {
                _e.Cast(sender.Position);
            }
        }
        /*AntiGapCloser*/

        /*Damage Indicator*/
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered && _r.IsReady() && Velkoz.Distance(x) < 2000 && x.VisibleOnScreen))
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
            
        private static bool IsQActive => _q.Name.Equals("VelkozQSplitActivate");
    }
}
