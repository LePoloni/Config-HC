using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;  //Incluído manualmente para uso de porta serial
//somente assim é possível saber as portas COM
//disponíveis
using System.Media;

//Link Convertendo byte[] para string e vice-versa
//http://www.codigofonte.net/dicas/csharp/550_convertendo-byte-para-string-e-viceversa
//Link byte[] Array to Hex String
//http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/3928b8cb-3703-4672-8ccd-33718148d1e3/

//namespace WinTeste
namespace Config_HC
{
    public partial class Form1 : Form
    {
        static int qual = 0; //Utilizado no pulling automático

        public Form1()
        {
            InitializeComponent();
            CarregaComPort();
            CarregaBaudRate();
            cbATBaud.SelectedIndex = 3; //9600 bps
            cbATRole.SelectedIndex = 0; //Slave
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                SerialPort1.Close();    //Fecha a conexão
            }
        }

        //Lista as portas seriais disponíveis
        public void CarregaComPort()
        {
            #region "carrega PORTA COM "                    //Apenas define uma região para code foldind
            String[] ListCom = SerialPort.GetPortNames();   //Retorna todas as portas COM disponíveis do computador
            int x = 0;
            for (int i = 0; i < ListCom.Length; i++)        //Popular o ComboBox
            {
                cbPorta.Items.Add(ListCom[i]);
                x = i;
            }
            if (ListCom.Length > 0)
            {
                cbPorta.SelectedIndex = x;
            }
            #endregion
        }
        //Lista as opções de baudrate
        public void CarregaBaudRate()
        {
            Int32[] BaudRateValores ={ 
                                     100,300,600,1200,2400,4800,9600,14400,19200,
                                     38400,56000,57600,115200,128000,256000
                                     };

            for (int i = 0; i < BaudRateValores.Length; i++)
            {
                cbBaudRate.Items.Add(BaudRateValores[i].ToString());
            }
            cbBaudRate.SelectedIndex = 6; //9600bps
        }

        private void bConectar_Click(object sender, EventArgs e)
        {

            try
            {
                if (bConectar.Text == "Conectar")
                {
                    SerialPort1.PortName = cbPorta.Text;
                    SerialPort1.BaudRate = int.Parse(cbBaudRate.Text);
                    SerialPort1.Open(); //Abre uma conexão serial nova

                    if (SerialPort1.IsOpen) //Se a conexão está aberta...
                    {
                        bConectar.Text = "Desconectar";
                        tbEnviar.Clear();
                        tbEnviarH.Clear();
                        bEnviar.Enabled = true;
                        bEnviarH.Enabled = true;
                        cbPorta.Enabled = false;
                        cbBaudRate.Enabled = false;

                        SystemSounds.Exclamation.Play();
                        tbEnviar.Focus();
                    }
                }
                else
                {
                    SerialPort1.Close();
                    bConectar.Text = "Conectar";
                    tbEnviar.Text = "<Conecte antes de enviar>";
                    tbEnviarH.Text = "<Conecte antes de enviar>";
                    bEnviar.Enabled = false;
                    bEnviarH.Enabled = false;
                    cbPorta.Enabled = true;
                    cbBaudRate.Enabled = true;

                    SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception)
            { tbEnviar.Text = "ERRO ao conectar!!!"; }

        }

        private void bEnviar_Click(object sender, EventArgs e)
        {
            //Cria inteiro para controle de bytes extras
            int extra = 0;
            if (rbHC05.Checked) extra += 2;
            //Cria vetores com tamanho proporcional a string a ser enviada
            string EnviarS = tbEnviar.Text;
            byte[] EnviarB = new byte[EnviarS.Length + extra];
            char[] EnviarC = new char[EnviarS.Length];
            //Converte de string -> vetor de chars -> vetor de bytes
            for (int i = 0; i < EnviarS.Length; i++)
            {
                EnviarC[i] = Convert.ToChar(EnviarS.Substring(i, 1));
                EnviarB[i] = Convert.ToByte(EnviarC[i]);
            }
            //Testa a presença de terminadores (Carrier return e Line feed)
            if (rbHC05.Checked)
            {
                EnviarB[EnviarB.Length - 2] = 0x0D;
                EnviarB[EnviarB.Length - 1] = 0x0A;
            }

            //Para mostrar em hexa
            //StringBuilder sb = new StringBuilder(EnviarS.Length * 3);//dois cracteres + um espaço
            //for (int i = 0; i < EnviarS.Length; i++)
            //{
            //    sb.AppendFormat("{0:x2} ", EnviarB[i]);
            //}
            //tbEnviarH.Text = sb.ToString();

            //Envia string pela serial
            //SerialPort1.Write(tbEnviar.Text);
            //Envia pacote de bytes pela serial
            SerialPort1.Write(EnviarB, 0, EnviarB.Length);
            //Limpa a caixa de texto
            if (cbAutoLimpa.Checked)
                tbEnviar.Clear();
            //Chama funções de atualização de estado
            Enviado(EnviarB);
        }

        private void bEnviarH_Click(object sender, EventArgs e)
        {
            //Cria inteiro para controle de bytes extras
            int extra = 0;
            if (rbHC05.Checked) extra += 2;
            //Cria vetores com tamanho proporcional a string a ser enviada
            string EnviarH = tbEnviarH.Text;
            byte[] EnviarB = new byte[EnviarH.Length / 2 + extra];
            string converte;
            //Teste a quantidade de caractres na string hexadecimal
            if (EnviarH.Length % 2 == 0)    //Para: OK
            {
                try
                {
                    //Converte de string hexa -> vetor de bytes
                    for (int i = 0; i < EnviarH.Length; i += 2)
                    {
                        converte = EnviarH.Substring(i, 2);
                        EnviarB[i / 2] = Convert.ToByte(converte, 16);  //Converte string hexadecimal para byte
                    }
                    //Testa a presença de terminadores (Carrier return e Line feed)
                    if (rbHC05.Checked)
                    //if (cbCR.Checked && cbLF.Checked)
                    {
                        EnviarB[EnviarB.Length - 2] = 0x0D;
                        EnviarB[EnviarB.Length - 1] = 0x0A;
                    }
                    //Envia pacote de bytes
                    SerialPort1.Write(EnviarB, 0, EnviarB.Length);
                    //Limpa a caixa de texto
                    if (cbAutoLimpa.Checked)
                        tbEnviarH.Clear();
                    //Chama funções de atualização de estado
                    Enviado(EnviarB);
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message, "ERRO"); }
            }
            else    //Ímpar: ERRO
            { MessageBox.Show("Falta um nibble!", "ERRO"); }
        }

        private void SerialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string Texto = SerialPort1.ReadExisting();    //Lê dados no buffer da serial
            //Esse loop cria uma espécia de timeout de recepção, evita quebra de pacotes.
            //Assim após recebido o primeiro byte pelo comando anterior,
            //passa algum tempo tentando receber outros bytes.
            for (int i = 0; i < 10000; i++)
            {
                if (SerialPort1.BytesToRead > 0)
                    Texto += SerialPort1.ReadExisting();
            }
            ////string Texto = SerialPort1.ReadTo(0x03.ToString());   //Lê dados no buffer da serial até encotrar 0x03
            //string Texto;
            //SerialPort1.ReadTimeout = 500;      //Define o Time-out em 500 ms
            //try
            //{
            //    Texto = SerialPort1.ReadLine(); //Lê dados no butter da serial até receber um NewLine (LineFeed 0x0A) - ajustar timeout
            //    SerialPort1.DiscardInBuffer();  //Descarta dados que sobreram no buffer de recebimento
            //}
            //catch (TimeoutException) //Quando ocorre ReadTimeOut uma exceção é gerada: TimeoutException
            //{
            //    Texto = "";
            //    //MessageBox.Show("TIMEOUT");
            //}

            if (Texto.Length > 0)
            {
                //tbRecebido.Text += Texto;
                //Isso não pode ser feito diretamente por contra do controle 
                //ter sido inicializado em uma thread diferente daquela que 
                //controla o recebimento da porta serial
                //A função abaixo é parte da solução deste problema               
                GravaTexto(Texto);
            }
        }

        delegate void PegaTexto(string text); //Cria um delegate que será um "ponteiro de função"

        private void GravaTexto(string t)
        {
            //Se o controle tbRecebido foi criado em uma thread diferente daquela
            //que está executando este método, uma instância do delegate deve ser criada
            //referenciando o método atual (GravaTexto).
            //Em seguida o método Invoke executa o delegate, que faz com o método SetTexto
            //passe a ser executado na thread que criou o controle tbRecebido, e a 
            //propriedade t passe a ser definida diretamente.
            try
            {
                if (this.tbRecebido.InvokeRequired) //cruzamento de threds 
                {
                    #region
                    //InvokeRequired retorna "true" se o Handle (identificador) do controle (tbRecebido) foi criado em 
                    //uma thread (segmento) diferente da thread que fez a chamada do teste (indicando que você
                    //deve fazer chamadas para o controle através do método invoke (invocar)).

                    //Se o ID da thread principal (interface gráfica) for diferente
                    //de quem está chamando a função, então InvokeRequired retorna TRUE

                    //InvokeRequired compara o "thread ID" da thread chamadora 
                    //com o da thread criadora.
                    //Se eles forem diferentes, ele retorna true.
                    #endregion
                    //Instancia o delegate do tipo PegaTexto
                    PegaTexto d = new PegaTexto(GravaTexto);
                    #region
                    //Em seguida é passado ao delegate o ponteiro da função 
                    //para que posteriormente possamos invocar (Invoke) o método SetTexto
                    //dentro da thread Principal, portando na segunta execução os IDs 
                    //são iguais, fato que permite a atribuição de valores na thread 
                    //principal. 

                    /*Executes the specified delegate, on the thread that owns the control's 
                     * underlying window handle, with the specified list of arguments.
                     * 
                     * Executa o delegate (representante) especificado, na thread que possui o 
                     * identificador do controle subjacente janela (this), com a lista de
                     * argumentos especificada.
                     */
                    #endregion
                    this.Invoke(d, new object[] { t });
                }
                else
                {
                    //Para mostrar em string
                    //this.tbRecebido.Text += t + Environment.NewLine;  //Acrescenta string e linha 
                    this.tbRecebido.AppendText(t + Environment.NewLine);    //Acrescenta string, linha e faz scroll automático

                    //Para mostrar em hexa
                    if (cbHexaEspaço.Checked)
                    {
                        StringBuilder sb = new StringBuilder(t.Length * 3);//dois cracteres + um espaço
                        foreach (byte b in t)
                        {
                            sb.AppendFormat("{0:x2} ", b);
                        }
                        //tbRecebidoH.Text += sb.ToString() + Environment.NewLine;    //Acrescenta string e linha
                        tbRecebidoH.AppendText(sb.ToString() + Environment.NewLine);    //Acrescenta string, linha e faz scroll automático
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(t.Length * 2);//dois cracteres
                        foreach (byte b in t)
                        {
                            sb.AppendFormat("{0:x2}", b);
                        }
                        //tbRecebidoH.Text += sb.ToString() + Environment.NewLine;    //Acrescenta string e linha
                        tbRecebidoH.AppendText(sb.ToString() + Environment.NewLine);    //Acrescenta string, linha e faz scroll automático
                    }
                }
            }
            catch
            { }
        }

        private void bLimpar_Click(object sender, EventArgs e)
        {
            tbEnviar.Clear();
            tbEnviarH.Clear();
        }

        private void bLimpar2_Click(object sender, EventArgs e)
        {
            tbRecebido.Clear();
            tbRecebidoH.Clear();
            tbEnviado.Clear();
            tbEnviadoH.Clear();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                bLimpar_Click(sender, e);
                bLimpar2_Click(sender, e);
            }
        }

        #region BOTÕES AT
        private void bAT_Click(object sender, EventArgs e)
        {
            tbEnviar.Text = ((Button)sender).Text;
            bEnviar.PerformClick();
        }

        private void bATName_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                if (rbHC05.Checked == true)
                {
                    tbEnviar.Text = "AT+NAME=" + tbATName.Text;
                }
                if (rbHC06.Checked == true)
                {
                    tbEnviar.Text = "AT+NAME" + tbATName.Text;  //Não usa o sinal de igual
                }
                bEnviar.PerformClick();
            }
        }

        private void bATPassword_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                tbEnviar.Text = "AT+PSWD=" + tbATPassword.Text;
                bEnviar.PerformClick();
            }
        }

        private void bATUart_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                tbEnviar.Text = "AT+UART=" + tbATUart.Text;
                bEnviar.PerformClick();
            }
        }

        private void bATRole_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                //Lê o modo selecionado e converte para string
                tbEnviar.Text = "AT+ROLE=" + Convert.ToString(cbATRole.SelectedIndex);
                bEnviar.PerformClick();
            }
        }

        private void bATfree_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                tbEnviar.Text = "AT+" + tbATFree.Text;
                bEnviar.PerformClick();
            }
        }

        private void bATBaud_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                //Lê o baudrate selecionado e converte para string hexa
                tbEnviar.Text = "AT+BAUD" + Convert.ToString(cbATBaud.SelectedIndex + 1, 16).ToUpper();
                bEnviar.PerformClick();
            }
        }

        private void bATPin_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                tbEnviar.Text = "AT+PIN" + tbATPin.Text;
                bEnviar.PerformClick();
            }
        }

        private void bATBind_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen) //Se a conexão está aberta...
            {
                tbEnviar.Text = "AT+BIND=" + tbATBind.Text;
                bEnviar.PerformClick();
            }
        }
        #endregion

        public void Enviado(byte[] pac)
        {
            string str; // String que irá receber a conversão
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            //Para mostrar em string
            str = enc.GetString(pac);
            //tbEnviado.Text += str + Environment.NewLine;    //Acrescenta string e linha
            tbEnviado.AppendText(str + Environment.NewLine);    //Acrescenta string, linha e faz scroll automático

            //Para mostrar em hexa
            if (cbHexaEspaço.Checked)
            {
                StringBuilder sb = new StringBuilder(pac.Length * 3);//dois cracteres + um espaço
                foreach (byte b in pac)
                {
                    sb.AppendFormat("{0:x2} ", b);
                }
                //tbEnviadoH.Text += sb.ToString() + Environment.NewLine; //Acrescenta string e linha
                tbEnviadoH.AppendText(sb.ToString() + Environment.NewLine); //Acrescenta string, linha e faz scroll automático
            }
            else
            {
                StringBuilder sb = new StringBuilder(pac.Length * 2);//dois cracteres
                foreach (byte b in pac)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                //tbEnviadoH.Text += sb.ToString() + Environment.NewLine; //Acrescenta string e linha
                tbEnviadoH.AppendText(sb.ToString() + Environment.NewLine); //Acrescenta string, linha e faz scroll automático
            }

            if (cbSom.Checked == true)
            {
                SystemSounds.Asterisk.Play();
            }
        }

        private void bCodigo_Click(object sender, EventArgs e)
        {
            FormCodigo fCodigo = new FormCodigo();
            fCodigo.ShowDialog();
        }

        private void rbHC05_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHC05.Checked == true)
                bATName.Text = "AT+NAME=";
            else
                bATName.Text = "AT+NAME";
        }

        private void manualDeInstruçõesAoUsuáriosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + @"\Documentação\HC_serial_bluetooth.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void datasheetHC05ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + @"\Documentação\HC_serail_module_AT_command.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void datasheetHC06ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + @"\Documentação\hc06 - Comandos AT.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void sobreToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FormSobre fs = new FormSobre();
            fs.ShowDialog();
        }

        private void vídeoDemonstraçãoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Navigate to a URL.
            System.Diagnostics.Process.Start("https://youtu.be/rHhRt6mNMqQ");
        }

        private void conexãoComArduinoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Verifica se já está existe alguma instância criada
            if (Application.OpenForms.OfType<FormArduino>().Count() == 0)
            {
                FormArduino fa = new FormArduino();
                fa.Show();
            }
        }
    }
}
/*
    // Converter o byte[] para String
    byte [] dBytes = ... // seu array de bytes.
    string str; // String que irá receber a conversão
    System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
    str = enc.GetString(dBytes);

    // Converte uma string em um byte[]
    public static byte[] StrToByteArray(string str)
    {
        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
        return encoding.GetBytes(str);
    }
 
    // byte[] Array to Hex String
    Byte[] ba = {0xFF, 0xD0, 0xFF, 0xD1} to "FFD0FFD1" ?
    
    StringBuilder sb = new StringBuilder(ba.Length * 2);
    foreach (byte b in ba)
    {
       sb.AppendFormat("{0:x2}", b)
    }
    return sb.ToString();
*/