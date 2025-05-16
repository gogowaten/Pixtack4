using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pixtack4
{
    /// <summary>
    /// InputBox.xaml の相互作用ロジック
    /// </summary>
    public partial class InputBox : Window
    {
        /// <summary>
        /// 入力を受け取るダイアログボックス
        /// </summary>
        /// <param name="prompt">表示するメッセージ</param>
        /// <param name="title">タイトル名</param>
        /// <param name="defaultText">入力用テキストボックスの初期表示文字</param>
        public InputBox(string prompt, string title = "InputBox", string defaultText = "")
        {
            InitializeComponent();

            MyPrompt.Text = prompt;
            this.Title = title;
            MyTextBox.Text = defaultText;
            MyTextBox.SelectAll();
        }
        
        private void MyOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void MyCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
