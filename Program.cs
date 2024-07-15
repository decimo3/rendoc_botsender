using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
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
try
{
    var corrente = System.IO.Directory.GetCurrentDirectory();
    var configuracoes = ArquivoConfiguracao("doc.conf", '=');
    var diretorios = System.IO.Directory.GetDirectories(corrente);
    var profundidade = Int32.Parse(configuracoes["PROFUNDIDADE"]);
    var service = ChromeDriverService.CreateDefaultService(corrente);
    var options = new ChromeOptions();
    options.BinaryLocation = configuracoes["GCHROME"];
    options.AddArgument($"--user-data-dir={corrente}\\tmp\\");
    options.AddArgument($"--app={configuracoes["WEBSITE"]}");
    options.AddArgument($"--unsafely-treat-insecure-origin-as-secure={configuracoes["WEBSITE"]}");
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
        foreach (var diretorio in diretorios)
        {
            var basename = diretorio.Split('\\').Last();
            if (basename == "tmp" || basename == "selenium-manager") continue;
            var argumentos = basename.Split(' ');
            if (argumentos.Length < 3)
            {
                ConsoleWrapper($"Diretório {basename} fora do padrão!");
                continue;
            }
            if (argumentos.Contains("OK"))
            {
                ConsoleWrapper($"Diretório {basename} Já foi enviado!");
                continue;
            }
            var arquivos = Directory.GetFiles(diretorio);
            ConsoleWrapper($"Lista de arquivos no diretório '{basename}':");
            foreach (var arquivo in arquivos) ConsoleWrapper(arquivo);
            var fotos = arquivos.Where(f => Path.GetExtension(f) == ".jpeg").ToList();
            var videos = arquivos.Where(f => Path.GetExtension(f) == ".mp4").ToList();
            var termos = arquivos.Where(f => Path.GetExtension(f) == ".pdf").ToList();
            if (arquivos.Count() != (termos.Count + fotos.Count + videos.Count))
            {
                ConsoleWrapper($"Diretório {basename} contém arquivos não suportados!");
                continue;
            }
            ConsoleWrapper("Enviando os arquivos com a(s) 'evidências da nota de serviço'...");
            while (true)
            {
                fotos = Directory.GetFiles(diretorio).Where(
                    f => Path.GetExtension(f) == ".jpeg" &&
                    !Path.GetFileNameWithoutExtension(f).Contains(".send")).ToList();
                if(fotos.Any())
                {
                    if(fotos.Count > 5) fotos = fotos.Take(5).ToList();
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
                if(Path.GetFileNameWithoutExtension(video).Contains(".send")) continue;
                EnviarArquivos(driver, argumentos[0], argumentos[1], "Vídeo das Inspeções", new List<String> { video });
            }
            ConsoleWrapper("Enviando os arquivos com o(s) 'Termo de Ocorrência e Inspeção'...");
            foreach (var termo in termos)
            {
                if(Path.GetFileNameWithoutExtension(termo).Contains(".send")) continue;
                EnviarArquivos(driver, argumentos[0], argumentos[1], "TOI-Termo de Ocorrência e Inspeção", new List<String> { termo });
            }
            // Rename directory to append "OK"
            var newDirName = diretorio + " OK";
            Directory.Move(diretorio, newDirName);
            ConsoleWrapper($"Envio dos arquivos da pasta {basename} efetuado com sucesso!");
        }
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
