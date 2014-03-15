﻿using System;
using System.Drawing;
using System.Threading;
using System.Text;
using System.Net.Sockets;

using System.Windows.Forms;
using smgiFuncs;

namespace osu_Twitch_Relay
{
    public partial class mainFrm : Form
    {
        readonly Settings settings = new Settings();
        readonly ToolTip infoTT = new ToolTip();
        static readonly LogForm log = new LogForm();

        static readonly Socket connSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        readonly byte[] buffer = new byte[50];

        private void Button1_Click(object sender, EventArgs e)
        {
            log.Write("Settings saved.");
            settings.AddSetting("oName", oNameTB.Text);
            settings.AddSetting("tName", tNameTB.Text);
            settings.AddSetting("tOAuth", tOAuthTB.Text);
            settings.Save();

            log.Write("Attempting to connect to server...");

            try
            {
                connSock.Connect(GlobalVars.server_IP, GlobalVars.server_Port);
                log.Write("Successfully connected to server.", 1);
            }
            catch
            {
                log.Write("Failed to connect to server.", 3);
            }
            if (connSock.Connected)
            {
                log.Write("Attempting to authenticate...");
                connSock.Send(Encoding.ASCII.GetBytes(oNameTB.Text + "," + tNameTB.Text + "," + tOAuthTB.Text + "," + GlobalVars.privKey + ","));
                connSock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, connRead, buffer);
            }
        }
        private static void connRead(IAsyncResult result)
        {
            byte[] receivedBytes = (byte[])result.AsyncState;
            int readlength = 0;
            try
            {
                readlength = connSock.EndReceive(result);
            }
            catch
            {
                log.Write("The server was disconnected.", 3);
            }
            if (readlength > 0)
            {
                Signals receivedMsg = (Signals)Enum.Parse(typeof(Signals), Encoding.ASCII.GetString(receivedBytes, 0, readlength));
                switch (receivedMsg)
                {
                    case Signals.TWITCH_CONNECT_SUCCESS:
                        log.Write("Successfully connected to twitch.tv IRC.", 1);
                        break;
                    case Signals.TWITCH_CONNECT_FAIL:
                        log.Write("Failed to connect to twitch.tv IRC.", 3);
                        break;
                    case Signals.TWITCH_RECONNECTING_ONE:
                        log.Write("Retrying to connect to twitch.tv IRC in 1 second.", 2);
                        break;
                    case Signals.TWITCH_RECONNECTED:
                        log.Write("Reconnected to twitch.tv IRC.", 1);
                        break;
                    case Signals.USER_ALREADY_AUTHENTICATED:
                        log.Write("The osu! user is already authenticated. Send \"!auth\" to smoogipooo to de-authenticate.", 2);
                        break;
                    case Signals.TWITCH_DISCONNECTED:
                        log.Write("Twitch.tv IRC connection lost.", 3);
                        break;
                    case Signals.TWITCH_AUTH_FAIL:
                        log.Write("Twitch.tv authentication failed - are your details correct?", 3);
                        break;
                    case Signals.TWITCH_AUTH_SUCCESS:
                        log.Write("Twitch.tv authentication successful! Send \"!auth\" to smoogipooo to authorize the relay to send in-game messages.", 1);
                        break;
                }
                receivedBytes = new byte[50];
                connSock.BeginReceive(receivedBytes, 0, receivedBytes.Length, SocketFlags.None, connRead, receivedBytes);
            }
        }

        #region "Form Events"
        public mainFrm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread UpdateThread = new Thread(UpdateStart);
            UpdateThread.IsBackground = true;
            UpdateThread.Start();

            oNameTB.Text = settings.GetSetting("oName");
            tNameTB.Text = settings.GetSetting("tName");
            tOAuthTB.Text = settings.GetSetting("tOAuth");
            if (oNameTB.Text == "")
                oNameTB.Text = @"osu! Username";
            if (tNameTB.Text == "")
                tNameTB.Text = @"Twitch.tv Username";
            if (tOAuthTB.Text == "")
                tOAuthTB.Text = @"Twitch.tv OAuth Token";

            log.Show(this);
            log.Height = Height;
            log.Location = new Point(Location.X + Size.Width, Location.Y);
            log.Write("Logging started.");
        }

        private void UpdateStart()
        {
            new Updater(settings);
        }
        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            log.Location = new Point(Location.X + Size.Width, Location.Y);
        }

        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            infoTT.Show("The twitch OAuth token allows the relay to send and receive twitch chat messages.\nA token may be generated at http://twitchapps.com/tmi/.\nClick this icon to open the link.", pictureBox2, new Point(17, -8));
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            infoTT.Hide(pictureBox2);
        }

        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Process.Start("http://twitchapps.com/tmi/");
        }
        #endregion

        private void forumLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://osu.ppy.sh/forum/p/2795350");
        }


     }
}
