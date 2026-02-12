using System.Text;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Interaction logic for ELFNoteInfoControl.xaml
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFNoteInfoControl : UserControl
    {
        #pragma warning restore CA1515
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