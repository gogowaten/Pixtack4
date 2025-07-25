﻿using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;

namespace Pixtack4
{
    /// <summary>
    /// RootThumbとAreaThumbを管理するCanvas
    /// </summary>
    public class ManageExCanvas : ExCanvas
    {
        public ManageData MyManageData { get; private set; } = null!;
        public RootThumb MyRootThumb { get; private set; }
        private readonly MainWindow MyMainWindow;

        //範囲選択Thumb
        public AreaThumb MyAreaThumb { get; private set; }
        private ContextMenu MyContextMenuForAreaThumb { get; set; } = new();

        //public ManageExCanvas() { }
        public ManageExCanvas(RootThumb rootThumb, ManageData manageData, MainWindow mainWindow)
        {
            MyMainWindow = mainWindow;
            MyRootThumb = rootThumb;
            MyAreaThumb = new(MyRootThumb);


            Children.Add(MyRootThumb);
            Children.Add(MyAreaThumb);


            InitializeContextMenu();
            InitializeMyAreaThumb();// 範囲選択Thumbの初期設定

            MyManageData = manageData;


            Loaded += ManageExCanvas_Loaded;
        }

        /// <summary>
        /// 範囲選択Thumbの初期設定
        /// </summary>
        private void InitializeMyAreaThumb()
        {
            Panel.SetZIndex(MyAreaThumb, 1);
            MyAreaThumb.ContextMenu = MyContextMenuForAreaThumb;
            MyAreaThumb.DataContext = MyManageData;
            MyAreaThumb.DragStarted += (a, b) => { MyAreaThumb.MyParentExCanvas.IsAutoResize = false; };
            MyAreaThumb.DragCompleted += (a, b) => { MyAreaThumb.MyParentExCanvas.IsAutoResize = true; };
            MyAreaThumb.DragDelta += MyAreaThumb_DragDelta;

        }

        /// <summary>
        /// 範囲選択Thumbのドラッグ移動、グリッドスナップ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyAreaThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (sender is AreaThumb area)
            {
                int gSize = MyRootThumb.MyActiveGroupThumb.MyItemData.GridSize;
                int yoko = (int)(e.HorizontalChange / gSize) * gSize;
                int tate = (int)(e.VerticalChange / gSize) * gSize;
                SetLeft(area, GetLeft(area) + yoko);
                SetTop(area, GetTop(area) + tate);
                e.Handled = true;
            }
        }

        private void ManageExCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            MyBindAreaThumb();
        }

        //範囲選択Thumbのバインド設定
        private void MyBindAreaThumb()
        {
            SetBind(VisibilityProperty, nameof(ManageData.AreaThumbVisibility));
            SetBind(WidthProperty, nameof(ManageData.AreaThumbWidth));
            SetBind(HeightProperty, nameof(ManageData.AreaThumbHeight));
            SetBind(LeftProperty, nameof(ManageData.AreaLeft));
            SetBind(TopProperty, nameof(ManageData.AreaTop));
            SetBind(OpacityProperty, nameof(ManageData.AreaThumbOpacity));
            MyAreaThumb.SetBinding(BackgroundProperty, new Binding() { Source = MyManageData, Path = new PropertyPath(ManageData.AreaThumbBackgroundProperty) });

            void SetBind(DependencyProperty dp, string prop)
            {
                MyAreaThumb.SetBinding(dp, new Binding(prop) { Source = MyManageData, Mode = BindingMode.TwoWay });
            }

        }

        //右クリックメニューの初期設定
        private void InitializeContextMenu()
        {
            MenuItem item;
            item = new() { Header = "範囲を画像として保存" };
            MyContextMenuForAreaThumb.Items.Add(item);
            item.Click += ItemAreaToImageFile_Click;
            item = new() { Header = "範囲を画像として複製" };
            MyContextMenuForAreaThumb.Items.Add(item);
            item.Click += Item_Click_DupulicateAsImage;
        }

        private void Item_Click_DupulicateAsImage(object sender, RoutedEventArgs e)
        {
            MyRootThumb.AddImageThumb(bitmap: GetAreaBitmap3(false));//画像として複製

        }


        private void ItemAreaToImageFile_Click(object sender, RoutedEventArgs e)
        {
            AreaToImageSaveFile();// 選択範囲を画像として保存
        }

        /// <summary>
        /// 選択範囲を画像として保存
        /// </summary>
        private void AreaToImageSaveFile()
        {
            var (result, filePath) = MainWindow.SaveBitmap(GetAreaBitmap3(false));
            if (result)
            {
                //既定ファイル名の更新
                MyMainWindow.MyAppData.DefaultSaveImageFileName = Path.GetFileNameWithoutExtension(filePath);
            }
        }



        #region 画像保存

        /// <summary>
        /// 要素を画像として取得する
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="fontClearTyoe">フォント(文字列)の描画時にClearTypeを使用する。通常はfalseでいいと思う</param>
        /// <returns></returns>
        private RenderTargetBitmap GetElementBitmap(FrameworkElement element, bool fontClearTyoe)
        {
            CacheMode tempCM = element.CacheMode;
            BitmapCache bc = new() { EnableClearType = fontClearTyoe };
            element.CacheMode = bc;

            var bounds = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
            DrawingVisual dv = new();
            using (var context = dv.RenderOpen())
            {
                BitmapCacheBrush brush = new(element);
                context.DrawRectangle(brush, null, bounds);
            }
            RenderTargetBitmap bmp = new((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);

            element.CacheMode = tempCM;
            return bmp;
        }


        //WPF/C# コントロールの要素をキャプチャする #C# - Qiita
        //https://qiita.com/Sakurai-Shinya/items/81a9c413c3265f0e8587

        //AreaThumb用、選択範囲の画像作成
        private RenderTargetBitmap GetAreaBitmap3(bool clearType)
        {
            //AreaThumbの位置とサイズ、選択(切り抜き)範囲のRect取得
            Rect getArea = new(GetLeft(MyAreaThumb), GetTop(MyAreaThumb), MyAreaThumb.ActualWidth, MyAreaThumb.ActualHeight);

            DrawingVisual dv = new() { Offset = new Vector(-getArea.X, -getArea.Y) };
            using (var context = dv.RenderOpen())
            {
                Rect rootBounds = new(MyRootThumb.RenderSize);
                //Rect rrootBounds = VisualTreeHelper.GetDescendantBounds(MyRootThumb);

                //BrushのキャッシュモードのフォントのClearTypeを有効にする
                BitmapCacheBrush brush = new(MyRootThumb);
                if (clearType)
                {
                    //RenderOptions.SetClearTypeHint(brush, ClearTypeHint.Enabled);
                    //TextOptions.SetTextFormattingMode(brush, TextFormattingMode.Display);
                    BitmapCache bc = new() { EnableClearType = clearType };
                    brush.BitmapCache = bc;
                }
                context.DrawRectangle(brush, null, rootBounds);
            }

            //Bitmap作成
            RenderTargetBitmap bitmap = new(
                (int)Math.Ceiling(getArea.Width),
                (int)Math.Ceiling(getArea.Height)
                , 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(dv);

            return bitmap;
        }

        #endregion 画像保存

        #region メソッド

        public void ChangeRootThumb(RootThumb thumb)
        {
            Children.Remove(MyRootThumb);
            MyRootThumb = thumb;
            Children.Add(MyRootThumb);
        }

        /// <summary>
        /// 範囲選択Itemの表示非表示、
        /// </summary>
        public void AreaThumbVisibleSwitch()
        {
            if (MyManageData.AreaThumbVisibility == Visibility.Visible)
            {
                MyManageData.AreaThumbVisibility = Visibility.Collapsed;
            }
            else
            {
                MyManageData.AreaThumbVisibility = Visibility.Visible;
            }
        }
        #endregion メソッド

    }






    /// <summary>
    /// 子要素全体が収まるようにサイズが自動変化するCanvas
    /// ただし、子要素のマージンとパディングは考慮していないし
    /// ArrangeOverrideを理解していないので不具合があるかも
    /// </summary>
    public class ExCanvas : Canvas
    {
        private bool isAutoResize = true;

        /// <summary>
        /// 自動リサイズの切り替えフラグ
        /// falseからtrueに変更時はInvalidateArrangeを実行してリサイズ
        /// </summary>
        public bool IsAutoResize
        {
            get => isAutoResize;
            set
            {
                if (isAutoResize != value)
                {
                    isAutoResize = value;
                    if (value) { InvalidateArrange(); }
                }
            }
        }

        public ExCanvas()
        {
            //Loaded += ExCanvas_Loaded;
            SetBinding(WidthProperty, new Binding() { Source = this, Path = new PropertyPath(ActualWidthProperty) });
            SetBinding(HeightProperty, new Binding() { Source = this, Path = new PropertyPath(ActualHeightProperty) });
        }

        //private void ExCanvas_Loaded(object sender, RoutedEventArgs e)
        //{
        //    SetBinding(WidthProperty, new Binding() { Source = this, Path = new PropertyPath(ActualWidthProperty) });
        //    SetBinding(HeightProperty, new Binding() { Source = this, Path = new PropertyPath(ActualHeightProperty) });
        //}

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            //if (double.IsNaN(Width) && double.IsNaN(Height) && IsAutoResize)
            if (IsAutoResize)
            {
                base.ArrangeOverride(arrangeSize);
                Size resultSize = new();
                foreach (var item in Children.OfType<FrameworkElement>())
                {

                    double x = GetLeft(item) + item.ActualWidth;
                    if (double.IsNaN(x)) { x = 0 + item.ActualWidth; }
                    double y = GetTop(item) + item.ActualHeight;
                    if (double.IsNaN(y)) { y = 0 + item.ActualHeight; }
                    if (resultSize.Width < x) resultSize.Width = x;
                    if (resultSize.Height < y) resultSize.Height = y;
                }
                //base.ArrangeOverride(resultSize);
                return resultSize;
            }
            else
            {
                return base.ArrangeOverride(arrangeSize);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

    }


}