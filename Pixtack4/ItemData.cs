using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Xml;
using System.Windows.Media.Imaging;


namespace Pixtack4
{

    //Thumbの種類の識別用
    public enum ThumbType { None = 0, Root, Group, Text, EllipseText, Ellipse, Rect, GeoShape, Image }





    //[KnownType(typeof(SolidColorBrush))]
    //[KnownType(typeof(MatrixTransform))]
    /// <summary>
    /// Data基礎
    /// </summary>

    public class ItemDataKiso : DependencyObject, IExtensibleDataObject, INotifyPropertyChanged
    {
        #region 必要
        public ExtensionDataObject? ExtensionData { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion 必要

        public bool Serialize(string filePath, ItemDataKiso data)
        {
            DataContractSerializer serializer = new(data.GetType());
            XmlWriterSettings settings = new()
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            try
            {
                using XmlWriter writer = XmlWriter.Create(filePath, settings);
                serializer.WriteObject(writer, data);
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public static T? Deserialize<T>(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return default;
            }
            DataContractSerializer serializer = new(typeof(T));
            using XmlReader reader = XmlReader.Create(filePath);
            try
            {
                if (serializer.ReadObject(reader) is T result)
                {
                    return result;
                }
                object? neko = serializer.ReadObject(reader);
                return serializer.ReadObject(reader) is T data ? data : default;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
                //throw;
            }
        }

    }


    /// <summary>
    /// アプリの設定用
    /// </summary>
    public class AppData : ItemDataKiso
    {
        public AppData()
        {
            //MyInitBind();
        }


        // フリーハンドでの曲げ具合の指定、0.0～1.0の値、0.0で直線、1.0で最大曲げ、0.3が適当
        private double _mage = 0.3;
        public double Mage { get => _mage; set => SetProperty(ref _mage, value); }


        // フリーハンドでの方向線の長さの決め方
        private DirectionLineLengthType _directionLineLengthType = DirectionLineLengthType.Separate別々;
        public DirectionLineLengthType DirectionLineLengthType { get => _directionLineLengthType; set => SetProperty(ref _directionLineLengthType, value); }

        // フリーハンド図形でのPointsの間引き間隔
        private int _pointChoiceInterval = 30;
        public int PointChoiceInterval { get => _pointChoiceInterval; set => SetProperty(ref _pointChoiceInterval, value); }


        // 図形の終端形状
        private HeadType _geoShapeEndHeadType = HeadType.Arrow;
        [DataMember] public HeadType GeoShapeEndHeadType { get => _geoShapeEndHeadType; set => SetProperty(ref _geoShapeEndHeadType, value); }

        // 図形の線の色
        private byte _geoShapeStrokeA;
        [DataMember] public byte GeoShapeStrokeA { get => _geoShapeStrokeA; set => SetProperty(ref _geoShapeStrokeA, value); }
        private byte _geoShapeStrokeR;
        [DataMember] public byte GeoShapeStrokeR { get => _geoShapeStrokeR; set => SetProperty(ref _geoShapeStrokeR, value); }
        private byte _geoShapeStrokeG;
        [DataMember] public byte GeoShapeStrokeG { get => _geoShapeStrokeG; set => SetProperty(ref _geoShapeStrokeG, value); }
        private byte _geoShapeStrokeB;
        [DataMember] public byte GeoShapeStrokeB { get => _geoShapeStrokeB; set => SetProperty(ref _geoShapeStrokeB, value); }

        [IgnoreDataMember]
        public Brush GeoShapeStroke
        {
            get { return (Brush)GetValue(GeoShapeStrokeProperty); }
            set { SetValue(GeoShapeStrokeProperty, value); }
        }
        public static readonly DependencyProperty GeoShapeStrokeProperty =
            DependencyProperty.Register(nameof(GeoShapeStroke), typeof(Brush), typeof(GeoShapeItemData),
                new FrameworkPropertyMetadata(Brushes.ForestGreen,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // 図形の線の太さ
        private double _geoShapeStrokeThickness = 20.0;
        [DataMember] public double GeoShapeStrokeThickness { get => _geoShapeStrokeThickness; set => SetProperty(ref _geoShapeStrokeThickness, value); }

        //図形のアンカーハンドルのサイズ
        private double _geoShapeHandleSize = 10.0;
        [DataMember] public double GeoShapeHandleSize { get => _geoShapeHandleSize; set => SetProperty(ref _geoShapeHandleSize, value); }


        //フォントweight選択ComboBoxの選択Index
        private int _fontWeightComboBoxSelectedIndex = -1;
        [DataMember] public int FontWeightComboBoxSelectedIndex { get => _fontWeightComboBoxSelectedIndex; set => SetProperty(ref _fontWeightComboBoxSelectedIndex, value); }

        //フォント選択ComboBoxの選択Index
        private int _fontComboBoxSelectedIndex;
        [DataMember] public int FontComboBoxSelectedIndex { get => _fontComboBoxSelectedIndex; set => SetProperty(ref _fontComboBoxSelectedIndex, value); }

        //フォントリスト
        private List<string> _fontNameList = [];
        [DataMember] public List<string> FontNameList { get => _fontNameList; set => SetProperty(ref _fontNameList, value); }


        //GridSizeの下限値
        private int _minGridSize = 1;
        [DataMember] public int MinGridSize { get => _minGridSize; set => SetProperty(ref _minGridSize, value); }

        private int _maxGridSize = 1080;
        [DataMember] public int MaxGridSize { get => _maxGridSize; set => SetProperty(ref _maxGridSize, value); }


        //Itemの枠表示状態、RootThumbのものとバインドする
        private Visibility _isWakuVisible = Visibility.Collapsed;
        [DataMember] public Visibility IsWakuVisible { get => _isWakuVisible; set => SetProperty(ref _isWakuVisible, value); }


        //画像とし保存時の既定ファイル名
        private string _defaultSaveImageFileName = string.Empty;
        [DataMember] public string DefaultSaveImageFileName { get => _defaultSaveImageFileName; set => SetProperty(ref _defaultSaveImageFileName, value); }

        //保存Dataファイル名の既定値
        private string _defaultSaveDataFileName = string.Empty;
        [DataMember] public string DefaultSaveDataFileName { get => _defaultSaveDataFileName; set => SetProperty(ref _defaultSaveDataFileName, value); }

        //jpeg保存時の品質
        private int _myJpegQuality = 90;
        [DataMember] public int MyJpegQuality { get => _myJpegQuality; set => SetProperty(ref _myJpegQuality, value); }

        //複数ファイルを開くときにファイル名の降順で開く
        private bool _isFileNameDescendingOrder;
        [DataMember] public bool IsFileNameDescendingOrder { get => _isFileNameDescendingOrder; set => SetProperty(ref _isFileNameDescendingOrder, value); }


        //

        //ファイルから追加のときのフォルダ

        //OpenFileDialogの初期フォルダ、初期値はマイドキュメント
        private string _initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        [DataMember] public string InitialDirectory { get => _initialDirectory; set => SetProperty(ref _initialDirectory, value); }

        //今開いているファイルのパス
        private string _currentOpenFilePath = string.Empty;
        [DataMember] public string CurrentOpenFilePath { get => _currentOpenFilePath; set => SetProperty(ref _currentOpenFilePath, value); }

        private void MyInitBind()
        {
            //直線図形の色
            MultiBinding mb;
            mb = new() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(GeoShapeStrokeA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(GeoShapeStrokeR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(GeoShapeStrokeG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(GeoShapeStrokeB)) { Source = this });
            _ = BindingOperations.SetBinding(this, GeoShapeStrokeProperty, mb);
        }

    }




    /// <summary>
    /// アプリのウィンドウ設定用Data
    /// </summary>
    public class AppWindowData : ItemDataKiso
    {
        public AppWindowData() { }


        /// <summary>
        /// シリアル化
        /// </summary>
        /// <param name="filePath">保存ファイルパス</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool Serialize(string filePath)
        {
            DataContractSerializer serializer = new(typeof(AppWindowData));
            XmlWriterSettings settings = new()
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            try
            {
                using XmlWriter writer = XmlWriter.Create(filePath, settings);
                serializer.WriteObject(writer, this);
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public static AppWindowData? Deserialize(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }
            DataContractSerializer serializer = new(typeof(AppWindowData));
            using XmlReader reader = XmlReader.Create(filePath);
            try
            {
                return serializer.ReadObject(reader) is AppWindowData data ? data : null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
                //throw;
            }
        }


        //位置とサイズ
        private double _left;
        [DataMember] public double Left { get => _left; set => SetProperty(ref _left, value); }

        private double _top;
        [DataMember] public double Top { get => _top; set => SetProperty(ref _top, value); }

        private double _width = 850.0;
        [DataMember] public double Width { get => _width; set => SetProperty(ref _width, value); }

        private double _height = 600.0;
        [DataMember] public double Height { get => _height; set => SetProperty(ref _height, value); }

        //ウィンドウの状態、最大化、最小化とか
        private WindowState _windowState = WindowState.Normal;
        [DataMember] public WindowState WindowState { get => _windowState; set => SetProperty(ref _windowState, value); }

    }



    /// <summary>
    /// ManageExCanvas用の設定保存データ
    /// </summary>
    public class ManageData : ItemDataKiso
    {
        public ManageData() { MyBindBrushes(); }

        #region 通知プロパティ、依存関係プロパティ

        #region 範囲選択Thumb用

        private double _areaLeft;
        public double AreaLeft { get => _areaLeft; set => SetProperty(ref _areaLeft, value); }

        private double _areaTop;
        public double AreaTop { get => _areaTop; set => SetProperty(ref _areaTop, value); }

        private double _areaThumbWidth = 100.0;
        public double AreaThumbWidth { get => _areaThumbWidth; set => SetProperty(ref _areaThumbWidth, value); }

        private double _areaThumbHeight = 100.0;
        public double AreaThumbHeight { get => _areaThumbHeight; set => SetProperty(ref _areaThumbHeight, value); }

        private Visibility _areaThumbVisibility = Visibility.Collapsed;
        public Visibility AreaThumbVisibility { get => _areaThumbVisibility; set => SetProperty(ref _areaThumbVisibility, value); }

        private double _areaThumbOpacity = 0.3;
        public double AreaThumbOpacity { get => _areaThumbOpacity; set => SetProperty(ref _areaThumbOpacity, value); }


        private byte _areaThumbBackgroundA = 255;
        [DataMember] public byte AreaThumbBackgroundA { get => _areaThumbBackgroundA; set => SetProperty(ref _areaThumbBackgroundA, value); }
        private byte _areaThumbBackgroundR;
        [DataMember] public byte AreaThumbBackgroundR { get => _areaThumbBackgroundR; set => SetProperty(ref _areaThumbBackgroundR, value); }
        private byte _areaThumbBackgroundG;
        [DataMember] public byte AreaThumbBackgroundG { get => _areaThumbBackgroundG; set => SetProperty(ref _areaThumbBackgroundG, value); }
        private byte _areaThumbBackgroundB;
        [DataMember] public byte AreaThumbBackgroundB { get => _areaThumbBackgroundB; set => SetProperty(ref _areaThumbBackgroundB, value); }

        //オプションでBindsTwoWayByDefault必須、Binding時にはTwoWayに設定しても反映されないので、ここで指定
        [IgnoreDataMember]
        public Brush AreaThumbBackground
        {
            get { return (Brush)GetValue(AreaThumbBackgroundProperty); }
            set { SetValue(AreaThumbBackgroundProperty, value); }
        }
        public static readonly DependencyProperty AreaThumbBackgroundProperty =
            DependencyProperty.Register(nameof(AreaThumbBackground), typeof(Brush), typeof(ManageData),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion 範囲選択Thumb用
        #endregion 通知プロパティ、依存関係プロパティ

        private void MyBindBrushes()
        {
            var mb = new MultiBinding() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(AreaThumbBackgroundA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(AreaThumbBackgroundR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(AreaThumbBackgroundG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(AreaThumbBackgroundB)) { Source = this });
            _ = BindingOperations.SetBinding(this, AreaThumbBackgroundProperty, mb);
        }

    }



    public class GeoShapeItemData : ItemDataKiso
    {
        public GeoShapeItemData() { MyInitBind(); }


        private HeadType _myGeoShapeHeadBeginCapType;
        public HeadType MyGeoShapeHeadBeginCapType { get => _myGeoShapeHeadBeginCapType; set => SetProperty(ref _myGeoShapeHeadBeginCapType, value); }

        private HeadType _myGeoShapeHeadEndCapType = HeadType.None;
        public HeadType MyGeoShapeHeadEndCapType { get => _myGeoShapeHeadEndCapType; set => SetProperty(ref _myGeoShapeHeadEndCapType, value); }


        private double _myStrokeThickness = 10.0;
        public double MyStrokeThickness { get => _myStrokeThickness; set => SetProperty(ref _myStrokeThickness, value); }


        private ShapeType _myShapeType = ShapeType.Line;
        public ShapeType MyShapeType { get => _myShapeType; set => SetProperty(ref _myShapeType, value); }


        private byte _myStrokeA;
        [DataMember] public byte MyStrokeA { get => _myStrokeA; set => SetProperty(ref _myStrokeA, value); }
        private byte _myStrokeR;
        [DataMember] public byte MyStrokeR { get => _myStrokeR; set => SetProperty(ref _myStrokeR, value); }
        private byte _myStrokeG;
        [DataMember] public byte MyStrokeG { get => _myStrokeG; set => SetProperty(ref _myStrokeG, value); }
        private byte _myStrokeB;
        [DataMember] public byte MyStrokeB { get => _myStrokeB; set => SetProperty(ref _myStrokeB, value); }

        [IgnoreDataMember]
        public Brush MyStroke
        {
            get { return (Brush)GetValue(MyStrokeProperty); }
            set { SetValue(MyStrokeProperty, value); }
        }
        public static readonly DependencyProperty MyStrokeProperty =
            DependencyProperty.Register(nameof(MyStroke), typeof(Brush), typeof(GeoShapeItemData),
                new FrameworkPropertyMetadata(Brushes.Red,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        //アンカーポイント群
        //通知プロパティだとリアルタイムで動作確認できないので依存関係プロパティにしている
        [DataMember]
        public PointCollection MyPoints
        {
            get { return (PointCollection)GetValue(MyPointsProperty); }
            set { SetValue(MyPointsProperty, value); }
        }
        public static readonly DependencyProperty MyPointsProperty =
            DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(GeoShapeItemData), new PropertyMetadata(null));

        private void MyInitBind()
        {
            //直線図形の色
            MultiBinding mb;
            mb = new() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(MyStrokeA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyStrokeR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyStrokeG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyStrokeB)) { Source = this });
            _ = BindingOperations.SetBinding(this, MyStrokeProperty, mb);
        }

    }

    /// <summary>
    /// 図形用
    /// </summary>
    public class ShapeItemData : ItemDataKiso
    {
        public ShapeItemData() { MyInitBind(); }

        private void MyInitBind()
        {
            //枠色
            MultiBinding mb;
            mb = new() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(WakuColorA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(WakuColorR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(WakuColorG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(WakuColorB)) { Source = this });
            _ = BindingOperations.SetBinding(this, WakuColorProperty, mb);

            //塗りつぶし
            mb = new() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(MyFillA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyFillR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyFillG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyFillB)) { Source = this });
            _ = BindingOperations.SetBinding(this, MyFillProperty, mb);

        }

        //長方形用の角の丸さ半径

        private double _roundnessRadius;
        public double RoundnessRadius { get => _roundnessRadius; set => SetProperty(ref _roundnessRadius, value); }


        //枠幅
        private double _strokeThickness = 0;
        public double StrokeThickness { get => _strokeThickness; set => SetProperty(ref _strokeThickness, value); }


        //枠色
        private byte _wakuColorA;
        [DataMember] public byte WakuColorA { get => _wakuColorA; set => SetProperty(ref _wakuColorA, value); }
        private byte _wakuColorR;
        [DataMember] public byte WakuColorR { get => _wakuColorR; set => SetProperty(ref _wakuColorR, value); }
        private byte _wakuColorG;
        [DataMember] public byte WakuColorG { get => _wakuColorG; set => SetProperty(ref _wakuColorG, value); }
        private byte _wakuColorB;
        [DataMember] public byte WakuColorB { get => _wakuColorB; set => SetProperty(ref _wakuColorB, value); }

        public Brush WakuColor
        {
            get { return (Brush)GetValue(WakuColorProperty); }
            set { SetValue(WakuColorProperty, value); }
        }
        public static readonly DependencyProperty WakuColorProperty =
            DependencyProperty.Register(nameof(WakuColor), typeof(Brush), typeof(ShapeItemData),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        //塗りつぶし色
        private byte _myFillA;
        [DataMember] public byte MyFillA { get => _myFillA; set => SetProperty(ref _myFillA, value); }
        private byte _myFillR;
        [DataMember] public byte MyFillR { get => _myFillR; set => SetProperty(ref _myFillR, value); }
        private byte _myFillG;
        [DataMember] public byte MyFillG { get => _myFillG; set => SetProperty(ref _myFillG, value); }
        private byte _myFillB;
        [DataMember] public byte MyFillB { get => _myFillB; set => SetProperty(ref _myFillB, value); }
        //オプションでBindsTwoWayByDefault必須、Binding時にはTwoWayに設定しても反映されないので、ここで指定
        [IgnoreDataMember]
        public Brush MyFill
        {
            get { return (Brush)GetValue(MyFillProperty); }
            set { SetValue(MyFillProperty, value); }
        }
        public static readonly DependencyProperty MyFillProperty =
            DependencyProperty.Register(nameof(MyFill), typeof(Brush), typeof(ShapeItemData),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));// TwoWay



    }

    //文字列用
    public class TextItemData : ItemDataKiso
    {

        //FontWdithそのままだとシリアル化できないみたい、エラーにもならないのでToString()のstring型にしてみた
        private string _fontWeight = "Normal";
        [DataMember] public string FontWeight { get => _fontWeight; set => SetProperty(ref _fontWeight, value); }

        private string _fontName = string.Empty;
        [DataMember] public string FontName { get => _fontName; set => SetProperty(ref _fontName, value); }

        private string _myText = string.Empty;
        [DataMember] public string MyText { get => _myText; set => SetProperty(ref _myText, value); }

        private double _myFontSize = SystemFonts.MessageFontSize;
        [DataMember] public double MyFontSize { get => _myFontSize; set => SetProperty(ref _myFontSize, value); }

    }


    /// <summary>
    /// アイテム用
    /// </summary>
    //[DataContract]
    [KnownType(typeof(ItemData))]

    [DebuggerDisplay("{MyThumbType}")]
    //public class ItemData : DependencyObject, IExtensibleDataObject, INotifyPropertyChanged
    public class ItemData : ItemDataKiso
    {

        public ItemData()
        {
            MyInitBind();
        }
        public ItemData(ThumbType type) : this()
        {
            MyThumbType = type;
        }

        #region シリアライズ

        //Pixtack3rd/Pixtack3rd/Pixtack3rd/Data.cs at main · gogowaten/Pixtack3rd
        //https://github.com/gogowaten/Pixtack3rd/blob/main/Pixtack3rd/Pixtack3rd/Data.cs
        //ディープコピーはこれのコピペ改変

        /// <summary>
        /// ディープコピー
        /// </summary>
        /// <returns></returns>
        public ItemData? DeepCopy()
        {
            try
            {
                using System.IO.MemoryStream stream = new();
                DataContractSerializer serializer = new(typeof(ItemData));
                serializer.WriteObject(stream, this);
                stream.Position = 0;
                if (serializer.ReadObject(stream) is ItemData data)
                {
                    data.MyGuid = System.Guid.NewGuid().ToString();//GUIDは新規作成
                    //画像はBitmapFrameで複製
                    if (data.MyThumbType == ThumbType.Image)
                    {
                        data.MyBitmapSource = BitmapFrame.Create(this.MyBitmapSource);
                    }
                    else if (data.MyThumbType == ThumbType.Root || data.MyThumbType == ThumbType.Group)
                    {
                        DatasDeepCopy(this.MyThumbsItemData, data.MyThumbsItemData);
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
            return null;
        }

        /// <summary>
        /// RootとGroupのItemDataのディープコピー用
        /// </summary>
        /// <param name="moto"></param>
        /// <param name="saki"></param>
        private void DatasDeepCopy(ObservableCollection<ItemData> moto, ObservableCollection<ItemData> saki)
        {
            for (int i = 0; i < moto.Count; i++)
            {
                ItemData motoItem = moto[i];
                ItemData sakiItem = saki[i];
                sakiItem.MyGuid = System.Guid.NewGuid().ToString();
                if (motoItem.MyThumbType == ThumbType.Image)
                {
                    sakiItem.MyBitmapSource = BitmapFrame.Create(motoItem.MyBitmapSource);
                }
                else if (motoItem.MyThumbType == ThumbType.Group)
                {
                    DatasDeepCopy(motoItem.MyThumbsItemData, sakiItem.MyThumbsItemData);
                }
            }
        }

        public bool Serialize(string filePath)
        {
            DataContractSerializer serializer = new(typeof(ItemData));
            XmlWriterSettings settings = new()
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };
            using XmlWriter writer = XmlWriter.Create(filePath, settings);

            try
            {
                serializer.WriteObject(writer, this);
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public static ItemData? Deserialize(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }
            DataContractSerializer serializer = new(typeof(ItemData));
            using XmlReader reader = XmlReader.Create(filePath);
            if (serializer.ReadObject(reader) is ItemData data)
            {
                data.MyGuid = Guid.NewGuid().ToString();
                return data;
            }
            else { return null; }
        }

        #endregion シリアライズ


        #region ブラシのバインド初期設定


        private void MyInitBind()
        {
            var mb = new MultiBinding() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(MyForegroundA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyForegroundR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyForegroundG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyForegroundB)) { Source = this });
            _ = BindingOperations.SetBinding(this, MyForegroundProperty, mb);

            mb = new MultiBinding() { Converter = new MyConverterARGBtoSolidBrush() };
            mb.Bindings.Add(new Binding(nameof(MyBackgroundA)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyBackgroundR)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyBackgroundG)) { Source = this });
            mb.Bindings.Add(new Binding(nameof(MyBackgroundB)) { Source = this });
            _ = BindingOperations.SetBinding(this, MyBackgroundProperty, mb);


            //mb = new MultiBinding() { Converter = new MyConverterARGBtoSolidBrush() };
            //mb.Bindings.Add(new Binding(nameof(MyStrokeA)) { Source = this });
            //mb.Bindings.Add(new Binding(nameof(MyStrokeR)) { Source = this });
            //mb.Bindings.Add(new Binding(nameof(MyStrokeG)) { Source = this });
            //mb.Bindings.Add(new Binding(nameof(MyStrokeB)) { Source = this });
            //_ = BindingOperations.SetBinding(this, MyStrokeProperty, mb);

        }

        #endregion ブラシのバインド初期設定


        #region 特殊


        private ObservableCollection<ItemData> _myThumbsItemData = [];
        public ObservableCollection<ItemData> MyThumbsItemData { get => _myThumbsItemData; set => SetProperty(ref _myThumbsItemData, value); }



        private ThumbType _myThumbType;
        [DataMember] public ThumbType MyThumbType { get => _myThumbType; set => SetProperty(ref _myThumbType, value); }

        [DataMember] public string MyGuid { get; set; } = Guid.NewGuid().ToString();

        [IgnoreDataMember] private BitmapSource? _myBitmapSource;
        [IgnoreDataMember] public BitmapSource? MyBitmapSource { get => _myBitmapSource; set => SetProperty(ref _myBitmapSource, value); }


        #endregion 特殊



        #region 図形Geometry系


        private GeoShapeItemData _GeoShapeItemData = new();
        public GeoShapeItemData GeoShapeItemData { get => _GeoShapeItemData; set => SetProperty(ref _GeoShapeItemData, value); }


        //private HeadType _myGeoShapeHeadCapType = HeadType.None;
        //public HeadType MyGeoShapeHeadCapType { get => _myGeoShapeHeadCapType; set => SetProperty(ref _myGeoShapeHeadCapType, value); }


        //private double _myStrokeThickness = 10.0;
        //public double MyStrokeThickness { get => _myStrokeThickness; set => SetProperty(ref _myStrokeThickness, value); }


        //private ShapeType _myShapeType = ShapeType.Line;
        //public ShapeType MyShapeType { get => _myShapeType; set => SetProperty(ref _myShapeType, value); }



        ////アンカーポイント群
        ////通知プロパティだとリアルタイムで動作確認できないので依存関係プロパティにしている
        //[DataMember]
        //public PointCollection MyPoints
        //{
        //    get { return (PointCollection)GetValue(MyPointsProperty); }
        //    set { SetValue(MyPointsProperty, value); }
        //}
        //public static readonly DependencyProperty MyPointsProperty =
        //    DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(ItemData), new PropertyMetadata(null));


        #endregion 図形Geometry系


        #region 共通

        private double _myAngle;
        public double MyAngle { get => _myAngle; set => SetProperty(ref _myAngle, value); }


        private double _myLeft = 0.0;
        [DataMember] public double MyLeft { get => _myLeft; set => SetProperty(ref _myLeft, value); }

        private double _myTop = 0.0;
        [DataMember] public double MyTop { get => _myTop; set => SetProperty(ref _myTop, value); }

        private int _myZIndex = 0;
        [DataMember] public int MyZIndex { get => _myZIndex; set => SetProperty(ref _myZIndex, value); }


        private double _myWidth;
        [DataMember] public double MyWidth { get => _myWidth; set => SetProperty(ref _myWidth, value); }

        private double _myHeight;
        [DataMember] public double MyHeight { get => _myHeight; set => SetProperty(ref _myHeight, value); }


        #endregion 共通

        #region ブラシ



        private byte _myForegroundA = 255;
        [DataMember] public byte MyForegroundA { get => _myForegroundA; set => SetProperty(ref _myForegroundA, value); }
        private byte _myForegroundR;
        [DataMember] public byte MyForegroundR { get => _myForegroundR; set => SetProperty(ref _myForegroundR, value); }
        private byte _myForegroundG;
        [DataMember] public byte MyForegroundG { get => _myForegroundG; set => SetProperty(ref _myForegroundG, value); }
        private byte _myForegroundB;
        [DataMember] public byte MyForegroundB { get => _myForegroundB; set => SetProperty(ref _myForegroundB, value); }

        //オプションでBindsTwoWayByDefault必須、Binding時にはTwoWayに設定しても反映されないので、ここで指定
        [IgnoreDataMember]
        public Brush MyForeground
        {
            get { return (Brush)GetValue(MyForegroundProperty); }
            set { SetValue(MyForegroundProperty, value); }
        }
        public static readonly DependencyProperty MyForegroundProperty =
            DependencyProperty.Register(nameof(MyForeground), typeof(Brush), typeof(ItemData),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        private byte _myBackgroundA = 0;
        [DataMember] public byte MyBackgroundA { get => _myBackgroundA; set => SetProperty(ref _myBackgroundA, value); }
        private byte _myBackgroundR = 0;
        [DataMember] public byte MyBackgroundR { get => _myBackgroundR; set => SetProperty(ref _myBackgroundR, value); }
        private byte _myBackgroundG = 0;
        [DataMember] public byte MyBackgroundG { get => _myBackgroundG; set => SetProperty(ref _myBackgroundG, value); }
        private byte _myBackgroundB = 0;
        [DataMember] public byte MyBackgroundB { get => _myBackgroundB; set => SetProperty(ref _myBackgroundB, value); }

        [IgnoreDataMember]
        public Brush? MyBackground
        {
            get { return (Brush)GetValue(MyBackgroundProperty); }
            set { SetValue(MyBackgroundProperty, value); }
        }
        public static readonly DependencyProperty MyBackgroundProperty =
            DependencyProperty.Register(nameof(MyBackground), typeof(Brush), typeof(ItemData),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        //private byte _myStrokeA;
        //[DataMember] public byte MyStrokeA { get => _myStrokeA; set => SetProperty(ref _myStrokeA, value); }
        //private byte _myStrokeR;
        //[DataMember] public byte MyStrokeR { get => _myStrokeR; set => SetProperty(ref _myStrokeR, value); }
        //private byte _myStrokeG;
        //[DataMember] public byte MyStrokeG { get => _myStrokeG; set => SetProperty(ref _myStrokeG, value); }
        //private byte _myStrokeB;
        //[DataMember] public byte MyStrokeB { get => _myStrokeB; set => SetProperty(ref _myStrokeB, value); }

        //[IgnoreDataMember]
        //public Brush MyStroke
        //{
        //    get { return (Brush)GetValue(MyStrokeProperty); }
        //    set { SetValue(MyStrokeProperty, value); }
        //}
        //public static readonly DependencyProperty MyStrokeProperty =
        //    DependencyProperty.Register(nameof(MyStroke), typeof(Brush), typeof(ItemData),
        //        new FrameworkPropertyMetadata(Brushes.Red,
        //        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));




        #endregion ブラシ


        //基本図形
        private ShapeItemData _shapeItemData = new();
        [DataMember] public ShapeItemData ShapeItemData { get => _shapeItemData; set => SetProperty(ref _shapeItemData, value); }


        #region テキスト系

        private TextItemData _textItemData = new();
        [DataMember] public TextItemData TextItemData { get => _textItemData; set => SetProperty(ref _textItemData, value); }

        #endregion テキスト系

        #region Group, Root用


        //private bool _isVisible = true;
        //[DataMember] public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }


        //スナップ移動で使うグリッドサイズ
        private int _gridSize = 8;
        [DataMember] public int GridSize { get => _gridSize; set => SetProperty(ref _gridSize, value); }

        //Thumb追加時の基準からの距離
        private int _myAddOffsetLeft = 32;
        [DataMember] public int MyAddOffsetLeft { get => _myAddOffsetLeft; set => SetProperty(ref _myAddOffsetLeft, value); }

        private int _myAddOffsetTop = 32;
        [DataMember] public int MyAddOffsetTop { get => _myAddOffsetTop; set => SetProperty(ref _myAddOffsetTop, value); }

        #endregion Group, Root用

        #region 保存しない系

        private Visibility _isWakuVisible;
        public Visibility IsWakuVisible { get => _isWakuVisible; set => SetProperty(ref _isWakuVisible, value); }

        private bool _isActiveGroup;
        public bool IsActiveGroup { get => _isActiveGroup; set => SetProperty(ref _isActiveGroup, value); }

        private bool _isSelectable;
        public bool IsSelectable { get => _isSelectable; set => SetProperty(ref _isSelectable, value); }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

        private bool _isFocus;
        public bool IsFocus { get => _isFocus; set => SetProperty(ref _isFocus, value); }

        #endregion 保存しない系

    }


}