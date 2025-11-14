// Utils/QrCodeUtil.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QRCoder;

namespace webAPI.Utils
{
    public static class QrCodeUtil
    {
        public static string? ToBase64Png(string content, int px = 200)
        {
            try
            {
                using var gen = new QRCodeGenerator();
                using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using var qr = new QRCode(data);
                using var bmp = qr.GetGraphic(10);

                using var resized = new Bitmap(bmp, new Size(px, px));
                using var ms = new MemoryStream();

                resized.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}
