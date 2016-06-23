using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security.Permissions;

namespace BluetoothTestApp
{
    public partial class Form1 : Form
    {
        BluetoothClient bluetoothClient;
        BluetoothDeviceInfo[] deviceList;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (BluetoothRadio.IsSupported)
                {
                    bluetoothClient = new BluetoothClient();

                    if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.PowerOff)
                        BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
                }
                else
                {
                    MessageBox.Show("Adaptar is not detected");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                var sbdd = new SelectBluetoothDeviceDialog();
                sbdd.ShowAuthenticated = true;
                sbdd.ShowRemembered = true;
                sbdd.ShowUnknown = true;

                BluetoothDeviceInfo deviceInfo = null;

                OpenFileDialog openFileDialog = new OpenFileDialog();

                if (sbdd.ShowDialog() == DialogResult.OK)
                {
                    deviceInfo = sbdd.SelectedDevice;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var file = openFileDialog.FileName;
                        var uri = new Uri("obex://" + deviceInfo.DeviceAddress + "/" + file);
                        var request = new ObexWebRequest(uri);
                        request.ReadFile(file);
                        var response = (ObexWebResponse)request.GetResponse();
                        MessageBox.Show(response.StatusCode.ToString());
                        response.Close();
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnReceive_Click(object sender, EventArgs e)
        {
            try
            {
                ObexListener listener = new ObexListener();
                listener.Start();

                ObexListenerContext context = null;
                frmWaitingConnection waitingConnectionForm = new frmWaitingConnection();
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    context = listener.GetContext();
                    if (waitingConnectionForm.Visible)
                        waitingConnectionForm.Invoke(new MethodInvoker(delegate { waitingConnectionForm.Close(); }));
                }).Start();

                if (waitingConnectionForm.ShowDialog() == DialogResult.Cancel)
                {
                    if (listener.IsListening)
                        listener.Stop();
                }

                if (context != null)
                {
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        string fileLocation = folderBrowserDialog.SelectedPath + context.Request.RawUrl;
                        context.Request.WriteFile(fileLocation);
                        MessageBox.Show("File saved at: \n" + fileLocation);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                deviceList = bluetoothClient.DiscoverDevices();

                listBox1.DataSource = deviceList;
                listBox1.DisplayMember = "DeviceName";
                listBox1.ValueMember = "DeviceAddress";          
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                EventHandler<BluetoothWin32AuthenticationEventArgs> handler = new EventHandler<BluetoothWin32AuthenticationEventArgs>(HandleRequests);
                BluetoothWin32Authentication auth = new BluetoothWin32Authentication(handler);

                if (BluetoothSecurity.PairRequest((BluetoothAddress)listBox1.SelectedValue, null))
                {
                    MessageBox.Show("Connected");
                }
                else
                {
                    MessageBox.Show("Unable to connect");
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void HandleRequests(object that, BluetoothWin32AuthenticationEventArgs e)
        {
            e.Confirm = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

    }
}
