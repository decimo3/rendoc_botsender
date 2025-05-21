namespace docbot
{
    public static class Startup
    {
        public static void Main
        (
            String[] args
        )
        {
            try
            {
                var corrente = System.AppContext.BaseDirectory;
                var driverpath = System.IO.Path.Combine(corrente, "chromedriver-win64", "chromedriver.exe");
                var configuracoes = Helpers.ArquivoConfiguracao(
                    System.IO.Path.Combine(System.AppContext.BaseDirectory, "doc.conf"), '=');
                Updater.Update(configuracoes["GCHROME"], driverpath);
                var website = $"http://{configuracoes["USUARIO"]}:{configuracoes["PALAVRA"]}@{configuracoes["BASEURL"]}/ren/";
                var profundidade = Int32.Parse(configuracoes["PROFUNDIDADE"]) - 1;
                var handler = new WebHandler(
                    chromepath: configuracoes["GCHROME"],
                    driverpath: driverpath,
                    data_folder: System.IO.Path.Combine(corrente, "tmp"),
                    website: website,
                    baseurl: $"http://{configuracoes["BASEURL"]}"
                );
                var program = new Program(handler);
                Helpers.ConsoleWrapper("Iniciando a checagem de uploads...");
                program.ChecarUpload();
                Helpers.ConsoleWrapper("Iniciando a navegação automatizada...");
                program.NavegacaoPastas(configuracoes["CAMINHO"], 0, profundidade);
            }
            catch (System.Exception erro)
            {
                Helpers.ConsoleWrapper(erro.Message);
                Helpers.ConsoleWrapper(erro.StackTrace);
            }
            finally
            {
                Helpers.ConsoleWrapper("Sistema finalizado! Aperte qualquer tecla para sair...");
                Console.ReadLine();
                Helpers.ConsoleWrapper("bye!");
            }
        }
    }
}
