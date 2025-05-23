﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;

namespace Pixtack4
{
    //ハンドルの表示位置、ターゲットの境界線の上、内側、外側
    public enum HandleLayoutType { Online = 0, Inside, Outside }


    /// <summary>
    /// グリッドスナップ対応版
    /// </summary>
    public class ResizeHandleAdornerGridSnap : ResizeHandleAdorner
    {
        //グリッドスナップに使うサイズ
        public int GridSize
        {
            get { return (int)GetValue(GridSizeProperty); }
            set { SetValue(GridSizeProperty, value); }
        }
        public static readonly DependencyProperty GridSizeProperty =
            DependencyProperty.Register(nameof(GridSize), typeof(int), typeof(ResizeHandleAdornerGridSnap), new PropertyMetadata(8));

        public ResizeHandleAdornerGridSnap(FrameworkElement adornedElement) : base(adornedElement)
        {
            if (adornedElement is AreaThumb area)
            {
                _ = SetBinding(GridSizeProperty, new Binding() { Source = area, Path = new PropertyPath(AreaThumb.GridSizeProperty), Mode = BindingMode.OneWay });
            }
        }

        public override event Action<double>? OnTargetLeftChanged;
        public override event Action<double>? OnTargetTopChanged;

        /// <summary>
        /// 移動量で水平移動とWidthを変更、移動量はグリッドサイズの倍数に丸める
        /// もしwidthがグリッドサイズ未満になるような移動量のときは、Widthはグリッドサイズにする
        /// </summary>
        /// <param name="target">ターゲット</param>
        /// <param name="horizontalChange">水平移動量</param>
        /// <returns></returns>
        protected override void HorizontalChange(FrameworkElement target, double horizontalChange)
        {
            if (horizontalChange == 0) { return; }
            //移動させる距離はグリッドサイズの倍数になる
            int move = (int)(horizontalChange / GridSize) * GridSize;

            //ターゲット要素の最小サイズはグリッドサイズ
            //それ以下になるような移動距離なら移動させない、サイズはグリッドサイズにする
            if (target.Width - move < GridSize)
            {
                move = 0;
                target.Width = GridSize;
            }
            else
            {
                target.Width -= move;
                Canvas.SetLeft(target, Canvas.GetLeft(target) + move);
                //リサイズハンドルによりターゲットの位置が変更されたことを知らせる
                OnTargetLeftChanged?.Invoke(move);
            }
        }

        protected override void VerticalChange(FrameworkElement target, double verticalChange)
        {
            if (verticalChange == 0) { return; }
            int move = (int)(verticalChange / GridSize) * GridSize;
            if (target.Height - move < GridSize)
            {
                move = 0;
                target.Height = GridSize;
            }
            else
            {
                target.Height -= move;
                Canvas.SetTop(target, Canvas.GetTop(target) + move);
                OnTargetTopChanged?.Invoke(move);
            }
        }

        /// <summary>
        /// 指定された水平方向の変化に基づいてターゲット要素の幅をグリッドサイズの倍数に調整する
        /// </summary>
        /// <remarks>このメソッドは、ターゲットの幅がグリッドサイズを下回らないようにします。計算された幅がグリッドサイズより小さい場合、幅はグリッドサイズに設定されます。</remarks>
        /// <param name="horizo​​ntalChange">ターゲット要素の幅に適用する水平方向のサイズ変化（デバイス非依存単位（DIP）単位）。</param>
        /// <returns><see langword="true"/> ターゲットの幅が正常に調整された場合、<see langword="false"/> を返します。</returns>
        ///
        protected override bool SetTargetWidth(double horizontalChange)
        {
            int move = (int)(horizontalChange / GridSize) * GridSize;
            //ターゲットサイズがグリッドサイズ未満にならないようにサイズ変更
            //サイズ以下になる場合はグリッドサイズに固定する
            double size = MyTarget.Width + move;
            if (size >= GridSize)
            {
                MyTarget.Width = size;
                return true;
            }
            else if (MyTarget.Width > GridSize)
            {
                MyTarget.Width = GridSize;
                return true;
            }
            else { return false; }
        }

        protected override bool SetTargetHeight(double verticalChange)
        {
            int move = (int)(verticalChange / GridSize) * GridSize;
            //ターゲットサイズがグリッドサイズ未満にならないようにサイズ変更
            //サイズ以下になる場合はグリッドサイズに固定する
            double size = MyTarget.Height + move;
            if (size >= GridSize)
            {
                MyTarget.Height = size;
                return true;
            }
            else if (MyTarget.Height > GridSize)
            {
                MyTarget.Height = GridSize;
                return true;
            }
            else { return false; }
        }
    }



    /// <summary>
    /// 要素にリサイズ用のハンドルを装飾表示する
    /// </summary>
    public class ResizeHandleAdorner : Adorner
    {


        #region VisualCollectionで必要        
        protected override int VisualChildrenCount => MyVisualChildren.Count;

        protected override Visual GetVisualChild(int index) => MyVisualChildren[index];
        protected readonly VisualCollection MyVisualChildren;//表示したい要素を管理する用？
        #endregion VisualCollectionで必要

        protected readonly List<HandleThumb> MyHandles = [];//ハンドルリスト、必要なかったかも
        private readonly Canvas MyCanvas = new();// ハンドルを表示する用
        protected readonly FrameworkElement MyTarget;// 装飾ターゲット
        private readonly HandleThumb Top = new();// 各ハンドル
        private readonly HandleThumb Left = new();
        private readonly HandleThumb Right = new();
        private readonly HandleThumb Bottom = new();
        private readonly HandleThumb TopLeft = new();
        private readonly HandleThumb TopRight = new();
        private readonly HandleThumb BottomLeft = new();
        private readonly HandleThumb BottomRight = new();

        //ハンドルの移動開始と終了の通知用イベント
        public event Action<DragStartedEventArgs>? OnHandleDragStarted;
        public event Action<DragCompletedEventArgs>? OnHandleDragCompleted;

        public ResizeHandleAdorner(FrameworkElement adornedElement) : base(adornedElement)
        {
            MyTarget = adornedElement;

            MyVisualChildren = new VisualCollection(this)
            {
                MyCanvas
            };
            MyHandles.Add(Top);
            MyHandles.Add(Left);
            MyHandles.Add(Right);
            MyHandles.Add(Bottom);
            MyHandles.Add(TopLeft);
            MyHandles.Add(TopRight);
            MyHandles.Add(BottomLeft);
            MyHandles.Add(BottomRight);

            //通常ではサイズが不確定な要素(TextBlockとかButton)のサイズを決定しておく
            if (double.IsNaN(adornedElement.Width))
            {
                adornedElement.Width = adornedElement.ActualWidth;
                adornedElement.Height = adornedElement.ActualHeight;
            }

            //ハンドルの設定
            foreach (HandleThumb item in MyHandles)
            {
                item.Cursor = Cursors.Hand;
                item.DragDelta += Handle_DragDelta;
                item.DragCompleted += (sender, e) => { OnHandleDragCompleted?.Invoke(e); };
                item.DragStarted += (sender, e) => { OnHandleDragStarted?.Invoke(e); };
                MyCanvas.Children.Add(item);

                item.SetBinding(WidthProperty, new Binding() { Source = this, Path = new PropertyPath(MyHandleSizeProperty) });
                item.SetBinding(HeightProperty, new Binding() { Source = this, Path = new PropertyPath(MyHandleSizeProperty) });


            }

            //装飾ターゲットの親要素がCanvasではない場合は、
            //以下の5個のハンドルは表示しない(外す)
            //これらはターゲットの位置変更に関係するからCanvas以外では不具合の元
            if (MyTarget.Parent is not Canvas)
            //if (MyTarget.Parent.GetType() != typeof(Canvas))
            {
                MyCanvas.Children.Remove(Top);
                MyCanvas.Children.Remove(Left);
                MyCanvas.Children.Remove(TopLeft);
                MyCanvas.Children.Remove(TopRight);
                MyCanvas.Children.Remove(BottomLeft);
            }

            InitializeSetBinding();
        }

        #region 初期バインド設定
        private void InitializeSetBinding()
        {
            //ハンドルの表示位置、ターゲットの辺の中間
            MyBind(Top, HandleThumb.MyLeftProperty, WidthProperty);
            MyBind(Left, HandleThumb.MyTopProperty, HeightProperty);
            MyBind(Right, HandleThumb.MyTopProperty, HeightProperty);
            MyBind(Bottom, HandleThumb.MyLeftProperty, WidthProperty);

            //ハンドルの位置とターゲットの縦横サイズをバインド、下、右用
            MyBindForBottomRight(Right, HandleThumb.MyLeftProperty, WidthProperty);
            MyBindForBottomRight(Bottom, HandleThumb.MyTopProperty, HeightProperty);
            MyBindForBottomRight(TopRight, HandleThumb.MyLeftProperty, WidthProperty);
            MyBindForBottomRight(BottomLeft, HandleThumb.MyTopProperty, HeightProperty);
            MyBindForBottomRight(BottomRight, HandleThumb.MyLeftProperty, WidthProperty);
            MyBindForBottomRight(BottomRight, HandleThumb.MyTopProperty, HeightProperty);

            //ハンドルの位置とターゲットの縦横サイズをバインド、上、左用
            MyBindForTopLeft(Top, HandleThumb.MyTopProperty);
            MyBindForTopLeft(Left, HandleThumb.MyLeftProperty);
            MyBindForTopLeft(TopLeft, HandleThumb.MyTopProperty);
            MyBindForTopLeft(TopLeft, HandleThumb.MyLeftProperty);
            MyBindForTopLeft(TopRight, HandleThumb.MyTopProperty);
            MyBindForTopLeft(BottomLeft, HandleThumb.MyLeftProperty);
        }

        //ハンドルの位置とターゲットの縦横サイズをバインド
        private void MyBindForBottomRight(HandleThumb handle, DependencyProperty handleDp, DependencyProperty targetDp)
        {
            MultiBinding mb = new() { Converter = new MyConvForBottomRight(), Mode = BindingMode.OneWay };
            mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyHandleSizeProperty) });
            mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyHandleLayoutProperty) });
            mb.Bindings.Add(new Binding() { Source = MyTarget, Path = new PropertyPath(targetDp) });
            handle.SetBinding(handleDp, mb);
        }


        //ハンドルの表示位置、ターゲットの辺の中間
        private void MyBind(HandleThumb handle, DependencyProperty dp, DependencyProperty targetProperty)
        {
            MultiBinding mb;
            mb = new() { Converter = new MyConvHalf() };
            mb.Bindings.Add(new Binding() { Source = MyTarget, Path = new PropertyPath(targetProperty) });
            mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyHandleSizeProperty) });
            handle.SetBinding(dp, mb);
        }

        //ハンドルの位置とハンドルのサイズをバインド       
        private void MyBindForTopLeft(HandleThumb handle, DependencyProperty dp)
        {
            var mb = new MultiBinding() { Converter = new MyConvForTopLeftHandle() };
            mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyHandleSizeProperty) });
            mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyHandleLayoutProperty) });
            handle.SetBinding(dp, mb);
        }

        #endregion 初期バインド設定



        #region イベント
        //ハンドルのドラッグ移動により、ターゲットの位置が変更されたときに、その変更量を通知
        public virtual event Action<double>? OnTargetLeftChanged;
        public virtual event Action<double>? OnTargetTopChanged;
        //public event EventHandler<double>? OnTargetLeftChanged;//こっちでもいい
        //public event PropertyChangedEventHandler PropertyChanged;//これは違う感じ
        #endregion イベント

        protected virtual bool SetTargetWidth(double horizontalChange)
        {
            if (MyTarget.Width + horizontalChange >= 1.0)
            {
                MyTarget.Width += horizontalChange;
                return true;
            }
            else if (MyTarget.Width > 1.0)
            {
                MyTarget.Width = 1.0;
                return true;
            }
            else { return false; }
        }

        protected virtual bool SetTargetHeight(double verticalChange)
        {
            if (MyTarget.Height + verticalChange >= 1.0)
            {
                MyTarget.Height += verticalChange;
                return true;
            }
            else if (MyTarget.Height > 1.0)
            {
                MyTarget.Height = 1.0;
                return true;
            }
            else { return false; }
        }

        //各ハンドルをドラッグ移動したとき
        private void Handle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender == Left)
            {
                HorizontalChange(MyTarget, e.HorizontalChange);
                e.Handled = true;
            }
            else if (sender == Top)
            {
                VerticalChange(MyTarget, e.VerticalChange);
                e.Handled = true;
            }
            else if (sender == Right)
            {
                _ = SetTargetWidth(e.HorizontalChange);
                e.Handled = true;
            }
            else if (sender == Bottom)
            {
                _ = SetTargetHeight(e.VerticalChange);
                e.Handled = true;
            }
            else if (TopLeft == sender)
            {
                HorizontalChange(MyTarget, e.HorizontalChange);
                VerticalChange(MyTarget, e.VerticalChange);
                e.Handled = true;
            }
            else if (TopRight == sender)
            {
                if (SetTargetWidth(e.HorizontalChange))
                {
                    VerticalChange(MyTarget, e.VerticalChange);
                }
                e.Handled = true;
            }
            else if (BottomLeft == sender)
            {
                if (SetTargetHeight(e.VerticalChange))
                {
                    HorizontalChange(MyTarget, e.HorizontalChange);
                }
                e.Handled = true;
            }
            else if (BottomRight == sender)
            {
                _ = SetTargetWidth(e.HorizontalChange);
                _ = SetTargetHeight(e.VerticalChange);
                e.Handled = true;
            }

        }


        #region 依存関係プロパティ


        public HandleLayoutType MyHandleLayout
        {
            get { return (HandleLayoutType)GetValue(MyHandleLayoutProperty); }
            set { SetValue(MyHandleLayoutProperty, value); }
        }
        public static readonly DependencyProperty MyHandleLayoutProperty =
            DependencyProperty.Register(nameof(MyHandleLayout), typeof(HandleLayoutType), typeof(ResizeHandleAdorner),
                new FrameworkPropertyMetadata(HandleLayoutType.Online,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double MyHandleSize
        {
            get { return (double)GetValue(MyHandleSizeProperty); }
            set { SetValue(MyHandleSizeProperty, value); }
        }
        public static readonly DependencyProperty MyHandleSizeProperty =
            DependencyProperty.Register(nameof(MyHandleSize), typeof(double), typeof(ResizeHandleAdorner), new FrameworkPropertyMetadata(20.0));
        #endregion 依存関係プロパティ

        //これがないとCanvasのサイズが0のままになって何も表示されない
        protected override Size ArrangeOverride(Size finalSize)
        {
            MyCanvas.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return base.ArrangeOverride(finalSize);
        }


        /// <summary>
        /// 移動量で水平移動とWidthを変更
        /// もしwidthが1未満になるような移動量のときは、Widthが1になるように移動量を変換して処理
        /// </summary>
        /// <param name="target">ターゲット</param>
        /// <param name="horizontalChange">水平移動量</param>
        /// <returns></returns>
        protected virtual void HorizontalChange(FrameworkElement target, double horizontalChange)
        {
            if (horizontalChange == 0) { return; }
            if (target.Width - horizontalChange > 0)
            {
                target.Width -= horizontalChange;
            }
            else
            {
                horizontalChange = target.Width - 1.0;
                target.Width = 1.0;
            }
            OffsetLeft(target, horizontalChange);
            //リサイズハンドルによりターゲットの位置が変更されたことを知らせる
            OnTargetLeftChanged?.Invoke(horizontalChange);
        }

        protected virtual void VerticalChange(FrameworkElement target, double verticalChange)
        {
            if (verticalChange == 0) { return; }
            if (target.Height - verticalChange > 0)
            {
                target.Height -= verticalChange;
            }
            else
            {
                verticalChange = target.Height - 1.0;
                target.Height = 1.0;
            }
            OffsetTop(target, verticalChange);
            OnTargetTopChanged?.Invoke(verticalChange);
        }


        /// <summary>
        /// 要素をオフセット移動する
        /// </summary>
        /// <param name="elem">ターゲット</param>
        /// <param name="offset">移動量</param>
        public static void OffsetTop(FrameworkElement elem, double offset)
        {
            Canvas.SetTop(elem, Canvas.GetTop(elem) + offset);
        }
        public static void OffsetLeft(FrameworkElement elem, double offset)
        {
            Canvas.SetLeft(elem, Canvas.GetLeft(elem) + offset);
        }

    }





    #region コンバーター
    /// <summary>
    /// ハンドルサイズと位置指定によって位置を計算
    /// 左要素か上要素があるハンドル用
    /// </summary>
    public class MyConvForTopLeftHandle : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var handleSize = (double)values[0];
            var layout = (HandleLayoutType)values[1];
            return layout switch
            {
                HandleLayoutType.Inside => 0.0,
                HandleLayoutType.Outside => -handleSize,
                HandleLayoutType.Online => -handleSize / 2.0,
                _ => (object)0.0,
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ハンドルサイズと位置指定とリサイズ対象のサイズからハンドルの位置を計算
    /// 右要素か下要素があるハンドル用
    /// </summary>
    public class MyConvForBottomRight : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var handleSize = (double)values[0];
            var layout = (HandleLayoutType)values[1];
            var targetSize = (double)values[2];
            if (targetSize <= 1.0) { targetSize = 1.0; }
            return layout switch
            {
                HandleLayoutType.Inside => targetSize - handleSize,
                HandleLayoutType.Outside => targetSize,
                HandleLayoutType.Online => targetSize - handleSize / 2.0,
                _ => targetSize
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ハンドルの位置を装飾ターゲットの辺の中間にするのに使う
    /// </summary>
    public class MyConvHalf : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var target = (double)values[0];// 装飾ターゲットの辺の長さ
            var handle = (double)values[1];// ハンドルの大きさ
            return (target - handle) / 2.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion コンバーター



}
