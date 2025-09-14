using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CBLAnki
{
    public partial class CBLForm1 : Form
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private string[] selectedAudioFiles = Array.Empty<string>();

        //static string userName = Environment.UserName;
        //static string ankiExe = @$"C:\Users\{userName}\AppData\Local\Programs\Anki";

        static string ankiExe = (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Anki\anki.exe");

        static bool controle = false;

        public class DeckResponse
        {
            public string[] result { get; set; }
            public object error { get; set; }
        }

        public CBLForm1()
        {
            InitializeComponent();
            txtFrases.AutoScroll = true;

        }

        private void CBLForm1_Load(object sender, EventArgs e)
        {
            getAnkiLocation();
        }

        async void getAnkiLocation()
        {
            if (ankiExe == null)
            {
                MessageBox.Show("Anki não encontrado. Favor instalar com diretório default!");
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = ankiExe,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
                // getAsyncData();
                await WaitForAnkiConnect();
            }
        }

        async Task<bool> WaitForAnkiConnect(int maxRetries = 10, int delayMs = 2000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var request = new { action = "deckNames", version = 6 };
                    string json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://127.0.0.1:8765", content);
                    string result = await response.Content.ReadAsStringAsync();

                    DeckResponse decks = JsonSerializer.Deserialize<DeckResponse>(result);
                    if (decks?.result != null)
                    {
                        // Preenche a combobox
                        guna2ComboBox1.Items.Clear();
                        foreach (var d in decks.result)
                            guna2ComboBox1.Items.Add(d);

                        lblStatus.Text = "Conectado ao Anki!";
                        lblStatus.ForeColor = Color.Lime;
                        return true;
                    }
                }
                catch
                {
                    // ignora, aguarda e tenta de novo
                }

                lblStatus.Text = $"Aguardando AnkiConnect... tentativa {i + 1}/{maxRetries}";
                await Task.Delay(delayMs);
            }

            lblStatus.Text = "Falha: AnkiConnect não respondeu!";
            lblStatus.ForeColor = Color.Red;
            return false;
        }


        private void cblAudios_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Arquivos de áudio (*.mp3)|*.mp3",
                Title = "Selecione os arquivos MP3 ( Ctrl + A para selecionar todos )"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedAudioFiles = ofd.FileNames.OrderBy(f => f).ToArray();
                lstAudios.Items.Clear();
                foreach (var f in selectedAudioFiles)
                    lstAudios.Items.Add(Path.GetFileName(f));

                lblStatus.Text = $"Selecionados: {selectedAudioFiles.Length} arquivos";
            }
        }

        private async void cblSendMultipliesCard_Click(object sender, EventArgs e)
        {
            try
            {
                var linhas = txtFrases.Text
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();

                if (linhas.Length == 0)
                {
                    MessageBox.Show("Cole as frases no campo antes de gerar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (linhas.Length % 2 != 0)
                {
                    MessageBox.Show("Número de linhas inválido! Deve ser sempre par (Inglês + Tradução).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int pares = linhas.Length / 2;

                if (selectedAudioFiles.Length < pares)
                {
                    MessageBox.Show($"Você selecionou {selectedAudioFiles.Length} áudios, mas precisa de {pares}.", "Áudios insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblStatus.Text = "Iniciando envio...";

                int sucesso = 0;
                for (int i = 0; i < pares; i++)
                {
                    string fraseEn = linhas[i * 2];
                    string frasePt = linhas[i * 2 + 1];
                    string audioPath = selectedAudioFiles[i];

                    lblStatus.Text = $"Enviando {i + 1}/{pares}: {Path.GetFileName(audioPath)}";
                    lblStatus.Refresh();

                    try
                    {
                        await AddCardToAnki(fraseEn, frasePt, audioPath);
                        sucesso++;
                    }
                    catch (Exception exCard)
                    {
                        MessageBox.Show($"Erro no cartão {i + 1}: {exCard.Message}");
                    }
                }
                lblStatus.Text = $"Status: Sucesso";
                lblStatus.ForeColor = Color.Lime;
                
            }
            finally
            {
                cblSendMultipliesCard.Enabled = true;
            }
        }

        private async Task AddCardToAnki(string frase, string traducao, string audioFile)
        {
            byte[] audioBytes = await File.ReadAllBytesAsync(audioFile);
            string base64Audio = Convert.ToBase64String(audioBytes);

            var note = new
            {
                deckName = guna2ComboBox1.SelectedItem.ToString(),
                modelName = "Básico (digite a resposta)",
                fields = new { Frente = frase, Verso = traducao },
                options = new { allowDuplicate = false },
                tags = new string[] { "cbl", "dk" },
                audio = new[]
                {
                    new { data = base64Audio, filename = Path.GetFileName(audioFile), fields = new string[] { "Frente" } }
                }
            };

            var request = new { action = "addNote", version = 6, @params = new { note } };
            string json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:8765", content);
            string result = await response.Content.ReadAsStringAsync();

            if (result.Contains("\"error\":"))
                throw new Exception(result);
        }

        private async Task SendNoteToAnki(object note)
        {
            var request = new { action = "addNote", version = 6, @params = new { note } };
            string json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:8765", content);
            string result = await response.Content.ReadAsStringAsync();

            if (result.Contains("\"error\":"))
            {
                throw new Exception(result);
            }
        }

        private void guna2ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TO DO : Incrementar lógica para capturar os decks ao clicar 
            // Ajudará caso o usuário tenha fechado o anki sem que o programa tenha conectado e/ou adicionou/renomeou/deletou algun deck o programa não atualizou.
        }
    }
}
