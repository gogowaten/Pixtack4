using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

//Pixtack3rd/Pixtack3rd/Pixtack3rd/MyClipboard.cs at main · gogowaten/Pixtack3rd
//https://github.com/gogowaten/Pixtack3rd/blob/main/Pixtack3rd/Pixtack3rd/MyClipboard.cs#L104
//からのコピペ改変

namespace Pixtack4
{
    internal static class MyClipboard
    {
        #region クリップボード監視、画像取得、画像保存
        //       クリップボードの中にある画像をWPFで取得してみた、Clipboard.GetImage() だけだと透明になる - 午後わてんのブログ
        //https://gogowaten.hatenablog.com/entry/2019/11/12/201852

        //        アルファ値を失わずに画像のコピペできた、.NET WPFのClipboard - 午後わてんのブログ
        //https://gogowaten.hatenablog.com/entry/2021/02/10/134406


        //四角形の場合は"PNG"で取得してBgr32に変換
        //テキストボックスはGetImage()で取得してBgr32に変換

        /// <summary>
        /// クリップボードから画像を取得する、なかった場合はnullを返す
        /// </summary>
        /// <returns>BitmapSource</returns>
        public static BitmapSource? GetImageFromClipboard()
        {

            BitmapSource? source = null;
            int count = 1;
            int limit = 5;
            do
            {
                try { source = Clipboard.GetImage(); }
                catch (Exception) { }
                finally { count++; }
            } while (limit >= count && source == null);

            if (source == null) { return null; }

            //エクセル系のデータだった場合はGetImageで取得、このままだとアルファ値が0になっているので
            //Bgr32に変換することでファルファ値を255にする
            if (IsExcelCell())
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            }
            //エクセル系以外はPNG形式で取得を試みて、得られなければGetImageで取得
            else
            {
                if (GetPngImageFromStream() is BitmapSource png)
                {
                    source = png;
                }
            }

            if (source == null) { return null; }

            //アルファ値が異常な画像ならピクセルフォーマットをBgr32に変換(アルファ値を255にする)
            if (IsExceptionTransparent(source))
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            }

            return source;
        }

        /// <summary>
        /// クリップボードから画像取得、
        /// アルファ値をチェックして異常だった場合は修正する
        /// </summary>
        /// <returns></returns>
        public static BitmapSource? GetImageFromClipboardWithAlphaFix()
        {
            BitmapSource? source = null;
            int count = 1;
            int limit = 5;
            do
            {
                try { source = Clipboard.GetImage(); }
                catch (Exception) { }
                finally { count++; }
            } while (limit >= count && source == null);

            if (source == null) { return null; }

            //アルファ値が異常な画像ならピクセルフォーマットをBgr32に変換(アルファ値を255にする)
            if (IsExceptionTransparent(source))
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            }
            return source;
        }


        //通常はこれを使えば問題ない
        /// <summary>
        /// クリップボードの画像取得、"PNG"形式で取得、できなければGetImage、アルファ値をチェックして異常ならBgr32変換
        /// </summary>
        /// <returns></returns>
        public static BitmapSource? GetImagePreferPNG()
        {
            //"PNG"形式で取得して、アルファ値が正常ならそれを返す
            if (GetImageAlphaFixPng() is BitmapSource png && IsProperAlphaValue(png))
            { return png; }

            //取得できなかった場合はGetImageで取得
            int count = 1;
            int limit = 5;
            BitmapSource? source = null;
            do
            {
                try { source = Clipboard.GetImage(); }
                catch (Exception) { }
                finally { count++; }
            } while (limit >= count && source == null);

            if (source == null) { return null; }

            //アルファ値が異常な画像ならピクセルフォーマットをBgr32に変換(アルファ値を255にする)
            if (IsProperAlphaValue(source) == false)
            {
                return new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            }

            return source;
        }

        /// <summary>
        /// GetImageで取得したものをBgr32変換
        /// </summary>
        /// <returns></returns>
        public static BitmapSource? GetImageConvertedBgr32()
        {
            BitmapSource? source = null;
            int count = 1;
            int limit = 5;
            do
            {
                try { source = Clipboard.GetImage(); }
                catch (Exception) { }
                finally { count++; }
            } while (limit >= count && source == null);

            if (source == null) { return null; }

            //ピクセルフォーマットをBgr32に変換(アルファ値を255にする)
            source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);

            return source;
        }

        /// <summary>
        /// クリップボードから"PNG"形式で画像取得、
        /// アルファ値をチェックして異常だった場合は修正する
        /// </summary>
        /// <returns></returns>
        public static BitmapSource? GetImageAlphaFixPng()
        {
            if (GetPngImageFromStream() is BitmapSource source)
            {
                if (IsExceptionTransparent(source))
                {
                    //アルファ値が異常な画像ならピクセルフォーマットをBgr32に変換(アルファ値を255にする)
                    return source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
                }
                else { return source; }
            }
            else { return null; }
        }

        /// <summary>
        /// Png形式で取得してからをBgr32へ変換して返す
        /// </summary>
        /// <returns></returns>
        public static BitmapSource? GetBgr32FromPng()
        {
            if (GetPngImageFromStream() is BitmapSource source)
            {
                //ピクセルフォーマットをBgr32に変換(アルファ値を255にする)
                return new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            }
            else { return null; }
        }


        /// <summary>
        /// BitmapSourceの全ピクセルのアルファ値を検査、一つでも1以上があれば正常なのでfalseを返す
        /// すべて0だった場合はtrueを返す、Bgra32専用
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsExceptionTransparent(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32) return false;
            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] > 0) return false;
            }
            return true;
        }

        /// <summary>
        /// 指定された <see cref="BitmapSource"/> にゼロ以外のアルファ値が含まれているかどうかを判断します。
        /// </summary>
        /// <remarks>このメソッドは、指定された <see cref="BitmapSource"/> 内の各ピクセルのアルファチャンネルをチェックします。このメソッドは、ピクセル形式が <see cref="PixelFormats.Bgra32"/> の画像に最適化されており、形式が一致しない場合は直ちに <see langword="false"/> を返します。</remarks>
        /// <param name="source">分析する <see cref="BitmapSource"/>。ピクセル形式は <see cref="PixelFormats.Bgra32"/> である必要があります。</param>
        /// <returns><see langword="true"/> は、<paramref name="source"/> にアルファ値がゼロ以外のピクセルが少なくとも 1 つ含まれている場合に返されます。それ以外の場合は <see langword="false"/> です。</returns>
        public static bool IsProperAlphaValue(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32) return false;
            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// クリップボードのPNG形式の画像を取得する、ない場合はnullを返す
        /// </summary>
        /// <returns></returns>
        public static BitmapFrame? GetPngImageFromStream()
        {
            try
            {
                using MemoryStream stream = (MemoryStream)Clipboard.GetData("PNG");
                //source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception)
            {

            }

            return null;
        }

        /// <summary>
        /// クリップボードのデータ判定、エクセル判定、
        /// データの中にEnhancedMetafile形式があればエクセルと判定してtrueを返す
        /// </summary>
        /// <returns></returns>
        public static bool IsExcelCell()
        {

            IDataObject? obj = null;
            int count = 1;
            int limit = 5;
            do
            {
                try { obj = Clipboard.GetDataObject(); }
                catch (Exception) { }
                finally
                {
                    count++;
                    Task.Delay(10);
                }
            } while (obj == null && limit >= count);

            if (obj == null) { return false; }

            string[] formats = obj.GetFormats();
            foreach (var item in formats)
            {
                if (item == "EnhancedMetafile")
                {
                    return true;
                }
            }
            return false;
        }



        #endregion クリップボード監視、画像取得

    }
}