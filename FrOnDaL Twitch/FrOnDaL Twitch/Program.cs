using System;
using SharpDX;
using EloBuddy;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace FrOnDaL_Twitch
{
    internal static class Program
    {
        private static AIHeroClient Twitch => Player.Instance;      
        private static Spellbook _lvl;
        private static readonly Item AutoGhostBlade = new Item(ItemId.Youmuus_Ghostblade);
        private static readonly Item AutoBotrk = new Item(ItemId.Blade_of_the_Ruined_King);
        private static readonly Item AutoCutlass = new Item(ItemId.Bilgewater_Cutlass);
        private static Spell.Active _q, _e;
        private static Spell.Skillshot _w;
        private static Spell.Targeted _smite;
        public static Text QTimer;
        private static float _dikeyj, _yatayj, _genislikj, _yukseklikj;
        private static float _dikey, _yatay;
        private static float _genislik = 104;
        private static float _yukseklik = 9.82f;
        private static readonly Vector2 BarOffset = new Vector2(1.25f, 14.25f);
        private static Menu _main, _combo, _laneclear, _jungleclear, _drawings, _misc;
        private static bool Passive(this Obj_AI_Base obj) { return obj.HasBuff("TwitchDeadlyVenom"); }
        private static void AutoItem(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (_misc["ghostBladeR"].Cast<CheckBox>().CurrentValue && sender.IsMe && AutoGhostBlade.IsOwned() && args.Slot == SpellSlot.R && AutoGhostBlade.IsReady())
            {
                AutoGhostBlade.Cast();
            }
            var botrkHedef = TargetSelector.GetTarget(EntityManager.Heroes.Enemies.Where(x => x != null && x.IsValidTarget() && x.IsInRange(Twitch, 550)), DamageType.Physical);
            if (botrkHedef != null && _misc["botrk"].Cast<CheckBox>().CurrentValue && AutoBotrk.IsOwned() && AutoBotrk.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {                
                AutoBotrk.Cast(botrkHedef);
            }
            if (botrkHedef != null && _misc["autoCutlass"].Cast<CheckBox>().CurrentValue && AutoCutlass.IsOwned() && AutoCutlass.IsReady() && Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                AutoCutlass.Cast(botrkHedef);
            }
        }
        private static void AutoGhostBladeQ(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (!sender.IsMe || args.Buff.Name != "twitchhideinshadowsbuff" || !(Twitch.CountEnemyHeroesInRangeWithPrediction(950, 350) >= 1)) return;
            if (_misc["ghostBladeQ"].Cast<CheckBox>().CurrentValue && AutoGhostBlade.IsOwned() && AutoGhostBlade.IsReady())
            {
                AutoGhostBlade.Cast();
            }
        }
        private static void Main(){Loading.OnLoadingComplete += OnLoadingComplete;}            
        private static void OnLoadingComplete(EventArgs args)
        {
            if (Twitch.Hero != Champion.Twitch) return;

            _lvl = Twitch.Spellbook;
            Game.OnTick += TwitchActive;
            Game.OnUpdate += AutoSmite;
            Game.OnNotify += QafterKill;
            Drawing.OnDraw += SpellDraw;
            Drawing.OnEndScene += HasarGostergesi;
            Drawing.OnEndScene += HasarGostergesiJungle;                                
            Obj_AI_Base.OnLevelUp += OnLevelUpR;
            Obj_AI_Base.OnBuffGain += AutoGhostBladeQ;                                     
            Spellbook.OnCastSpell += auto_baseQ;
            Obj_AI_Base.OnProcessSpellCast += AutoItem;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Twitch Successfully loaded.");
            QTimer = new Text("", new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 11, System.Drawing.FontStyle.Strikeout));
            _q = new Spell.Active(SpellSlot.Q);
            _w = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1400, 280) { AllowedCollisionCount = -1, MinimumHitChance = HitChance.Medium };
            _e = new Spell.Active(SpellSlot.E, 1200);

            var firstOrDefault = Twitch.Spellbook.Spells.FirstOrDefault(s => s.SData.Name.ToLower().Contains("smite"));
            if (firstOrDefault != null)
            {
                _smite = new Spell.Targeted(firstOrDefault.Slot, 570);
            }               

            _main = MainMenu.AddMenu("FrOnDaL Twitch", "index");
            _main.AddGroupLabel("Welcome FrOnDaL Twitch");
            _main.AddSeparator(5);
            _main.AddLabel("For faults please visit the 'elobuddy' forum and let me know.");
            _main.AddSeparator(5);
            _main.AddLabel("My good deeds -> FrOnDaL");

            _combo = _main.AddSubMenu("Combo");
            _combo.AddGroupLabel("Combo mode settings for Twitch");
            _combo.AddLabel("Use Combo W (On/Off)" + "                                 " + "Auto E Kill Steal");
            _combo.Add("w", new CheckBox("Use W"));
            _combo.Add("autoE", new CheckBox("Auto E"));
            _combo.AddLabel("Use Q After Kill (On/Off)");
            _combo.Add("Qafterkill", new CheckBox("Use Q After Kill"));

            _laneclear = _main.AddSubMenu("Laneclear");
            _laneclear.AddGroupLabel("LaneClear mode settings for Twitch");
            _laneclear.Add("LmanaP", new Slider("LaneClear Mana Control Min mana percentage ({0}%) to use W and E", 50, 1));
            _laneclear.AddSeparator(5);
            _laneclear.Add("w", new CheckBox("Venom Cask (W) settings", false));
            _laneclear.Add("UnitsWhit", new Slider("Hit {0} Units Enemy and Minions", 4, 1, 8));
            _laneclear.AddSeparator(5);
            _laneclear.Add("e", new CheckBox("Use E", false));
            _laneclear.Add("MinionsEhit", new Slider("Min minions hit {0} to use E", 3, 1, 8));
            _laneclear.Add("MinionsEstacks", new Slider("{0} stacks aa", 3, 1, 4));

            _jungleclear = _main.AddSubMenu("Jungleclear");
            _jungleclear.AddGroupLabel("JungleClear mode settings for Twitch");         
            _jungleclear.Add("JmanaP", new Slider("Jungle Clear Mana Control Min mana percentage ({0}%) to use W and E", 30, 1));
            _jungleclear.AddLabel("Venom Cask (W) settings :"+"                               "+ "Contaminate (E) settings :");
            _jungleclear.Add("w", new CheckBox("Use W"));
            _jungleclear.Add("e", new CheckBox("Use E"));

            _drawings = _main.AddSubMenu("Drawings");
            _drawings.AddLabel("Use Drawings Q Timer (On/Off)");
            _drawings.Add("qTimer", new CheckBox("Draw Q Timer"));
            _drawings.AddLabel("Use Drawings W-E-R (On/Off)");
            _drawings.Add("w", new CheckBox("Draw W and R", false));
            _drawings.Add("e", new CheckBox("Draw E", false));
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Smite Draw (On/Off)");
            if (firstOrDefault != null)
            {
                _drawings.Add("smite", new CheckBox("Smite Draw", true));
            }
            else
            {
                _drawings.Add("smite", new CheckBox("Smite Draw", false));
            }
            
            _drawings.AddSeparator(5);
            _drawings.AddLabel("Use Draw E Damage (On/Off)"+ "                           " + "Monsters Smite Damage (On/Off)");
            _drawings.Add("EKillStealD", new CheckBox("Damage Indicator [E Damage]"));
            if (firstOrDefault != null)
            {
                _drawings.Add("smiteDamage", new CheckBox("Damage Indicator [Smite Damage]", true));
            } else
            {
                _drawings.Add("smiteDamage", new CheckBox("Damage Indicator [Smite Damage]", false));
            }
                

            _misc = _main.AddSubMenu("Misc");
            _misc.AddLabel("Auto base use Q (On/Off)");
            _misc.Add("autob2", new CheckBox("Auto Base Q (On/Off)"));
            _misc.Add("autob", new KeyBind("Auto Base Q Key", false, KeyBind.BindTypes.HoldActive, 'B'));           
            _misc.AddSeparator(5);
            _misc.AddLabel("Auto Youmuu Ghost Blade");
            _misc.Add("ghostBladeR", new CheckBox("Youmuu Ghost Blade --> Use R"));
            _misc.Add("ghostBladeQ", new CheckBox("Youmuu Ghost Blade --> Use Q ends"));
            _misc.AddSeparator(5);
            _misc.AddLabel("Auto Blade of the Ruined King and Bilgewater Cutlass");
            _misc.Add("botrk", new CheckBox("Use BotRk (On/Off)"));
            _misc.Add("autoCutlass", new CheckBox("Use Bilgewater Cutlass (On/Off)"));          
            _misc.AddSeparator(5);
            _misc.AddLabel("Auto Smite Settings");
            if (firstOrDefault != null)
            {
                _misc.Add("autosmite", new KeyBind("Use Auto Smite (On/Off)", true, KeyBind.BindTypes.PressToggle, 'M'));
            }
            else
            {
                _misc.Add("autosmite", new KeyBind("Use Auto Smite (On/Off)", false, KeyBind.BindTypes.PressToggle, 'M'));
            }
            
            _misc.AddLabel("Enable Auto Smite for");
            _misc.Add("reblue", new CheckBox("Auto Smite Blue and Red (On/Off)"));
            _misc.Add("wolf", new CheckBox("Auto Smite MurkWolf (On/Off)", false));
            _misc.Add("gromp", new CheckBox("Auto Smite Gromp (On/Off)", false));
            _misc.Add("krug", new CheckBox("Auto Smite Krugs (On/Off)", false));
            _misc.Add("razor", new CheckBox("Auto Smite Razorbeak (On/Off)", false));
            _misc.Add("hero", new CheckBox("Auto Smite Champions (On/Off)"));            
        }

        public static void OnLevelUpR(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (Twitch.Level > 4)
            {
                _lvl.LevelSpell(SpellSlot.R);
            }
        }
        private static void auto_baseQ(Spellbook sender, SpellbookCastSpellEventArgs eventArgs)
        {     
            if (eventArgs.Slot != SpellSlot.Recall || !_q.IsReady() || !_misc["autob2"].Cast<CheckBox>().CurrentValue || !_misc["autob"].Cast<KeyBind>().CurrentValue) return;
            _q.Cast();
            Core.DelayAction(() => ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall), _q.CastDelay + 300);
            eventArgs.Process = false;       
        }      
        private static void TwitchActive(EventArgs args)
        {         
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                var hedef = TargetSelector.GetTarget(_w.Range, DamageType.True);
                var tahmin = _w.GetPrediction(hedef);
                if ( _combo["w"].Cast<CheckBox>().CurrentValue && hedef != null && !hedef.IsInvulnerable && tahmin != null && tahmin.HitChance > HitChance.Medium && _w.IsReady())
                {
                    _w.Cast(hedef);
                }               
            }       
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Twitch.ManaPercent >= _laneclear["LmanaP"].Cast<Slider>().CurrentValue)
                {
                    LanClear();
                }              
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                if (Twitch.ManaPercent >= _jungleclear["JmanaP"].Cast<Slider>().CurrentValue)
                {
                    JunClear();
                }            
            }
            if (!_combo["autoE"].Cast<CheckBox>().CurrentValue) return;
            var rival = EntityManager.Heroes.Enemies.Where(x => x.IsValid && !x.IsDead && !x.IsInvulnerable && x.Passive() && Prediction.Health.GetPrediction(x, 275) < ESpellDamage(x)).FirstOrDefault(d => d.IsInRange(Twitch, _e.Range));
            if (rival != null && _e.IsReady())
            {
                _e.Cast();
            }
        }    
        private static void SpellDraw(EventArgs args)
        {
            if (_drawings["w"].Cast<CheckBox>().CurrentValue){ _w.DrawRange(Color.FromArgb(130, Color.Green));}
            if (_drawings["e"].Cast<CheckBox>().CurrentValue){ _e.DrawRange(Color.FromArgb(130, Color.Green));}
            if (_drawings["smite"].Cast<CheckBox>().CurrentValue)
            {
                _smite.DrawRange(_misc["autosmite"].Cast<KeyBind>().CurrentValue ? Color.FromArgb(130, Color.White) : Color.FromArgb(130, Color.Gray));
            }
            if (!_drawings["qTimer"].Cast<CheckBox>().CurrentValue || !ObjectManager.Player.HasBuff("TwitchHideInShadows")) return;
            QTimer.Position = Drawing.WorldToScreen(Player.Instance.Position) - new Vector2(20, 84);
            QTimer.Color = Color.AntiqueWhite;
            QTimer.TextValue = "Q Timer : " + $"{ObjectManager.Player.GetRemainingBuffTime("TwitchHideInShadows"):0.0}";
            QTimer.Draw();         
        }
        /*iJabbaReborn Thanks*/
        private static float GetRemainingBuffTime(this Obj_AI_Base target, string buffName)
        {
            return target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time).Where(buff => string.Equals(buff.Name, buffName, StringComparison.CurrentCultureIgnoreCase)).Select(buff => buff.EndTime).FirstOrDefault() - Game.Time;
        }
        /*iJabbaReborn Thanks*/
        private static void LanClear()
        {
            if (_laneclear["w"].Cast<CheckBox>().CurrentValue)
            {
                var farm = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Twitch.ServerPosition).Where(x => x.IsInRange(Twitch, _w.Range));              
                var keyhit = _w.GetBestCircularCastPosition(farm);
                if (keyhit.HitNumber >= _laneclear["UnitsWhit"].Cast<Slider>().CurrentValue && _e.IsReady())
                {
                    _w.Cast(keyhit.CastPosition);
                }              
            }
            if (!_laneclear["e"].Cast<CheckBox>().CurrentValue) return;
            {
                var farm = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Twitch.ServerPosition).Where(x => x.IsInRange(Twitch, _e.Range) && x.Passive());
                var objAiMinions = farm as Obj_AI_Minion[] ?? farm.ToArray();
                if (
                    objAiMinions.Count(
                        c =>
                            Prediction.Health.GetPrediction(c, 275) < ESpellDamage(c) &&
                            StacksPassive(c) >= _laneclear["MinionsEstacks"].Cast<Slider>().CurrentValue) >=
                    _laneclear["MinionsEhit"].Cast<Slider>().CurrentValue && _e.IsReady())
                {
                    _e.Cast();
                }
                if (objAiMinions.Count(c => StacksPassive(c) >= _laneclear["MinionsEstacks"].Cast<Slider>().CurrentValue) < _laneclear["MinionsEhit"].Cast<Slider>().CurrentValue && _e.IsReady()) return;
                {
                    _e.Cast();
                }                
            }
        }
        private static void JunClear()
        {
            if (!_jungleclear["w"].Cast<CheckBox>().CurrentValue) return;
            var farmjung = EntityManager.MinionsAndMonsters.GetJungleMonsters(Twitch.ServerPosition).FirstOrDefault(x => x.IsInRange(Twitch, 550));
            if (farmjung != null && _w.IsReady())
            {
                _w.Cast(farmjung.ServerPosition);
            }
        }
        private static void AutoSmite(EventArgs args)
        {
            if (_misc["autob2"].Cast<CheckBox>().CurrentValue && _misc["autob"].Cast<KeyBind>().CurrentValue)
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall);
            }
            if (!Twitch.IsDead && _misc["autosmite"].Cast<KeyBind>().CurrentValue)
            { 
            var smiteChampion = TargetSelector.GetTarget(_smite.Range, DamageType.Physical);
            var smiteMonster = EntityManager.MinionsAndMonsters.Monsters.LastOrDefault(x => Twitch.Distance(x) < 1000 && !x.BaseSkinName.ToLower().Contains("mini") && !x.BaseSkinName.Contains("Crab"));
            if (_smite.IsReady())
            {
                if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo) && smiteChampion != null && _smite.IsInRange(smiteChampion) && _misc["hero"].Cast<CheckBox>().CurrentValue)
                {
                    _smite.Cast(smiteChampion);
                }
                if (smiteMonster != null &&
                    _smite.IsInRange(smiteMonster) && smiteMonster.Health <= Twitch.GetSummonerSpellDamage(smiteChampion, DamageLibrary.SummonerSpells.Smite))
                {
                    if (_misc["reblue"].Cast<CheckBox>().CurrentValue && (smiteMonster.BaseSkinName.Contains("Blue") || smiteMonster.BaseSkinName.Contains("Red")))
                    {
                        _smite.Cast(smiteMonster);
                    }
                    if (_misc["wolf"].Cast<CheckBox>().CurrentValue && smiteMonster.BaseSkinName.Contains("Murkwolf"))
                    {
                        _smite.Cast(smiteMonster);
                    }
                    if (_misc["gromp"].Cast<CheckBox>().CurrentValue && smiteMonster.BaseSkinName.Contains("Gromp"))
                    {
                        _smite.Cast(smiteMonster);
                    }
                    if (_misc["krug"].Cast<CheckBox>().CurrentValue && smiteMonster.BaseSkinName.Contains("Krug"))
                    {
                        _smite.Cast(smiteMonster);
                    }
                    if (_misc["razor"].Cast<CheckBox>().CurrentValue && smiteMonster.BaseSkinName.Contains("Razorbeak"))
                    {
                        _smite.Cast(smiteMonster);
                    }
                    if (smiteMonster.BaseSkinName.Contains("Dragon") || smiteMonster.BaseSkinName.Contains("Baron") || smiteMonster.BaseSkinName.Contains("RiftHerald"))
                    {
                        _smite.Cast(smiteMonster);
                    }
                }                       
            }
            }
            if (!_jungleclear["e"].Cast<CheckBox>().CurrentValue) return;
            {
                var farmjung = EntityManager.MinionsAndMonsters.Monsters.LastOrDefault(x => x.IsInRange(Twitch, _e.Range) && x.Passive() && !x.BaseSkinName.ToLower().Contains("mini"));
                if (farmjung == null || !_e.IsReady()) return;
                if (farmjung.Health <= ESpellDamage(farmjung))
                {
                    _e.Cast();
                }
            }                      
        }
        private static void QafterKill(GameNotifyEventArgs args)
        {

            if (_combo["Qafterkill"].Cast<CheckBox>().CurrentValue && (args.NetworkId == Player.Instance.NetworkId) && (args.EventId == GameEventId.OnChampionKill))
            {               
                Core.DelayAction(() =>
                {                  
                    if (EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(1500)))
                    {                      
                        _q.Cast();
                    }
                }, 150);
            }
        }
        public static int StacksPassive(Obj_AI_Base obj)
        {
            var i = 0;
            if (obj.IsInRange(Twitch, _e.Range))
            {
                var passiveHit = ObjectManager.Get<Obj_GeneralParticleEmitter>().Where(x => x.Name.Contains("Twitch_Base_P_Stack")).FirstOrDefault();

                if (passiveHit == null) i = 0;

                switch (passiveHit.Name)
                {
                    case "Twitch_Base_P_Stack_01.troy": i = 1; break;
                    case "Twitch_Base_P_Stack_02.troy": i = 2; break;
                    case "Twitch_Base_P_Stack_03.troy": i = 3; break;
                    case "Twitch_Base_P_Stack_04.troy": i = 4; break;
                    case "Twitch_Base_P_Stack_05.troy": i = 5; break;
                    case "Twitch_Base_P_Stack_06.troy": i = 6; break;
                }
            }
            else
            {
                i = 0;
            }
            return i;
        }
        public static float ESpellDamage(Obj_AI_Base obj)
        {
            var temel = Player.GetSpell(SpellSlot.E).Level - 1;
            float temelDamage = 0;
            if (_e.IsReady())
            {
                var temelDeger = new float[] { 20, 35, 50, 65, 80 }[temel];
                var temelHit = new float[] { 15, 20, 25, 30, 35 }[temel];
                var ekstraHasarD = 0.25f * Twitch.FlatPhysicalDamageMod;
                var ekstraHasarP = 0.20f * Twitch.TotalMagicalDamage;
                temelDamage = temelDeger + ((temelHit + ekstraHasarD + ekstraHasarP + 1) * StacksPassive(obj));
            }
            return Twitch.CalculateDamageOnUnit(obj, DamageType.Physical, temelDamage);
        }
        private static void HasarGostergesi(EventArgs args)
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => x.VisibleOnScreen && x.Passive()))
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
                var damage = ESpellDamage(enemy);
                if (damage < 1) return;           
                if (_drawings["EKillStealD"].Cast<CheckBox>().CurrentValue)
                {        
                    var hasarX = (enemy.TotalShieldHealth() - damage > 0 ? enemy.TotalShieldHealth() - damage : 0) / enemy.TotalShieldMaxHealth();
                    var hasarY = enemy.TotalShieldHealth() / enemy.TotalShieldMaxHealth();
                    var go = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarX * _genislik), (int)enemy.HPBarPosition.Y + _dikey);
                    var finish = new Vector2((int)(enemy.HPBarPosition.X + _yatay + hasarY * _genislik) + 1, (int)enemy.HPBarPosition.Y + _dikey);
                    Drawing.DrawLine(go, finish, _yukseklik, Color.FromArgb(180, Color.Green));
                
                }
            }
        }

        private static void HasarGostergesiJungle(EventArgs args)
        {
            foreach (var monstersDamage in EntityManager.MinionsAndMonsters.Monsters.Where(x => Twitch.Distance(x) < 1000 && !x.BaseSkinName.ToLower().Contains("mini") && !x.BaseSkinName.Contains("Crab") && !x.IsDead && x.IsHPBarRendered && x.VisibleOnScreen))
            {
                if (monstersDamage.BaseSkinName.Contains("RiftHerald"))
                {_genislikj = 142; _yukseklikj = 9.92f;_dikeyj = 8; _yatayj = -3;}              
                if (monstersDamage.BaseSkinName.Contains("Blue") || monstersDamage.BaseSkinName.Contains("Red"))
                {_genislikj = 142; _yukseklikj = 9.82f;_dikeyj = 7; _yatayj = -3;}
                if (monstersDamage.BaseSkinName.Contains("Gromp") || monstersDamage.BaseSkinName.Contains("Razorbeak") || monstersDamage.BaseSkinName.Contains("Krug") || monstersDamage.BaseSkinName.Contains("Murkwolf"))
                {_genislikj = 91; _yukseklikj = 3.999f;_dikeyj = 7.999f; _yatayj = 22;}   
                if (!_drawings["smiteDamage"].Cast<CheckBox>().CurrentValue) continue;
                var smiteDamage = Twitch.GetSummonerSpellDamage(monstersDamage, DamageLibrary.SummonerSpells.Smite);
                var hasarXj = (monstersDamage.TotalShieldHealth() - smiteDamage > 0 ? monstersDamage.TotalShieldHealth() - smiteDamage : 0) / (monstersDamage.MaxHealth + monstersDamage.AllShield + monstersDamage.AttackShield + monstersDamage.MagicShield);
                var hasarYj = monstersDamage.TotalShieldHealth() / (monstersDamage.MaxHealth + monstersDamage.AllShield + monstersDamage.AttackShield + monstersDamage.MagicShield);
                var goj = new Vector2((int)(monstersDamage.HPBarPosition.X + _yatayj + hasarXj * _genislikj), (int)monstersDamage.HPBarPosition.Y + _dikeyj);
                var finishj = new Vector2((int)(monstersDamage.HPBarPosition.X + _yatayj + hasarYj * _genislikj) + 1, (int)monstersDamage.HPBarPosition.Y + _dikeyj);
                Drawing.DrawLine(goj, finishj, _yukseklikj, Color.FromArgb(180, Color.Green));             
                if (monstersDamage.BaseSkinName.Contains("Dragon") || monstersDamage.BaseSkinName.Contains("Baron"))
                {
                    var x = (int)monstersDamage.HPBarPosition[0] - 45;
                    var y = (int)monstersDamage.HPBarPosition[1];
                    var damagesmite = (smiteDamage / monstersDamage.Health) * 100;
                    var percent = damagesmite > 100 ? 100 : damagesmite;
                    Drawing.DrawText(x, y, Color.WhiteSmoke, string.Concat(Math.Ceiling(percent), " %"));
                }
            }
        }
    }
}