using System;
using EloBuddy;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using EloBuddy.Sandbox;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Events;
using System.Globalization;
using EloBuddy.SDK.Menu.Values;
using System.Collections.Generic;

namespace FrOnDaL_TurkishLanguage
{
    internal class Program
    {    
        private static bool _go;
        private static Menu _main;
        private static bool _finish;
        private static string _addonWay;
        private static string _languageWay;
        private static bool _languageWayEx;
        private const string LanguageUrl = "https://raw.githubusercontent.com/FrOnDaL/FrOnDaL/master/FrOnDaL TurkishLanguage/FrOnDaL%20TurkishLanguage/_turkishLanguage.json";
        private static Dictionary<string, Dictionary<Language, Dictionary<int, string>>> _turkishLanguage = new Dictionary<string, Dictionary<Language, Dictionary<int, string>>>();
        private static readonly Dictionary<string, Language> DefaultLanguage = new Dictionary<string, Language>{{ "en-US", Language.English }, { "en-GB", Language.English }, { "tr-TR", Language.Turkish }}; 
        private static Language DefaultLang => DefaultLanguage.ContainsKey(CultureInfo.InstalledUICulture.ToString()) ? DefaultLanguage[CultureInfo.InstalledUICulture.ToString()] : Language.English;
        private static void Main()
        {
            Loading.OnLoadingComplete += delegate
            {
                var languageLoad = Game.Time;
                Game.OnTick += delegate
                {
                    if (Game.Time - languageLoad >= 2 && languageLoad > 0)
                    {
                        languageLoad = 0;
                        _addonWay = Path.Combine(SandboxConfig.DataDirectory, "FrOnDaL_TurkishLanguage");
                        if (!Directory.Exists(_addonWay))
                        {
                            Directory.CreateDirectory(_addonWay);
                        }
                        _languageWay = Path.Combine(_addonWay, "_turkishLanguage.json");
                        _languageWayEx = File.Exists(_languageWay);
                        if (!_languageWayEx)
                        {
                            File.Create(_languageWay).Close();
                            LanguageRefresh();
                        }
                        else
                        {
                            var languageTransform = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<Language, Dictionary<int, string>>>>(File.ReadAllText(_languageWay));
                            if (languageTransform != null)
                            {
                                _turkishLanguage = languageTransform;
                            }
                            LanguageRefresh();                      
                        }
                    }
                    if (_finish)
                    {
                        TurkishLanguageActive();
                    }
                };
            };
        }     
        private static void LanguageRefresh()
        {
            var gitHub = new WebClient { Encoding = Encoding.UTF8 };
            gitHub.DownloadStringCompleted += LanguageRefreshed;
            gitHub.DownloadStringAsync(new Uri(LanguageUrl, UriKind.Absolute));
        }
        private static void LanguageRefreshed(object sender, DownloadStringCompletedEventArgs args)
        {
            if (args.Cancelled || args.Error != null)
            {              
                if (_languageWayEx)
                {
                    _finish = true;
                } return;
            }
            File.WriteAllText(_languageWay, args.Result);
            var languageTransform = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<Language, Dictionary<int, string>>>>(args.Result);
            if (languageTransform != null)
            {
                _turkishLanguage = languageTransform;
            }
            _finish = true;
        }
        private static void TurkishLanguageActive()
        {
            if (_go) return;
            _finish = false;
            _go = true;
            Chat.Print("<font color='#00FFCC'><b>[FrOnDaL]</b></font> Turkish Language Successfully loaded.");
            _main = MainMenu.AddMenu("Turkish Language", "FrOnDaL_TurkishLanguage");
            _main.AddGroupLabel("Welcome FrOnDaL TurkishLanguage");
            var turkishexisting = Enum.GetValues(typeof(Language)).Cast<Language>().ToArray().Select(x => x.ToString());
            var selectedlanguage = (int)DefaultLang;
            var chooselanguage = _main.Add("Language", new ComboBox("Var olan diller :", turkishexisting, selectedlanguage));
            chooselanguage.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args) { TurkishTranslation((Language)args.OldValue, (Language)args.NewValue); };
            TurkishTranslation(Language.English, (Language)chooselanguage.CurrentValue);
            _main.AddSeparator(5);
            _main.AddLabel("Her oyun başladığında 'F5' tuşuna basarak script inject etmeniz gerekiyor eğer inject etmezseniz");
            _main.AddLabel("'Shift' tuşuna bastığınızda ekrana gelen menüde 'Turkish Language' addon görünmez ve türkçeye ");
            _main.AddLabel("çevirmez. Bunu yapamamızdaki temel neden sizin o anda kullandığınız addon'ların 'Turkish ");
            _main.AddLabel("Language' bildirmek ve çevirisini yapmasını sağlamak.");
        }
        private static void TurkishTranslation(Language from, Language to)
        {
            foreach (var particletranslate in MainMenu.MenuInstances)
            {
                foreach (var main in particletranslate.Value)
                {
                    var addonName = main.Parent == null ? main.DisplayName : main.Parent.DisplayName;
                    main.DisplayName = GetTurkishTranslation(addonName, from, to, main.DisplayName);
                    foreach (var particletranslatefor in main.LinkedValues)
                    {
                        particletranslatefor.Value.DisplayName = GetTurkishTranslation(addonName, from, to, particletranslatefor.Value.DisplayName);
                        var chooselanguage = particletranslatefor.Value as ComboBox;
                        if (chooselanguage == null) continue;
                        foreach (var valuelang in chooselanguage.Overlay.Children)
                        {
                            valuelang.TextValue = GetTurkishTranslation(addonName, @from, to, valuelang.TextValue);
                        }
                    }
                    foreach (var subMain in main.SubMenus)
                    {
                        subMain.DisplayName = GetTurkishTranslation(addonName, from, to, subMain.DisplayName);
                    }
                    foreach (var particletranslatefor in main.SubMenus.SelectMany(subMain => subMain.LinkedValues))
                    {
                        particletranslatefor.Value.DisplayName = GetTurkishTranslation(addonName, @from, to, particletranslatefor.Value.DisplayName);
                        var chooselanguage = particletranslatefor.Value as ComboBox;
                        if (chooselanguage == null) continue;
                        foreach (var valuelang in chooselanguage.Overlay.Children)
                        {
                            valuelang.TextValue = GetTurkishTranslation(addonName, @from, to, valuelang.TextValue);
                        }
                    }
                }
            }
        }
        private static string GetTurkishTranslation(string addonName, Language from, Language to, string displayName)
        {
            if (!_turkishLanguage.ContainsKey(addonName)) return displayName;
            var words = _turkishLanguage[addonName];
            to = words.ContainsKey(to) ? to : Language.English;
            if (!words.ContainsKey(to)) return displayName;
            if (words.ContainsKey(@from))
            {
                foreach (var particletranslate in words[@from].Where(particletranslate => particletranslate.Value == displayName).Where(particletranslate => words[to].ContainsKey(particletranslate.Key)))
                {
                    return words[to][particletranslate.Key];
                }
            }
            if (!words.ContainsKey(Language.English)) return displayName;
            {
                foreach (var particletranslate in words[Language.English].Where(particletranslate => particletranslate.Value == displayName).Where(particletranslate => words[to].ContainsKey(particletranslate.Key)))
                {
                    return words[to][particletranslate.Key];
                }
            } return displayName;
        }
        private enum Language { English, Turkish }
    }
}
