using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace docbot
{
    public class WebHandler : IDisposable
    {
        private const int DEFAULT_PORT = 7826;
        private readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromMinutes(3);
        private readonly ChromeDriverService service;
        private readonly ChromeOptions options;
        private readonly IWebDriver driver;
        public WebHandler
        (
            String chromepath,
            String driverpath,
            String data_folder,
            String website,
            String baseurl
        )
        {
            if (!System.IO.File.Exists(chromepath) || !System.IO.File.Exists(driverpath))
                throw new FileNotFoundException($"O arquivo {chromepath} não foi encontrado!");
            service = ChromeDriverService.CreateDefaultService(driverpath);
            service.Port = DEFAULT_PORT;
            service.Start();

            options = new ChromeOptions();
            options.BinaryLocation = chromepath;
            options.AddArgument($"--user-data-dir={data_folder}");
            options.AddArgument($"--app={website}");
            options.AddArgument($"--unsafely-treat-insecure-origin-as-secure={baseurl}");

            driver = new RemoteWebDriver(
                new Uri($"http://localhost:{DEFAULT_PORT}/"),
                options.ToCapabilities(),
                DEFAULT_TIMEOUT);
            this.driver.Manage().Window.Maximize();

            Helpers.ConsoleWrapper("Aguardando a liberação da página...");
            while (!driver.FindElements(By.Id("titulo")).Any())
            {
                Thread.Sleep(1_000);
            }
        }
        public void EnviarArquivos
        (
            String nota,
            String inst,
            String option,
            List<String> files
        )
        {
            if (!files.Any()) return;
            var stringinputfiles = (files.Count > 1) ? String.Join(" \n", files) : files.Single();
            Helpers.ConsoleWrapper($"{{Nota: {nota}, Inst: {inst}, Files: \"{stringinputfiles}\"}}");
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
            Helpers.ConsoleWrapper(alert.Text);
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
        public Int32 ChecarArquivos
        (
            Int64 nota
        )
        {
            if (driver.FindElement(By.Id("titulo")).Text != "Página de Pesquisa")
            {
                driver.FindElement(By.Name("BtnPesquisar")).Click();
            }
            driver.FindElement(By.Name("TxtNF")).Clear();
            driver.FindElement(By.Name("TxtNF")).SendKeys(nota.ToString());
            driver.FindElement(By.Name("BtnPesquisar")).Click();
            Thread.Sleep(500);
            var arvore_principal = driver.FindElements(By.Id("TreeView1t0")).SingleOrDefault();
            if(arvore_principal == null)
            {
                return 0;
            }
            if(arvore_principal.Text == "Nenhum documento encontrado")
            {
                return 0;
            }
            var lista = driver.FindElements(By.Id("TreeView1n2Nodes")).SingleOrDefault();
            if(lista == null)
            {
                return 0;
            }
            return lista.FindElements(By.TagName("table")).Count;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (driver != null)
                {
                    driver.Quit();
                    driver.Dispose();
                    service.Dispose();
                }
            }
        }
    }
}
