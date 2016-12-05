using System;
using System.Security;
using System.Windows.Forms;

namespace MSG_SERVER
{
    public partial class Startup : Form
    {
        public Startup()
        {
            InitializeComponent();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textPort.Text))
            {
                SecureString s = new SecureString();
                if (!string.IsNullOrWhiteSpace(passWord.Text))
                    foreach (var c in passWord.Text.ToCharArray())
                        s.AppendChar(c);
                else
                    foreach (var c in "EN-CON_12345678910?!".ToCharArray())
                        s.AppendChar(c);
                Main m = new Main(Convert.ToInt32(textPort.Text), s);
                Hide();
                m.ShowDialog(this);
                Show();
                m.Dispose();
            }
            else
                MessageBox.Show("Please fil in teh missing fields correctly.", "ERR_INCOMPLETE", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void checkPass_CheckedChanged(object sender, EventArgs e)
        {
            if (checkPass.Checked == true)
                passWord.Enabled = true;
            else
                passWord.Enabled = false;
        }

        private void Startup_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Startup_Load(object sender, EventArgs e)
        {
            passWord.Register(groupData);
        }
    }
}
