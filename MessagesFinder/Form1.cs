using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace MessagesFinder
{
    public partial class Form1 : Form
    {
        Dictionary<string, string> Mensajes = new Dictionary<string, string>();
        Queue Participantes = new Queue();
        Queue LabelsParticipantes = new Queue();

        public Form1()
        {
            InitializeComponent();
            menuStrip1.BackColor = Color.Black;
            menuStrip1.ForeColor = Color.White;
        }

        // Boton para seleccionar la carpeta donde se encuentran los mensajes/archivos JSON
        private void agregarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string RutaCarpeta = string.Empty;
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            Queue partcipantes = new Queue();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                //RutaCarpeta = folderBrowserDialog.SelectedPath;
                RutaCarpeta = "C:\\Users\\Emmanuel\\Desktop\\Proyectos\\MessageFinder\\facebook-emmanuelfriasavina-JSON\\messages\\inbox";
                Queue Folders = new Queue();
                Participantes = GetParticipantes(RutaCarpeta);

                SetParticipantesInPanel(Participantes);
            }
        }

        // TODO: Obtener solo los participantes y no los mensajes
        // Metodo para obtener los participantes de las conversaciones
        Queue GetParticipantes(string RutaCarpeta)
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
                    break;
                }
                c++;
            }

            return participantes;
        }

        // Metodo para agregar los participantes al panel de las personas con las que se ha hablado
        void SetParticipantesInPanel(Queue participantes)
        {
            int y = 2;
            Queue temp = participantes;
            for (int i = 0; i < participantes.Count; i++)
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

        List<Messages> messages = new List<Messages>();
        private void GetMessages(object sender, EventArgs e)
        {
            Stopwatch sp = new Stopwatch();
            sp.Start();
            pnlMessages.Controls.Clear();
            Label label = sender as Label;
            int cantidadMensajes = 0;

            string value = "";
            Mensajes.TryGetValue(label.Text, out value);

            DirectoryInfo TemporalFolder = new DirectoryInfo(value);
            // Si hay mas de un archivo, se toma un camino diferente
            if (!(TemporalFolder.GetFiles().Length > 1))
            {
                StreamReader jsonStream = File.OpenText(TemporalFolder.GetFiles()[0].FullName);
                string json = jsonStream.ReadToEnd();
                Data dataTemp = JsonConvert.DeserializeObject<Data>(json);

                lblConversationtitle.Text = ToUTF8(dataTemp.title);
                cantidadMensajes += dataTemp.messages.Count;

                setMensajes(dataTemp.messages);
            }
            else
            {
                Data dataTemp = new Data();
                foreach (var item in TemporalFolder.GetFiles())
                {
                    int CantidadFiles = TemporalFolder.GetFiles().Length;
                    StreamReader jsonStream = File.OpenText(item.FullName);
                    string json = jsonStream.ReadToEnd();
                    dataTemp = JsonConvert.DeserializeObject<Data>(json);
                    messages.AddRange(dataTemp.messages);
                }

                lblConversationtitle.Text = ToUTF8(dataTemp.title);
                cantidadMensajes += messages.Count;

                setMensajes(messages, 1);
            }

            lblConversationtitle.Location = new Point(Centrar(lblConversationtitle.Size.Width, pnlTitle.Size.Width), lblConversationtitle.Location.Y);
            lblCantidadMensajes.Text = cantidadMensajes.ToString();

            sp.Stop();
            MessageBox.Show(sp.Elapsed.ToString());
        }

        void setMensajes(List<Messages> messages)
        {
            string senderValue = messages[0].sender;
            int y = 20, pnlwidth = pnlMessages.Width / 2;
            for (int i = messages.Count - 1; i > 0; i--)
            {
                if (messages[i].content == null)
                    messages[i].content = "**En progreso**";
                if (senderValue == messages[i].sender)
                {
                    Label senderMessage = new Label();
                    senderMessage.Parent = pnlMessages;
                    senderMessage.Text = ToUTF8(messages[i].content);
                    senderMessage.Location = new Point(0, y);
                    senderMessage.MaximumSize = new Size(pnlwidth - 30, 0);
                    senderMessage.AutoSize = true;
                    y = y + senderMessage.Size.Height + 5;
                }
                else
                {
                    Label content = new Label();
                    content.Parent = pnlMessages;
                    content.Text = ToUTF8(messages[i].content);
                    content.Location = new Point(pnlwidth, y);
                    content.MaximumSize = new Size(pnlwidth - 30, 0);
                    content.AutoSize = true;
                    y = y + content.Size.Height + 5;
                }
            }
        }

        void setMensajes(List<Messages> messages, int pagina)
        {
            string senderValue = messages[0].sender;
            int y = 20, limite = 0, TamanoPagina = 400;
            if (messages.Count - TamanoPagina * pagina > 0)
                limite = messages.Count - TamanoPagina * pagina;

            for (int i = messages.Count - 1 - ((pagina - 1) * TamanoPagina); i > limite; i--)
            {
                if (messages[i].content == null)
                    messages[i].content = "En progreso";
                if (senderValue == messages[i].sender)
                {
                    Label senderMessage = new Label();
                    senderMessage.Parent = pnlMessages;
                    senderMessage.Text = ToUTF8(messages[i].content);
                    senderMessage.Location = new Point(0, y);
                    senderMessage.AutoSize = true;
                    y = y + senderMessage.Size.Height + 5;
                }
                else
                {
                    Label content = new Label();
                    content.Parent = pnlMessages;
                    content.Text = ToUTF8(messages[i].content);
                    content.Location = new Point(pnlMessages.Width / 2, y);
                    content.AutoSize = true;
                    y = y + content.Size.Height + 5;
                }
            }

            Button button = new Button();
            button.Text = "Cargar mas";
            button.Location = new Point(Centrar(button.Size.Width, pnlMessages.Width), y + 25);
            pnlMessages.Controls.Add(button);
        }

        Queue tempParticipantes = new Queue();
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
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

        // Convertir de double-decode a UTF-8
        string ToUTF8(string str)
        {
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        #region Eventos varios
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

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Close();
        }

        int Centrar(int size, int parent)
        {
            return (parent / 2) - (size / 2);
        }
        #endregion
    }

    // Estructura para la obtencion de datos en los archivos .JSON
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
        public Int64 date { get; set; }

        [JsonProperty("content")]
        public string content { get; set; }
    }
}

//public string[] messages { get; set; }
//public string title { get; set; }
//public bool isStillParticipant { get; set; }