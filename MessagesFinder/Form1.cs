using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Linq;

namespace MessagesFinder
{
    public partial class Form1 : Form
    {
        #region Global variables
        Dictionary<string, string> Mensajes = new Dictionary<string, string>();
        Queue Participantes = new Queue();
        Queue LabelsParticipantes = new Queue();
        string RutaCarpeta, owner;
        string mensajesGlobales, añoMasMensajes, mesMasMensajes, diaMasMensajes, palabrasEnvidas, palabrasRecividas;
        bool settingsFound = false;

        string folderDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MessageFinder");
        string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MessageFinder", "Setting.txt");

        ProgressBarForm progress = new ProgressBarForm();
        #endregion

        public Form1()
        {
            InitializeComponent();
            menuStrip1.BackColor = Color.Black;
            menuStrip1.ForeColor = Color.White;

            FirtsStart();
            if (settingsFound)
            {
                MetodoParticipantes();
                setGlobalMetrics();
            }
        }

        #region Settings
        void FirtsStart()
        {
            if (!Directory.Exists(folderDirectory))
            {
                Directory.CreateDirectory(folderDirectory);
                File.Create(settingsFile);

                if (!File.Exists(settingsFile))
                    File.Create(settingsFile);
                else
                    GetSettings();
            }
            else
            {
                if (!File.Exists(settingsFile))
                    File.Create(settingsFile);
                else
                    GetSettings();
            }
        }

        void GetSettings()
        {
            try
            {
                StreamReader reader = new StreamReader(settingsFile);
                string[] settings = reader.ReadToEnd().Split(',');

                RutaCarpeta = settings[0];
                owner = settings[1];

                mensajesGlobales = settings[2];

                añoMasMensajes = settings[3];
                mesMasMensajes = settings[4];
                diaMasMensajes = settings[5];

                palabrasEnvidas = settings[6];
                palabrasRecividas = settings[7];

                settingsFound = true;
            }
            catch
            {
                settingsFound = false;
            }
        }

        void saveSetting()
        {
            StreamWriter writer = new StreamWriter(settingsFile);
            writer.Write(RutaCarpeta + ",");
            writer.Write(owner + ",");
            writer.Write(mensajesGlobales + ",");

            writer.Write(añoMasMensajes + ",");
            writer.Write(mesMasMensajes + ",");
            writer.Write(diaMasMensajes + ",");

            writer.Write(palabrasEnvidas + ",");
            writer.Write(palabrasRecividas + ",");
            writer.Close();
        }

        void getOwner()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(RutaCarpeta);
            Dictionary<string, int> AparicionPorParticipante = new Dictionary<string, int>();

            int c = 1;
            foreach (var Folder in directoryInfo.GetDirectories())
            {
                string RutaArchivo = RutaCarpeta + "\\" + Folder.Name, part;
                DirectoryInfo TemporalFolder = new DirectoryInfo(RutaArchivo);

                // TODO: Encontrar una mejor forma para obtener el titilo sin entrar a varios archivos en caso de que exista mas de uno
                foreach (var file in TemporalFolder.GetFiles())
                {
                    StreamReader jsonStream = File.OpenText(file.FullName);
                    string json = jsonStream.ReadToEnd();
                    Data dataTemp = JsonConvert.DeserializeObject<Data>(json);

                    for (int i = 0; i < dataTemp.participantes.Count; i++)
                    {
                        part = ToUTF8(dataTemp.participantes[i].name);
                        if (AparicionPorParticipante.ContainsKey(part))
                            AparicionPorParticipante[part]++;
                        else
                            AparicionPorParticipante.Add(part, 1);
                    }

                    break; // Se rompe el ciclo porque solo es necesario analizar el primer archivo
                }
                c++;
            }

            owner = AparicionPorParticipante.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        }

        void setGlobalMetrics()
        {
            lblGeneralCantidadMensajes.Text = mensajesGlobales;
            
            lblGeneralAñoMasMensajes.Text = añoMasMensajes;
            lblGeneralMesMasMensajes.Text = mesMasMensajes;
            lblGeneralDiaMasMensajes.Text = diaMasMensajes;

            lblGeneralPalabasEnviadas.Text = palabrasEnvidas;
            lblGeneralPalablasRecividas.Text = palabrasRecividas;
        }
        #endregion

        #region Metricas globales
        void GlobalMetrics()
        {
            progress.Show();
            progress.setMax(3);

            TotalMessages();
            progress.Step();
            
            WordsGlobalPerUser();
            progress.Step();

            GetGlobalMessagesMetrics();
            progress.Step();
            progress.Hide();
        }

        void TotalMessages()
        {
            int cantidadMensajes = 0;

            DirectoryInfo TemporalFolder = new DirectoryInfo(RutaCarpeta);
            foreach (var item in TemporalFolder.GetDirectories())
            {
                cantidadMensajes += CountMessages(item);
            }
            mensajesGlobales = string.Format("{0:###,###,###,###}", cantidadMensajes);
            lblGeneralCantidadMensajes.Text = mensajesGlobales;
        }

        void GetGlobalMessagesMetrics()
        {
            Dictionary<int, int> Years = new Dictionary<int, int>();
            Dictionary<int, int> Months = new Dictionary<int, int>();
            Dictionary<int, int> Days = new Dictionary<int, int>();
            DateTime Fecha;

            DirectoryInfo TemporalFolder = new DirectoryInfo(RutaCarpeta);
            foreach (var item in TemporalFolder.GetDirectories())
            {
                for (int i = 0; i < item.GetFiles().Length; i++)
                {
                    StreamReader jsonStream;
                    Data dataTemp;
                    string file, json;
                    for (int j = 0; j < item.GetFiles().Length; j++)
                    {
                        file = item.GetFiles()[j].FullName;
                        jsonStream = File.OpenText(file);
                        json = jsonStream.ReadToEnd();
                        dataTemp = JsonConvert.DeserializeObject<Data>(json);

                        for (int k = 0; k < dataTemp.messages.Count; k++)
                        {
                            Fecha = ToDateTime(dataTemp.messages[i].date);
                            // Año
                            if (Years.ContainsKey(Fecha.Year))
                                Years[Fecha.Year]++;
                            else
                                Years.Add(Fecha.Year, 1);

                            // Mes
                            if (Months.ContainsKey(Fecha.Month))
                                Months[Fecha.Month]++;
                            else
                                Months.Add(Fecha.Month, 1);

                            // Dia
                            if (Days.ContainsKey(Fecha.Day))
                                Days[Fecha.Day]++;
                            else
                                Days.Add(Fecha.Day, 1);
                        }
                    }
                }
            }

            añoMasMensajes = Years.Aggregate((l, r) => l.Value > r.Value ? l : r).Key.ToString();
            mesMasMensajes = ToMonth(Months.Aggregate((l, r) => l.Value > r.Value ? l : r).Key);
            diaMasMensajes = Days.Aggregate((l, r) => l.Value > r.Value ? l : r).Key.ToString();

            lblGeneralAñoMasMensajes.Text = añoMasMensajes;
            lblGeneralMesMasMensajes.Text = mesMasMensajes;
            lblGeneralDiaMasMensajes.Text = diaMasMensajes;
        }

        void WordsGlobalPerUser()
        {
            int[] cantidadPalabras = new int[2];
            int[] cantidadTemp;

            DirectoryInfo TemporalFolder = new DirectoryInfo(RutaCarpeta);
            foreach (var item in TemporalFolder.GetDirectories())
            {
                for (int i = 0; i < item.GetFiles().Length; i++)
                {
                    StreamReader jsonStream;
                    Data dataTemp;
                    string file, json;
                    for (int j = 0; j < item.GetFiles().Length; j++)
                    {
                        file = item.GetFiles()[j].FullName;
                        jsonStream = File.OpenText(file);
                        json = jsonStream.ReadToEnd();
                        dataTemp = JsonConvert.DeserializeObject<Data>(json);
                        cantidadTemp = GetWordsPerUser(dataTemp.messages);
                        cantidadPalabras[0] += cantidadTemp[0];
                        cantidadPalabras[1] += cantidadTemp[1];
                    }
                }
            }

            palabrasEnvidas = string.Format("{0:###,###,###,###}", cantidadPalabras[0]);
            palabrasRecividas = string.Format("{0:###,###,###,###}", cantidadPalabras[1]);

            lblGeneralPalabasEnviadas.Text = palabrasEnvidas;
            lblGeneralPalablasRecividas.Text = palabrasRecividas;
        }
        #endregion

        #region Main core
        // Boton para seleccionar la carpeta donde se encuentran los mensajes/archivos JSON
        private void agregarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (!settingsFound)
                {
                    RutaCarpeta = folderBrowserDialog.SelectedPath;
                    getOwner();
                    GlobalMetrics();
                    saveSetting();
                }
                MetodoParticipantes();
            }
        }

        void MetodoParticipantes()
        {
            Participantes = GetParticipantes();

            SetParticipantesInPanel(Participantes);
        }

        // TODO: Obtener solo los participantes y no los mensajes
        // Metodo para obtener los participantes de las conversaciones
        Queue GetParticipantes()
        {
            Queue participantes = new Queue();

            DirectoryInfo directoryInfo = new DirectoryInfo(RutaCarpeta);

            int c = 1;
            foreach (var Folder in directoryInfo.GetDirectories())
            {
                string RutaArchivo = RutaCarpeta + "\\" + Folder.Name;
                DirectoryInfo TemporalFolder = new DirectoryInfo(RutaArchivo);

                // TODO: Encontrar una mejor forma para obtener el titilo sin entrar a varios archivos en caso de que exista mas de uno
                foreach (var file in TemporalFolder.GetFiles())
                {
                    StreamReader jsonStream = File.OpenText(file.FullName);

                    string json = jsonStream.ReadToEnd();

                    Data dataTemp = JsonConvert.DeserializeObject<Data>(json);

                    if (dataTemp.title == "")
                        dataTemp.title = "Desconocido";
                    string str = c + ". " + ToUTF8(dataTemp.title);

                    participantes.Enqueue(str);

                    Mensajes.Add(str, RutaArchivo);
                    break; // Se rompe el ciclo porque solo es necesario analizar el primer archivo
                }
                c++;
            }

            return participantes;
        }

        // Metodo para agregar los participantes al panel de las personas con las que se ha hablado
        void SetParticipantesInPanel(Queue participantes)
        {
            int y = 2, total = participantes.Count;
            Queue temp = (Queue)participantes.Clone();

            for (int i = 0; i < total; i++)
            {
                Label newLabel = new Label();
                newLabel.Parent = pnlSenders;
                newLabel.Text = temp.Dequeue().ToString();
                newLabel.Location = new Point(0, y);
                newLabel.AutoSize = true;

                newLabel.MouseHover += new EventHandler(LabelMouseHover);
                newLabel.MouseLeave += new EventHandler(LabelMouseLeave);
                newLabel.Click += new EventHandler(GetMessages);

                y = y + newLabel.Size.Height + 2;
                LabelsParticipantes.Enqueue(newLabel);
            }
        }
        #endregion

        #region Mensajes metricas
        void GetMetrics()
        {
            // Obtiene las fechas de la conversacion y hace los calculos necesarios para obtener
            // el año, mes y dia con mas mensajes enviados, ademas de las palabras enviadas y recividas
            DateTime day1 = ToDateTime(messages[messages.Count - 1].date), day2 = ToDateTime(messages[0].date);
            lblPrimerMensaje.Text = day1.ToString("dd/MM/yyyy");
            lblUltimoMensaje.Text = day2.ToString("dd/MM/yyyy");
            int Dias = TotalDays(day1, day2);
            lblTotalDays.Text = Dias + " dias";

            string[] MessagesPerDate = GetMessagesMetrics();
            lblActualAñoMasMensajes.Text = MessagesPerDate[0];
            lblActualMesMasMensajes.Text = MessagesPerDate[1];
            lblActualDiaMasMensajes.Text = MessagesPerDate[2];

            int[] NumberOfWords = GetWordsPerUser(messages);
            lblActualPalabasEnviadadas.Text = string.Format("{0:###,###,###,###}", NumberOfWords[0]);
            lblActualPalablasRecividas.Text = string.Format("{0:###,###,###,###}", NumberOfWords[1]);
        }

        // TODO: Crear graficos con los datos
        string[] GetMessagesMetrics()
        {
            // Obtiene el año, mes y dia con mas mensajes enviados
            Dictionary<int, int> Years = new Dictionary<int, int>();
            Dictionary<int, int> Months = new Dictionary<int, int>();
            Dictionary<int, int> Days = new Dictionary<int, int>();
            DateTime Fecha;

            for (int i = 0; i < messages.Count; i++)
            {
                Fecha = ToDateTime(messages[i].date);
                // Año
                if (Years.ContainsKey(Fecha.Year))
                    Years[Fecha.Year]++;
                else
                    Years.Add(Fecha.Year, 1);

                // Mes
                if (Months.ContainsKey(Fecha.Month))
                    Months[Fecha.Month]++;
                else
                    Months.Add(Fecha.Month, 1);

                // Dia
                if (Days.ContainsKey(Fecha.Day))
                    Days[Fecha.Day]++;
                else
                    Days.Add(Fecha.Day, 1);
            }

            int Year = Years.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            int Month = Months.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            int Day = Days.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

            string[] temp = new string[3] { Year.ToString(), ToMonth(Month), Day.ToString() };

            return temp;
        }

        int[] GetWordsPerUser(List<Messages> messagesTemp)
        {
            // Obtiene las cantidad de palabras enviadas y recividas
            Dictionary<string, int> Words = new Dictionary<string, int>();
            string sender;
            int messagesLength;
            Words.Add(owner, 0);
            Words.Add("Otro", 0);

            for (int i = 0; i < messagesTemp.Count; i++)
            {
                sender = ToUTF8(messagesTemp[i].sender);

                if (messagesTemp[i].content != null)
                {
                    messagesLength = messagesTemp[i].content.Split(' ').Length;

                    if (sender == owner)
                        Words[sender] += messagesLength;
                    else
                        Words["Otro"] += messagesLength;
                }
                else
                {
                    if (sender == owner)
                        Words[sender]++;
                    else
                        Words["Otro"]++;
                }
            }

            int[] temp = new int[2] { Words[owner], Words["Otro"] };

            return temp;
        }
        #endregion

        #region Mensajes por participante
        List<Messages> messages = new List<Messages>();
        private void GetMessages(object sender, EventArgs e)
        {
            // Obtiene los mensajes de la conversacion seleccionada y los almacena en una lista
            // global para que los demas metodos los utilicen para sus determinados procesos
            // e imprime los mensajes en un panel
            messages.Clear();
            pnlMessages.Controls.Clear();
            Label label = sender as Label;
            int cantidadMensajes = 0;

            string value = "";
            Mensajes.TryGetValue(label.Text, out value);

            DirectoryInfo TemporalFolder = new DirectoryInfo(value);
            // Si hay mas de un archivo, se toma un camino diferente
            Data dataTemp = new Data();
            foreach (var item in TemporalFolder.GetFiles())
            {
                int CantidadFiles = TemporalFolder.GetFiles().Length;
                StreamReader jsonStream = File.OpenText(item.FullName);
                string json = jsonStream.ReadToEnd();
                dataTemp = JsonConvert.DeserializeObject<Data>(json);
                messages.AddRange(dataTemp.messages);
                cantidadMensajes += dataTemp.messages.Count;
            }

            lblActualCantidadMensajes.Text = string.Format("{0:###,###,###,###}", cantidadMensajes);
            lblConversationtitle.Text = ToUTF8(dataTemp.title);

            GetMetrics();

            // Coloca los mensajes correspondientes en el panel de mensajes
            if (messages.Count < 800)
                setMensajes(dataTemp.messages);
            else
                setMensajes(messages, 1);

            lblConversationtitle.Location = new Point(Centrar(lblConversationtitle.Size.Width, pnlTitle.Size.Width), lblConversationtitle.Location.Y);
        }

        void setMensajes(List<Messages> messages)
        {
            progress.Show();
            progress.setMax(messages.Count);
            // Imprime los mensajes con una estructura en el panel de mensajes
            int y = 20, pnlwidth = pnlMessages.Width / 2;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].content == null)
                    messages[i].content = "**En progreso**";
                if (owner == ToUTF8(messages[i].sender))
                {
                    Label content = new Label();
                    content.Parent = pnlMessages;
                    content.Text = ToUTF8(messages[i].content);
                    content.Location = new Point(pnlwidth, y);
                    content.MaximumSize = new Size(pnlwidth - 30, 0);
                    content.AutoSize = true;
                    y = y + content.Size.Height + 5;
                }
                else
                {
                    Label senderMessage = new Label();
                    senderMessage.Parent = pnlMessages;
                    senderMessage.Text = ToUTF8(messages[i].content);
                    senderMessage.Location = new Point(0, y);
                    senderMessage.MaximumSize = new Size(pnlwidth - 30, 0);
                    senderMessage.AutoSize = true;
                    y = y + senderMessage.Size.Height + 5;
                }
                progress.Step();
            }
            progress.Hide();
        }

        // TODO: Pasar de pagina los mensajes
        void setMensajes(List<Messages> messages, int pagina)
        {
            progress.Show();
            // Imprime una parte de los mensajes con una estructura en el panel de mensajes
            int y = 20, limite = 0, TamanoPagina = 800, pnlwidth = pnlMessages.Width / 2;
            if (messages.Count - TamanoPagina * pagina > 0)
                limite = messages.Count - TamanoPagina * pagina;

            progress.setMax(TamanoPagina);
            for (int i = messages.Count - 1 - ((pagina - 1) * TamanoPagina); i > limite; i--)
            {
                if (messages[i].content == null)
                    messages[i].content = "En progreso";
                if (owner == ToUTF8(messages[i].sender))
                {
                    Label content = new Label();
                    content.Parent = pnlMessages;
                    content.Text = ToUTF8(messages[i].content);
                    content.Location = new Point(pnlMessages.Width / 2, y);
                    content.MaximumSize = new Size(pnlwidth - 30, 0);
                    content.AutoSize = true;
                    y = y + content.Size.Height + 5;
                }
                else
                {
                    Label senderMessage = new Label();
                    senderMessage.Parent = pnlMessages;
                    senderMessage.Text = ToUTF8(messages[i].content);
                    senderMessage.Location = new Point(0, y);
                    senderMessage.MaximumSize = new Size(pnlwidth - 30, 0);
                    senderMessage.AutoSize = true;
                    y = y + senderMessage.Size.Height + 5;
                }
                progress.Step();
            }

            Button button = new Button();
            button.Text = "Cargar mas";
            button.Location = new Point(Centrar(button.Size.Width, pnlMessages.Width), y + 25);
            pnlMessages.Controls.Add(button);
            progress.Hide();
        }

        // TODO: Mejorar proceso de busqueda
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Busca entre las conversaciones a la indicada en el textbox
            Queue tempParticipantes = new Queue();
            pnlSenders.Controls.Clear();
            TextBox textBox = sender as TextBox;
            if (!(textBox.Text == ""))
            {
                 foreach (string item in Participantes)
                {
                    string tempItem = item.ToLower();
                    string temptxt = textBox.Text.ToLower();
                    if (tempItem.Contains(temptxt))
                        tempParticipantes.Enqueue(item);
                }
                SetParticipantesInPanel(tempParticipantes);
            }
            else
                SetParticipantesInPanel(Participantes);
        }
        #endregion

        #region Eventos y metodos varios
        // Mover la ventana
        public int xClick = 0, yClick = 0;
        private void menuStrip1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                xClick = e.X; yClick = e.Y;
            }
            else
            {
                this.Left = this.Left + (e.X - xClick); this.Top = this.Top + (e.Y - yClick);
            }
        }

        // Cierra la ventana
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region Eventos MouseHover en labels de participantes
        private void LabelMouseHover(object sender, EventArgs e)
        {
            Label label = sender as Label;
            label.Font = new Font(label.Font.Name, label.Font.Size, FontStyle.Bold);
        }

        private void LabelMouseLeave(object sender, EventArgs e)
        {
            Label label = sender as Label;
            label.Font = new Font(label.Font.Name, label.Font.Size, FontStyle.Regular);
        }
        #endregion

        // Centra un control con respecto al padre
        int Centrar(int size, int parent)
        {
            return (parent / 2) - (size / 2);
        }

        // Convertir de double-decode a UTF-8
        string ToUTF8(string str)
        {
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        DateTime ToDateTime(double seconds)
        {
            DateTime t = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            t = t.AddMilliseconds(seconds);
            t = t.ToLocalTime();

            return t;
        }

        int TotalDays(DateTime day1, DateTime day2)
        {
            return Convert.ToInt16(Math.Round(day2.Subtract(day1).TotalDays));
        }

        string ToMonth(int MonthNumber)
        {
            string MonthString;
            switch (MonthNumber)
            {
                case 1:
                    MonthString = "Enero";
                    break;
                case 2:
                    MonthString = "Febrero";
                    break;
                case 3:
                    MonthString = "Marzo";
                    break;
                case 4:
                    MonthString = "Abril";
                    break;
                case 5:
                    MonthString = "Mayo";
                    break;
                case 6:
                    MonthString = "Junio";
                    break;
                case 7:
                    MonthString = "Julio";
                    break;
                case 8:
                    MonthString = "Agosto";
                    break;
                case 9:
                    MonthString = "Septiembre";
                    break;
                case 10:
                    MonthString = "Octubre";
                    break;
                case 11:
                    MonthString = "Noviembre";
                    break;
                case 12:
                    MonthString = "Diciembre";
                    break;
                default:
                    MonthString = "Error";
                    break;
            }

            return MonthString;
        }

        int CountMessages(DirectoryInfo directory)
        {
            int cantidadMensajes = 0;

            StreamReader jsonStream;
            Data dataTemp;
            string file, json;
            for (int i = 0; i < directory.GetFiles().Length; i++)
            {
                file = directory.GetFiles()[i].FullName;
                jsonStream = File.OpenText(file);
                json = jsonStream.ReadToEnd();
                dataTemp = JsonConvert.DeserializeObject<Data>(json);
                cantidadMensajes += dataTemp.messages.Count;
            }

            return cantidadMensajes;
        }
        #endregion
    }

    #region Estructura para la obtencion de datos en los archivos .JSON
    public class Data
    {
        [JsonProperty("participants")]
        public List<Participantes> participantes { get; set; }

        [JsonProperty("messages")]
        public List<Messages> messages { get; set; }

        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("is_still_participant")]
        public bool stillParticipant { get; set; }
    }

    public class Participantes
    {
        [JsonProperty("name")]
        public string name { get; set; }
    }

    public class Messages
    {
        [JsonProperty("sender_name")]
        public string sender { get; set; }

        [JsonProperty("timestamp_ms")]
        public double date { get; set; }

        [JsonProperty("content")]
        public string content { get; set; }
    }
    #endregion
}

//public string[] messages { get; set; }
//public string title { get; set; }
//public bool isStillParticipant { get; set; }