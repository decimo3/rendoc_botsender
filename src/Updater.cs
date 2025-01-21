namespace docbot
{
    public static class Updater
    {
        private static String CheckNewerChromeDriverVersion()
        {
            // https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_116
            using(var client = new HttpClient())
            {
                var last_version_url = "https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_STABLE";
                var request = new System.Net.Http.HttpRequestMessage(HttpMethod.Get, last_version_url);
                var response = client.Send(request);
                response.EnsureSuccessStatusCode();
                using(var stream = new StreamReader(response.Content.ReadAsStream()))
                {
                    return stream.ReadToEnd();
                }
            }
        }
        private static void DownloadNewerChromeDriver(String driver_version)
        {
            // https://storage.googleapis.com/chrome-for-testing-public/127.0.6533.88/win64/chromedriver-win64.zip
            using(var client = new HttpClient())
            {
                var last_version_url = $"https://storage.googleapis.com/chrome-for-testing-public/{driver_version}/win64/chromedriver-win64.zip";
                var request = new System.Net.Http.HttpRequestMessage(HttpMethod.Get, last_version_url);
                var response = client.Send(request);
                response.EnsureSuccessStatusCode();
                using(var stream = response.Content.ReadAsStream())
                {
                    using(var file = System.IO.File.Create("chromedriver-win64.zip"))
                    {
                        stream.CopyTo(file);
                        file.Flush();
                    }
                }
            }
        }
        private static void DeleteOlderDriverFile()
        {
            var files = System.IO.Directory.GetFiles("chromedriver-win64");
            foreach (var file in files) System.IO.File.Delete(file);
        }
        private static void UnzipChromeDriverFile()
        {
            var current_folder = System.AppContext.BaseDirectory;
            System.IO.Compression.ZipFile.ExtractToDirectory("chromedriver-win64.zip", current_folder);
        }
        private static Int32 GetVersionAplicationOutput(String aplication, String arguments)
        {
            var stdoutput = Helpers.Executor(aplication, arguments);
            var regex = new System.Text.RegularExpressions.Regex(@"\d+");
            var match = regex.Match(stdoutput);
            if(!match.Success)
                throw new InvalidOperationException("Não foi encontrada a versão da aplicação nas propriedades do arquivo!");
            return Int32.Parse(match.Value);
        }
        public static void Update(String chromepath, String driverpath)
        {
            try
            {
                Console.WriteLine("Verificando as versões do browser e do driver...");
                var argumento = $"-c \"(Get-Item '{chromepath}').VersionInfo.ProductVersion.ToString()\"";
                var chrome_version = GetVersionAplicationOutput("powershell", argumento);
                Console.WriteLine($"Chrome major version: {chrome_version}.");
                var driver_version = GetVersionAplicationOutput(driverpath, "--version");
                Console.WriteLine($"Driver major version: {driver_version}.");
                if(driver_version >= chrome_version) return;
                Console.WriteLine("Buscando as novas versões do chromedriver...");
                var newer_version = CheckNewerChromeDriverVersion();
                Console.WriteLine($"Versão do chromedriver no canal STABLE: {newer_version}");
                Console.Write("Baixando a nova versão do chromedriver...");
                DownloadNewerChromeDriver(newer_version);
                Console.Write(" Download concluído!\n");
                Console.Write("Removendo a versão antiga do chromedriver...");
                DeleteOlderDriverFile();
                Console.Write(" Remoção concluída!\n");
                Console.Write("Descompactando atualização...");
                UnzipChromeDriverFile();
                Console.Write(" Atualização concluída!\n");
            }
            catch (System.Exception erro)
            {
                Console.WriteLine(erro.Message);
                Console.WriteLine(erro.StackTrace);
            }
        }
    }
}
