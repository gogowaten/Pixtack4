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
        public TabControl MyTabControl { get; private set; }

        public ContextTabMenu()
        {
            MyTabControl = SetTemplate();
            //マウスホイールでタブ切り替え
            MyTabControl.MouseWheel += (s, e) =>
            {
                int ima = MyTabControl.SelectedIndex;
                int delta = e.Delta;
                //下に回転
                if (delta < 0)
                {
                    if (ima + 1 < MyTabControl.Items.Count)
                    {
                        MyTabControl.SelectedIndex = ima + 1;
                    }
                }
                //上に回転
                else
                {
                    if (ima - 1 >= 0)
                    {
                        MyTabControl.SelectedIndex = ima - 1;
                    }
                }
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
