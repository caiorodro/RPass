using RPass.classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static string titulo = "RPass";

        private string arquivoDatabase = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "RPass.sqlite");

        public MainPage()
        {
            this.InitializeComponent();

            this.WV_POLITICA.Text = new RPass_Politica().termo();

            if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(arquivoDatabase))
            {
                using (RP_Database d = new classes.RP_Database())
                {
                    TXT_EMAIL.Text = d.buscaLogin();
                    TXT_EMAIL.Focus(FocusState.Pointer);
                }
            }
            else
            {
                PV1.SelectedIndex = 3;
            }
        }

        private void TXT_SENHA_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                validaLogin();
        }

        private void TXT_EMAIL_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                TXT_SENHA.Focus(FocusState.Pointer);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            validaLogin();
        }

        private void validaLogin()
        {
            try
            {
                if (this.TXT_EMAIL.Text.Trim().Equals("") ||
                    this.TXT_SENHA.Password.Trim().Equals(""))
                {
                    throw new Exception("Preencha os campos");
                }

                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new classes.RP_Database())
                {
                    int ID_USUARIO = d.validaLogin(TXT_EMAIL.Text, TXT_SENHA.Password);

                    d.gravaEmailLogin(new EMAIL_LOGIN()
                    {
                        EMAIL = TXT_EMAIL.Text.Trim().ToLower()
                    });

                    Navigator nav = new Navigator();
                    nav.navigatorName = "ID_USUARIO";
                    nav.navigatorValue = ID_USUARIO.ToString();

                    this.Frame.Navigate(typeof(Gerenciador), nav);
                }
            }
            catch (Exception ex)
            {
                ShowMessage(titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        public static async void ShowMessage(string title, string content)
        {
            var showDialog = new MessageDialog(content, MainPage.titulo);
            showDialog.Commands.Add(new UICommand("Ok")
            {
                Id = 0
            });
            showDialog.DefaultCommandIndex = 0;
            var result = await showDialog.ShowAsync();
            if ((int)result.Id == 0)
            {
                //do your task  
            }
            else
            {
                //skip your task  
            }
        }

        public static MessageDialog ConfirmMessage(string message)
        {
            var dialog = new Windows.UI.Popups.MessageDialog(message, MainPage.titulo);

            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Sim") { Id = 0 });
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Não") { Id = 1 });

            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 1;

            return dialog;
        }

        private void BTN_REGISTRESE_Click(object sender, RoutedEventArgs e)
        {
            this.PV1.SelectedIndex = 1;
        }

        private void BTN_ALTERAR_SENHA1_Click(object sender, RoutedEventArgs e)
        {
            PV1.SelectedIndex = 2;

            this.TXT_EMAIL2.Text = TXT_EMAIL.Text;
        }

        private void BTN_CADASTRAR_Click(object sender, RoutedEventArgs e)
        {
            bool erro = false;

            if (this.TXT_NOME.Text.Trim().Length == 0 ||
                this.TXT_EMAIL1.Text.Trim().Length == 0 ||
                this.TXT_SENHA1.Password.Trim().Length == 0 ||
                this.TXT_SENHA2.Password.Trim().Length == 0)
            {
                erro = true;
                ShowMessage(MainPage.titulo, "Todos os campos são obrigatório");
            }

            if (!erro)
            {
                if (!this.TXT_SENHA1.Password.Trim().Equals(this.TXT_SENHA2.Password.Trim()))
                {
                    erro = true;
                    ShowMessage(MainPage.titulo, "As senhas não conferem");
                }
            }

            if (!erro)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                try
                {
                    using (RP_Database d = new RP_Database())
                    {
                        string cipherText = d.Encrypt(TXT_SENHA1.Password.Trim());

                        d.Novo_Usuario(new TB_USUARIO()
                        {
                            NOME_USUARIO = this.TXT_NOME.Text.Trim().ToUpper(),
                            SENHA_USUARIO = cipherText,
                            EMAIL_USUARIO = this.TXT_EMAIL1.Text.Trim().ToLower()
                        });
                    }

                    this.TXT_EMAIL.Text = this.TXT_EMAIL1.Text.Trim().ToLower();
                    this.PV1.SelectedIndex = 0;

                    this.LBL_CASASTRO.Text = "Cadastro realizado com sucesso! Faça o seu login";
                }
                catch (Exception ex)
                {
                    ShowMessage(MainPage.titulo, ex.Message);
                }
                finally
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
                }
            }
        }

        private void BTN_VOLTAR_Click(object sender, RoutedEventArgs e)
        {
            this.PV1.SelectedIndex = 0;
        }

        private void PV1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool dataExists = IsolatedStorageFile.GetUserStoreForApplication().FileExists(this.arquivoDatabase);

            if (PV1.SelectedIndex < 3)
                if (!dataExists)
                    PV1.SelectedIndex = 3;

            this.BTN_VOLTAR.Visibility = this.PV1.SelectedIndex == 0 ? Visibility.Collapsed :
                dataExists ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BTN_ALTERAR_SENHA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.TXT_EMAIL2.Text.Trim().Length == 0 ||
                    this.TXT_SENHA_ATUAL.Password.Trim().Length == 0 ||
                    this.TXT_NOVA_SENHA.Password.Trim().Length == 0 ||
                    this.TXT_NOVA_SENHA2.Password.Trim().Length == 0)
                {
                    throw new Exception("Informe todos os campos");
                }

                if (!this.TXT_NOVA_SENHA.Password.Trim().Equals(this.TXT_NOVA_SENHA2.Password.Trim()))
                    throw new Exception("A nova senha não confere");

                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new RP_Database())
                {
                    d.alteraSenha(this.TXT_EMAIL2.Text.Trim(), this.TXT_SENHA_ATUAL.Password.Trim(), TXT_NOVA_SENHA.Password.Trim());
                }

                this.LBL_CASASTRO.Text = "Senha alterada com sucesso!";
                this.TXT_EMAIL.Text = this.TXT_EMAIL2.Text;
                this.PV1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private void BTN_ACEITE_Click(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

            using (RP_Database d = new classes.RP_Database())
            {
                PV1.SelectedIndex = 0;
            }

            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
        }

        private void BTN_TERMO_Click(object sender, RoutedEventArgs e)
        {
            PV1.SelectedIndex = 3;
        }
    }
}

