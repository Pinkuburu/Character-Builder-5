﻿using OGL;
using OGL.Common;
using OGL.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CB_5e.Views
{
    public interface IHTMLService
    {
        string Convert(IXML obj);
        void Reset(ConfigManager config);
    }
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InfoPage : ContentPage
    {
        private InfoPage(IXML obj)
        {
            InitializeComponent();
            BindingContext = this;
            Obj = obj;
            if (Obj is IOGLElement o) Title = o.Name;
            else if (Obj is Feature f) Title = f.Name;
            else Title = "No Info";
        }
        private HtmlWebViewSource src = new HtmlWebViewSource();
        public HtmlWebViewSource Info { get { return src; } set { src = value; OnPropertyChanged("Info"); } }

        public IXML Obj { get; private set; }

        public static InfoPage Show(IXML obj)
        {
            InfoPage Instance = new InfoPage(obj);
            return Instance;
        }

        protected override void OnAppearing()
        {
            Task.Run(() =>
            {
                if (Obj != null)
                {
                    src.Html = DependencyService.Get<IHTMLService>().Convert(Obj);
                }
                else
                {
                    src.Html = "";
                }
            }).Forget();
        }
    }
}