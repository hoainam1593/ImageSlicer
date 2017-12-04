using System.Windows.Forms;

namespace ImageSlicer
{
    public partial class RenameForm : Form
    {
        public string NewName { get; set; }

        public RenameForm(string name)
        {
            InitializeComponent();

            m_textBox.Text = name;
        }

        private void OKButton_Click(object sender, System.EventArgs e)
        {
            OnButtonOK_Clicked();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            OnButtonCancel_Clicked();
        }

        private void OnButtonOK_Clicked()
        {
            NewName = m_textBox.Text;
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
            switch(keyData)
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
