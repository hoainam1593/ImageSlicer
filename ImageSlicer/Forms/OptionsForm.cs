using System.Drawing;
using System.Windows.Forms;

namespace ImageSlicer
{
    public partial class OptionsForm : Form
    {
        public int NewAlpha { get; set; }
        public Color NewColor { get; set; }

        public OptionsForm(int maxAlpha, Color boundColor)
        {
            InitializeComponent();

            m_textBox.Text = maxAlpha.ToString();
            m_pictureBox.BackColor = boundColor;
        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            m_colorDialog.Color = m_pictureBox.BackColor;
            if (m_colorDialog.ShowDialog() == DialogResult.OK)
            {
                m_pictureBox.BackColor = m_colorDialog.Color;
            }
        }

        private void ButtonOK_Click(object sender, System.EventArgs e)
        {
            OnButtonOK_Clicked();
        }

        private void ButtonCancel_Click(object sender, System.EventArgs e)
        {
            OnButtonCancel_Clicked();
        }

        private void OnButtonOK_Clicked()
        {
            NewAlpha = int.Parse(m_textBox.Text);
            NewColor = m_pictureBox.BackColor;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnButtonCancel_Clicked()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    OnButtonOK_Clicked();
                    return true;
                case Keys.Escape:
                    OnButtonCancel_Clicked();
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
