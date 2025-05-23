﻿using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using static System.Net.WebRequestMethods;

namespace Pixtack4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ManageExCanvas MyManageExCanvas { get; set; } = null!;

        public RootThumb MyRoot
        {
            get => (RootThumb)GetValue(MyRootProperty);
            private set => SetValue(MyRootProperty, value);
        }
        public static readonly DependencyProperty MyRootProperty =
            DependencyProperty.Register(nameof(MyRoot), typeof(RootThumb), typeof(MainWindow), new PropertyMetadata(null));

        //private string ROOT_DATA_FILE_NAME = "RootData.px4";
        //RootのDataの拡張子はpx4
        //それ以外のDataの拡張子はpx4item

        //アプリ名
        private const string APP_NAME = "Pixtack4";
        //アプリのバージョン
        private string MyAppVersion = null!;
        //アプリのフォルダパス
        private string MyAppDirectory = null!;

        //アプリのウィンドウData
        private AppWindowData MyAppWindowData { get; set; } = null!;
        //アプリのウィンドウDataファイル名
        private const string APP_WINDOW_DATA_FILE_NAME = "AppWindowData.xml";

        //アプリの設定Data
        public AppData MyAppData { get; private set; } = null!;// 確認用でパブリックにしている
        //アプリのDataファイル名
        private const string APP_DATA_FILE_NAME = "AppData.xml";


        //datetime.tostringの書式、これを既定値にする
        private const string DATE_TIME_STRING_FORMAT = "HHmmss";
        //private const string DATE_TIME_STRING_FORMAT = "yyyMMdd'_'HHmmss'.'fff";

        //タブ型右クリックメニュー
        private ContextTabMenu MyRootContextMenu { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();

            MyInitialize();
            MyInitialize2();
            Closing += MainWindow_Closing;
            DataContext = this;

        }



        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {

            AppClosing(e);// アプリ終了直前の処理
        }


        #region アプリ終了時の処理
        /// <summary>
        /// アプリ終了直前の処理
        /// </summary>
        private void AppClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Itemがある場合は、保存するかを確認する
            if (MyRoot.MyThumbs.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    "ファイルに保存してから終了する？\n\n\n" +
                    "はい：保存して終了\n\n" +
                    "いいえ：保存しないで終了\n\n" +
                    "キャンセル：終了をキャンセル",
                    "アプリ終了前の確認",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;// 終了をキャンセル
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveItemOverwriteCurrentFilePath();// 上書き保存
                }
            }


            string filePath = System.IO.Path.Combine(MyAppDirectory, APP_WINDOW_DATA_FILE_NAME);
            if (!MyAppWindowData.Serialize(filePath))
            {
                MessageBox.Show("アプリのWindow設定を保存できなかった");
            }

            filePath = MyAppData.CurrentOpenFilePath;
            if (filePath == string.Empty)
            {
            }
            filePath = System.IO.Path.Combine(MyAppDirectory, APP_DATA_FILE_NAME);
            if (!MyAppData.Serialize(filePath, MyAppData))
            {
                MessageBox.Show("アプリの設定を保存できなかった");
            }
        }
        #endregion アプリ終了前の処理

        #region 初期処理

        private void MyInitialize()
        {
            //アプリのパスとバージョン取得
            MyAppDirectory = Environment.CurrentDirectory;
            MyAppVersion = GetAppVersion();

            //アプリの設定の読み込みと設定
            if (LoadAppData() is AppData appData) { MyAppData = appData; }
            else { MyAppData = new AppData(); }


        }


        private void MyInitialize2()
        {
            this.Title = APP_NAME + "_" + MyAppVersion;
            LoadAppWindowData();// ウィンドウ設定ファイルの読み込み
            MyBindWindowData();// ウィンドウのバインド設定

            // RootThumbとManageExCanvasの初期化
            MyInitializeRootThumb();

            //前回に開いていたファイルを開く
            OpenPx4FileRootThumb(MyAppData.CurrentOpenFilePath);

            MyBind();

            //RootThumbの右クリックメニュー作成
            MyInitializeMyRootContextMenu();

        }

        private void MyInitializeMyRootContextMenu()
        {

            MyRootContextMenu = new();
            MyRoot.ContextMenu = MyRootContextMenu;
            MyRootContextMenu.MyTabControl.TabStripPlacement = Dock.Left;



            MyRootContextMenu.AddTabItem(MakeContextMenuTabItem1());

            MyRootContextMenu.AddTabItem(MakeContextMenuTabItem2());





            //TabItem tabItem2 = new();
            //Rectangle rectangle2 = new() { Width = 100, Height = 100 };
            //VisualBrush brush2 = new() { Stretch = Stretch.Uniform };
            //BindingOperations.SetBinding(brush2, VisualBrush.VisualProperty, new Binding() { Source = MyRoot, Path = new PropertyPath(RootThumb.MyClickedThumbProperty) });
            //rectangle2.Fill = brush2;
            //tabItem2.Header = rectangle2;

            //StackPanel stackPanel2 = new();
            //TextBlock textBlock22 = new() { Text = "textb111" };
            //TextBlock textBlock222 = new() { Text = "textb222" };
            //stackPanel2.Children.Add(textBlock22);
            //stackPanel2.Children.Add(textBlock222);
            //tabItem2.Content = stackPanel2;
            //MyRootContextMenu.AddTabItem(tabItem2);

        }

        private TabItem MakeContextMenuTabItem1()
        {
            Rectangle rectangle = new() { Width = 100, Height = 100 };
            VisualBrush brush = new() { Stretch = Stretch.Uniform };
            BindingOperations.SetBinding(brush, VisualBrush.VisualProperty, new Binding() { Source = MyRoot, Path = new PropertyPath(RootThumb.MyFocusThumbProperty) });
            rectangle.Fill = brush;

            StackPanel menuPanel = new();
            TabItem tabItem = new()
            {
                Header = rectangle,
                Content = menuPanel
            };

            MenuItem mItem = new() { Header = "複製" };
            mItem.Click += (a, b) => { MyRoot.Dupulicate(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "画像として複製" };
            mItem.Click += (a, b) => { MyRoot.DupulicateAsImage(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "名前を付けて保存" };
            mItem.Click += (a, b) =>
            {
                if (MyRoot.MyFocusThumb is KisoThumb item)
                {
                    SaveFileDialog dialog = MakeSaveFileDialog(item);
                    if (dialog.ShowDialog() == true)
                    {
                        _ = MyRoot.SaveItemData(item.MyItemData, dialog.FileName);
                    }
                }
            };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "名前を付けて画像として保存" };
            mItem.Click += (a, b) => { _ = SaveItemToImageFile(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(mItem);

            //mItem = new() { Header = "削除" };
            //menuPanel.Children.Add(mItem);
            //mItem.Click += (a, b) => { MyRoot.RemoveThumb(MyRoot.MyFocusThumb); };

            mItem = new();
            StackPanel sp = new() { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock() { Text = "選択Item " });
            TextBlock tb = new();
            tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(ObservableCollection<KisoThumb>.Count)) { Source = MyRoot.MySelectedThumbs });
            sp.Children.Add(tb);
            sp.Children.Add(new TextBlock() { Text = " 個すべてを削除" });
            mItem.Header = sp;
            mItem.Click += (a, b) => { MyRoot.RemoveSelectedThumbs(); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "最前面" };
            menuPanel.Children.Add(mItem);
            mItem.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexTop(); };
            mItem = new() { Header = "前面" };
            menuPanel.Children.Add(mItem);
            mItem.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexUp(); };
            mItem = new() { Header = "背面" };
            menuPanel.Children.Add(mItem);
            mItem.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexDown(); };
            mItem = new() { Header = "最背面" };
            mItem.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexBottom(); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "Focusを画像としてコピー" };
            mItem.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(mItem);
            mItem = new() { Header = "Groupを画像としてコピー" };
            mItem.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyActiveGroupThumb); };
            menuPanel.Children.Add(mItem);
            mItem = new() { Header = "Rootを画像としてコピー" };
            mItem.Click += (a, b) => { CopyAsImageForItem(MyRoot); };
            menuPanel.Children.Add(mItem);
            mItem = new() { Header = "Clickedを画像としてコピー" };
            mItem.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyClickedThumb); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "画像を貼り付け(png)" };
            mItem.Click += (a, b) => { AddImageFromClipboardPng(); };
            menuPanel.Children.Add(mItem);
            mItem = new() { Header = "画像を貼り付け(bmp)" };
            mItem.Click += (a, b) => { AddImageFromClipboardBmp(); };
            menuPanel.Children.Add(mItem);

            mItem = new() { Header = "グループ化" };
            mItem.Click += (a, b) => { MyRoot.AddGroupingFromSelected(); };
            menuPanel.Children.Add(mItem);
            mItem = new() { Header = "グループ解除" };
            mItem.Click += (a, b) => { MyRoot.UngroupFocusThumb(); };
            menuPanel.Children.Add(mItem);

            return tabItem;
        }

        private TabItem MakeContextMenuTabItem2()
        {
            Rectangle rectangle = new() { Width = 100, Height = 100 };
            VisualBrush brush = new() { Stretch = Stretch.Uniform };
            BindingOperations.SetBinding(brush, VisualBrush.VisualProperty, new Binding() { Source = MyRoot, Path = new PropertyPath(RootThumb.MyActiveGroupThumbProperty) });
            rectangle.Fill = brush;
            TextBlock headerText = new() { Text = "ActiveGroup" };
            StackPanel headerPanel = new() { Orientation = Orientation.Vertical };
            headerPanel.Children.Add(headerText);
            headerPanel.Children.Add(rectangle);

            StackPanel menuPanel = new();
            TabItem tabItem = new()
            {
                Header = headerPanel,
                Content = menuPanel
            };

            StackPanel menuStackPanel = new();
            tabItem.Content = menuStackPanel;

            MenuItem item = new() { Header = "IN(内側)" };
            item.Click += (a, b) => { MyRoot.ActiveGroupToInside(); };
            menuStackPanel.Children.Add(item);
            item = new() { Header = "OUT" };
            item.Click += (a, b) => { MyRoot.ActiveGroupToOutside(); };
            menuStackPanel.Children.Add(item);
            item = new() { Header = "Clicked" };
            item.Click += (a, b) => { MyRoot.ActiveGroupFromClickedThumbsParent(); };
            menuStackPanel.Children.Add(item);
            item = new() { Header = "Root" };
            item.Click += (a, b) => { MyRoot.ChangeActiveGroupToRootActivate(); };
            menuStackPanel.Children.Add(item);

            TextBlock gridsize = new();
            gridsize.SetBinding(TextBlock.TextProperty, new Binding(nameof(ItemData.GridSize)) { Source = MyRoot.MyActiveGroupThumb.MyItemData, StringFormat = $"今のGridSize {0}" });
            menuStackPanel.Children.Add(gridsize);

            Button btn = new() { Content = "GridSizeUp"};
            btn.Click += (a, b) => { ChangeGridSizeUp(); };
            menuStackPanel.Children.Add(btn);
            btn = new() { Content = "GridSizeDown"};
            btn.Click += (a, b) => { ChangeGridSizeDown(); };
            menuStackPanel.Children.Add(btn);
            item = new() { Header = "GridSize指定" };
            item.Click += (a, b) => { ChangeGridSize(); };
            menuStackPanel.Children.Add(item);



            return tabItem;
        }

        private void MyBind()
        {
            //今開いているファイル名をステータスバーに表示
            MyStatusCurrentFileName.SetBinding(TextBlock.TextProperty, new Binding(nameof(MyAppData.CurrentOpenFilePath)) { Source = MyAppData, Converter = new MyConvPathFileName() });
        }


        #region アプリのウィンドウ設定

        /// <summary>
        /// ウィンドウ設定ファイルの読み込み
        /// </summary>
        private void LoadAppWindowData()
        {
            //アプリのフォルダから設定ファイルを読み込む、ファイルがなかったら新規作成
            string filePath = System.IO.Path.Combine(MyAppDirectory, APP_WINDOW_DATA_FILE_NAME);
            if (AppWindowData.Deserialize(filePath) is AppWindowData data)
            {
                MyAppWindowData = data;
            }
            else
            {
                MyAppWindowData = new AppWindowData();
            }
        }

        /// <summary>
        /// ウィンドウのバインド設定
        /// </summary>
        private void MyBindWindowData()
        {
            //バインド設定、ウィンドウの最大化も？
            SetBinding(LeftProperty, new Binding(nameof(AppWindowData.Left)) { Source = MyAppWindowData, Mode = BindingMode.TwoWay });
            SetBinding(TopProperty, new Binding(nameof(AppWindowData.Top)) { Source = MyAppWindowData, Mode = BindingMode.TwoWay });
            SetBinding(WidthProperty, new Binding(nameof(AppWindowData.Width)) { Source = MyAppWindowData, Mode = BindingMode.TwoWay });
            SetBinding(HeightProperty, new Binding(nameof(AppWindowData.Height)) { Source = MyAppWindowData, Mode = BindingMode.TwoWay });
            SetBinding(WindowStateProperty, new Binding(nameof(AppWindowData.WindowState)) { Source = MyAppWindowData, Mode = BindingMode.TwoWay });

            FixWindowLocate();// ウィンドウ位置設定が画面外だった場合は0(左上)にする
            //最小化されていた場合はNormalに戻す
            if (WindowState == WindowState.Minimized) { WindowState = WindowState.Normal; }
        }

        /// <summary>
        /// ウィンドウ位置設定が画面外だった場合は0(左上)にする
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private void FixWindowLocate()
        {
            if (MyAppWindowData.Left < -10 ||
                MyAppWindowData.Left > SystemParameters.VirtualScreenWidth - 100)
            {
                MyAppWindowData.Left = 0;
            }
            if (MyAppWindowData.Top < -10 ||
                MyAppWindowData.Top > SystemParameters.VirtualScreenHeight - 100)
            {
                MyAppWindowData.Top = 0;
            }
        }

        #endregion アプリのウィンドウ設定

        /// <summary>
        /// アプリのバージョン取得
        /// </summary>
        /// <returns></returns>
        private static string GetAppVersion()
        {
            //実行ファイルのバージョン取得
            string[] cl = Environment.GetCommandLineArgs();

            //System.Diagnostics.FileVersionInfo
            if (FileVersionInfo.GetVersionInfo(cl[0]).FileVersion is string ver)
            {
                return ver;
            }
            else { return string.Empty; }
        }

        #endregion 初期処理








        #region ボタンクリック        
        //確認テスト用

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var neko = MyAppWindowData;
            var inu = MyAppData;
            //var grid = MyManageExCanvas.MyAreaThumb.GridSize;
            var area = MyManageExCanvas.MyRootThumb.MyActiveGroupThumb.MyThumbs.Count;
            var gri = MyManageExCanvas.MyRootThumb.MyActiveGroupThumb.MyItemData.GridSize;
        }

        private void Button_Click_(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_AreaItemVisibleSwitch(object sender, RoutedEventArgs e)
        {
            MyManageExCanvas.AreaThumbVisibleSwitch();
        }

        #region 完了






        private void Button_Click_DupulicateAsImageForFocusItem(object sender, RoutedEventArgs e)
        {
            _ = MyRoot.DupulicateAsImage(MyRoot.MyFocusThumb);// 指定されたItemを画像として複製
        }

        private void Button_Click_DuplicateFocusItem(object sender, RoutedEventArgs e)
        {
            MyRoot.Dupulicate(MyRoot.MyFocusThumb);// Data複製
        }

        private void Button_Click_CopyAsImageForRoot(object sender, RoutedEventArgs e)
        {
            CopyAsImageForItem(MyRoot);// Rootを画像としてコピーする
        }

        private void Button_Click_CopyAsImageForFocusItem(object sender, RoutedEventArgs e)
        {
            CopyAsImageForItem(MyRoot.MyFocusThumb);// FocusItemを画像としてコピーする
        }

        private void Button_Click_CopyAsImageForClickedItem(object sender, RoutedEventArgs e)
        {
            CopyAsImageForItem(MyRoot.MyClickedThumb);// ClickedItemを画像としてコピーする
        }

        private void Button_Click_AddImageFromClipboardPng(object sender, RoutedEventArgs e)
        {
            AddImageFromClipboardPng();// クリップボードから画像取得して追加する。PNG形式優先で取得
        }

        private void Button_Click_AddImageFromClipboardBmp(object sender, RoutedEventArgs e)
        {
            AddImageFromClipboardBmp();// クリップボードから画像取得して追加する。画像は完全不透明で取得
        }


        private void Button_Click_ChangeGridSizeUp(object sender, RoutedEventArgs e)
        {
            ChangeGridSizeUp();// アクティブグループの現在のグリッドサイズを2倍にして、許容範囲内に収まるようにします。
        }

        private void Button_Click_ChangeGridSizeDown(object sender, RoutedEventArgs e)
        {
            ChangeGridSizeDown();// アクティブグループのグリッドサイズを、次に小さい有効な値に縮小します。
        }

        private void Button_Click_ChangeGridSize(object sender, RoutedEventArgs e)
        {
            ChangeGridSize();// ユーザー入力に基づいて、アクティブなグループアイテムのグリッドサイズを更新します。
        }

        private void Button_Click_ZUp(object sender, RoutedEventArgs e)
        {
            MyRoot.MyFocusThumb?.ZIndexUp();
        }
        private void Button_Click_ZDown(object sender, RoutedEventArgs e)
        {
            MyRoot.MyFocusThumb?.ZIndexDown();
        }
        private void Button_Click_ZtoTop(object sender, RoutedEventArgs e)
        {
            MyRoot.MyFocusThumb?.ZIndexTop();
        }
        private void Button_Click_ZtoBottom(object sender, RoutedEventArgs e)
        {
            MyRoot.MyFocusThumb?.ZIndexBottom();
        }

        private void Button_Click_ChangeActiveGroupRootActivate(object sender, RoutedEventArgs e)
        {
            MyRoot.ChangeActiveGroupToRootActivate();// ActiveGroupをRootに変更する
        }

        private void Button_Click_ChangeActiveGroupClickedParent(object sender, RoutedEventArgs e)
        {
            MyRoot.ActiveGroupFromClickedThumbsParent();// ClickedのParentをActiveGroupThumbにする
        }

        private void Button_Click_ChangeActiveGroupToOutside(object sender, RoutedEventArgs e)
        {
            MyRoot.ActiveGroupToOutside();// ActiveGroupThumbを外(Root)側のGroupThumbへ変更
        }

        private void Button_Click_ChangeActiveGroupToInside(object sender, RoutedEventArgs e)
        {
            MyRoot.ActiveGroupToInside();// ActiveGroupThumbを内側のGroupThumbへ変更
        }

        private void Button_Click_UnGroup(object sender, RoutedEventArgs e)
        {
            MyRoot.UngroupFocusThumb();// グループ解除、FocusThumbが対象
        }

        private void Button_Click_Grouping(object sender, RoutedEventArgs e)
        {
            MyRoot.AddGroupingFromSelected();// SelectedThumbsからGroupThumbを生成、ActiveThumbに追加
        }

        private void Button_Click_ItemsTreePanelVisible(object sender, RoutedEventArgs e)
        {
            ChangeVisible(MyGridMyItemsTree);// パネルの表示非表示を切り替える、Visible or Collapsed
        }

        private void Button_Click_SelectedItemsPropertyPanelVisible(object sender, RoutedEventArgs e)
        {
            ChangeVisible(MyPanelSelectedItemsProperty);// パネルの表示非表示を切り替える、Visible or Collapsed
        }

        private void Button_Click_ActiveGroupItemPropertyPanelVisible(object sender, RoutedEventArgs e)
        {
            ChangeVisible(MyPanelActiveGroupItemProperty);// パネルの表示非表示を切り替える、Visible or Collapsed
        }

        private void Button_Click_FocusItemPropertyPanelVisible(object sender, RoutedEventArgs e)
        {
            ChangeVisible(MyPanelFocusItemProperty);// パネルの表示非表示を切り替える、Visible or Collapsed
        }

        private void Button_Click_RemoveAllItems(object sender, RoutedEventArgs e)
        {
            MyRoot.RemoveAll();// 全Item削除
        }

        private void Button_Click_RemoveSelectedItems(object sender, RoutedEventArgs e)
        {
            MyRoot.RemoveSelectedThumbs();// 選択Itemすべての削除
        }

        private void Button_Click_SaveFocusItemToImageFile(object sender, RoutedEventArgs e)
        {
            // FocusItemを画像として保存する
            if (MyRoot.MyFocusThumb != null)
            {
                if (SaveItemToImageFile(MyRoot.MyFocusThumb)) { MyStatusMessage.Text = MakeStatusMessage("保存完了"); }
            }
        }

        private void Button_Click_SaveRootToImageFile(object sender, RoutedEventArgs e)
        {
            // RootItemを画像として保存する
            if (SaveItemToImageFile(MyRoot)) { MyStatusMessage.Text = MakeStatusMessage("保存完了"); }
        }

        //破線枠の表示
        private void Button_Click_SwitchWaku(object sender, RoutedEventArgs e)
        {
            //if (MyRoot.IsWakuVisible == Visibility.Visible)
            //{
            //    MyRoot.IsWakuVisible = Visibility.Collapsed;
            //}
            //else { MyRoot.IsWakuVisible = Visibility.Collapsed; }
            var neko = MyRoot.IsWakuVisible;
            if (MyAppData.IsWakuVisible == Visibility.Visible)
            {
                MyAppData.IsWakuVisible = Visibility.Collapsed;
            }
            else
            {
                MyAppData.IsWakuVisible = Visibility.Visible;
            }
            var inu = MyRoot.IsWakuVisible;
        }

        private void Button_Click_SaveFocusItem(object sender, RoutedEventArgs e)
        {
            if (MyRoot.MyFocusThumb != null)
            {
                // Dataを名前を付けて保存
                (_, string message) = SaveItem(MyRoot.MyFocusThumb);
                MyStatusMessage.Text = message;
            }
        }

        private void Button_Click_MyRootStatusPanelVisible(object sender, RoutedEventArgs e)
        {
            MyRootStatusPanelVisible();
        }

        private void Button_Click_OpenPx4File(object sender, RoutedEventArgs e)
        {
            MyStatusMessage.Text = OpenPx4File();// px4ファイルを開く
        }

        private void Button_Click_OpenFile(object sender, RoutedEventArgs e)
        {
            OpenItemFile();// 対応ファイルを開いて、今のRootに追加する
        }


        private void Button_Click_ResetRoot(object sender, RoutedEventArgs e)
        {
            MyStatusMessage.Text = ResetRootThumb();// RootThumbを新規作成してリセット
        }

        private void Button_Click_SaveData(object sender, RoutedEventArgs e)
        {
            SaveItem(MyRoot);// Dataを名前を付けて保存
        }


        private void Button_Click_OverwriteSave(object sender, RoutedEventArgs e)
        {
            SaveItemOverwriteCurrentFilePath();// 上書き保存
        }

        private void Button_Click_ResetWindow(object sender, RoutedEventArgs e)
        {
            ResetWindowState();// ウィンドウの位置とサイズをリセット
        }
        #endregion 完了

        #endregion ボタンクリック


        //ウィンドウにファイルドロップ時
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //パスはファイル名でソートする
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                OpenFiles(paths);
            }
        }

        #region メソッド

        #region 初期化、リセット系

        /// <summary>
        /// RootThumbを新規作成してリセット
        /// </summary>
        private string ResetRootThumb()
        {
            if (MyRoot.MyThumbs.Count == 0)
            {
                return MakeStatusMessage("Item数が0だったのでリセットの必要がなかった");
            }

            MessageBoxResult result = MessageBox.Show(
                "今の状態を保存してからリセットする？\n\n\n" +
                "はい＿：保存してからリセット\n\n" +
                "いいえ：保存しないでリセット\n\n" +
                "キャンセル：リセットをキャンセル",
                "リセット前の確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if (result == MessageBoxResult.Cancel)
            {
                return MakeStatusMessage("リセットは中止された");
            }
            else if (result == MessageBoxResult.Yes)
            {
                //ファイルに保存
                //SaveData(MyAppData.CurrentOpenFilePath, MyRoot.MyItemData);
                SaveItem(MyRoot);
            }
            MyInitializeRootThumb();
            MyAppData.CurrentOpenFilePath = string.Empty;//今開いているファイルパスもリセット
            return MakeStatusMessage("リセット完了");
        }

        /// <summary>
        /// RootThumbとManageExCanvasの初期化
        /// </summary>
        private void MyInitializeRootThumb()
        {
            MyRoot = new RootThumb(new ItemData(ThumbType.Root));
            _ = MyRoot.SetBinding(KisoThumb.IsWakuVisibleProperty, new Binding(nameof(AppData.IsWakuVisible)) { Source = MyAppData, Mode = BindingMode.TwoWay });
            MyManageExCanvas = new ManageExCanvas(MyRoot, new ManageData(), this);
            MyScrollViewer.Content = MyManageExCanvas;
            //MyGridMyItemsTree.DataContext = MyRoot.MyThumbs;
            //DataContext = this;

        }

        /// <summary>
        /// ウィンドウの位置とサイズをリセット
        /// </summary>
        private void ResetWindowState()
        {
            MyAppWindowData = new AppWindowData();
            MyBindWindowData();
        }

        #endregion 初期化、リセット系

        #region クリップボード


        /// <summary>
        /// 対象Itemを画像としてコピーする
        /// </summary>
        private void CopyAsImageForItem(KisoThumb? item)
        {
            if (item == null) { return; }
            if (MyRoot.MakeBitmapFromThumb(item) is RenderTargetBitmap bmp)
            {
                SetImageToClipboard(bmp);
            }
        }

        ///// <summary>
        ///// RootItemを画像としてコピーする
        ///// </summary>
        //private void CopyAsImageForRoot()
        //{
        //    if (MakeBitmapFromThumb(MyRoot) is RenderTargetBitmap bmp)
        //    {
        //        SetImageToClipboard(bmp);
        //    }
        //}

        ///// <summary>
        ///// FocusItemを画像としてコピーする
        ///// </summary>
        //private void CopyAsImageForFocusItem()
        //{
        //    if (MakeBitmapFromThumb(MyRoot.MyFocusThumb) is RenderTargetBitmap bmp)
        //    {
        //        SetImageToClipboard(bmp);
        //    }
        //}


        //アルファ値を失わずに画像のコピペできた、.NET WPFのClipboard - 午後わてんのブログ
        //        https://gogowaten.hatenablog.com/entry/2021/02/10/134406

        /// <summary>
        /// 画像をクリップボードにコピーする。BitmapSourceとそれをPNG形式に変換したもの両方をセットする
        /// </summary>
        /// <param name="source"></param>
        public void SetImageToClipboard(BitmapSource source)
        {
            //DataObjectに入れたいデータを入れて、それをクリップボードにセットする
            DataObject data = new();

            //BitmapSource形式そのままでセット
            data.SetData(typeof(BitmapSource), source);

            //PNG形式にエンコードしたものをMemoryStreamして、それをセット
            //画像をPNGにエンコード
            PngBitmapEncoder pngEnc = new();
            pngEnc.Frames.Add(BitmapFrame.Create(source));

            //エンコードした画像をMemoryStreamにSava
            using MemoryStream ms = new();
            pngEnc.Save(ms);
            data.SetData("PNG", ms);

            //クリップボードにセット
            Clipboard.SetDataObject(data, true);
        }

        /// <summary>
        /// クリップボードから画像取得して追加する。画像は完全不透明で取得
        /// </summary>
        private void AddImageFromClipboardBmp()
        {
            if (MyClipboard.GetImageConvertedBgr32() is BitmapSource bmp)
            {
                MyRoot.AddImageThumb(bmp);
            }
            else
            {
                MessageBox.Show("クリップボードから画像取得できなかった");
            }
        }

        /// <summary>
        /// クリップボードから画像取得して追加する。PNG形式優先で取得
        /// </summary>
        private void AddImageFromClipboardPng()
        {
            //if (MyClipboard.GetImageAlphaFixPng() is BitmapSource bmp) //エラー
            //if (MyClipboard.GetImageFromClipboardWithAlphaFix() is BitmapSource bmp)//図形はいいけどグラフが変
            //if(MyClipboard.GetPngImageFromStream() is BitmapSource bmp)//図形はいいけどグラフが拡大、通常がエラー
            //if(MyClipboard.GetImageConvertedBgr32() is BitmapSource bmp)//不透明だけどグラフが正常
            //if(MyClipboard.GetBgr32FromPng() is BitmapSource bmp)//通常がエラー、グラフ拡大、不透明
            //if(MyClipboard.GetImageFromClipboard() is BitmapSource bmp)//グラフ正常、不透明
            if (MyClipboard.GetImagePreferPNG() is BitmapSource bmp)//すべて正常            
            {
                MyRoot.AddImageThumb(bmp);
            }
            else
            {
                MessageBox.Show("クリップボードから画像取得できなかった");
            }
        }

        #endregion クリップボード

        #region 画像保存

        /// <summary>
        /// ThumbItemを画像として保存する
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool SaveItemToImageFile(KisoThumb? item)
        {
            if (item == null) { return false; }
            if (MyRoot.MakeBitmapFromThumb(item) is RenderTargetBitmap bmp)// ThumbをBitmapに変換
            {
                // Bitmapをファイル保存
                var (result, filePath) = SaveBitmap(bmp, MyAppData.DefaultSaveImageFileName, MyAppData.MyJpegQuality);
                if (result)
                {
                    //既定ファイル名の更新
                    MyAppData.DefaultSaveImageFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    return true;
                }
            }
            return false;
        }


        ///// <summary>
        ///// ThumbをBitmapに変換
        ///// </summary>
        ///// <param name="thumb">Bitmapにする要素</param>
        ///// <param name="clearType">フォントのClearTypeを有効にして保存</param>
        ///// <returns></returns>
        //public RenderTargetBitmap? MakeBitmapFromThumb(KisoThumb? thumb, bool clearType = false)
        //{
        //    if (thumb == null) { return null; }
        //    if (thumb.ActualHeight == 0 || thumb.ActualWidth == 0) { return null; }

        //    Rect bounds = VisualTreeHelper.GetDescendantBounds(thumb);
        //    bounds = thumb.RenderTransform.TransformBounds(bounds);
        //    DrawingVisual dVisual = new();
        //    //サイズを四捨五入
        //    bounds.Width = Math.Round(bounds.Width, MidpointRounding.AwayFromZero);
        //    bounds.Height = Math.Round(bounds.Height, MidpointRounding.AwayFromZero);
        //    using (DrawingContext context = dVisual.RenderOpen())
        //    {
        //        var bru = new BitmapCacheBrush(thumb);
        //        if (clearType)
        //        {
        //            BitmapCache bc = new() { EnableClearType = true };
        //            bru.BitmapCache = bc;
        //        }
        //        context.DrawRectangle(bru, null, new Rect(bounds.Size));
        //    }
        //    RenderTargetBitmap bitmap
        //        = new((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96.0, 96.0, PixelFormats.Pbgra32);
        //    bitmap.Render(dVisual);

        //    return bitmap;
        //}


        /// <summary>
        /// Bitmapをファイル保存ダイアログから保存する
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="fileName">ダイアログに表示する規定のファイル名</param>
        /// <param name="jpegQuality">jpeg画像の品質値、jpeg以外で保存するときは無視される</param>
        /// <returns></returns>
        public static (bool result, string filePath) SaveBitmap(BitmapSource bitmap, string fileName = "", int jpegQuality = 90)
        {
            SaveFileDialog dialog = MakeSaveFileDialogForImage(fileName);

            if (dialog.ShowDialog() == true)
            {
                //エンコーダー取得
                (BitmapEncoder? encoder, BitmapMetadata? meta) = GetEncoderWithMetaData(dialog.FilterIndex, jpegQuality);
                if (encoder is null) { return (false, string.Empty); }
                //保存
                encoder.Frames.Add(BitmapFrame.Create(bitmap, null, meta, null));
                try
                {
                    using FileStream stream = new(dialog.FileName, FileMode.Create, FileAccess.Write);
                    encoder.Save(stream);
                    return (true, dialog.FileName);
                }
                catch (Exception)
                {
                    return (false, string.Empty);
                }
            }
            return (false, string.Empty);
        }


        private static SaveFileDialog MakeSaveFileDialogForImage(string fileName)
        {
            SaveFileDialog dialog = new()
            {
                Filter = "*.png|*.png|*.jpg|*.jpg;*.jpeg|*.bmp|*.bmp|*.gif|*.gif|*.tiff|*.tiff",
                FileName = fileName,
                AddExtension = true,
            };

            return dialog;
        }


        /// <summary>
        /// 画像エンコーダー作成
        /// </summary>
        /// <param name="filterIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        private static (BitmapEncoder? encoder, BitmapMetadata? meta) GetEncoderWithMetaData(int filterIndex, int jpegQuality)
        {
            BitmapMetadata? meta = null;
            //string software = APP_NAME + "_" + AppVersion;
            string software = APP_NAME;// "Pixtack4";

            switch (filterIndex)
            {
                case 1:
                    meta = new BitmapMetadata("png");
                    meta.SetQuery("/tEXt/Software", software);
                    return (new PngBitmapEncoder(), meta);
                case 2:
                    meta = new BitmapMetadata("jpg");
                    meta.SetQuery("/app1/ifd/{ushort=305}", software);
                    var jpeg = new JpegBitmapEncoder
                    {
                        //QualityLevel = MyItemData.MyJpegQuality,
                        //QualityLevel = jpegQuality
                        QualityLevel = jpegQuality
                        //QualityLevel = MyAppData.JpegQuality,
                    };
                    return (jpeg, meta);
                case 3:
                    return (new BmpBitmapEncoder(), meta);
                case 4:
                    meta = new BitmapMetadata("Gif");
                    //tData.SetQuery("/xmp/xmp:CreatorTool", "Pixtrim2");
                    //tData.SetQuery("/XMP/XMP:CreatorTool", "Pixtrim2");
                    meta.SetQuery("/XMP/XMP:CreatorTool", software);

                    return (new GifBitmapEncoder(), meta);
                case 5:
                    meta = new BitmapMetadata("tiff")
                    {
                        ApplicationName = software
                    };
                    return (new TiffBitmapEncoder(), meta);
                default:
                    throw new Exception();
            }

        }


        #endregion 画像保存

        #region SaveData


        /// <summary>
        /// Dataを名前を付けて保存
        /// </summary>
        /// <param name="thumb"></param>
        /// <returns></returns>
        private (bool result, string message) SaveItem(KisoThumb thumb)
        {
            SaveFileDialog dialog = MakeSaveFileDialog(thumb);
            if (dialog.ShowDialog() == true)
            {
                if (MyRoot.SaveItemData(thumb.MyItemData, dialog.FileName))
                {
                    //保存ファイル名の既定値更新
                    MyAppData.DefaultSaveDataFileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    // Rootの場合はCurrentPath更新
                    if (thumb.MyItemData.MyThumbType == ThumbType.Root)
                    {
                        MyAppData.CurrentOpenFilePath = dialog.FileName;
                    }
                    return (true, MakeStatusMessage("保存完了"));
                    //MyStatusMessage.Text = MakeStatusMessage("保存完了");
                }
                else
                {
                    return (false, MakeStatusMessage("保存に失敗"));
                    //MyStatusMessage.Text = MakeStatusMessage("保存できなかった");
                }
            }
            else
            {
                return (false, MakeStatusMessage("保存はキャンセルされた"));
                //MyStatusMessage.Text = MakeStatusMessage("保存はキャンセルされた");
            }
        }


        /// <summary>
        /// RootDataをCurrentFileに上書き保存
        /// </summary>
        /// <returns></returns>
        private bool SaveItemOverwriteCurrentFilePath()
        {
            string filePath = MyAppData.CurrentOpenFilePath;
            //CurrentFilePathがないときは、ダイアログ表示して取得
            if (filePath == string.Empty)
            {
                var dialog = MakeSaveFileDialog(MyRoot);
                if (dialog.ShowDialog() == true)
                {
                    filePath = dialog.FileName;
                }
                MyStatusMessage.Text = MakeStatusMessage("保存はキャンセルされた");
                return false;
            }

            //保存
            if (MyRoot.SaveItemData(MyRoot.MyItemData, filePath))
            {
                MyAppData.CurrentOpenFilePath = filePath;
                MyStatusMessage.Text = MakeStatusMessage("保存完了");
                //既定ファイル名の更新
                MyAppData.DefaultSaveDataFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                return true;
            }
            MyStatusMessage.Text = MakeStatusMessage("保存できなかった");
            return false;
        }


        /// <summary>
        /// ItemDataの保存用ダイアログ作成、Rootとそれ以外のDataで拡張子が異なる
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private SaveFileDialog MakeSaveFileDialog(ItemData data)
        {
            SaveFileDialog dialog = new();
            dialog.FileName = MyAppData.DefaultSaveDataFileName;
            if (data.MyThumbType == ThumbType.Root)
            {
                dialog.Filter = "*.px4|*.px4";
                dialog.AddExtension = true;
            }
            else
            {
                dialog.Filter = "*.px4item|*.px4item";
                dialog.AddExtension = true;
            }
            return dialog;
        }
        private SaveFileDialog MakeSaveFileDialog(KisoThumb thumb)
        {
            return MakeSaveFileDialog(thumb.MyItemData);
        }

        #endregion SaveData


        #region その他

        ///// <summary>
        ///// 指定されたItemを画像として複製し、ルートコンテナに追加します。
        ///// </summary>
        ///// <param name="thumb">複製するItem</param>
        ///// <returns><see langword="true"/> の場合、Itemが正常に複製され、ルートコンテナに追加されました。
        ///// それ以外の場合は <see langword="false"/> です。</returns>
        //private bool DupulicateAsImage(KisoThumb? thumb)
        //{
        //    if (MakeBitmapFromThumb(thumb) is RenderTargetBitmap bmp)
        //    {
        //        MyRoot.AddImageThumb(bmp);
        //        return true;
        //    }
        //    return false;
        //}


        /// <summary>
        /// アクティブグループの現在のグリッドサイズを2倍にして、許容範囲内に収まるようにします。
        /// </summary>
        /// <remarks>計算されたグリッドサイズが最小値より小さいか最大値より大きい場合、
        /// 問題を示すメッセージボックスが表示され、グリッドサイズは更新されません。</remarks>
        private void ChangeGridSizeUp()
        {
            int gs = MyRoot.MyActiveGroupThumb.MyItemData.GridSize * 2;
            int min = MyAppData.MinGridSize;
            int max = MyAppData.MaxGridSize;
            if (gs < min)
            {
                MessageBox.Show($"変更できなかった、指定値{gs}、下限値{min}");
            }
            else if (gs > max)
            {
                MessageBox.Show($"変更できなかった、指定値{gs}、上限値{max}");
            }
            else
            {
                MyRoot.MyActiveGroupThumb.MyItemData.GridSize = gs;
            }
        }

        /// <summary>
        /// アクティブグループのグリッドサイズを、次に小さい有効な値に縮小します。
        /// </summary>
        /// <remarks>このメソッドは、現在アクティブなグループのグリッドサイズを、
        /// <see cref="GetYakusuu"/> メソッドによって決定された次に小さい有効な値に設定することで調整します。
        /// 新しいグリッドサイズは、アクティブグループのアイテムデータに適用されます。</remarks>
        private void ChangeGridSizeDown()
        {
            MyRoot.MyActiveGroupThumb.MyItemData.GridSize = GetYakusuu(MyRoot.MyActiveGroupThumb.MyItemData.GridSize);
        }

        /// <summary>
        /// 最大約数の一個下の約数を返す、自然数の100まで対応
        /// </summary>
        /// <remarks>素数で割り切れるかどうかで判定</remarks>
        /// <param name="a"></param>
        /// <returns></returns>
        private int GetYakusuu(int a)
        {
            List<int> sosuu = [2, 3, 5, 7, 11, 13, 17, 19, 23, 19, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97];
            foreach (var item in sosuu)
            {
                if (a % item == 0) { return a / item; }
            }
            return a;
        }

        //最大公約数とは？最大公約数の簡単な求め方を1から解説！ | 明光プラス
        //https://www.meikogijuku.jp/meiko-plus/study/common-divisor.html
        //数学の質問です。19と0の最大公約数は19ですか？０ですか？または違... - Yahoo!知恵袋
        //https://detail.chiebukuro.yahoo.co.jp/qa/question_detail/q14115930874
        /// <summary>
        /// ユークリッドの互除法を用いて、2つの整数の最大公約数 (GCD) を計算します。
        /// </summary>
        /// <remarks>このメソッドは、ユークリッドの互除法の再帰実装を用いて、GCD を計算します。
        /// GCD とは、2つの入力整数を余りなく割り切れる最大の正の整数です。
        /// </remarks>
        /// <param name="a">最初の整数。負でない値である必要があります。</param>
        /// <param name="b">2番目の整数。負でない値である必要があります。</param>
        /// <returns> <paramref name="a"/> と <paramref name="b"/> の最大公約数。両方の値が0の場合、結果は未定義です。</returns>
        private int GetKouyakusuu(int a, int b)
        {
            if (b == 0) { return a; }
            int amari = a % b;
            if (amari == 0) { return b; }
            else
            {
                a = b;
                b = amari;
                return GetKouyakusuu(a, b);
            }
        }

        /// <summary>
        /// 2つの整数の最大公約数（GCD）をユークリッドの互除法で求める
        /// </summary>
        /// <param name="a">1つ目の整数（0以上）</param>
        /// <param name="b">2つ目の整数（0以上）</param>
        /// <returns>aとbの最大公約数（両方0の場合は0）</returns>
        public static int GetGCD_SaidaiKouyakusuu(int a, int b)
        {
            if (a == 0) return b;
            if (b == 0) return a;
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return Math.Abs(a);
        }


        /// <summary>
        /// ユーザー入力に基づいて、アクティブなグループアイテムのグリッドサイズを更新します。
        /// </summary>
        /// <remarks>ユーザーは、-1080～1080 の範囲で新しいグリッドサイズを入力するよう求められます。
        /// 入力した値がこの範囲外の場合、ユーザーは値を確認して適用するかどうかを選択できます。
        /// グリッドサイズを変更すると、新しいサイズが以前のサイズの公倍数または約数でない場合、
        /// アイテムの位置が異なる場合があります。</remarks>
        private void ChangeGridSize()
        {
            ItemData data = MyRoot.MyActiveGroupThumb.MyItemData;
            InputBox box = new(
                "今のサイズの公約数or公倍数以外にするとずれる\n" +
                "例\n" +
                "グリッドサイズ10で50にスナップしているItemがある状態の時に\n" +
                "グリッドサイズを7に変更してから、そのItemをクリックすると56にスナップ(移動)する\n" +
                "\n" +
                "入力できる範囲は基本的には 1 から 1080",

                "ActiveGroupのグリッドサイズの変更",
                data.GridSize.ToString());
            box.Owner = this;
            if (box.ShowDialog() == true && int.TryParse(box.MyTextBox.Text, out var result))
            {
                if (result <= 0)
                {
                    MessageBox.Show($"変更できなかった。指定できる範囲は1以上{MyAppData.MaxGridSize}以下");
                }
                else if (result <= MyAppData.MaxGridSize)
                {
                    data.GridSize = result;
                }
                else
                {
                    MessageBoxResult mbResult =
                        MessageBox.Show(
                            "範囲を超えているけど、それでも実行する？",
                            "確認",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.No);

                    if (mbResult == MessageBoxResult.Yes) { data.GridSize = result; }

                }
            }
        }


        /// <summary>
        /// パネルの表示非表示を切り替える、Visible or Collapsed
        /// </summary>
        /// <param name="panel"></param>
        private void ChangeVisible(Panel panel)
        {
            if (panel.Visibility == Visibility.Visible)
            {
                panel.Visibility = Visibility.Collapsed;
            }
            else { panel.Visibility = Visibility.Visible; }
        }

        /// <summary>
        /// メッセージに現在時刻を付けて返す
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string MakeStatusMessage(string message)
        {
            return MakeStringNowTime() + "_" + message;
        }

        //今の日時をStringで作成
        private string MakeStringNowTime()
        {
            DateTime dt = DateTime.Now;
            //string str = dt.ToString("yyyyMMdd");            
            //string str = dt.ToString("yyyyMMdd" + "_" + "HHmmssfff");
            string str = dt.ToString(DATE_TIME_STRING_FORMAT);
            //string str = dt.ToString("yyyyMMdd" + "_" + "HH" + "_" + "mm" + "_" + "ss" + "_" + "fff");
            return str;
        }

        #endregion その他

        #region Load、Open、読み込み系


        /// <summary>
        /// 対応ファイルを開いて、今のRootに追加する
        /// 対応ファイルは画像ファイルとpx4item、px4でpx4はGroupItemとして追加
        /// </summary>
        private void OpenItemFile()
        {
            OpenFileDialog dialog = new()
            {
                InitialDirectory = MyAppData.InitialDirectory,
                Multiselect = true,
                Filter =
                    "対応ファイル | *.bmp; *.jpg; *.png; *.gif; *.tiff; *.px4item; *.px4;" +
                    "|Pixtack4 | *.px4item; *.px4;" +
                    "|画像系 | *.bmp; *.jpg; *.png; *.gif; *.tiff;" +
                    "|すべて | *.* "
            };

            //ダイアログから開く
            if (dialog.ShowDialog() == true)
            {
                OpenFiles(dialog.FileNames);
            }

        }

        /// <summary>
        /// ファイルパスをソートしてから開く、RootにItemとして追加
        /// </summary>
        /// <param name="paths">ファイルパスリスト</param>
        private void OpenFiles(string[] paths)
        {
            Array.Sort(paths);//ファイル名でソート
            if (MyAppData.IsFileNameDescendingOrder) { Array.Reverse(paths); }
            List<string> errors = [];

            //Item作成して追加、追加できなかったファイルはメッセージボックスで表示
            foreach (var item in paths)
            {
                //開いて追加できたら、既定ファイル名の更新
                if (MyRoot.OpenFile(item))
                {
                    string ext = System.IO.Path.GetExtension(item).Trim('.');
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(item);
                    if (ext == "px4" || ext == "px4item")
                    {
                        //データファイル既定名
                        MyAppData.DefaultSaveDataFileName = fileName + "_";
                    }
                    else
                    {
                        //画像ファイル既定名
                        MyAppData.DefaultSaveImageFileName = fileName + "_";
                    }

                }
                //開けなかったファイル名をリストに追加
                else { errors.Add(System.IO.Path.GetFileName(item)); }
            }
            if (errors.Count > 0)
            {
                ShowMessageBoxStringList(errors, "開くことができなかったファイル一覧");
            }
            //MyRoot.OpenFiles(paths);
        }


        /// <summary>
        /// 文字列リストをメッセージボックスに表示
        /// </summary>
        /// <param name="list"></param>
        public static void ShowMessageBoxStringList(List<string> list, string caption)
        {
            if (list.Count != 0)
            {
                string text = "";
                foreach (var name in list)
                {
                    text += $"{name}\n";
                }
                MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        /// <summary>
        /// px4ファイルを開く
        /// </summary>
        private string OpenPx4File()
        {
            if (MyRoot.MyThumbs.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                   "今の状態を保存してから開く？\n\n\n" +
                   "はい＿：保存してから開く\n\n" +
                   "いいえ：保存しないで開く\n\n" +
                   "キャンセル：開くのををキャンセル",
                   "px4ファイルを開く前の確認",
                   MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.Cancel)
                {
                    return MakeStatusMessage("ファイルを開くのをキャンセルした");
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveItemOverwriteCurrentFilePath();// 上書き保存
                }
            }

            return OpenPx4File2();
        }

        /// <summary>
        /// ダイアログからpx4ファイルを開く
        /// </summary>
        /// <returns></returns>
        private string OpenPx4File2()
        {
            OpenFileDialog dialog = new()
            {
                InitialDirectory = MyAppData.InitialDirectory,
                Multiselect = false,
                Filter = "対応ファイル | *.px4;"
            };
            if (dialog.ShowDialog() == true)
            {
                if (OpenPx4FileRootThumb(dialog.FileName))
                {
                    return MakeStatusMessage("px4ファイルを開いた");
                }
                return MakeStatusMessage("ファイルを開けなかった");
            }
            return MakeStatusMessage("ファイルを開くのをキャンセルした");
        }



        /// <summary>
        /// px4ファイルを開いて、今のRootと入れ替える
        /// </summary>
        private bool OpenPx4FileRootThumb(string filePath)
        {
            if (filePath != string.Empty)
            {
                if (MyRoot.LoadItemData(filePath) is ItemData data)
                {
                    if (new RootThumb(data) is RootThumb root)
                    {
                        MyManageExCanvas.ChangeRootThumb(root);
                        MyRoot = root;
                        MyAppData.CurrentOpenFilePath = filePath;
                        return true;
                    }
                }
            }
            return false;
        }



        /// <summary>
        /// アプリ設定ファイルの読み込み
        /// </summary>
        private AppData? LoadAppData(string filePath)
        {
            //アプリのフォルダから設定ファイルを読み込む、ファイルがなかったら新規作成
            if (ItemDataKiso.Deserialize<AppData>(filePath) is AppData data)
            {
                return data;
            }
            return null;
        }
        private AppData? LoadAppData()
        {
            //アプリのフォルダから設定ファイルを読み込む、ファイルがなかったら新規作成
            string filePath = System.IO.Path.Combine(MyAppDirectory, APP_DATA_FILE_NAME);
            return LoadAppData(filePath);
        }

        #endregion Load、Open、読み込み系

        #endregion メソッド

        #region テスト用
        private void MyRootStatusPanelVisible()
        {
            if (MyRootStatusView.Visibility == Visibility.Visible)
            {
                MyRootStatusView.Visibility = Visibility.Collapsed;
            }
            else { MyRootStatusView.Visibility = Visibility.Visible; }
        }
        #endregion テスト用
    }
}