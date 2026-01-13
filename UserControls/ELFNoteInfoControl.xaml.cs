using System.Text;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Interaction logic for ELFNoteInfoControl.xaml
    /// </summary>
    public partial class ELFNoteInfoControl : UserControl
    {
        public ELFNoteInfoControl()
        {
            InitializeComponent();
        }

        public void SetNoteInfo(string noteInfo)
        {
            NoteInfoTextBox.Text = noteInfo ?? "No note information available.";
        }
    }
}