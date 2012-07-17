using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BattleShips
{
    public partial class Form1 : Form
    {
        BattleShips _game;
        public int _myPlayerId = -1;

        public Form1(BattleShips game)
        {
            _game = game;
            InitializeComponent();
            KeyPreview = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //textBoxMessage.Select();
            panelGame.Focus();
            buttonSetBoats.Visible = false;
            buttonStrike.Visible = false;
            buttonEndGame.Visible = false;
            ToggleButtons(true);
        }

        private void textMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && textBoxMessage.Text != "")
            {
                _game.SendChatMessage(textBoxMessage.Text);
                textBoxMessage.Clear();
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                _game._connectionClient.running = false;
                _game.Exit();
                this.Close();
            }
        }

        delegate void StringCallBack(string text);

        public void AddChatMessage(string message)
        {
            if (this.textBoxMessage.InvokeRequired)
            {
                StringCallBack d = new StringCallBack(AddChatMessage);
                this.Invoke(d, new object[] { message });
            }
            else
            {
                if (textBoxChat.Text != "")
                    textBoxChat.Text += "\r\n";
                textBoxChat.Text += message;
            }
        }

        delegate void IntStringCallBack(int number, string text);

        public void AddPlayer(int id, string name)
        {
            if (listBoxPlayers.InvokeRequired)
            {
                IntStringCallBack d = new IntStringCallBack(AddPlayer);
                this.Invoke(d, new object[] { id, name});
            }
            else
            {
                listBoxPlayers.Items.Add(new Player(id, name));
                AddChatMessage(name + " has joined the server");
            }
        }

        delegate void CallbackDelegate();

        public void ClearPlayers()
        {
            if (listBoxPlayers.InvokeRequired)
            {
                CallbackDelegate d = new CallbackDelegate(ClearPlayers);
                this.Invoke(d);
            }
            else
            {
                listBoxPlayers.Items.Clear();
            }
        }

        public void ToggleButtons(bool really = true)
        {
            if (buttonSetBoats.InvokeRequired)
            {
                ShowButtonDelegate d = new ShowButtonDelegate(ToggleButtons);
                this.Invoke(d, new object[] { really });
            }
            else
            {
                buttonSetBoats.Enabled = really;
                buttonStrike.Enabled = !really;
            }
        }

        public void SetButtons(bool really = true)
        {
            if (buttonSetBoats.InvokeRequired)
            {
                ShowButtonDelegate d = new ShowButtonDelegate(SetButtons);
                this.Invoke(d, new object[] { really });
            }
            else
            {
                buttonSetBoats.Enabled = really;
                buttonStrike.Enabled = really;
            }
        }

        delegate void IntCallBack(int number);

        public void RemovePlayer(int id)
        {
            if (listBoxPlayers.InvokeRequired)
            {
                IntCallBack d = new IntCallBack(RemovePlayer);
                this.Invoke(d, new object[] { id });
            }
            else
            {
                for (int i = 0; i < listBoxPlayers.Items.Count; i++)
                {
                    if (((Player)listBoxPlayers.Items[i]).ID == id)
                    {
                        listBoxPlayers.Items.Remove((Player)listBoxPlayers.Items[i]);
                        i--;
                        continue;
                    }
                }
            }
        }

        public IntPtr PanelHandle
        {
            get
            {
                return this.panelGame.IsHandleCreated ?
                    this.panelGame.Handle : IntPtr.Zero;
            }
        }

        private void textBoxChat_TextChanged(object sender, EventArgs e)
        {
            textBoxChat.SelectionStart = textBoxChat.Text.Length;
            textBoxChat.ScrollToCaret();
        }

        private void buttonChallenge_Click(object sender, EventArgs e)
        {
            if (listBoxPlayers.SelectedItem != null)
            {
                if (((Player)listBoxPlayers.SelectedItem).ID != _myPlayerId)
                {
                    if (listBoxPlayers.Enabled == true)
                    {
                        _game._connectionClient.SendChallenge(((Player)listBoxPlayers.SelectedItem).ID);
                        listBoxPlayers.Enabled = false;
                        buttonChallenge.Text = "Cancel";
                        AddChatMessage("Challenging " + ((Player)listBoxPlayers.SelectedItem).Name);
                    }
                    else
                    {
                        _game._connectionClient.CancelChallenge(((Player)listBoxPlayers.SelectedItem).ID);
                        listBoxPlayers.Enabled = true;
                        buttonChallenge.Text = "Challenge";
                        AddChatMessage("Cancelling challenge to " + ((Player)listBoxPlayers.SelectedItem).Name + ", you wuss.");
                    }
                }
                else
                {
                    AddChatMessage("You can't challenge yourself!");
                }
            }
        }

        public int PlayerCount
        {
            get
            {
                return listBoxPlayers.Items.Count;
            }
        }

        delegate void ShowButtonDelegate(bool really);

        public void ShowButtons(bool really)
        {
            if (buttonSetBoats.InvokeRequired || buttonStrike.InvokeRequired)
            {
                ShowButtonDelegate d = new ShowButtonDelegate(ShowButtons);
                this.Invoke(d, new object[] { really });
            }
            else
            {
                buttonSetBoats.Visible = really;
                buttonStrike.Visible = really;
                buttonEndGame.Visible = really;
            }
        }

        delegate void DisableInterfaceDelegate(bool really);

        public void DisableInterface(bool really = true)
        {
            if (listBoxPlayers.InvokeRequired)
            {
                DisableInterfaceDelegate d = new DisableInterfaceDelegate(DisableInterface);
                this.Invoke(d, new Object[] { really });

            }
            else
            {
                listBoxPlayers.Enabled = !really;
                buttonChallenge.Enabled = !really;
                if (!really)
                {
                    buttonChallenge.Text = "Challenge";
                }
            }
        }

        public string GetPlayerName(int id)
        {
            foreach (Player player in listBoxPlayers.Items)
            {
                if (player.ID == id)
                    return player.Name;
            }
            return "Invalid ID";
        }

        delegate void StringArgumentDelegate(string message);

        public void SetTitle(string title)
        {
            if (this.InvokeRequired)
            {
                StringArgumentDelegate d = new StringArgumentDelegate(SetTitle);
                this.Invoke(d, new object[] { title });
            }
            else
            {
                this.Text = title;
            }
        }

        private void panelGame_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBoxMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void listBoxPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void buttonStrike_Click(object sender, EventArgs e)
        {
            _game._connectionClient.SendTargets();
        }

        private void buttonEndGame_Click(object sender, EventArgs e)
        {
            if (buttonEndGame.Text == "End Game")
                buttonEndGame.Text = "Really?";
            else if (buttonEndGame.Text == "Really?")
                buttonEndGame.Text = "WHY!?";
            else
            {
                buttonEndGame.Text = "End Game";
                buttonEndGame.Visible = false;
                _game._connectionClient.SendData(new byte[] { 18 });
            }
        }

        private void buttonSetBoats_Click(object sender, EventArgs e)
        {
            if (_game._selectedTiles.Count == 17)
            {
                byte[] buffer = new byte[137];
                buffer[0] = 17;
                for (int i = 0; i < _game._selectedTiles.Count; i++)
                {
                    Console.WriteLine("Sent boat ({0}, {1})", _game._selectedTiles[i].X.ToString(), _game._selectedTiles[i].Y.ToString());
                    Array.Copy(BitConverter.GetBytes(_game._selectedTiles[i].X), 0, buffer, (8 * i) + 1, 4);
                    Array.Copy(BitConverter.GetBytes(_game._selectedTiles[i].Y), 0, buffer, (8 * i) + 5, 4);
                }
                _game._connectionClient.SendData(buffer);
            }
        }
    }
}
