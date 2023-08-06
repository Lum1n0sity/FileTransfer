using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace FileTransfer
{
   public partial class Form1 : Form
    {
        [Serializable]
        public class FileDataInfo
        {
            public string? FileName { get; set; }
            public string? Extension { get; set; }
            public byte[]? Content { get; set; }
        }

        private byte[] SerializeObject(object obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        private T DeserializeObject<T>(byte[] data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                return (T)formatter.Deserialize(memoryStream);
            }
        }

        private ListBox sendBox;
        private ListBox recvBox;
        private List<FileListItem> fileItems = new List<FileListItem>();
        private TextBox ipInput;
        private TextBox portInput;
        private Button sendButton;
        private Button setPort;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1; 

        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#868b8f");

            Label titleLabel = new Label();
            titleLabel.Text = "File Transfer";
            titleLabel.Font = new Font("Arial", 50);
            titleLabel.Location = new Point(200, 0);
            titleLabel.AutoSize = true;
            titleLabel.Anchor = AnchorStyles.Top;
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;

            Label inputLabel = new Label();
            inputLabel.Text = "Insert IP-Address:";
            inputLabel.Font = new Font("Arial", 20);
            inputLabel.Location = new Point(600, 150);
            inputLabel.AutoSize = true;

            ipInput = new TextBox();
            ipInput.BorderStyle = BorderStyle.FixedSingle;
            ipInput.Font = new Font("Arial", 20);
            ipInput.Location = new Point(600, 200);
            ipInput.Width = 300;

            Label portInputLabel = new Label();
            portInputLabel.Text = "Insert Port:";
            portInputLabel.Font = new Font("Arial", 20);
            portInputLabel.Location = new Point(950, 150);
            portInputLabel.AutoSize = true;

            portInput = new TextBox();
            portInput.BorderStyle = BorderStyle.FixedSingle;
            portInput.Font = new Font("Arial", 20);
            portInput.Location = new Point(950, 200);
            portInput.Width = 150;

            sendBox = new ListBox();
            sendBox.AllowDrop = true;
            sendBox.Location = new Point(400, 500);
            sendBox.BorderStyle = BorderStyle.FixedSingle;
            sendBox.DragEnter += sendBox_DragEnter;
            sendBox.DragDrop += sendBox_DragDrop;
            sendBox.KeyDown += sendBox_DeleteFile;
            sendBox.Width = 400;
            sendBox.Height = 400;
            sendBox.SelectionMode = SelectionMode.MultiExtended;

            recvBox = new ListBox();
            recvBox.AllowDrop = false;
            recvBox.BorderStyle = BorderStyle.FixedSingle;
            recvBox.Location = new Point(900, 500);
            recvBox.Width = 400;
            recvBox.Height = 400;
            recvBox.MouseDoubleClick += recvBox_OpenFiles;

            sendButton = new Button();
            sendButton.Text = "Send Files";
            sendButton.Font = new Font("Arial", 18);
            sendButton.BackColor = ColorTranslator.FromHtml("#d9d9d9");
            sendButton.ForeColor = ColorTranslator.FromHtml("#000000");
            sendButton.FlatStyle = FlatStyle.Flat;
            sendButton.Location = new Point(780, 400);
            sendButton.AutoSize = true;
            sendButton.Click += sendButton_Click;

            setPort = new Button();
            setPort.Text = "Set Port";
            setPort.Font = new Font("Arial", 16);
            setPort.BackColor = ColorTranslator.FromHtml("#d9d9d9");
            setPort.ForeColor = ColorTranslator.FromHtml("#000000");
            setPort.FlatStyle = FlatStyle.Flat;
            setPort.Location = new Point(1110, 200);
            setPort.AutoSize = true;
            setPort.Click += SetPort_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(inputLabel);
            this.Controls.Add(ipInput);
            this.Controls.Add(portInputLabel);
            this.Controls.Add(portInput);
            this.Controls.Add(sendBox);
            this.Controls.Add(recvBox);
            this.Controls.Add(sendButton);
            this.Controls.Add(setPort);
        }

        private Icon GetFileIcon(string filePath)
        {
            SHGetFileInfo(filePath, 0, out SHFILEINFO fileInfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_ICON | SHGFI_LARGEICON);
            Icon icon = (Icon)Icon.FromHandle(fileInfo.hIcon).Clone();
            DestroyIcon(fileInfo.hIcon);
            return icon;
        }

        private void sendBox_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void sendBox_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string filePath in files)
            {
                FileListItem fileItem = new FileListItem(filePath, GetFileIcon(filePath));
                sendBox.Items.Add(fileItem);
            }
        }

        private void sendBox_DeleteFile(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                List<FileListItem> selectedItems = sendBox.SelectedItems.Cast<FileListItem>().ToList();
                foreach (FileListItem selectedItem in selectedItems)
                {
                    sendBox.Items.Remove(selectedItem);
                }
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            string ipAddress = ipInput.Text;
            string portText = portInput.Text;

            int port = Int32.Parse(portText);

            MessageBox.Show("Port: " + port + "IpAdress: " + ipAddress);

            foreach (FileListItem fileItem in sendBox.Items)
            {
                string filePath = fileItem.FilePath;
                byte[] fileContent = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);
                string fileExtension = Path.GetExtension(filePath);

                Thread sendThread = new Thread(() => SendData(fileContent, fileName, fileExtension, port, ipAddress));
                sendThread.Start();
            }

            sendBox.Items.Clear();
        }

        private async void SendData(byte[] fileContent, string fileName, string fileExtension, Int32 port, string ips)
        {
            FileDataInfo fileDataInfo = new FileDataInfo
            {
                FileName = fileName,
                Extension = fileExtension,
                Content = fileContent
            };

            byte[] serializedData = SerializeObject(fileDataInfo);

            IPAddress host = IPAddress.Parse(ips);
            IPEndPoint ipEndPoint = new IPEndPoint(host, port);

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await socket.ConnectAsync(ipEndPoint);
                }
                catch (SocketException e)
                {
                    MessageBox.Show("Connection error: " + e.Message);
                    return;
                }

                try
                {
                    await Task.Run(() => socket.Send(serializedData));
                }
                catch (SocketException e)
                {
                    MessageBox.Show("Send error: " + e.Message);
                    return;
                }
            }
        }    

        private void SetPort_Click(object sender, EventArgs e)
        {
            if (int.TryParse(portInput.Text, out int port))
            {
                if (IsPortAvailable(port))
                {
                    Thread receiveThread = new Thread(() => ReceiveData(port));
                    receiveThread.Start();
                }
                else 
                {
                    MessageBox.Show("Port: " + port + " is already in use!");
                }
            }
            else
            {
                MessageBox.Show("Invalid port number.");
            }
        }

        private bool IsPortAvailable(int port)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C netstat -an | findstr" + port;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrEmpty(output);
        }

        public void recvBox_OpenFiles(object sender, MouseEventArgs e)
        {
            var selectedFileItem = recvBox.SelectedItem as string;

            if (selectedFileItem != null)
            {
                Process.Start(selectedFileItem);
            }
        }

        public void recvBox_DeleteFiles(object sender, KeyEventArgs e, string folderPath)
        {
            if (e.KeyCode == Keys.Delete)
            {
                List<FileListItem> selectedItems = recvBox.SelectedItems.Cast<FileListItem>().ToList();
                foreach (FileListItem selectedItem in selectedItems)
                {
                    string fileName = Path.GetFileName(selectedItem.FilePath);
                    string filePath = Path.Combine(folderPath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    recvBox.Items.Remove(selectedItem);
                }
            }
        }

        private void recvBox_KeyDelete(object sender, KeyEventArgs e)
        {
            string folderPath = @"C:\\FileTransfer";
            recvBox_DeleteFiles(sender, e, folderPath);
        }

        public void ReceiveData(int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            MessageBox.Show($"Server started and listening on {IPAddress.Any}:{port}");

            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    try 
                    {
                        using (NetworkStream networkStream = client.GetStream())
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;

                            byte[] receivedData = new byte[0];
                            int totalBytesReceived = 0;

                            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                Array.Resize(ref receivedData, totalBytesReceived + bytesRead);
                                Array.Copy(buffer, 0, receivedData, totalBytesReceived, bytesRead);
                                totalBytesReceived += bytesRead;
                            }

                            FileDataInfo fileDataInfo = DeserializeObject<FileDataInfo>(receivedData);

                            string receivedFilePath = Path.Combine(fileDataInfo.FileName);
                            File.WriteAllBytes(receivedFilePath, fileDataInfo.Content);

                            recvBox.Invoke(new Action(() => recvBox.Items.Add(receivedFilePath)));
                            string downloadsFolder = @"C:\\FileTransfer";
                            File.Copy(downloadsFolder, receivedFilePath);
                        }                     
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error receiving file: " + ex.Message);
                    }
                    finally
                    {
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in receiving data: " + ex.Message);
            }
            finally
            {
                listener.Stop();
            }
        }

        static string GetLocalIPAddress()
        {
            string ipAddress = "";
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wman ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wwanpp ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wwanpp2)
                {
                    IPInterfaceProperties properties = networkInterface.GetIPProperties();

                    foreach (UnicastIPAddressInformation iPAddressInformation in properties.UnicastAddresses)
                    {
                        if (iPAddressInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipAddress = iPAddressInformation.Address.ToString();
                            return ipAddress;
                        }
                    }
                }
            }

            return ipAddress;
        }
    }
}
