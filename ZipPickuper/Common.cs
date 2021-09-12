using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPickuper {
    public static class Common {
        public static void ExtractSelectedItem(string zipPath, IEnumerable<string> fullNames, string saveDir) {
            using (var zip = ZipFile.OpenRead(zipPath)) {
                foreach (var entry in zip.Entries) {
                    if (fullNames.Contains(entry.FullName)) {
                        var path = System.IO.Path.Combine(saveDir, entry.Name).GetUniquePath();
                        entry.ExtractToFile(path);
                    }
                }
            }
        }
        public static string GetUniquePath(this string path) {
            var fi = new FileInfo(path);
            var dir = fi.Directory.FullName;
            var name = fi.Name.Substring(0, fi.Name.LastIndexOf('.'));
            var ext= fi.Name.Substring(fi.Name.LastIndexOf('.'));
            var cnt = 0;
            while (File.Exists(Path.Combine(dir, name + ext)))
                name = name + (++cnt).ToString();
            return Path.Combine(dir, name + ext);
        }
        public static byte[] ReadAll(this Stream s) {
            byte[] buff = new byte[1024];
            List<byte> bs = new List<byte>();
            int len = 0;
            while ((len = s.Read(buff)) > 0) {
                bs.AddRange(buff.Take(len));
            }
            return bs.ToArray();
        }
        public static bool IsImageFile(this string name) {
            return Regex.IsMatch(name, @"\.(jpg|jpeg|png|bmp|gif|tiff|tif|svg|psd|webp)$", RegexOptions.IgnoreCase);
        }
        //[コピペ]https://dobon.net/vb/dotnet/string/detectcode.html
        /// <summary>
        /// 文字コードを判別する
        /// </summary>
        /// <remarks>
        /// Jcode.pmのgetcodeメソッドを移植したものです。
        /// Jcode.pm(http://openlab.ring.gr.jp/Jcode/index-j.html)
        /// Jcode.pmの著作権情報
        /// Copyright 1999-2005 Dan Kogai <dankogai@dan.co.jp>
        /// This library is free software; you can redistribute it and/or modify it
        ///  under the same terms as Perl itself.
        /// </remarks>
        /// <param name="bytes">文字コードを調べるデータ</param>
        /// <returns>適当と思われるEncodingオブジェクト。
        /// 判断できなかった時はnull。</returns>
        public static System.Text.Encoding GetEncoding(this byte[] bytes) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #region GetEncodingFromBOM
            if (bytes.Length >= 2) {
                if ((bytes[0] == 0xfe) && (bytes[1] == 0xff)) {
                    //UTF-16 BE
                    return new System.Text.UnicodeEncoding(true, true);
                }
                if ((bytes[0] == 0xff) && (bytes[1] == 0xfe)) {
                    if ((4 <= bytes.Length) &&
                        (bytes[2] == 0x00) && (bytes[3] == 0x00)) {
                        //UTF-32 LE
                        return new System.Text.UTF32Encoding(false, true);
                    }
                    //UTF-16 LE
                    return new System.Text.UnicodeEncoding(false, true);
                }
                if (bytes.Length < 3) {
                    return null;
                }
                if ((bytes[0] == 0xef) && (bytes[1] == 0xbb) && (bytes[2] == 0xbf)) {
                    //UTF-8
                    return new System.Text.UTF8Encoding(true, true);
                }
                if (bytes.Length < 4) {
                    return null;
                }
                if ((bytes[0] == 0x00) && (bytes[1] == 0x00) &&
                    (bytes[2] == 0xfe) && (bytes[3] == 0xff)) {
                    //UTF-32 BE
                    return new System.Text.UTF32Encoding(true, true);
                }
            }
            #endregion

            const byte bEscape = 0x1B;
            const byte bAt = 0x40;
            const byte bDollar = 0x24;
            const byte bAnd = 0x26;
            const byte bOpen = 0x28;    //'('
            const byte bB = 0x42;
            const byte bD = 0x44;
            const byte bJ = 0x4A;
            const byte bI = 0x49;

            int len = bytes.Length;
            byte b1, b2, b3, b4;

            //Encode::is_utf8 は無視

            bool isBinary = false;
            for (int i = 0; i < len; i++) {
                b1 = bytes[i];
                if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF) {
                    //'binary'
                    isBinary = true;
                    if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F) {
                        //smells like raw unicode
                        return System.Text.Encoding.Unicode;
                    }
                }
            }
            if (isBinary) {
                return null;
            }

            //not Japanese
            bool notJapanese = true;
            for (int i = 0; i < len; i++) {
                b1 = bytes[i];
                if (b1 == bEscape || 0x80 <= b1) {
                    notJapanese = false;
                    break;
                }
            }
            if (notJapanese) {
                return System.Text.Encoding.ASCII;
            }

            for (int i = 0; i < len - 2; i++) {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                b3 = bytes[i + 2];

                if (b1 == bEscape) {
                    if (b2 == bDollar && b3 == bAt) {
                        //JIS_0208 1978
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    } else if (b2 == bDollar && b3 == bB) {
                        //JIS_0208 1983
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    } else if (b2 == bOpen && (b3 == bB || b3 == bJ)) {
                        //JIS_ASC
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    } else if (b2 == bOpen && b3 == bI) {
                        //JIS_KANA
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    if (i < len - 3) {
                        b4 = bytes[i + 3];
                        if (b2 == bDollar && b3 == bOpen && b4 == bD) {
                            //JIS_0212
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                        if (i < len - 5 &&
                            b2 == bAnd && b3 == bAt && b4 == bEscape &&
                            bytes[i + 4] == bDollar && bytes[i + 5] == bB) {
                            //JIS_0208 1990
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                    }
                }
            }

            //should be euc|sjis|utf8
            //use of (?:) by Hiroki Ohzaki <ohzaki@iod.ricoh.co.jp>
            int sjis = 0;
            int euc = 0;
            int utf8 = 0;
            for (int i = 0; i < len - 1; i++) {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
                    ((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC))) {
                    //SJIS_C
                    sjis += 2;
                    i++;
                }
            }
            for (int i = 0; i < len - 1; i++) {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
                    (b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF))) {
                    //EUC_C
                    //EUC_KANA
                    euc += 2;
                    i++;
                } else if (i < len - 2) {
                    b3 = bytes[i + 2];
                    if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
                        (0xA1 <= b3 && b3 <= 0xFE)) {
                        //EUC_0212
                        euc += 3;
                        i += 2;
                    }
                }
            }
            for (int i = 0; i < len - 1; i++) {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF)) {
                    //UTF8
                    utf8 += 2;
                    i++;
                } else if (i < len - 2) {
                    b3 = bytes[i + 2];
                    if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
                        (0x80 <= b3 && b3 <= 0xBF)) {
                        //UTF8
                        utf8 += 3;
                        i += 2;
                    }
                }
            }
            //M. Takahashi's suggestion
            //utf8 += utf8 / 2;

            //System.Diagnostics.Debug.WriteLine(string.Format("sjis = {0}, euc = {1}, utf8 = {2}", sjis, euc, utf8));
            if (euc > sjis && euc > utf8) {
                //EUC
                return System.Text.Encoding.GetEncoding(51932);
            } else if (sjis > euc && sjis > utf8) {
                //SJIS
                return System.Text.Encoding.GetEncoding(932);
            } else if (utf8 > euc && utf8 > sjis) {
                //UTF8
                return System.Text.Encoding.UTF8;
            }

            return null;
        }
    }
}
