using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V124.ServiceWorker;
using OpenQA.Selenium.Support.UI;
static Int32 GetVersionAplicationOutput(String aplication, String arguments)
{
    using(var process = new System.Diagnostics.Process())
    {
        process.StartInfo.FileName = aplication;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        var stdoutput = process.StandardOutput.ReadToEnd();
        var erroutput = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if(process.ExitCode != 0) throw new InvalidOperationException($"Erro ao executar o processo {process.ExitCode}: {erroutput}");
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var match = regex.Match(stdoutput);
        if(!match.Success) throw new InvalidOperationException("Não foi encontrada a versão da aplicação nas propriedades do arquivo!");
        return Int32.Parse(match.Value);
    }
}
static String CheckNewerChromeDriverVersion()
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
static void DownloadNewerChromeDriver(String driver_version)
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
static void DeleteOlderDriverFile()
{
    var files = System.IO.Directory.GetFiles("chromedriver-win64");
    foreach (var file in files) System.IO.File.Delete(file);
}
static void UnzipChromeDriverFile()
{
    var current_folder = System.AppContext.BaseDirectory;
    System.IO.Compression.ZipFile.ExtractToDirectory("chromedriver-win64.zip", current_folder);
}
static void Update(String chromepath, String driverpath)
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
static void ConsoleWrapper(String? mensagem)
{
    if(mensagem == null) return;
    var logfile = $"{DateTime.Now.ToString("yyyyMMdd")}.log";
    var escrever = $"{DateTime.Now} - {mensagem}\n";
    Console.WriteLine(escrever);
    File.AppendAllText(logfile, escrever);
}
static Dictionary<string, string> ArquivoConfiguracao(String arquivo, Char separador)
{
    var parametros = new Dictionary<string, string>();
    if (!System.IO.File.Exists(arquivo))
        throw new InvalidOperationException($"O arquivo {arquivo} não foi encontrado!");
    var file = System.IO.File.ReadAllLines(arquivo);
    foreach (var line in file)
    {
        if (String.IsNullOrEmpty(line)) continue;
        var args = line.Split(separador);
        if (args.Length != 2) continue;
        var cfg = args[0];
        var val = args[1];
        parametros.Add(cfg, val);
    }
    return parametros;
}
static void EnviarArquivos(WebDriver driver, String nota, String inst, String option, List<String> files)
{
    if (!files.Any()) return;
    var stringinputfiles = (files.Count > 1) ? String.Join(" \n", files) : files.Single();
    ConsoleWrapper($"{{Nota: {nota}, Inst: {inst}, Files: \"{stringinputfiles}\"}}");
    if (driver.FindElement(By.Id("titulo")).Text == "Página de Pesquisa")
        driver.FindElement(By.Name("Btnincluir")).Click();
    driver.FindElement(By.Name("TxtNI")).SendKeys(inst);
    driver.FindElement(By.Name("TxtNF")).SendKeys(nota);
    var select = driver.FindElement(By.Name("SelecaoTipoDocumento"));
    select.FindElement(By.XPath($".//option[@value='{option}']")).Click();
    driver.FindElement(By.Name("FileUpload1")).SendKeys(String.Join(" \n", files));
    driver.FindElement(By.Name("Btnincluir")).Click();
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
    IAlert alert = wait.Until(a => a.SwitchTo().Alert());
    ConsoleWrapper(alert.Text);
    Thread.Sleep(500);
    alert.Accept();
    foreach (var file in files)
    {
        var dir = Path.GetDirectoryName(file) ?? throw new DirectoryNotFoundException();
        var ext = Path.GetExtension(file);
        var oldfilename = Path.GetFileNameWithoutExtension(file);
        var newfilename = oldfilename + ".send" + ext;
        System.IO.File.Move(file, Path.Combine(dir, newfilename));
    }
}
static void NavegacaoPastas(WebDriver driver, String caminho_atual, Int32 profundidade_atual, Int32 profundidade_maxima)
{
    if (profundidade_atual > profundidade_maxima) return;
    foreach (var pasta in Directory.GetDirectories(caminho_atual))
    {
        if (profundidade_atual == profundidade_maxima)
            PrepararArquivos(driver, pasta);
        NavegacaoPastas(driver, pasta, profundidade_atual + 1, profundidade_maxima);
    }
}
static void PrepararArquivos(WebDriver driver, String diretorio)
{
    var basename = diretorio.Split('\\').Last();
    var argumentos = basename.Split(' ');
    if (argumentos.Length < 3)
    {
        ConsoleWrapper($"Diretório {basename} fora do padrão!");
        return;
    }
    if (argumentos.Contains("OK"))
    {
        ConsoleWrapper($"Diretório {basename} Já foi enviado!");
        return;
    }
    var arquivos = Directory.GetFiles(diretorio);
    ConsoleWrapper($"Lista de arquivos no diretório '{basename}':");
    foreach (var arquivo in arquivos) ConsoleWrapper(arquivo);
    var fotos = arquivos.Where(
        f => Path.GetExtension(f) == ".jpeg" || Path.GetExtension(f) == ".jpg"
        ).ToList();
    var videos = arquivos.Where(f => Path.GetExtension(f) == ".mp4").ToList();
    var termos = arquivos.Where(f => Path.GetExtension(f) == ".pdf").ToList();
    if (arquivos.Length != (termos.Count + fotos.Count + videos.Count))
    {
        ConsoleWrapper($"Diretório {basename} contém arquivos não suportados!");
        return;
    }
    ConsoleWrapper("Enviando os arquivos com a(s) 'evidências da nota de serviço'...");
    while (true)
    {
        fotos = Directory.GetFiles(diretorio).Where(
            f => (Path.GetExtension(f) == ".jpeg" || Path.GetExtension(f) == ".pdf")
            && !Path.GetFileNameWithoutExtension(f).Contains(".send")).ToList();
        if (fotos.Any())
        {
            if (fotos.Count > 5) fotos = fotos.Take(5).ToList();
            EnviarArquivos(driver, argumentos[0], argumentos[1], "Foto", fotos);
        }
        else
        {
            break;
        }
    }
    ConsoleWrapper("Enviando os arquivos com a(s) 'filmagens da ocorrência de inspeção'...");
    foreach (var video in videos)
    {
        if (Path.GetFileNameWithoutExtension(video).Contains(".send")) continue;
        EnviarArquivos(driver, argumentos[0], argumentos[1], "Vídeo das Inspeções", new List<String> { video });
    }
    ConsoleWrapper("Enviando os arquivos com o(s) 'Termo de Ocorrência e Inspeção'...");
    foreach (var termo in termos)
    {
        if (Path.GetFileNameWithoutExtension(termo).Contains(".send")) continue;
        EnviarArquivos(driver, argumentos[0], argumentos[1], "TOI-Termo de Ocorrência e Inspeção", new List<String> { termo });
    }
    // Rename directory to append "OK"
    var newDirName = diretorio + " OK";
    Directory.Move(diretorio, newDirName);
    ConsoleWrapper($"Envio dos arquivos da pasta {basename} efetuado com sucesso!");
}
static void Main()
{
    try
    {
        var corrente = System.AppContext.BaseDirectory;
        var driverpath = System.IO.Path.Combine(corrente, "chromedriver-win64/chromedriver.exe");
        var configuracoes = ArquivoConfiguracao(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "doc.conf"), '=');
        Update(configuracoes["GCHROME"], driverpath);
        var website = $"http://{configuracoes["USUARIO"]}:{configuracoes["PALAVRA"]}@{configuracoes["BASEURL"]}/ren/";
        var profundidade = Int32.Parse(configuracoes["PROFUNDIDADE"]) - 1;
        var service = ChromeDriverService.CreateDefaultService(driverpath);
        var options = new ChromeOptions();
        options.BinaryLocation = configuracoes["GCHROME"];
        options.AddArgument($"--user-data-dir={corrente}\\tmp\\");
        options.AddArgument($"--app={website}");
        options.AddArgument($"--unsafely-treat-insecure-origin-as-secure={configuracoes["BASEURL"]}");
        ConsoleWrapper("Iniciando a navegação automatizada...");
        using (var driver = new ChromeDriver(service, options))
        {
            driver.Manage().Window.Maximize();
            ConsoleWrapper("Aguardando a liberação da página...");
            while (!driver.FindElements(By.Id("titulo")).Any())
            {
                Thread.Sleep(1_000);
            }
            ConsoleWrapper("Iniciando o escaneamento...");
            NavegacaoPastas(driver, configuracoes["CAMINHO"], 0, profundidade);
        }
    }
    catch (System.Exception erro)
    {
        ConsoleWrapper(erro.Message);
        ConsoleWrapper(erro.StackTrace);
    }
    finally
    {
        ConsoleWrapper("Sistema finalizado! Aperte qualquer tecla para sair...");
        Console.ReadLine();
        ConsoleWrapper("bye!");
    }
}
Main();
