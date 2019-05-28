using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net;
using System.IO;
using SQLite.Net.Attributes;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace RPass.classes
{
    public class RP_Database : IDisposable
    {
        private SQLiteConnection conn { get; set; }

        public RP_Database()
        {
            string arquivoDatabase = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "RPass.sqlite");

            conn = new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(),
                Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "RPass.sqlite"));

            criaTabelas();
        }

        #region usuarios

        public int validaLogin(string EMAIL, string SENHA)
        {
            var existe = conn.Find<TB_USUARIO>(_ => _.EMAIL_USUARIO == EMAIL);

            if (existe == null)
                throw new Exception("E-mail inexistente no cadastro");

            string cipherText = Encrypt(SENHA.Trim());

            var login = conn.Find<TB_USUARIO>(_ => _.EMAIL_USUARIO == EMAIL && _.SENHA_USUARIO == cipherText);

            if (login == null)
                throw new Exception("Senha incorreta");

            return login.ID_USUARIO;
        }

        public void Novo_Usuario(TB_USUARIO usuario)
        {
            var existe = conn.Find<TB_USUARIO>(_ => _.EMAIL_USUARIO == usuario.EMAIL_USUARIO);

            if (existe == null)
            {
                conn.Insert(new TB_USUARIO()
                {
                    NOME_USUARIO = usuario.NOME_USUARIO,
                    SENHA_USUARIO = usuario.SENHA_USUARIO,
                    EMAIL_USUARIO = usuario.EMAIL_USUARIO
                });
            }
            else
                throw new Exception("Já existe um usuário cadastrado com este e-mail [" + usuario.EMAIL_USUARIO.Trim().ToLower() + "]");
        }

        public void alteraSenha(string EMAIL, string SENHA_ATUAL, string NOVA_SENHA)
        {
            var existe = conn.Find<TB_USUARIO>(_ => _.EMAIL_USUARIO == EMAIL);

            if (existe == null)
                throw new Exception("E-mail não cadastrado");

            string senha = Encrypt(SENHA_ATUAL);

            var usuario = conn.Find<TB_USUARIO>(_ => _.EMAIL_USUARIO == EMAIL && _.SENHA_USUARIO == senha);

            if (usuario == null)
                throw new Exception("Senha atual incorreta");

            var x = conn.Table<TB_USUARIO>().Where(_ => _.EMAIL_USUARIO == EMAIL).First();

            senha = Encrypt(NOVA_SENHA);

            x.SENHA_USUARIO = senha;

            conn.Update(x);
        }

        public void gravaEmailLogin(EMAIL_LOGIN item)
        {
            if (!conn.Table<EMAIL_LOGIN>().Any())
                conn.Insert(item);
            else
            {
                conn.Delete(item);
                conn.Insert(item);
            }
        }

        public string buscaLogin()
        {
            string retorno = string.Empty;

            var query = conn.Table<EMAIL_LOGIN>();

            foreach (var item in query)
            {
                retorno = item.EMAIL;
                break;
            }

            return retorno;
        }

        #endregion

        #region criptografia

        public string Encrypt(string passwordText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(passwordText);

            return System.Convert.ToBase64String(plainTextBytes);
        }

        public string Decrypt(string passwordText)
        {
            byte[] bytes = System.Convert.FromBase64String(passwordText);

            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region SENHAS

        public List<TB_TIPO> listaTipos()
        {
            return conn.Table<TB_TIPO>().OrderBy(_ => _.TIPO).ToList();
        }

        public void salvaTipo(TB_TIPO item)
        {
            if (item.ID_TIPO == 0)
                conn.Insert(item);
            else
                conn.Update(item);
        }

        public void salvaSenha(TB_SENHA item)
        {
            if (item.ID_SENHA == 0)
                conn.Insert(item);
            else
                conn.Update(item);
        }

        public void deletaSenha(TB_SENHA item)
        {
            conn.Delete(item);
        }

        public List<LISTA_DE_SENHAS> listaSenhas(bool exibir, string FILTRO)
        {
            var query = FILTRO.Trim().Length > 0 ? 
                conn.Table<TB_SENHA>().Where(_ => _.DESCRICAO_SENHA.Contains(FILTRO) || _.TIPO_SENHA.Contains(FILTRO)) :
                conn.Table<TB_SENHA>();

            List<LISTA_DE_SENHAS> retorno = new List<classes.LISTA_DE_SENHAS>();

            foreach (var item in query)
            {
                retorno.Add(new LISTA_DE_SENHAS()
                {
                    DESCRICAO_SENHA = item.DESCRICAO_SENHA,
                    ID_SENHA = item.ID_SENHA,
                    SENHA = Decrypt(item.SENHA),
                    SENHA_MASCARA = exibir ? Decrypt(item.SENHA) : "********",
                    TIPO = item.TIPO_SENHA,
                    ANEXOS = conn.Table<TB_ANEXO_SENHA>().Where(_ => _.ID_SENHA == item.ID_SENHA).Count()
                });
            }

            return retorno.OrderBy(_ => _.TIPO).ToList();
        }

        public void deletaAnexo(TB_ANEXO_SENHA item)
        {
            conn.Delete(item);
        }

        #endregion

        public void criaTabelas()
        {
            if (!existeTabela("TB_USUARIO"))
            {
                conn.CreateTable<TB_USUARIO>();
                conn.CreateIndex("TB_USUARIO", new List<string>() { "EMAIL_USUARIO" }.ToArray());
            }

            if (!existeTabela("TB_SENHA"))
            {
                conn.CreateTable<TB_SENHA>();
                conn.CreateIndex("TB_SENHA", new List<string>() { "ID_USUARIO", "TIPO_SENHA" }.ToArray());
            }

            if (!existeTabela("EMAIL_LOGIN"))
                conn.CreateTable<EMAIL_LOGIN>();

            if (!existeTabela("TB_TIPO"))
            {
                conn.CreateTable<TB_TIPO>();
                conn.Insert(new TB_TIPO() { TIPO = "CARTÃO DE CRÉDITO" });
                conn.Insert(new TB_TIPO() { TIPO = "CARTÃO DE DÉBITO" });
                conn.Insert(new TB_TIPO() { TIPO = "SENHA DE E-MAIL" });
                conn.Insert(new TB_TIPO() { TIPO = "SENHA BANCÁRIA" });
                conn.Insert(new TB_TIPO() { TIPO = "REDES SOCIAIS" });
            }

            if (!existeTabela("TB_ANEXO_SENHA"))
                conn.CreateTable<TB_ANEXO_SENHA>();
        }

        public bool existeTabela(string tableName)
        {
            var query = conn.Query<List<object>>("SELECT name FROM sqlite_master WHERE type ='table' AND name = ?", tableName);

            return query.Any();
        }

        public string Sugere_Senha()
        {
            string senha = string.Empty;

            for (int i = 0; i < 8; i++)
            {
                Random rnd = new Random();
                int digit = rnd.Next(0, 4);

                string digit1 = "";

                if (digit == 0)
                    while (true)
                    {
                        digit1 = geraMinuscula();

                        if (!senha.Contains(digit1)) { senha += digit1; break; }
                    }

                else if (digit == 1)
                    while (true)
                    {
                        digit1 = geraMaiuscula();

                        if (!senha.Contains(digit1)) { senha += digit1; break; }
                    }

                else if (digit == 2)
                    while (true)
                    {
                        digit1 = geraDig();

                        if (!senha.Contains(digit1)) { senha += digit1; break; }
                    }

                else if (digit == 3)
                    while (true)
                    {
                        digit1 = geraSpecial();

                        if (!senha.Contains(digit1)) { senha += digit1; break; }
                    }

            }

            return senha;
        }

        private string geraMinuscula()
        {
            string senha = string.Empty;

            string min = "abcdefghijklmnopqrstuvwyz";

            Random rnd = new Random();
            int digit = rnd.Next(0, 24);

            senha += min.Substring(digit, 1);

            return senha;
        }

        private string geraMaiuscula()
        {
            string senha = "";

            string mai = "ABCDEFGHIJKLMNOPQRSTUVWYZ";

            Random rnd = new Random();
            int digit = rnd.Next(0, 24);

            senha += mai.Substring(digit, 1);

            return senha;
        }

        private string geraDig()
        {
            string senha = "";
            string num = "0123456789";

            for (int i = 0; i < 1; i++)
            {
                Random rnd = new Random();
                int digit = rnd.Next(0, 9);

                senha += num.Substring(digit, 1);
            }

            return senha;
        }

        private string geraSpecial()
        {
            string senha = "";

            string spec = "!@#$%&*";

            Random rnd = new Random();
            int digit = rnd.Next(0, 6);

            senha += spec.Substring(digit, 1);

            return senha;
        }

        public void gravaAnexos(List<TB_ANEXO_SENHA> items)
        {
            foreach (var item in items)
            {
                conn.Insert(item);
            }
        }

        public async Task<List<ANEXO_SENHA>> carregaAnexos(int ID_SENHA)
        {
            List<TB_ANEXO_SENHA> lista = conn.Table<TB_ANEXO_SENHA>().Where(_ => _.ID_SENHA == ID_SENHA).OrderBy(_ => _.ANEXO).ToList();

            List<ANEXO_SENHA> retorno = new List<classes.ANEXO_SENHA>();

            foreach (var item in lista)
            {
                retorno.Add(new ANEXO_SENHA()
                {
                    ID_SENHA = item.ID_SENHA,
                    ID_ANEXO = item.ID_ANEXO,
                    ANEXO = item.ANEXO,
                    CONTEUDO = item.CONTEUDO,
                    IMAGE = await getThumbNail(item.CONTEUDO, item.ANEXO)
                });
            }

            return retorno;
        }

        public async Task<BitmapImage> getThumbNail(byte[] bytes, string fileName)
        {
            StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);

            BitmapImage bitmapImage = null;

            using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView))
            {
                if (thumbnail != null)
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(thumbnail);
                }
            }

            return bitmapImage;
        }

        public void Dispose()
        {

        }
    }

    public class TB_USUARIO
    {
        public TB_USUARIO() { }

        [PrimaryKey, AutoIncrement]
        public int ID_USUARIO { get; set; }
        public string NOME_USUARIO { get; set; }
        public string EMAIL_USUARIO { get; set; }
        public string SENHA_USUARIO { get; set; }
    }

    public class TB_SENHA
    {
        public TB_SENHA() { }

        [PrimaryKey, AutoIncrement]
        public int ID_SENHA { get; set; }
        public int ID_USUARIO { get; set; }
        public string TIPO_SENHA { get; set; }
        public string DESCRICAO_SENHA { get; set; }
        public string SENHA { get; set; }
    }

    public class TB_TIPO
    {
        public TB_TIPO() { }

        [PrimaryKey, AutoIncrement]
        public int ID_TIPO { get; set; }
        public string TIPO { get; set; }
    }

    public class EMAIL_LOGIN
    {
        public EMAIL_LOGIN() { }

        [PrimaryKey]
        public string EMAIL { get; set; }
    }

    public class LISTA_DE_SENHAS
    {
        public LISTA_DE_SENHAS() { }

        public int ID_SENHA { get; set; }
        public string DESCRICAO_SENHA { get; set; }
        public string SENHA_MASCARA { get; set; }
        public string SENHA { get; set; }
        public string TIPO { get; set; }
        public int ANEXOS { get; set; }
    }

    public class TB_ANEXO_SENHA
    {
        public TB_ANEXO_SENHA() { }

        [PrimaryKey, AutoIncrement]
        public int ID_ANEXO { get; set; }
        public int ID_SENHA { get; set; }
        public string ANEXO { get; set; }
        public byte[] CONTEUDO { get; set; }
    }

    public class ANEXO_SENHA
    {
        public ANEXO_SENHA() { }

        public int ID_ANEXO { get; set; }
        public int ID_SENHA { get; set; }
        public string ANEXO { get; set; }
        public byte[] CONTEUDO { get; set; }
        public BitmapImage IMAGE { get; set; }
    }
}