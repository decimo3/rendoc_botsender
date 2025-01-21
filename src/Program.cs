using OpenQA.Selenium;

namespace docbot
{
    public class Program
    {
        private readonly WebHandler handler;
        public Program(WebHandler _handler)
        {
            this.handler = _handler;
        }
        private void PrepararArquivos
        (
            String diretorio
        )
        {
            var basename = diretorio.Split('\\').Last();
            var argumentos = basename.Split(' ');
            if (argumentos.Length < 3)
            {
                Helpers.ConsoleWrapper($"Diretório {basename} fora do padrão!");
                return;
            }
            if (argumentos.Contains("OK"))
            {
                Helpers.ConsoleWrapper($"Diretório {basename} Já foi enviado!");
                return;
            }
            var arquivos = Directory.GetFiles(diretorio);
            Helpers.ConsoleWrapper($"Lista de arquivos no diretório '{basename}':");
            foreach (var arquivo in arquivos) Helpers.ConsoleWrapper(arquivo);
            var fotos = arquivos.Where(
                f => Path.GetExtension(f) == ".jpeg" || Path.GetExtension(f) == ".jpg"
                ).ToList();
            var videos = arquivos.Where(f => Path.GetExtension(f) == ".mp4").ToList();
            var termos = arquivos.Where(f => Path.GetExtension(f) == ".pdf").ToList();
            if (arquivos.Length != (termos.Count + fotos.Count + videos.Count))
            {
                Helpers.ConsoleWrapper($"Diretório {basename} contém arquivos não suportados!");
                return;
            }
            Helpers.ConsoleWrapper("Enviando os arquivos com a(s) 'evidências da nota de serviço'...");
            while (true)
            {
                fotos = Directory.GetFiles(diretorio).Where(
                    f => (Path.GetExtension(f) == ".jpeg" || Path.GetExtension(f) == ".jpg")
                    && !Path.GetFileNameWithoutExtension(f).Contains(".send")).ToList();
                if (fotos.Any())
                {
                    if (fotos.Count > 5) fotos = fotos.Take(5).ToList();
                    handler.EnviarArquivos(argumentos[0], argumentos[1], "Foto", fotos);
                }
                else
                {
                    break;
                }
            }
            Helpers.ConsoleWrapper("Enviando os arquivos com a(s) 'filmagens da ocorrência de inspeção'...");
            foreach (var video in videos)
            {
                if (Path.GetFileNameWithoutExtension(video).Contains(".send")) continue;
                handler.EnviarArquivos(argumentos[0], argumentos[1], "Vídeo das Inspeções", new List<String> { video });
            }
            Helpers.ConsoleWrapper("Enviando os arquivos com o(s) 'Termo de Ocorrência e Inspeção'...");
            foreach (var termo in termos)
            {
                if (Path.GetFileNameWithoutExtension(termo).Contains(".send")) continue;
                handler.EnviarArquivos(argumentos[0], argumentos[1], "TOI-Termo de Ocorrência e Inspeção", new List<String> { termo });
            }
            // Rename directory to append "OK"
            var newDirName = diretorio + " OK";
            Directory.Move(diretorio, newDirName);
            Helpers.ConsoleWrapper($"Envio dos arquivos da pasta {basename} efetuado com sucesso!");
        }
        public void NavegacaoPastas
        (
            String caminho_atual,
            Int32 profundidade_atual,
            Int32 profundidade_maxima
        )
        {
            if (profundidade_atual > profundidade_maxima) return;
            foreach (var pasta in Directory.GetDirectories(caminho_atual))
            {
                if (profundidade_atual == profundidade_maxima)
                    PrepararArquivos(pasta);
                NavegacaoPastas(pasta, profundidade_atual + 1, profundidade_maxima);
            }
        }

    }
}
