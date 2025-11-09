using System.IO;
using System.IO.Compression;
using System.Text;

namespace NIS.Models
{
    public class CompressTool
    {

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                dest.Write(bytes, 0, cnt);
        }

        /// <summary>
        /// 將文字壓縮
        /// </summary>
        /// <param name="str">需要壓縮的字串</param>
        /// <returns>二進制陣列</returns>
        public static byte[] CompressString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }

        /// <summary>
        /// 解壓縮文字
        /// </summary>
        /// <param name="bytes">待解壓的文字</param>
        /// <returns>原文</returns>
        public static string DecompressString(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

    }
}