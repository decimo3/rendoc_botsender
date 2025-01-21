namespace docbot
{
    public static class Helpers
    {
        public static void ConsoleWrapper(String? mensagem)
        {
            if(mensagem == null) return;
            var logfile = $"{DateTime.Now.ToString("yyyyMMdd")}.log";
            var escrever = $"{DateTime.Now} - {mensagem}\n";
            Console.WriteLine(escrever);
            File.AppendAllText(logfile, escrever);
        }
        public static Dictionary<string, string> ArquivoConfiguracao(String arquivo, Char separador)
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
                parametros.Add(args[0], args[1]);
            }
            return parametros;
        }
        public static String Executor(String aplication, String arguments)
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
                if(process.ExitCode != 0)
                    throw new InvalidOperationException($"Erro ao executar o processo {process.ExitCode}: {erroutput}");
                return stdoutput;
            }
        }

    }
}
