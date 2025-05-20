using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pixtack4
{

    /// <summary>
    /// TabControlに改変したContextMenu右クリックメニュー
    /// </summary>
    public class ContextTabMenu : ContextMenu
    {
        private TabControl MyTabControl { get; set; }

        public ContextTabMenu()
        {
            MyTabControl = SetTemplate();
            //マウスホイールでタブ切り替え
            MyTabControl.MouseWheel += (s, e) =>
            {
                int index = MyTabControl.SelectedIndex;
                if (e.Delta > 0 && index < MyTabControl.Items.Count - 1)
                {
                    MyTabControl.SelectedIndex++;
                }
                else if (index > 0) MyTabControl.SelectedIndex--;
            };
        }

        private TabControl SetTemplate()
        {
            FrameworkElementFactory factory = new(typeof(TabControl), "nemo");
            Template = new() { VisualTree = factory };
            ApplyTemplate();
            if (Template.FindName("nemo", this) is TabControl tab)
            {
                return tab;
            }
            else throw new ArgumentException();
        }

        /// <summary>
        /// Adds a <see cref="TabItem"/> to the tab control and returns its index.
        /// </summary>
        /// <param name="item">The <see cref="TabItem"/> to add to the tab control. Cannot be <see langword="null"/>.</param>
        /// <returns>The zero-based index at which the <see cref="TabItem"/> was added.</returns>
        /// 
        public int AddTabItem(TabItem item)
        {
            return MyTabControl.Items.Add(item);
        }
    }
}
