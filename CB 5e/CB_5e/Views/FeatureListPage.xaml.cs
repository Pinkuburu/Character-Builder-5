﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CB_5e.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CB_5e.ViewModels;
using OGL.Features;


namespace CB_5e.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FeatureListPage : ContentPage
	{
        private List<Feature> features;
        public ObservableRangeCollection<FeatureViewModel> Features { get; set; } = new ObservableRangeCollection<FeatureViewModel>();
        public IEditModel Model { get; private set; }
        public string Property { get; private set; }
        private int move = -1;
        private bool Modal = true;

        public FeatureListPage (IEditModel parent, string property, bool modal = true)
		{
            Model = parent;
            Modal = modal;
            parent.PropertyChanged += Parent_PropertyChanged;
            Property = property;
            UpdateFeatures();
			InitializeComponent ();
            InitToolbar(Modal);
            BindingContext = this;
        }

        private void InitToolbar(bool modal)
        {
            if (Modal)
            {
                ToolbarItem undo = new ToolbarItem() { Text = "Undo" };
                undo.SetBinding(MenuItem.CommandProperty, new Binding("Undo"));
                ToolbarItems.Add(undo);
                ToolbarItem redo = new ToolbarItem() { Text = "Redo" };
                redo.SetBinding(MenuItem.CommandProperty, new Binding("Redo"));
                ToolbarItems.Add(redo);
            }
            ToolbarItem add = new ToolbarItem() { Text = "Add" };
            add.Clicked += Add_Clicked;
            ToolbarItems.Add(add);
            ToolbarItem paste = new ToolbarItem() { Text = "Paste" };
            paste.Clicked += Paste_Clicked;
            ToolbarItems.Add(paste);
        }

        private void Parent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "" || e.PropertyName == null || e.PropertyName == Property) UpdateFeatures();
        }

        private void UpdateFeatures()
        {
            features = (List<Feature>)Model.GetType().GetRuntimeProperty(Property).GetValue(Model);
            Fill();
        }
        private void Fill() => Features.ReplaceRange(features.Select(f => new FeatureViewModel(f)));

        private void Paste_Clicked(object sender, EventArgs e)
        {
            try
            {
                Model.MakeHistory();
                foreach (Feature f in Feature.LoadString(DependencyService.Get<IClipboardService>().GetTextData())) features.Add(f);
                Fill();
            } catch (Exception ex)
            {
                DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void Add_Clicked(object sender, EventArgs e)
        {

        }

        private void Features_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is FeatureViewModel fvm) {
                if (move >= 0)
                {
                    Model.MakeHistory();
                    foreach (FeatureViewModel ff in Features) ff.Moving = false;
                    int target = features.FindIndex(ff => fvm.Feature == ff);
                    if (target >= 0 && move != target)
                    {
                        features.Insert(target, features[move]);
                        if (target < move) move++;
                        features.RemoveAt(move);
                        Fill();
                        (sender as ListView).SelectedItem = null;
                    }
                    move = -1;
                } else
                {

                }
            }
        }

        private void Delete_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is FeatureViewModel f)
            {
                Model.MakeHistory();
                int i = features.FindIndex(ff => f.Feature == ff);
                features.RemoveAt(i);
                Fill();
            }
        }

        private async void Info_Clicked(object sender, EventArgs e)
        {
            if ((sender as MenuItem).BindingContext is FeatureViewModel f) await Navigation.PushAsync(InfoPage.Show(f.Feature));
        }

        private void Move_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is FeatureViewModel f)
            {
                foreach (FeatureViewModel ff in Features) ff.Moving = false;
                f.Moving = true;
                move = features.FindIndex(ff => f.Feature == ff);
            }
            
        }

        private void Cut_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is FeatureViewModel f)
            {
                Model.MakeHistory();
                DependencyService.Get<IClipboardService>().PutTextData(f.Feature.Save(), f.Detail);
                int i = features.FindIndex(ff => f.Feature == ff);
                features.RemoveAt(i);
                Fill();
            }
        }

        private void Copy_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is FeatureViewModel f)
            {
                DependencyService.Get<IClipboardService>().PutTextData(f.Feature.Save(), f.Detail);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (Modal)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (Model.UnsavedChanges > 0)
                    {
                        if (await DisplayAlert("Unsaved Changes", "You have " + Model.UnsavedChanges + " unsaved changes. Do you want to save them before leaving?", "Yes", "No"))
                        {
                            bool written = await Model.SaveAsync(false);
                            if (!written)
                            {
                                if (await DisplayAlert("File Exists", "Overwrite File?", "Yes", "No"))
                                {
                                    await Model.SaveAsync(true);
                                    await Navigation.PopModalAsync();
                                }
                            }
                            else await Navigation.PopModalAsync();
                        }
                        else await Navigation.PopModalAsync();
                    }
                    else await Navigation.PopModalAsync();
                });
            }
            return Modal;
        }
    }
}