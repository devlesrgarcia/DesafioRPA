using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Npgsql;
using System.Diagnostics;

namespace WordPress
{
    public class AutomacaoSelenium
    {
        public IWebDriver driver; // driver do selenium
        private Stopwatch cronometro; // cronometro 

        public AutomacaoSelenium()
        {
            // Configurações do ChromeDriver
            ChromeOptions options = new ChromeOptions();
            //options.AddArgument("--headless"); // modo backend (funciona 50% das vezes, buga por causa dos anuncios)
            driver = new ChromeDriver(options); 
            cronometro = new Stopwatch(); 
        }

        // funçãop principal que executa a automação
        public void Executar()
        {
            Actions actions = new Actions(driver); // inicia o action para apertar espaço
            driver.Manage().Window.Maximize(); // maximiza a janela do chrome
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            driver.Navigate().GoToUrl("https://10fastfingers.com/typing-test/portuguese"); // vai para o site

            cronometro.Start(); // inicia o cronômetro 

            // Tentativa de aceitar os cookies
            try
            {                
                IWebElement botaoCookies = driver.FindElement(By.CssSelector("#CybotCookiebotDialogBodyLevelButtonLevelOptinAllowAll"));
                botaoCookies.Click(); // clica no botão aceitar os cookies
                Console.WriteLine("Cookie aceito com sucesso!");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Erro ao aceitar cookies. Exceção: {ex}");
            }

            // pega todas as palavras na div
            IWebElement divPalavras = driver.FindElement(By.Id("row1"));
            IReadOnlyCollection<IWebElement> palavras = divPalavras.FindElements(By.TagName("span")); 

            foreach (var palavra in palavras)
            {
                string palavraTexto = palavra.Text; // pega o texto da palavra que estava no span
                IWebElement campoEntrada = driver.FindElement(By.XPath("//*[@id=\"inputfield\"]"));// clica no campo de texto
                campoEntrada.SendKeys(palavraTexto); // coloca a palavra no campo de texto
                actions.SendKeys(Keys.Space).Build().Perform(); // aperta espaço
                Thread.Sleep(500); // aguarda meio segundo

                // Verifica se o tempo de execução esgotou (65 segundos)
                if (TempoEsgotado())
                {
                    Console.WriteLine("Tempo esgotado");
                    break;
                }
            }

            Salvartudo(); // salva os dados
        }

        // funçao que verifica se passou 65 segundos
        private bool TempoEsgotado()
        {
            return cronometro.Elapsed.TotalSeconds > 65; // da true se o tempo passou 65 segundos
        }

        // função que salva os dados no bd
        private void Salvartudo()
        {
            try
            {
                // pega os dados da página por id
                var WPMString = driver.FindElement(By.XPath("//*[@id=\"wpm\"]/strong")).Text.Replace(" WPM", "");
                int WPM = int.Parse(WPMString);
                var KSString = driver.FindElement(By.CssSelector("td.value small span.correct")).Text;
                int KS = int.Parse(KSString);
                var OCC = driver.FindElement(By.XPath("//*[@id=\"accuracy\"]/td[2]/strong"));
                var CWString = driver.FindElement(By.XPath("//*[@id=\"correct\"]/td[2]/strong")).Text;
                int CW = int.Parse(CWString);
                var WWString = driver.FindElement(By.XPath("//*[@id=\"wrong\"]/td[2]/strong")).Text;
                int WW = int.Parse(WWString);

                // mostra os dados no console para controle se estiver no backend
                Console.WriteLine($"Número de WPM: {WPM}");
                Console.WriteLine($"Número de KeyStrokes: {KS}");
                Console.WriteLine($"% de OCCURACY: {OCC.Text}");
                Console.WriteLine($"Número de acertos: {CW}");
                Console.WriteLine($"Número de erros: {WW}");

                // conectando no bd
                using (var connection = new NpgsqlConnection("Server=localhost;Database=WordPress;User Id=postgres;Password=5110;"))
                {
                    connection.Open(); 
                    //print para controle no backend
                    Console.WriteLine("Abriu conexao");

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = "INSERT INTO dadosbot (WPM, OCC, KS, CW, WW) VALUES (@WPM, @OCC,@KS, @CW, @WW)";
                        cmd.Parameters.AddWithValue("@WPM", WPM);
                        cmd.Parameters.AddWithValue("@OCC", OCC.Text);
                        cmd.Parameters.AddWithValue("@KS", KS);
                        cmd.Parameters.AddWithValue("@CW", CW);
                        cmd.Parameters.AddWithValue("@WW", WW);

                        cmd.ExecuteNonQuery(); // executa o insert no bd
                        Console.WriteLine("Registro de bot incluido com sucesso!");
                        driver.Quit(); // Fecha o navegador
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Não foi possivel salvar. Erro: {ex}");
                return;
            }
            finally
            {
                cronometro.Stop(); // para o cronômetro
            }
        }
    }
}
