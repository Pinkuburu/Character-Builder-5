﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OGL.Features;
using Xamarin.Forms;
using Character_Builder;
using CB_5e.Helpers;
using System.Globalization;
using OGL;
using OGL.Base;
using OGL.Spells;
using CB_5e.Views;

namespace CB_5e.ViewModels
{
    public class SpellPrepareViewModel : SpellbookViewModel
    {
        private static CultureInfo culture = CultureInfo.InvariantCulture;
        public Spellcasting Spellcasting { get; private set; }
        public SpellPrepareViewModel(PlayerViewModel model, SpellcastingFeature spellcasting) : base(model, spellcasting)
        {
            Title = spellcasting.DisplayName;
            if (Title == null || Title == "") Title = SpellcastingID;
            Spellcasting = Model.Context.Player.GetSpellcasting(SpellcastingID);
            OnPrepare = new Command((par) =>
            {
                if (par is SpellViewModel svm && SpellcastingFeature != null && !svm.AddAlwaysPreparedToName)
                {
                    if (!svm.Prepared)
                    {
                        if (Count < Able)
                        {
                            svm.Prepared = true;
                            Model.MakeHistory();
                            Spellcasting.GetPreparedList(Model.Context.Player, Model.Context).Add(svm.Name + " " + ConfigManager.SourceSeperator + " " + svm.Source);
                            Model.Save();
                        }
                    } else
                    {
                        svm.Prepared = false;
                        Model.MakeHistory();
                        string r = svm.Name + " " + ConfigManager.SourceSeperator + " " + svm.Source;
                        Spellcasting.GetPreparedList(Model.Context.Player, Model.Context).RemoveAll(s => ConfigManager.SourceInvariantComparer.Equals(s, r));
                        Model.Save();
                    }
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Prepared");
                    Model.ChangedPreparedSpells(SpellcastingID);
                }
            }, (par) => par is SpellViewModel svm && !svm.AddAlwaysPreparedToName);
            ShowInfo = new Command(async (par) =>
            {
                if (par is SpellViewModel svm)
                {
                    if (svm.Spell is ModifiedSpell ms)
                    {
                        ms.Info = Model.Context.Player.GetAttack(ms, ms.differentAbility == Ability.None ? SpellcastingFeature.SpellcastingAbility : ms.differentAbility);
                        ms.Modifikations.AddRange(from f in Model.Context.Player.GetFeatures() where f is SpellModifyFeature && Utils.Matches(Model.Context, ms, ((SpellModifyFeature)f).Spells, null) select f);
                        ms.Modifikations = ms.Modifikations.Distinct().ToList();
                    }
                    await Navigation.PushAsync(InfoPage.Show(svm.Spell));
                }
            });
            ResetPrepared = new Command(() =>
            {
                IsBusy = true;
                Model.MakeHistory();
                Spellcasting.GetPreparedList(Model.Context.Player, Model.Context).Clear();
                Model.Save();
                foreach (SpellViewModel s in spells)
                {
                    s.Prepared = s.AddAlwaysPreparedToName;
                }
                UpdateSpells();
                OnPropertyChanged("Count");
                OnPropertyChanged("Prepared");
                Model.ChangedPreparedSpells(SpellcastingID);
                IsBusy = false;
            });
            AddSpells();
        }
        public void AddSpells()
        {
            spells.Clear();
            Spells.ReplaceRange(spells);
            if (SpellcastingFeature.Preparation == PreparationMode.ClassList)
            {
                spells.AddRange(from s in Spellcasting.GetPrepared(Model.Context.Player, Model.Context)
                                select new SpellViewModel(s)
                                {
                                    Prepared = true,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
                spells.AddRange(from s in Spellcasting.GetAdditionalClassSpells(Model.Context.Player, Model.Context)
                                where !spells.Exists(t => t.Name == s.Name && s.Source == t.Spell.Source)
                                select new SpellViewModel(s)
                                {
                                    Prepared = false,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
                spells.AddRange(from s in Utils.FilterSpell(Model.Context, SpellcastingFeature.PrepareableSpells, SpellcastingID, Model.Context.Player.GetClassLevel(SpellcastingID))
                                where !spells.Exists(t => t.Name == s.Name && s.Source == t.Spell.Source)
                                select new SpellViewModel(s)
                                {
                                    Prepared = false,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
            }
            else if (SpellcastingFeature.Preparation == PreparationMode.Spellbook)
            {
                spells.AddRange(from s in Spellcasting.GetPrepared(Model.Context.Player, Model.Context)
                                select new SpellViewModel(s)
                                {
                                    Prepared = true,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
                spells.AddRange(from s in Spellcasting.GetAdditionalClassSpells(Model.Context.Player, Model.Context)
                                where !spells.Exists(t => t.Name == s.Name && s.Source == t.Spell.Source)
                                select new SpellViewModel(s)
                                {
                                    Prepared = true,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
                
                spells.AddRange(from s in Spellcasting.GetSpellbook(Model.Context.Player, Model.Context)
                                where !spells.Exists(t => t.Name == s.Name && s.Source == t.Spell.Source)
                                select new SpellViewModel(s)
                                {
                                    Prepared = false,
                                    Prepare = OnPrepare,
                                    ShowInfo = ShowInfo
                                });
            }
            spells.Sort();
            UpdateSpells();
        }

        public override void Refresh()
        {
            Spellcasting = Model.Context.Player.GetSpellcasting(SpellcastingID);
            AddSpells();
            base.Refresh();
        }

        private List<SpellViewModel> spells = new List<SpellViewModel>();
        public ObservableRangeCollection<SpellViewModel> Spells { get; private set; } = new ObservableRangeCollection<SpellViewModel>();
        private string spellSearch;
        public String SpellSearch
        {
            get => spellSearch; set
            {
                SetProperty(ref spellSearch, value);
                UpdateSpells();
            }
        }

        public Command ShowInfo { get; private set; }
        public Command OnPrepare { get; private set; }

        public void UpdateSpells() => Spells.ReplaceRange(from f in spells where spellSearch == null || spellSearch == ""
            || culture.CompareInfo.IndexOf(f.Spell.Name, spellSearch, CompareOptions.IgnoreCase) >= 0
            || culture.CompareInfo.IndexOf(f.Spell.Description, spellSearch, CompareOptions.IgnoreCase) >= 0
            || f.Spell.GetKeywords().Exists(k => culture.CompareInfo.IndexOf(k.Name, spellSearch, CompareOptions.IgnoreCase) >= 0)
            || f.Spell.Descriptions.Exists(k => culture.CompareInfo.IndexOf(k.Text, spellSearch, CompareOptions.IgnoreCase) >= 0)
            orderby f.Name select f);

        public int Count { get => Spellcasting.GetPreparedList(Model.Context.Player, Model.Context).Count; }
        public int Able { get => Utils.AvailableToPrepare(Model.Context, SpellcastingFeature, Model.Context.Player.GetClassLevel(SpellcastingID)); }
        public string Prepared { get => Count + "/" + Able + " Spells Prepared"; }
        public Command ResetPrepared { get; private set; }
    }
}
