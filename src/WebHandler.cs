using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
namespace docbot
{
    public class WebHandler : IDisposable
    {
        private ChromeDriver? driver;
        public WebHandler
        (
            String chromepath,
            String driverpath,
            String data_folder,
            String website,
            String baseurl
        )
        {
            var service = ChromeDriverService.CreateDefaultService(driverpath);
            var options = new ChromeOptions();
            options.BinaryLocation = chromepath;
            options.AddArgument($"--user-data-dir={data_folder}");
            options.AddArgument($"--app={website}");
            options.AddArgument($"--unsafely-treat-insecure-origin-as-secure={baseurl}");
            this.driver = new ChromeDriver(service, options);
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
                    driver = null;
                }
            }
        }
    }
}
