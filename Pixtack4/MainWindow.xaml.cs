using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

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

        public System.ComponentModel.ICollectionView MyGroupItemView { get; set; } = null!;
        public CollectionViewSource MyCollectionViewOnlyGroupItems { get; set; } = null!;

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
        private const string APP_WINDOW_DATA_FILE_NAME = "Px4AppWindowData.xml";

        //アプリの設定Data
        public AppData MyAppData { get; private set; } = null!;// 確認用でパブリックにしている
        //アプリのDataファイル名
        private const string APP_DATA_FILE_NAME = "Px4AppData.xml";


        //datetime.tostringの書式、これを既定値にする
        private const string DATE_TIME_STRING_FORMAT = "HHmmss";
        //private const string DATE_TIME_STRING_FORMAT = "yyyMMdd'_'HHmmss'.'fff";

        //タブ型右クリックメニュー、メインメニュー
        private ContextTabMenu MyRootContextMenu { get; set; } = new();

        //頂点図形用右クリックメニュー
        private ContextMenu MyShapesContextMenu { get; set; } = new();

        // 頂点追加時に使う、MainGridPanel上での右クリック位置
        private Point MyRightClickDownPoint { get; set; }

        // マウスクリックで図形追加で使う
        private PointCollection MyPoints { get; set; } = new();


        public MainWindow()
        {
            InitializeComponent();

            MyInitialize();
            MyInitialize2();
            PreviewKeyDown += MainWindow_PreviewKeyDown;// 主にホットキーの設定
            DataContext = this;

        }


        #region ホットキー


        //ホットキーの設定、ショートカットキー
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.PageDown) { MyRoot.MyFocusThumb?.ZIndexDown(); }// PageDown：背面
                else if (e.Key == Key.PageUp) { MyRoot.MyFocusThumb?.ZIndexUp(); }// PageUp：前面                
                else if (e.Key == Key.F4) { MyRoot.RemoveSelectedThumbs(); }// F4：削除
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.G) { MyRoot.UngroupFocusThumb(); }// G：グループ解除
                else if (e.Key == Key.F4) { MyRoot.RemoveAll(); }// F4：全削除
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {

            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.S) { SaveRootItemOverwrite(); }// S：Root上書き保存、名前を付けて保存
                else if (e.Key == Key.G) { MyRoot.AddGroupingFromSelected(); }// G：グループ化
                else if (e.Key == Key.D) { MyRoot.Dupulicate(MyRoot.MyFocusThumb); }// D：複製
            }
        }

        #endregion ホットキー


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {

            AppClosing(e);// アプリ終了直前の処理
        }



        #region 初期処理

        private void MyInitialize()
        {
            //アプリのパスとバージョン取得
            MyAppDirectory = Environment.CurrentDirectory;
            MyAppVersion = GetAppVersion();

            //アプリの設定の読み込みと設定
            if (LoadAppData() is AppData appData) { MyAppData = appData; }
            else { MyAppData = new AppData(); }

            // アプリ終了直前の処理
            Closing += MainWindow_Closing;

            // 右クリック位置を記録
            MyMainGridPanel.PreviewMouseRightButtonDown += (a, b) =>
            {
                MyRightClickDownPoint = b.GetPosition(MyMainGridPanel);
            };

            // TextItem入力用テキストボックスクリック時、テキスト全選択
            MyTextBoxAddText.PreviewMouseLeftButtonDown += (a, b) =>
            {
                MyTextBoxAddText.Focus();
                b.Handled = true;
            };


            // マウスクリックでCanvasに直線を描画その2、Polyline、WPFとC# - 午後わてんのブログ
            // https://gogowaten.hatenablog.com/entry/15540488
            // クリックで図形追加用、頂点追加
            MyMainGridCover.PreviewMouseLeftButtonDown += (s, e) => { AddClickPoint(MyMainGridCover, MyPoints, e); };

            // クリックで図形追加用、右クリックで終了
            MyMainGridCover.PreviewMouseRightButtonDown += (a, b) => { AddGeoShapeFromMouseClickEnd(); };

            // マウス移動時
            MyMainGridCover.MouseMove += (s, e) => { LastPointLoacteForPolyline(MyMainGridCover, MyPoints, e); };



            // ベジェ曲線、クリックでアンカー点追加
            MyMainGridCoverBezier.PreviewMouseLeftButtonDown += (s, e) =>
            {
                AddClickPointForBezier(MyMainGridCoverBezier, MyPoints, e);
            };

            // ベジェ曲線、マウス移動時
            MyMainGridCoverBezier.MouseMove += (s, e) =>
            {
                LastPointLocateForBezier(MyMainGridCoverBezier, MyPoints, e, 0.3);
            };

            // ベジェ曲線、右クリックで終了
            MyMainGridCoverBezier.PreviewMouseRightButtonDown += (a, b) =>
            {
                AddGeoShapeBezierFromMouseClickEnd(MyPoints, 0.3);
            };





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
            MyInitializeGeoShapeContextMenu();

            //フォント一覧のコンボボックスの初期化
            InitializeMyComboBoxFont();

            MyGroupItemView = new CollectionViewSource() { Source = MyRoot.MyThumbs }.View;
            MyGroupItemView.Filter = x =>
            {
                var v = (KisoThumb)x;
                return v.MyThumbType == ThumbType.Group;
            };
            MyCollectionViewOnlyGroupItems = new()
            {
                Source = MyRoot.MyThumbs,
                IsLiveFilteringRequested = true
            };
            MyCollectionViewOnlyGroupItems.Filter += MyCollectionViewSource_Filter;
        }

        private void MyCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            var neko = e.Item;
            var tt = (KisoThumb)e.Item;
            if (tt.MyThumbType == ThumbType.Group) { e.Accepted = true; }
            else { e.Accepted = false; }
        }






        #region 右クリックメニュー作成

        /// <summary>
        /// 図形編集時専用右クリックメニュー作成
        /// </summary>
        private void MyInitializeGeoShapeContextMenu()
        {
            MyMainGridPanel.ContextMenuOpening += MyMainGridPanel_ContextMenuOpening;

            MyShapesContextMenu = new ContextMenu();
            MenuItem item;

            item = new() { Header = "ここに頂点追加" }; MyShapesContextMenu.Items.Add(item);
            item.Click += (a, b) => { AddPointFromRightClickPoint(); };

            item = new() { Header = "ここに頂点追加(延長)" }; MyShapesContextMenu.Items.Add(item);
            item.Click += (a, b) => { AddPointEndFromRightClickPoint(); };

            MyShapesContextMenu.Items.Add(new Separator());

            item = new() { Header = "この頂点を削除" }; MyShapesContextMenu.Items.Add(item);
            item.Click += (a, b) => { RemovePoint(); };

            MyShapesContextMenu.Items.Add(new Separator());

            item = new() { Header = "頂点編集終了" }; MyShapesContextMenu.Items.Add(item);
            item.Click += (a, b) =>
            {
                if (MyRoot.MyFocusThumb is GeoShapeThumb2 geo)
                {
                    //図形専用右クリックメニューを外す
                    MyMainGridPanel.ContextMenu = null;
                    geo.IsEditing = false;
                }
            };


        }



        //MainGridPanelの右クリックメニューを開くとき
        private void MyMainGridPanel_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //FocusThumbが編集状態なら、図形専用メニューに切り替えて開く
            if (MyRoot.MyFocusThumb?.IsEditing == true)
            {
                MyMainGridPanel.ContextMenu = MyShapesContextMenu;
                MyShapesContextMenu.IsOpen = true;
            }
            // 非編集状態なら
            else
            {
                MyMainGridPanel.ContextMenu = null;
            }
        }

        /// <summary>
        /// 右クリックメニューの作成
        /// </summary>
        private void MyInitializeMyRootContextMenu()
        {

            MyRootContextMenu = new();
            MyRoot.ContextMenu = MyRootContextMenu;
            MyRootContextMenu.MyTabControl.TabStripPlacement = Dock.Left;


            MyRootContextMenu.AddTabItem(MakeContextMenuTabItem1());// 右クリックメニュータブ1
            MyRootContextMenu.AddTabItem(MakeContextMenuTabItem2());// 右クリックメニュータブ2

            MyRoot.ContextMenuOpening += MyRoot_ContextMenuOpening;
            //MyRootContextMenu.Opened += MyRootContextMenu_Opened;



        }

        /// <summary>
        /// ルート要素の <see cref="FrameworkElement.ContextMenuOpening"/> イベントを処理します。
        /// </summary>
        /// <remarks>このメソッドは、ルート要素内のフォーカスされたサムネイルの状態に基づいて、表示するコンテキストメニューを決定します。フォーカスされたサムネイルが編集モードの場合は、図形固有のコンテキストメニューが表示され、それ以外の場合は、一般的なルートコンテキストメニューが表示されます。</remarks>
        /// <param name="sender">イベントのソース。通常はルート要素です。</param>
        /// <param name="e">カーソル位置など、コンテキストメニューの開きに関する情報を含むイベントデータです。</param>
        ///
        private void MyRoot_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (MyRoot.MyFocusThumb != null)
            {
                if (MyRoot.MyFocusThumb.IsEditing)
                {
                    MyRoot.ContextMenu = null;
                }
                else
                {
                    MyRoot.ContextMenu = MyRootContextMenu;
                    MyRootContextMenu.IsOpen = true;
                }
            }

        }


        /// <summary>
        /// 右クリックメニュータブ1
        /// </summary>
        /// <returns></returns>
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

            MenuItem item;
            item = new() { Header = "頂点編集開始" }; menuPanel.Children.Add(item);
            item.Click += (a, b) =>
            {
                //MainGridPanelの右クリックメニューをGeoShape用に切り替える
                MainGridPanelContextMenuToGeoShapeType();
            };

            item = new() { Header = "複製", InputGestureText = "Ctrl+D" };
            item.Click += (a, b) => { MyRoot.Dupulicate(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(item);

            item = new() { Header = "画像として複製" };
            item.Click += (a, b) => { MyRoot.DupulicateAsImage(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(item);

            item = new() { Header = "名前を付けて保存" };
            item.Click += (a, b) =>
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
            menuPanel.Children.Add(item);

            item = new() { Header = "名前を付けて画像として保存" };
            item.Click += (a, b) => { _ = SaveItemToImageFile(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(item);

            //mItem = new() { Header = "削除" };
            //menuPanel.Children.Add(mItem);
            //mItem.Click += (a, b) => { MyRoot.RemoveThumb(MyRoot.MyFocusThumb); };

            item = new() { InputGestureText = "F4" };
            StackPanel sp = new() { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock() { Text = "削除：選択Item " });
            TextBlock tb = new();
            tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(ObservableCollection<KisoThumb>.Count)) { Source = MyRoot.MySelectedThumbs });
            sp.Children.Add(tb);
            sp.Children.Add(new TextBlock() { Text = " 個を削除" });
            item.Header = sp;
            item.Click += (a, b) => { MyRoot.RemoveSelectedThumbs(); };
            menuPanel.Children.Add(item);

            item = new() { Header = "最前面" };
            menuPanel.Children.Add(item);
            item.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexTop(); };
            item = new() { Header = "前面", InputGestureText = "PageUp" };
            menuPanel.Children.Add(item);
            item.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexUp(); };
            item = new() { Header = "背面", InputGestureText = "PageDown" };
            menuPanel.Children.Add(item);
            item.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexDown(); };
            item = new() { Header = "最背面" };
            item.Click += (a, b) => { MyRoot.MyFocusThumb?.ZIndexBottom(); };
            menuPanel.Children.Add(item);

            item = new() { Header = "Focusを画像としてコピー" };
            item.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyFocusThumb); };
            menuPanel.Children.Add(item);
            item = new() { Header = "Groupを画像としてコピー" };
            item.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyActiveGroupThumb); };
            menuPanel.Children.Add(item);
            item = new() { Header = "Rootを画像としてコピー" };
            item.Click += (a, b) => { CopyAsImageForItem(MyRoot); };
            menuPanel.Children.Add(item);
            item = new() { Header = "Clickedを画像としてコピー" };
            item.Click += (a, b) => { CopyAsImageForItem(MyRoot.MyClickedThumb); };
            menuPanel.Children.Add(item);

            item = new() { Header = "画像を貼り付け(png)" };
            item.Click += (a, b) => { AddImageFromClipboardPng(); };
            menuPanel.Children.Add(item);
            item = new() { Header = "画像を貼り付け(bmp)" };
            item.Click += (a, b) => { AddImageFromClipboardBmp(); };
            menuPanel.Children.Add(item);

            item = new() { Header = "グループ化", InputGestureText = "Ctrl+G" };
            item.Click += (a, b) => { MyRoot.AddGroupingFromSelected(); };
            menuPanel.Children.Add(item);
            item = new() { Header = "グループ解除", InputGestureText = "Ctrl+Shift+G" };
            item.Click += (a, b) => { MyRoot.UngroupFocusThumb(); };
            menuPanel.Children.Add(item);

            return tabItem;
        }

        /// <summary>
        /// 右クリックメニュータブ2
        /// </summary>
        /// <returns></returns>
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

            Button btn = new() { Content = "GridSizeUp" };
            btn.Click += (a, b) => { ChangeGridSizeUp(); };
            menuStackPanel.Children.Add(btn);
            btn = new() { Content = "GridSizeDown" };
            btn.Click += (a, b) => { ChangeGridSizeDown(); };
            menuStackPanel.Children.Add(btn);
            item = new() { Header = "GridSize指定" };
            item.Click += (a, b) => { ChangeGridSize(); };
            menuStackPanel.Children.Add(item);



            return tabItem;
        }

        #endregion 右クリックメニュー作成

        private void MyBind()
        {
            //今開いているファイル名をステータスバーに表示
            MyStatusCurrentFileName.SetBinding(TextBlock.TextProperty, new Binding(nameof(MyAppData.CurrentOpenFilePath)) { Source = MyAppData, Converter = new MyConvPathFileName() });
        }



        /// <summary>
        /// コンボボックスの初期化
        /// </summary>
        private void InitializeMyComboBoxFont()
        {
            Dictionary<string, Brush> brushDictionary = MakeBrushesDictionary();

            //Font
            ComboBoxTextBackColor.ItemsSource = brushDictionary;
            ComboBoxTextForeColor.ItemsSource = brushDictionary;
            if (MyAppData.FontNameList == null || MyAppData.FontNameList.Count == 0)
            {
                RenewAppDataFontList();// アプリの設定のフォントリストを更新する
            }


            //FontWeight
            Dictionary<string, object> dict = MakePropertyDictionary(typeof(FontWeights));
            dict.Add("default", this.FontWeight);
            MyComboBoxFontWeight.ItemsSource = dict;

            //ShapeFill、基本図形塗りつぶし
            ComboBoxShapeFill.ItemsSource = brushDictionary;
            ComboBoxShapeStrokeColor.ItemsSource = brushDictionary;

            //直線図形の色
            ComboBoxGeoShapeStrokeColor.ItemsSource = brushDictionary;
            ComboBoxGeoShapeStrokeColor.SelectedIndex = 12;

            //始端形状
            ComboBoxGeoShapeStartCapType.ItemsSource = Enum.GetValues(typeof(HeadType));
            ComboBoxGeoShapeStartCapType.SelectedValue = MyAppData.GeoShapeEndHeadType;

            //終端形状
            ComboBoxGeoShapeEndCapType.ItemsSource = Enum.GetValues(typeof(HeadType));
            ComboBoxGeoShapeEndCapType.SelectedValue = MyAppData.GeoShapeEndHeadType;

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


        #endregion 初期処理








        #region ボタンクリック        
        //確認テスト用

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PGeoShape free = MyFreehandGrid.MyListOfPGeoShape[0];
            var po = free.MyPoints;
            var geo = free.MyGeoShape.MyPoints;
            var ori = free.MyOriginPoints;
            var neko = MyPoints;
            var inu = MyCollectionViewOnlyGroupItems;
        }



        private void Button_Click_(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_ButtonAddGeoShapeFreehandEnd(object sender, RoutedEventArgs e)
        {
            AddGeoShapeFreehandEnd();
        }

        private void AddGeoShapeFreehandEnd()
        {
            // フリーハンド終了
            MyScrollViewer.IsEnabled = true;

            // グループとして追加、1個だけのときはGeoShapeItem作成追加
            AddFreehandShape(MyFreehandGrid.MyListOfPGeoShape);

            MyFreehandGrid.Visibility = Visibility.Collapsed;
            MyFreehandGrid.DrawClear();


            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeLine.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeBezier.IsEnabled = true;
            ButtonAddGeoShapeMouseFreehandBegin.IsEnabled = true;
            ButtonAddGeoShapeMouseFreehandEnd.IsEnabled = false;
        }

        private void Button_Click_ButtonAddGeoShapeFreehandBegin(object sender, RoutedEventArgs e)
        {
            AddGeoShapeFreehandBegin();
        }

        private void AddGeoShapeFreehandBegin()
        {
            //// フリーハンド開始
            MyScrollViewer.IsEnabled = false;
            //MyMainGridCoverFreehand.Visibility = Visibility.Visible;
            MyFreehandGrid.Visibility = Visibility.Visible;

            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeLine.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeBezier.IsEnabled = false;
            ButtonAddGeoShapeMouseFreehandBegin.IsEnabled = false;
            ButtonAddGeoShapeMouseFreehandEnd.IsEnabled = true;// 終了ボタンだけ有効化

        }



        #region 完成

        // 曲線新規追加終了
        private void Button_Click_AddGeoShapeBezierFromMouseClickEnd(object sender, RoutedEventArgs e)
        {
            // 確定していない終端の3つを削除
            MyPoints.Remove(MyPoints[^1]);
            MyPoints.Remove(MyPoints[^1]);
            MyPoints.Remove(MyPoints[^1]);

            // 終点の制御点を設定
            SetBezierEndControlPoint(MyPoints, 0.3);

            AddGeoShapeBezierFromMouseClickEnd(MyPoints, 0.3);
        }

        private void Button_Click_AddGeoShapeBezierFromMouseClickBegin(object sender, RoutedEventArgs e)
        {
            AddGeoShapeBezierFromMouseClickBegin();// 曲線新規追加開始、クリックで頂点追加
        }


        private void Button_Click_AddGeoShapeBezierItem(object sender, RoutedEventArgs e)
        {
            AddGeoShapeBezierItem();// 曲線図形Itemの追加
        }

        private void Button_Click_AddGeoShapeFromMouseClickEnd(object sender, RoutedEventArgs e)
        {
            // 直線新規追加終了
            AddGeoShapeFromMouseClickEnd(isRemoveEndPoint: true);
        }

        private void Button_Click_AddGeoShapeFromMouseClick(object sender, RoutedEventArgs e)
        {
            AddGeoShapeFromMouseClick();// 直線新規追加開始、クリックで頂点追加
        }


        private void Button_Click_AddGeoShapeLineItem(object sender, RoutedEventArgs e)
        {
            AddGeoShapeLineItem();// 直線図形Itemの追加
        }

        private void Button_Click_AddEllipseItem(object sender, RoutedEventArgs e)
        {
            AddEllipseItem();// 楕円形Itemの追加
        }

        private void Button_Click_AddRectItem(object sender, RoutedEventArgs e)
        {
            AddRectItem();// RectangleThumbの追加
        }

        private void Button_Click_AddTextBlockItem(object sender, RoutedEventArgs e)
        {
            AddTextBlockItem();// MainWindowからTextBlockItemをMyRootに追加する
        }

        private void Button_Click_RenewFontList(object sender, RoutedEventArgs e)
        {
            RenewAppDataFontList();// アプリの設定のフォントリストを更新する
        }

        private void Button_Click_AreaItemVisibleSwitch(object sender, RoutedEventArgs e)
        {
            MyManageExCanvas.AreaThumbVisibleSwitch();// 範囲選択Itemの表示非表示、
        }

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
            SaveRootItemOverwrite();// 上書き保存
        }

        private void Button_Click_ResetWindow(object sender, RoutedEventArgs e)
        {
            ResetWindowState();// ウィンドウの位置とサイズをリセット
        }
        #endregion 完成

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

        #region ドラッグ移動で図形描画

        #region ベジェ曲線

        private void LastPointLocateForBezier(Panel panel, PointCollection pc, MouseEventArgs e, double mage)
        {
            if (pc.Count > 0)
            {
                Point ima = GetIntPosition(e, panel);
                pc[^1] = ima;
                MouseMoveBezier(pc, ima, mage);
            }
        }

        private void AddClickPointForBezier(Panel panel, PointCollection pc, MouseButtonEventArgs e)
        {
            var po = GetIntPosition(e, panel);
            if (pc.Count == 0) { pc.Add(po); }
            pc.Add(po); pc.Add(po); pc.Add(po);
        }

        #endregion ベジェ曲線

        #region 直線

        /// <summary>
        /// PointCollectionの最後のポイントをマウスに合わせる
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="pc"></param>
        /// <param name="e"></param>
        private void LastPointLoacteForPolyline(Panel panel, PointCollection pc, MouseEventArgs e)
        {
            if (pc.Count > 0)
            {
                var po = GetIntPosition(e, panel);
                int i = pc.Count - 1;
                pc[i] = po;
            }
        }

        /// <summary>
        /// クリック位置をPointCollectionに追加、クリックでPolyline用
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="e"></param>
        private void AddClickPoint(Panel panel, PointCollection pc, MouseEventArgs e)
        {
            // クリックで図形追加用、頂点追加
            var po = GetIntPosition(e, panel);
            if (pc.Count == 0) { pc.Add(po); }
            pc.Add(po);
        }
        #endregion 直線


        #endregion ドラッグ移動で図形描画

        #region GeoShapeItem関連


        // FreehandPolyline
        private Polyline MakeFreehandPolyline()
        {
            var poliline = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 10,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
            };
            if (ComboBoxGeoShapeStrokeColor.SelectedValue is SolidColorBrush brush)
            {
                poliline.Stroke = brush;
            }
            return poliline;
        }


        private void RemovePoint()
        {
            if (MyRoot.MyFocusThumb is GeoShapeThumb2 geo)
            {
                geo.RemovePoint(geo.MyDragMovePointIndex);
            }
        }

        // 右クリックした座標にPointを追加する、GeoShapeのPointsの適切なインデックスにPointを追加する
        private void AddPointFromRightClickPoint()
        {
            if (MyRoot.MyFocusThumb is GeoShapeThumb2 geo)
            {
                int insertIndex = 0;// 求める適切なインデックス
                double maxAngle = 0.0;// 最大角度、これが180に一番近いものを選択
                Point migi = GetPointRightClickAtGeoShape(geo);// 右クリック座標
                PointCollection pc = geo.MyItemData.GeoShapeItemData.MyPoints;

                // Pointが1個のときは末尾に追加して終わり
                if (pc.Count == 1) { geo.AddPoint(migi); return; }

                // すべてのPointと右クリック座標を中心とする角度を比較
                for (int i = 0; i < pc.Count - 1; i++)
                {
                    Point front = pc[i];
                    Point rear = pc[i + 1];

                    // abc3点からできる∠abcの角度を取得、最大角度になるPointのIndexを取得
                    double angle = CalculateAngleABC(front, migi, rear);
                    if (angle > maxAngle)
                    {
                        maxAngle = angle;
                        insertIndex = i + 1;
                    }
                }
                geo.AddPoint(migi, insertIndex);
            }
        }

        /// <summary>
        /// GeoShapeItemから見た、右クリックの座標を返す、
        /// MainGridPanelの右クリックメニュー専用、頂点追加するときに使う
        /// </summary>
        /// <param name="geo"></param>
        /// <returns></returns>
        private Point GetPointRightClickAtGeoShape(GeoShapeThumb2 geo)
        {
            // MainGridPanel上での右クリック位置 - GeoShapeItemの位置 - ハンドルサイズ / 2
            var x = MyRightClickDownPoint.X - geo.MyItemData.MyLeft - MyAppData.GeoShapeHandleSize / 2.0;
            var y = MyRightClickDownPoint.Y - geo.MyItemData.MyTop - MyAppData.GeoShapeHandleSize / 2.0;
            // GeoShapeのPointsの左上座標を考慮
            var (left, top) = GetTopLeftFromPoints(geo.MyItemData.GeoShapeItemData.MyPoints);
            x += left;
            y += top;
            // 四捨五入
            x = (int)(x + 0.5);
            y = (int)(y + 0.5);
            return new Point(x, y);
        }

        // 右クリックしたところに頂点を追加
        // 始点、終点で近い方に延長追加する
        private void AddPointEndFromRightClickPoint()
        {
            if (MyRoot.MyFocusThumb is GeoShapeThumb2 geo)
            {
                var migi = GetPointRightClickAtGeoShape(geo);
                PointCollection pc = geo.MyItemData.GeoShapeItemData.MyPoints;

                // 右クリック地点との距離、始点 > 終点なら
                if (EuclideanDistance(migi, pc[0]) > EuclideanDistance(migi, pc[^1]))
                {
                    geo.AddPoint(migi, pc.Count);// 末尾に追加
                }
                else
                {
                    geo.AddPoint(migi, 0);// 先頭に追加
                }
            }
        }


        /// <summary>
        /// MainGridPanelの右クリックメニューをGeoShape用に切り替える
        /// </summary>
        private void MainGridPanelContextMenuToGeoShapeType()
        {
            //MainGridPanelの右クリックメニューをGeoShape用に切り替える
            if (MyRoot.MyFocusThumb is GeoShapeThumb2 geo)
            {
                geo.IsEditing = true;
                MyMainGridPanel.ContextMenu = MyShapesContextMenu;
                GeoShapeAnchorHandleSizeBindAppData(geo);// GeoShapeのアンカーハンドルのサイズをアプリの設定にバインド
            }
        }

        /// <summary>
        /// GeoShapeのアンカーハンドルのサイズをアプリの設定にバインド
        /// </summary>
        /// <param name="geo"></param>
        private void GeoShapeAnchorHandleSizeBindAppData(GeoShapeThumb2 geo)
        {
            geo.MyAnchorHandleAdorner?.SetBinding(AnchorHandleAdorner.MyAnchorHandleSizeProperty, new Binding(nameof(AppData.GeoShapeHandleSize)) { Source = MyAppData, Mode = BindingMode.TwoWay });
            //サイズと位置の更新
            geo.UpdateLocateAndSize();
            geo.MyParentThumb?.ReLayout3();
        }
        #endregion GeoShapeItem関連


        #region Item追加

        // フリーハンドでの図形を追加
        // 複数図形ならグループ化して追加
        private void AddFreehandShape(List<PGeoShape> geoShapeList)
        {
            if (geoShapeList.Count == 0) { return; }

            // 全Pointsの左上座標を取得
            double x = double.MaxValue; double y = double.MaxValue;
            for (int i = 0; i < geoShapeList.Count; i++)
            {
                var item = geoShapeList[i];
                var (left, top) = GetTopLeftFromPoints(item.MyPoints);
                if (x > left) { x = left; }
                if (y > top) { y = top; }
            }

            // 全体を左上に寄せる
            for (int i = 0; i < geoShapeList.Count; i++)
            {
                PointCollection pc = geoShapeList[i].MyPoints;
                for (int j = 0; j < pc.Count; j++)
                {
                    Point po = pc[j];
                    pc[j] = new Point(po.X - x, po.Y - y);
                }
            }

            GroupThumb activeGroup = MyRoot.MyActiveGroupThumb;// 追加先のグループ
            double scrollX = MyScrollViewer.HorizontalOffset;// スクロール位置X
            double scrollY = MyScrollViewer.VerticalOffset;// スクロール位置Y

            // 複数図形ならグループ化で追加
            if (geoShapeList.Count >= 2)
            {
                // グループItem作成、座標は全左上にスクロール位置を加算
                // 追加先のグループの座標を引き算
                ItemData groupData = new(ThumbType.Group)
                {
                    MyLeft = x + scrollX - activeGroup.MyItemData.MyLeft,
                    MyTop = y + scrollY - activeGroup.MyItemData.MyTop,
                };
                GroupThumb group = new(groupData);

                // 各図形作成、座標は各Pointsの左上にする
                for (int i = 0; i < geoShapeList.Count; i++)
                {
                    // 各座標、Pointsの左上を取得
                    var (left, top) = GetTopLeftFromPoints(geoShapeList[i].MyPoints);
                    // ItemData作成
                    ItemData geoData = new(ThumbType.GeoShape)
                    {
                        MyLeft = left,
                        MyTop = top,
                        MyZIndex = i,
                        GeoShapeItemData = SubMakeGeoShapeItemData(geoShapeList[i].MyPoints)
                    };

                    GeoShapeThumb2 geoshapeitem = new(geoData);
                    group.MyThumbs.Add(geoshapeitem);
                }
                MyRoot.AddThumb(group, activeGroup, true);
            }
            // 単体を追加
            else if (geoShapeList.Count == 1)
            {
                var (left, top) = GetTopLeftFromPoints(geoShapeList[0].MyPoints);
                // ItemData作成
                ItemData geoData = new(ThumbType.GeoShape)
                {
                    MyLeft = x + left + scrollX - activeGroup.MyItemData.MyLeft,
                    MyTop = y + top + scrollY - activeGroup.MyItemData.MyTop,
                    GeoShapeItemData = SubMakeGeoShapeItemData(geoShapeList[0].MyPoints)
                };
                GeoShapeThumb2 geoShapeThumb2 = new(geoData);
                MyRoot.AddThumb(geoShapeThumb2, activeGroup, true);
            }

            // GeoShapeItemData作成
            GeoShapeItemData SubMakeGeoShapeItemData(PointCollection pc)
            {
                return new GeoShapeItemData()
                {
                    MyShapeType = ShapeType.Bezier,
                    MyPoints = pc.Clone(),
                    MyStroke = (Brush)ComboBoxGeoShapeStrokeColor.SelectedValue,
                    MyStrokeThickness = MyAppData.GeoShapeStrokeThickness,
                    MyGeoShapeHeadEndCapType = MyAppData.GeoShapeEndHeadType,
                    MyGeoShapeHeadBeginCapType = MyAppData.GeoShapeStartHeadType,
                };
            }

        }



        // 曲線図形新規追加開始、クリックで頂点追加
        private void AddGeoShapeBezierFromMouseClickBegin()
        {
            MyScrollViewer.IsEnabled = false;
            MyMainGridCoverBezier.Visibility = Visibility.Visible;

            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeLine.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = true;// 終了ボタンだけ有効化
            ButtonAddGeoShapeBezier.IsEnabled = false;

            MyPoints = [];
            //MyTempBezierline.Points = MyPoints;
            MyTempBezier.MyPoints = MyPoints;
        }

        // 曲線追加終了
        private void AddGeoShapeBezierFromMouseClickEnd(PointCollection pc, double magari)
        {
            MyScrollViewer.IsEnabled = true;
            MyMainGridCoverBezier.Visibility = Visibility.Collapsed;

            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeLine.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeBezier.IsEnabled = true;

            if (MyPoints.Count >= 4)
            {
                ItemData data = MakeGeoShapeLineItemData(MyPoints, ShapeType.Bezier);
                _ = MyRoot.AddNewThumbFromItemData(data, MyRoot.MyActiveGroupThumb, true);
            }
        }


        // 直線図形新規追加開始、クリックで頂点追加
        private void AddGeoShapeFromMouseClick()
        {
            MyScrollViewer.IsEnabled = false;// スクロール無効
            MyMainGridCover.Visibility = Visibility.Visible;// 直線描画用パネル表示
            MyPoints = [];
            MyTempPolyline.Points = MyPoints;

            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = true;
            ButtonAddGeoShapeLine.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = false;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeBezier.IsEnabled = false;
        }

        // 直線図形新規追加終了、クリック箇所が2個以上なら図形追加
        /// <summary>
        /// ユーザーのマウスクリックに基づいて、幾何学的な直線図形の追加を完了します。
        /// </summary>
        /// <remarks>このメソッドは、スクロールビューアを有効にし、メインのグリッドカバーを非表示にします。ユーザーが少なくとも2つの点をクリックした場合、新しい直線図形が作成され、アクティブなグループに追加されます。</remarks>
        /// <param name="isRemoveEndPoint">図形の端点を削除するかどうかを示します。<see langword="true"/> の場合、端点は図形から除外されます。それ以外の場合は、端点は図形に含まれます。</param>
        ///
        private void AddGeoShapeFromMouseClickEnd(bool isRemoveEndPoint = false)
        {
            MyScrollViewer.IsEnabled = true;
            MyMainGridCover.Visibility = Visibility.Collapsed;

            // ボタン有効化制御
            ButtonAddGeoShapeLineFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeLineFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeLine.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickBegin.IsEnabled = true;
            ButtonAddGeoShapeBezierFromClickEnd.IsEnabled = false;
            ButtonAddGeoShapeBezier.IsEnabled = true;

            if (MyPoints.Count <= 0) return;
            if (isRemoveEndPoint) { MyPoints.RemoveAt(MyPoints.Count - 1); }
            if (MyPoints.Count >= 2)
            {
                ItemData data = MakeGeoShapeLineItemData(MyPoints, ShapeType.Line);
                _ = MyRoot.AddNewThumbFromItemData(data, MyRoot.MyActiveGroupThumb, true);
            }
        }



        /// <summary>
        /// 曲線図形Itemの追加
        /// </summary>
        /// <returns></returns>
        private bool AddGeoShapeBezierItem()
        {
            PointCollection pc = [new Point(), new Point(100, 0), new Point(100, 100), new Point(0, 100)];
            return MyRoot.AddNewThumbFromItemData(MakeGeoShapeLineItemData(pc, ShapeType.Bezier));
        }

        /// <summary>
        /// 直線図形Itemの追加
        /// </summary>
        /// <returns></returns>
        private bool AddGeoShapeLineItem()
        {
            PointCollection pc = [new Point(), new Point(100, 100)];
            return MyRoot.AddNewThumbFromItemData(MakeGeoShapeLineItemData(pc, ShapeType.Line));
        }


        /// <summary>
        /// 線図形のItemDataを作成
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        private ItemData MakeGeoShapeLineItemData(PointCollection pc, ShapeType shapeType)
        {
            ItemData data = new(ThumbType.GeoShape);
            var (left, top) = ZeroFixPointCollection(pc);// Point全体を左上に寄せる
            left += MyScrollViewer.HorizontalOffset;// スクロール位置を考慮
            top += MyScrollViewer.VerticalOffset;
            data.MyLeft = left;
            data.MyTop = top;
            data.GeoShapeItemData.MyPoints = pc;
            data.GeoShapeItemData.MyShapeType = shapeType;
            //data.GeoShapeItemData.MyShapeType = ShapeType.Line;

            // 終端形状
            data.GeoShapeItemData.MyGeoShapeHeadEndCapType = HeadType.None;
            if (ComboBoxGeoShapeEndCapType.SelectedValue is HeadType head)
            {
                data.GeoShapeItemData.MyGeoShapeHeadEndCapType = head;
            }

            // 始端形状
            data.GeoShapeItemData.MyGeoShapeHeadBeginCapType = HeadType.None;
            if (ComboBoxGeoShapeStartCapType.SelectedValue is HeadType startHead)
            {
                data.GeoShapeItemData.MyGeoShapeHeadBeginCapType = startHead;
            }

            data.GeoShapeItemData.MyStrokeThickness = MyAppData.GeoShapeStrokeThickness;
            data.GeoShapeItemData.MyStroke = Brushes.Maroon;
            if (ComboBoxGeoShapeStrokeColor.SelectedValue is Brush b)
            {
                data.GeoShapeItemData.MyStroke = b;
            }

            // 位置の調整、線の太さを考慮した位置を取得したいので
            // 実際に図形を作成してBoundsを取得、それを実際の位置に追加
            GeoShape geo = new();
            geo.MyPoints = pc;
            geo.StrokeThickness = SliderGeoShapeStrokeThickness.Value;
            geo.MyHeadEndType = MyAppData.GeoShapeEndHeadType;
            geo.MyHeadBeginType = MyAppData.GeoShapeStartHeadType;
            var bounds = geo.GetRenderBounds();
            data.MyLeft += bounds.X;
            data.MyTop += bounds.Y;

            return data;
        }

        /// <summary>
        /// 楕円形Itemの追加
        /// </summary>
        /// <returns></returns>
        private bool AddEllipseItem()
        {
            ItemData data = new(ThumbType.Ellipse)
            {
                MyWidth = SliderShapeWidht.Value,
                MyHeight = SliderShapeHeight.Value,
            };
            data.ShapeItemData.MyFill = Brushes.Tomato;
            if (ComboBoxShapeFill.SelectedValue is Brush bb) { data.ShapeItemData.MyFill = bb; }
            data.ShapeItemData.WakuColor = Brushes.Transparent;
            if (ComboBoxShapeStrokeColor.SelectedValue is Brush wakuBrush)
            {
                data.ShapeItemData.WakuColor = wakuBrush;
            }
            data.ShapeItemData.StrokeThickness = SliderShapeStrokeThickness.Value;

            return MyRoot.AddNewThumbFromItemData(data);
        }



        /// <summary>
        /// RectangleThumbの追加
        /// </summary>
        /// <returns></returns>
        private bool AddRectItem()
        {
            ItemData data = new(ThumbType.Rect)
            {
                MyWidth = SliderShapeWidht.Value,
                MyHeight = SliderShapeHeight.Value
            };
            data.ShapeItemData.MyFill = Brushes.Tomato;
            if (ComboBoxShapeFill.SelectedValue is Brush bb) { data.ShapeItemData.MyFill = bb; }
            data.ShapeItemData.WakuColor = Brushes.Transparent;
            if (ComboBoxShapeStrokeColor.SelectedValue is Brush wakuBrush)
            {
                data.ShapeItemData.WakuColor = wakuBrush;
            }
            data.ShapeItemData.StrokeThickness = SliderShapeStrokeThickness.Value;
            data.ShapeItemData.RoundnessRadius = SliderShapeRectRoundnessRadius.Value;

            return MyRoot.AddNewThumbFromItemData(data);
        }

        /// <summary>
        /// MainWindowからTextBlockItemをMyRootに追加する
        /// </summary>
        private void AddTextBlockItem()
        {
            if (MyTextBoxAddText.Text == string.Empty) { return; }
            ItemData data = new(ThumbType.Text);
            data.TextItemData.MyText = MyTextBoxAddText.Text;
            data.TextItemData.MyFontSize = MySliderFontSize.Value;
            //フォント名
            //ComboBoxから取得できないときは規定のフォント
            data.TextItemData.FontName = FontFamily.Source;
            if (MyComboBoxFont.SelectedValue is string str && str.Length != 0)
            {
                data.TextItemData.FontName = str;
            }

            //FontWeight
            if (MyComboBoxFontWeight.SelectedValue is FontWeight fw)
            {
                data.TextItemData.FontWeight = fw.ToString();
            }
            else { data.TextItemData.FontWeight = this.FontWeight.ToString(); }

            //文字色と背景色
            data.MyForeground = Brushes.Black;
            data.MyBackground = Brushes.Transparent;
            if (ComboBoxTextForeColor.SelectedValue is Brush b) { data.MyForeground = b; }
            if (ComboBoxTextBackColor.SelectedValue is Brush bb) { data.MyBackground = bb; }

            MyRoot.AddNewThumbFromItemData(data);
        }

        #endregion Item追加

        #region 初期化、リセット系

        /// <summary>
        /// アプリの設定のフォントリストを更新する
        /// </summary>
        private void RenewAppDataFontList()
        {
            var ar = GetFontNames();
            List<string> ll = ar.ToList();
            ll.Insert(0, string.Empty);// 戦闘に空白をいれる、規定のフォント指定用
            MyAppData.FontNameList = ll;

            MyAppData.FontComboBoxSelectedIndex = 0;
        }

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

            //リセット処理
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
            _ = MyRoot.SetBinding(KisoThumb.IsWakuVisibleProperty, new Binding(nameof(AppData.IsWakuVisible)) { Source = MyAppData });
            MyManageExCanvas = new ManageExCanvas(MyRoot, new ManageData(), this);
            MyScrollViewer.Content = MyManageExCanvas;

            //FocusThumbが変更されたとき
            //MainGridPanelの右クリックメニューをnullにする
            MyRoot.MyFocusThumbChenged += (arata, old) =>
            {
                if (old is GeoShapeThumb2 geo)
                {
                    MyMainGridPanel.ContextMenu = null;
                }
            };

            //MyRoot.PreviewMouseRightButtonUp += MyRoot_PreviewMouseRightButtonUp;
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
                    SaveRootItemOverwrite();// 上書き保存
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
                }
                else
                {
                    return (false, MakeStatusMessage("保存に失敗"));
                }
            }
            else
            {
                return (false, MakeStatusMessage("保存はキャンセルされた"));
            }
        }


        /// <summary>
        /// RootDataをCurrentFileに上書き保存
        /// </summary>
        /// <returns></returns>
        private bool SaveRootItemOverwrite()
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
                else
                {
                    MyStatusMessage.Text = MakeStatusMessage("保存はキャンセルされた");
                    return false;
                }
            }

            //保存
            if (MyRoot.SaveItemData(MyRoot.MyItemData, filePath))
            {
                MyStatusMessage.Text = MakeStatusMessage("保存完了");
                //上書き保存のファイルパスと既定ファイル名の更新
                MyAppData.CurrentOpenFilePath = filePath;
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

        // WPF、ベジェ曲線、違和感なく滑らかになるような制御点座標はどこ？その3(終) - 午後わてんのブログ
        //        https://gogowaten.hatenablog.com/entry/15735391
        // マウス移動中ベジェ曲線
        private void MouseMoveBezier(PointCollection pc, Point mousePoint, double magari)
        {
            if (pc.Count < 7 || (pc.Count - 1) % 3 != 0) { return; }

            //終点の一個手前のアンカーの制御点を設定
            Point beginP = pc[^7];// 前の前のアンカーポイント
            Point middleP = pc[^4];// 前のアンカーポイント、これの制御点を設定

            (double begin, double end) = GeoShape.DistanceSeparate(beginP, middleP, mousePoint);
            (double beginSide, double endSide) = GetRadianDirectionLine(beginP, middleP, mousePoint);

            //始点側制御点座標
            double xDiff = Math.Cos(beginSide) * begin * magari;
            double yDiff = Math.Sin(beginSide) * begin * magari;
            pc[^5] = new Point(middleP.X + xDiff, middleP.Y + yDiff);// [2],[5],[8]

            //終点側制御点座標
            xDiff = Math.Cos(endSide) * end * magari;
            yDiff = Math.Sin(endSide) * end * magari;
            pc[^3] = new Point(middleP.X + xDiff, middleP.Y + yDiff);// [4], [7], [10]

            // 終点の制御点を設定
            SetBezierEndControlPoint(pc, magari);

            // 始点の制御点を設定
            if (pc.Count == 7)
            {
                xDiff = magari * (pc[0].X - pc[2].X);
                yDiff = magari * (pc[0].Y - pc[2].Y);
                pc[1] = new Point(pc[0].X - xDiff, pc[0].Y - yDiff);
            }
        }

        // ベジェ曲線の終点の制御点を設定する
        private static void SetBezierEndControlPoint(PointCollection pc, double magari)
        {
            pc[^2] = new Point(
                pc[^1].X - (magari * (pc[^1].X - pc[^3].X)),
                pc[^1].Y - (magari * (pc[^1].Y - pc[^3].Y)));
        }

        //制御点座標を決めて曲線化
        /// <summary>
        /// 制御点の座標を調整することで、点のコレクションを曲線に変換します。
        /// </summary>
        /// <remarks>このメソッドは、曲率係数 (<paramref name="magari"/>) とアンカーポイントの相対位置に基づいて制御点を再計算することにより、入力された <paramref name="pc"/> を直接変更します。入力コレクションが、アンカーポイントと制御点の想定される構造に従っていることを確認してください。</remarks>
        /// <param name="pc">変更する点のコレクション。このコレクションには、アンカーポイントと制御点が特定の順序で含まれている必要があります。</param>
        /// <param name="magari">制御点の曲率を決定する乗数。値が大きいほど、曲線は急になります。</param>
        /// <param name="isAll">コレクション内のすべての点を処理するかどうかを示すブール値。 <see langword="true"/> の場合、
        /// すべてのポイントが処理されます。それ以外の場合は、最後のセグメントのみが処理されます。</param>
        ///
        private bool ToCurveTypeC(PointCollection pc, double magari, bool isAll)
        {
            if (pc.Count < 4 || pc.Count - 1 % 3 != 0) { return false; }

            int beginCount = 3;
            if (!isAll) { beginCount = pc.Count - 3; }

            for (int i = beginCount; i < pc.Count - 1; i += 3)
            {
                Point beginP = pc[i - 3];// 始点側のアンカーポイント
                Point middleP = pc[i];// 中間のアンカーポイント
                Point endP = pc[i + 3];// 終点側のアンカーポイント
                //前後の方向線長さ取得
                double beginDistance;
                double endDistance;
                (beginDistance, endDistance) = GeoShape.DistanceSeparate(beginP, middleP, endP);

                //方向線弧度取得
                (double bRadian, double eRadian) = GetRadianDirectionLine(beginP, middleP, endP);

                //始点側制御点座標
                double xDiff = Math.Cos(bRadian) * beginDistance * magari;
                double yDiff = Math.Sin(bRadian) * beginDistance * magari;
                pc[i - 1] = new Point(middleP.X + xDiff, middleP.Y + yDiff);// [2],[5],[8]

                //終点側制御点座標
                xDiff = Math.Cos(eRadian) * endDistance * magari;
                yDiff = Math.Sin(eRadian) * endDistance * magari;
                pc[i + 1] = new Point(middleP.X + xDiff, middleP.Y + yDiff);// [4], [7], [10]
            }


            return true;
        }

        /// <summary>
        /// 現在アンカー点とその前後のアンカー点それぞれの中間弧度に直角な弧度を計算
        /// </summary>
        /// <param name="beginP">始点側アンカー点</param>
        /// <param name="currentP">現在アンカー点</param>
        /// <param name="endP">終点側アンカー点</param>
        /// <returns>始点側方向線弧度、終点側方向線弧度</returns>
        private (double beginSide, double endSide) GetRadianDirectionLine(Point beginP, Point currentP, Point endP)
        {
            //ラジアン(角度)
            double bRadian = GeoShape.GetRadianFrom2Points(currentP, beginP);//現在から始点側
            double eRadian = GeoShape.GetRadianFrom2Points(currentP, endP);//現在から終点側
            double midRadian = (bRadian + eRadian) / 2.0;//中間角度

            //中間角度に直角なのは90度を足した右回りと、90を引いた左回りがある
            //始点側、終点側角度を比較して大きい方が、右回りの方向線角度になる
            double bControlRadian, eControlRadian;
            if (bRadian > eRadian)
            {
                bControlRadian = midRadian + (Math.PI / 2.0);
                eControlRadian = midRadian - (Math.PI / 2.0);
            }
            else
            {
                bControlRadian = midRadian - (Math.PI / 2.0);
                eControlRadian = midRadian + (Math.PI / 2.0);
            }

            return (bControlRadian, eControlRadian);
        }


        /// <summary>
        /// 指定された入力要素を基準としたマウスポインターの相対的な整数位置を計算します。
        /// </summary>
        /// <remarks>このメソッドは、マウス位置のX座標とY座標を四捨五入して整数に丸めます。</remarks>
        /// <param name="e">マウスイベントデータを含む<see cref="MouseEventArgs"/>。</param>
        /// <param name="element">マウス位置の計算の基準となる<see cref="IInputElement"/>。</param>
        /// <returns>マウス位置を表す<see cref="Point"/>。X座標とY座標は最も近い整数に丸められます。</returns>
        ///
        private static Point GetIntPosition(MouseEventArgs e, IInputElement element)
        {
            var po = e.GetPosition(element);
            po = new Point((int)(po.X + 0.5), (int)(po.Y + 0.5));
            return po;
        }

        /// <summary>
        /// 指定された <see cref="PointCollection"/> 内のすべての点を調整し、左上点が原点 (0, 0) になるようにします。
        /// </summary>
        /// <remarks>このメソッドは、入力された <see cref="PointCollection"/> を、各点から左上座標を減算することで、すべての点が左上点 (0, 0) になるようにシフトされます。</remarks>
        /// <param name="pc">調整する <see cref="PointCollection"/>。null にすることはできません。</param>
        /// <returns> <see cref="PointCollection"/> の元の左上座標を含むタプル。最初の値は X 座標、2 番目の値は Y 座標です。</returns>
        ///
        private (double left, double top) ZeroFixPointCollection(PointCollection pc)
        {
            var (left, top) = GetTopLeftFromPoints(pc);
            for (int i = 0; i < pc.Count; i++)
            {
                pc[i] = new Point(pc[i].X - left, pc[i].Y - top);
            }
            return (left, top);
        }


        /// <summary>
        /// 2点間のユークリッド距離を返す
        /// </summary>
        /// <param name="a">1つ目の点</param>
        /// <param name="b">2つ目の点</param>
        /// <returns>ユークリッド距離</returns>
        private double EuclideanDistance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 3点（a, b, c）から点bを頂点とする角度（∠abc）を度で返す
        /// </summary>
        /// <param name="a">始点座標</param>
        /// <param name="b">頂点座標</param>
        /// <param name="c">終点座標</param>
        /// <returns>角度（度）</returns>
        public static double CalculateAngleABC(Vector2 a, Vector2 b, Vector2 c)
        {
            // ベクトルba, bcを求める
            Vector2 ba = a - b;
            Vector2 bc = c - b;

            // ベクトルの大きさ
            double lenBA = ba.Length();
            double lenBC = bc.Length();

            if (lenBA == 0 || lenBC == 0) return 0; // 0除算防止

            // 内積
            double dot = Vector2.Dot(ba, bc);

            // 余弦定理で角度（ラジアン）
            double cosTheta = dot / (lenBA * lenBC);
            // 計算誤差対策で範囲を制限
            cosTheta = Math.Clamp(cosTheta, -1.0, 1.0);

            double angleRad = Math.Acos(cosTheta);

            // 度に変換
            return angleRad * 180.0 / Math.PI;
        }

        public static double CalculateAngleABC(Point a, Point b, Point c)
        {
            return CalculateAngleABC(new Vector2((float)a.X, (float)a.Y), new Vector2((float)b.X, (float)b.Y), new Vector2((float)c.X, (float)c.Y));
        }

        /// <summary>
        /// PointCollectionの左上座標を返す
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private (double left, double top) GetTopLeftFromPoints(PointCollection points)
        {
            double left = double.MaxValue;
            double top = double.MaxValue;
            foreach (var item in points)
            {
                if (left > item.X) { left = item.X; }
                if (top > item.Y) { top = item.Y; }
            }
            return (left, top);
        }


        /// <summary>
        /// 定義済みブラシの名前を対応する <see cref="Brush"/> オブジェクトにマッピングするディクショナリを作成します。
        /// </summary>
        /// <remarks>このメソッドは、定義済みブラシを表す <see cref="Brushes"/> クラスのすべての public static プロパティを取得し、プロパティ名をキー、対応する <see cref="Brush"/> インスタンスを値とするディクショナリを構築します。</remarks>
        /// <returns> <see cref="Dictionary{TKey, TValue}"/> を返します。キーは定義済みブラシの名前、値は対応する <see cref="Brush"/> オブジェクトです。</returns>
        ///
        private static Dictionary<string, Brush> MakeBrushesDictionary()
        {
            var brushInfos = typeof(Brushes).GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);

            Dictionary<string, Brush> dict = new();
            foreach (var item in brushInfos)
            {
                if (item.GetValue(null) is not Brush bu)
                {
                    continue;
                }
                dict.Add(item.Name, bu);
            }
            return dict;
        }

        /// <summary>
        /// 指定された型のすべての public static プロパティの名前と値を含むディクショナリを作成します。
        /// </summary>
        /// <remarks>このメソッドは、リフレクションを使用して、指定された型のすべての public static プロパティを取得します。<see langword="null"/> 値を持つプロパティは、結果のディクショナリから除外されます。</remarks>
        /// <param name="t">ディクショナリに含まれる public static プロパティの <see cref="Type"/>。</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>。キーは指定された型の public static プロパティの名前、値は対応する値です。</returns>
        private static Dictionary<string, object> MakePropertyDictionary(Type t)
        {
            System.Reflection.PropertyInfo[]? info = t.GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);

            Dictionary<string, object>? dict = new();
            foreach (var item in info)
            {
                if (item.GetValue(null) is not object o)
                {
                    continue;
                }
                dict.Add(item.Name, o);
            }
            return dict;
        }


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

        /// <summary>
        /// SystemFontFamiliesから日本語フォント名で並べ替えたフォント一覧を返す、1ファイルに別名のフォントがある場合も取得
        /// </summary>
        /// <returns></returns>
        private SortedDictionary<string, FontFamily> GetFontFamilies()
        {
            //今のPCで使っている言語(日本語)のCulture取得
            //var language =
            //    System.Windows.Markup.XmlLanguage.GetLanguage(
            //    CultureInfo.CurrentCulture.IetfLanguageTag);
            CultureInfo culture = CultureInfo.CurrentCulture;//日本
            CultureInfo cultureUS = new("en-US");//英語？米国？

            List<string> uName = new();//フォント名の重複判定に使う
            Dictionary<string, FontFamily> tempDictionary = new();
            foreach (var item in Fonts.SystemFontFamilies)
            {
                ICollection<Typeface> typefaces = item.GetTypefaces();
                foreach (var typeface in typefaces)
                {
                    _ = typeface.TryGetGlyphTypeface(out GlyphTypeface gType);
                    if (gType != null)
                    {
                        //フォント名取得はFamilyNamesではなく、Win32FamilyNamesを使う
                        //FamilyNamesだと違うフォントなのに同じフォント名で取得されるものがあるので
                        //Win32FamilyNamesを使う
                        //日本語名がなければ英語名
                        string fontName = gType.Win32FamilyNames[culture] ?? gType.Win32FamilyNames[cultureUS];
                        //string fontName = gType.FamilyNames[culture] ?? gType.FamilyNames[cultureUS];

                        //フォント名で重複判定
                        var uri = gType.FontUri;
                        if (uName.Contains(fontName) == false)
                        {
                            uName.Add(fontName);
                            tempDictionary.Add(fontName, new(uri, fontName));
                        }
                    }
                }
            }
            SortedDictionary<string, FontFamily> fontDictionary = new(tempDictionary);
            return fontDictionary;
        }

        private string[] GetFontNames()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;//日本
            CultureInfo cultureUS = new("en-US");//英語？米国？
            List<string> names = [];

            foreach (var item in Fonts.SystemFontFamilies)
            {
                ICollection<Typeface> tf = item.GetTypefaces();
                foreach (var typeface in tf)
                {
                    _ = typeface.TryGetGlyphTypeface(out GlyphTypeface gType);
                    if (gType != null)
                    {
                        names.Add(gType.Win32FamilyNames[culture] ?? gType.Win32FamilyNames[cultureUS]);
                    }
                }
            }
            names.Sort();
            return names.Distinct().ToArray();


        }



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
            //今の状態を保存するかの確認してから開く
            if (MyRoot.MyThumbs.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                   "今の状態を保存してから開く？\n\n\n" +
                   "はい＿：保存してから開く\n\n" +
                   "いいえ：保存しないで開く\n\n" +
                   "キャンセル：開くのをキャンセル",
                   "px4ファイルを開く前の確認",
                   MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                if (result == MessageBoxResult.Cancel)
                {
                    return MakeStatusMessage("ファイルを開くのをキャンセルした");
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveRootItemOverwrite();// 上書き保存
                }
            }

            //実際に開く
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
                    //開いたら、上書き保存用のパスを更新
                    MyAppData.CurrentOpenFilePath = dialog.FileName;
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
                        // バインド
                        // 枠表示
                        _ = MyRoot.SetBinding(KisoThumb.IsWakuVisibleProperty, new Binding(nameof(AppData.IsWakuVisible)) { Source = MyAppData });
                        
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

        #region TreeView


        private void MyThumbsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is KisoThumb clicked)
            {
                // MyRootのClickedThumbとFocusThumbを変更を試みる
                ChangeFocusThumb(clicked);
            }
        }

        /// <summary>
        /// TreeViewのSelectedItemChanged用。MyRootのClickedThumbとFocusThumbを変更を試みる
        /// </summary>
        /// <param name="clickedItem"></param>
        private void ChangeFocusThumb(KisoThumb clickedItem)
        {
            if (MyRoot.GetSelectableThumb(clickedItem) is KisoThumb thumb)
            {
                MyRoot.TestPreviewMouseDown(thumb, clickedItem);
                clickedItem.BringIntoView();
            }
        }
        #endregion TreeView

        #region ボタンクリック以外のイベントでの動作

        private void Viewbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ori = e.OriginalSource;
            var sou = e.Source;
        }

        // 入力用テキストボックスクリック時、テキスト全選択
        private void MyTextBoxAddText_GotFocus(object sender, RoutedEventArgs e)
        {
            MyTextBoxAddText.SelectAll();
        }


        #endregion ボタンクリック以外のイベントでの動作
    }
}