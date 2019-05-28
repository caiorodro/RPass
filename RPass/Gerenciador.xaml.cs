using RPass.classes;
using System;
using System.Collections.Generic;
using System.IO;
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

using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Web;
using Windows.System;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RPass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Gerenciador : Page
    {
        private int ID_USUARIO { get; set; }

        public Gerenciador()
        {
            this.InitializeComponent();

            caregaComboTipo();
            listaSenhas();
        }

        #region senhas
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Navigator nav = e.Parameter as Navigator;

            if (nav != null && nav.navigatorName == "ID_USUARIO")
                ID_USUARIO = Convert.ToInt32(nav.navigatorValue);
        }

        private void caregaComboTipo()
        {
            using (RP_Database d = new RP_Database())
            {
                CB_TIPO.ItemsSource = d.listaTipos();
            }
        }

        private void PV1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.BTN_VOLTAR.Visibility = PV1.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (PV1.SelectedIndex == 2)
                carregaTipos();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int ID_SENHA = Convert.ToInt32(((Button)sender).Tag);

            LISTA_DE_SENHAS item = ((List<LISTA_DE_SENHAS>)LBX_senhas.ItemsSource).Where(_ => _.ID_SENHA == ID_SENHA).First();

            Edita_Senha(item);
        }

        private void Edita_Senha(LISTA_DE_SENHAS item)
        {
            CB_TIPO.SelectedItem = ((List<TB_TIPO>)CB_TIPO.ItemsSource).Where(_ => _.TIPO == item.TIPO).First();
            TXT_CREDENCIAIS.Text = item.DESCRICAO_SENHA;
            TXT_SENHA.Password = item.SENHA;

            CB_EXIBIR_SENHA.IsChecked = false;

            LBL_ID_SENHA.Text = item.ID_SENHA.ToString();

            LBL_FORM_SENHA.Text = "Alterar senha";

            PV1.SelectedIndex = 1;

            TXT_CREDENCIAIS.Focus(FocusState.Pointer);

            LBL_CADASTRO.Text = string.Empty;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int ID_SENHA = Convert.ToInt32(((Button)sender).Tag);

            deletaSenha(ID_SENHA);
        }

        private async void deletaSenha(int ID_SENHA)
        {
            MessageDialog message1 = MainPage.ConfirmMessage("Você confirma deletar esta senha?");

            var result = await message1.ShowAsync();

            if (result.Id.Equals(0)) // Sim
            {
                try
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                    LISTA_DE_SENHAS item = ((List<LISTA_DE_SENHAS>)LBX_senhas.ItemsSource).Where(_ => _.ID_SENHA == ID_SENHA).First();

                    using (RP_Database d = new classes.RP_Database())
                    {
                        d.deletaSenha(new TB_SENHA()
                        {
                            DESCRICAO_SENHA = item.DESCRICAO_SENHA,
                            ID_SENHA = ID_SENHA,
                            SENHA = d.Encrypt(item.SENHA),
                            ID_USUARIO = this.ID_USUARIO,
                            TIPO_SENHA = item.TIPO
                        });
                    }

                    listaSenhas();

                    PV1.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MainPage.ShowMessage(MainPage.titulo, ex.Message);
                }
                finally
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
                }
            }
        }

        private void TXT_credenciais_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TXT_SENHA.Focus(FocusState.Pointer);
        }

        private void BTN_SALVAR_emails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CB_TIPO.SelectedItem == null ||
                    TXT_CREDENCIAIS.Text.Trim().Length == 0 ||
                    TXT_SENHA.Password.Trim().Length == 0)
                {
                    throw new Exception("Preencha todos os campos");
                }

                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new RP_Database())
                {
                    string senha = d.Encrypt(TXT_SENHA.Password);

                    d.salvaSenha(new TB_SENHA()
                    {
                        ID_SENHA = Convert.ToInt32(LBL_ID_SENHA.Text),
                        DESCRICAO_SENHA = TXT_CREDENCIAIS.Text,
                        SENHA = senha,
                        ID_USUARIO = this.ID_USUARIO,
                        TIPO_SENHA = ((TB_TIPO)CB_TIPO.SelectedItem).TIPO
                    });
                }

                if (Convert.ToInt32(LBL_ID_SENHA.Text) > 0)
                    PV1.SelectedIndex = 0;

                listaSenhas();
                resetForm();

                LBL_CADASTRO.Text = Convert.ToInt32(LBL_ID_SENHA.Text) > 0 ? "Senha alterada com sucesso!" : "Senha gravada com sucesso!";
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private void listaSenhas()
        {
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new classes.RP_Database())
                {
                    this.LBX_senhas.ItemsSource = d.listaSenhas(this.TGL_SENHAS.IsOn, TXT_FILTRO.Text);
                }

                LBL_TOTAL_SENHAS.Text = ((List<LISTA_DE_SENHAS>)LBX_senhas.ItemsSource).Count.ToString();
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private void TXT_TIPO_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }

        private void TXT_SENHA_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }

        private void BTN_PESQUISAR_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TXT_TIPO_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void TXT_credenciais_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void BTN_NOVO_Click(object sender, RoutedEventArgs e)
        {
            this.PV1.SelectedIndex = 1;
            resetForm();

            //Navigator nav = new Navigator();
            //nav.navigatorName = "ID_USUARIO";
            //nav.navigatorValue = ID_USUARIO.ToString();

            //this.Frame.Navigate(typeof(Camera), nav);
        }

        private void BTN_VOLTAR_Click(object sender, RoutedEventArgs e)
        {
            this.PV1.SelectedIndex = PV1.SelectedIndex == 2 ? 1 : 0;
        }

        private void resetForm()
        {
            this.TXT_CREDENCIAIS.Text = string.Empty;
            this.TXT_SENHA.Password = string.Empty;
            this.TXT_SENHA1.Text = string.Empty;
            this.CB_EXIBIR_SENHA.IsChecked = false;

            LBL_FORM_SENHA.Text = "Nova senha";
            LBL_ID_SENHA.Text = "0";

            LBL_CADASTRO.Text = string.Empty;

            this.CB_TIPO.Focus(FocusState.Pointer);
        }

        private void TGL_SENHAS_Toggled(object sender, RoutedEventArgs e)
        {
            List<LISTA_DE_SENHAS> lista = (List<LISTA_DE_SENHAS>)LBX_senhas.ItemsSource;

            foreach (var item in lista)
                item.SENHA_MASCARA = TGL_SENHAS.IsOn ? item.SENHA : "*******";

            List<LISTA_DE_SENHAS> lista1 = new List<classes.LISTA_DE_SENHAS>();

            foreach (var item in lista)
                lista1.Add(item);

            LBX_senhas.ItemsSource = lista1;
        }

        #endregion

        #region camera 

        #endregion

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            filesPicker();
        }

        private async void filesPicker()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;

            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                // Application now has read/write access to the picked file
                // this.textBlock.Text = "Picked photo: " + file.Name;
            }
            else
            {
                // this.textBlock.Text = "Operation cancelled.";
            }
        }

        private void BTN_SUGERIR_SENHA_Click(object sender, RoutedEventArgs e)
        {
            using (RP_Database d = new RP_Database())
            {
                TXT_SENHA.Password = d.Sugere_Senha();
                TXT_SENHA1.Text = TXT_SENHA.Password;

                this.CB_EXIBIR_SENHA.IsChecked = true;
            }
        }

        private void CB_EXIBIR_SENHA_Checked(object sender, RoutedEventArgs e)
        {
            TXT_SENHA1.Text = TXT_SENHA.Password;

            TXT_SENHA.Visibility = CB_EXIBIR_SENHA.IsChecked.Value ? Visibility.Collapsed : Visibility.Visible;
            TXT_SENHA1.Visibility = !CB_EXIBIR_SENHA.IsChecked.Value ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BTN_NOVO_TIPO_Click(object sender, RoutedEventArgs e)
        {
            resetaTipo();
        }

        private void resetaTipo()
        {
            TXT_TIPO.Text = string.Empty;
            hTIPO.Text = "0";

            TXT_TIPO.Focus(FocusState.Pointer);
        }

        private void BTN_SALVAR_TIPO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TXT_TIPO.Text.Trim().Length == 0)
                    throw new Exception("Preencha o campo");

                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new RP_Database())
                {
                    d.salvaTipo(new TB_TIPO()
                    {
                        ID_TIPO = Convert.ToInt32(hTIPO.Text),
                        TIPO = TXT_TIPO.Text.ToUpper()
                    });
                }

                carregaTipos();
                caregaComboTipo();
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private void carregaTipos()
        {
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new classes.RP_Database())
                {
                    LBX_TIPO.ItemsSource = d.listaTipos();
                }
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            hTIPO.Text = ((Button)sender).Tag.ToString();
            TXT_TIPO.Text = ((List<TB_TIPO>)LBX_TIPO.ItemsSource).Where(_ => _.ID_TIPO == Convert.ToInt32(hTIPO.Text)).First().TIPO;

            TXT_TIPO.Focus(FocusState.Pointer);
        }

        private void BTN_NOVO_TIPO1_Click(object sender, RoutedEventArgs e)
        {
            this.PV1.SelectedIndex = 2;
            resetaTipo();
            TXT_TIPO.Focus(FocusState.Pointer);
        }

        // Files Thumbnails

        #region anexos

        private void BTN_PIC_Click(object sender, RoutedEventArgs e)
        {
            Anexos_da_Senha();
        }

        private async void Anexos_da_Senha()
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);

            FileOpenPicker openPicker = new FileOpenPicker();

            openPicker.FileTypeFilter.Add("*");

            var files = await openPicker.PickMultipleFilesAsync();

            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                List<ANEXO_SENHA> lista = new List<classes.ANEXO_SENHA>();

                foreach (var file in files)
                {
                    using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView))
                    {
                        if (thumbnail != null)
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);

                            lista.Add(new ANEXO_SENHA()
                            {
                                ID_SENHA = Convert.ToInt32(this.LBL_ID_SENHA_ANEXO.Text),
                                ANEXO = file.Name,
                                CONTEUDO = await ReadFile(file),
                                IMAGE = bitmapImage
                            });
                        }
                    }
                }

                List<TB_ANEXO_SENHA> lista1 = new List<TB_ANEXO_SENHA>();

                foreach (var item in lista)
                {
                    lista1.Add(new TB_ANEXO_SENHA()
                    {
                        ID_SENHA = item.ID_SENHA,
                        ANEXO = item.ANEXO,
                        CONTEUDO = item.CONTEUDO
                    });
                }

                if (lista1.Any())
                {
                    using (RP_Database d = new RP_Database())
                    {
                        d.gravaAnexos(lista1);
                    }

                    carregaAnexos();
                    listaSenhas();
                }
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        private async void carregaAnexos()
        {
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                using (RP_Database d = new RP_Database())
                {
                    LBX_ANEXO.ItemsSource = await d.carregaAnexos(Convert.ToInt32(LBL_ID_SENHA_ANEXO.Text));
                }
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }

        public async Task<byte[]> ReadFile(StorageFile file)
        {
            byte[] fileBytes = null;
            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }

            return fileBytes;
        }

        #endregion

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            int ID_ANEXO = Convert.ToInt32(((Button)sender).Tag);
            deletaAnexo(ID_ANEXO);
        }

        private async void deletaAnexo(int ID_ANEXO)
        {
            MessageDialog message1 = MainPage.ConfirmMessage("Você confirma deletar este anexo?");

            var result = await message1.ShowAsync();

            if (result.Id.Equals(0)) // Sim
            {
                try
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                    ANEXO_SENHA item = ((List<ANEXO_SENHA>)LBX_ANEXO.ItemsSource).Where(_ => _.ID_ANEXO == ID_ANEXO).First();

                    using (RP_Database d = new classes.RP_Database())
                    {
                        d.deletaAnexo(new TB_ANEXO_SENHA()
                        {
                            ID_ANEXO = item.ID_ANEXO,
                            ID_SENHA = item.ID_SENHA,
                            CONTEUDO = item.CONTEUDO,
                            ANEXO = item.ANEXO
                        });
                    }

                    carregaAnexos();
                    listaSenhas();
                }
                catch (Exception ex)
                {
                    MainPage.ShowMessage(MainPage.titulo, ex.Message);
                }
                finally
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
                }
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            int ID_SENHA = Convert.ToInt32(((Button)sender).Tag);

            PV1.SelectedIndex = 3;

            this.LBL_ID_SENHA_ANEXO.Text = ID_SENHA.ToString();

            carregaAnexos();
        }

        private void BTN_FILTRO_Click(object sender, RoutedEventArgs e)
        {
            listaSenhas();
        }

        private void TXT_FILTRO_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                listaSenhas();
        }

        private void CB_TIPO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TXT_CREDENCIAIS.Focus(FocusState.Pointer);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            int ID_ANEXO = Convert.ToInt32(((Button)sender).Tag);
            abreAnexo(ID_ANEXO);
        }

        private async void abreAnexo(int ID_ANEXO)
        {
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 10);

                var item = ((List<ANEXO_SENHA>)LBX_ANEXO.ItemsSource).Where(_ => _.ID_ANEXO == ID_ANEXO).First();

                StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(item.ANEXO, CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteBytesAsync(file, item.CONTEUDO);

                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex)
            {
                MainPage.ShowMessage(MainPage.titulo, ex.Message);
            }
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 10);
            }
        }
    }

    public sealed class StreamUriWinRTResolver : IUriToStreamResolver
    {
        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            if (uri == null)
                throw new Exception();

            string path = uri.AbsolutePath;

            return GetContent(path).AsAsyncOperation();
        }

        private async Task<IInputStream> GetContent(string path)
        {
            try
            {
                Uri localUri = new Uri("ms-appx://" + path);
                StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(localUri);
                IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                return stream;
            }
            catch (Exception) { throw new Exception("Invalid path"); }
        }
    }
}