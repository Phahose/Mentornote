using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mentornote.Desktop.Pages;

namespace Mentornote.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new SignUpPage(this));
        }

        public void NavigateToSignup()
        {
            MainFrame.Navigate(new SignUpPage(this));
        }

        public void NavigateToLogin()
        {
            MainFrame.Navigate(new LoginPage(this));
        }
    }
}
