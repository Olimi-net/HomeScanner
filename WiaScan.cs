using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using WIA;

/// <author>
/// Lada 
/// </author>
/// <created>
/// 2019.05.28
/// </created>

namespace Scaner
{
    public class ScanArg : EventArgs
    {
        public int Status;

        public string Message { get; private set; }

        public ScanArg(Exception ex)
        {
            Status = 0;
            Message = ex.Message;
        }

        public ScanArg(int p, string msg)
        {
            Status = p;
            Message = msg;
        }

    }

    public class WiaScan
    {
        public const string wiaFormatBMP = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";
        public const string wiaFormatGIF = "{B96B3CB0-0728-11D3-9D7B-0000F81EF32E}";
        public const string wiaFormatJPEG = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
        public const string wiaFormatPNG = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
        public const string wiaFormatTIFF = "{B96B3CB1-0728-11D3-9D7B-0000F81EF32E}";

        public string Dir = "";

        WIA.ICommonDialog dialog;
        WIA.Device device;
        private string _deviceId;

        int dpi = 300;

        public WiaScan()
        {
            
        }

        public EventHandler<ScanArg> ScanerEvent;

        public void Init(string dir)
        {
            try
            {
                Dir = dir;

                dialog = new WIA.CommonDialog();
                 device = dialog.ShowSelectDevice(WIA.WiaDeviceType.UnspecifiedDeviceType, true, false);
                _deviceId = device.DeviceID;
                
                if (ScanerEvent != null)
                {
                    ScanerEvent.Invoke(this, new ScanArg(1, ""));
                }
            }
            catch (Exception ex)
            {
                if (ScanerEvent != null)
                {
                    ScanerEvent.Invoke(this, new ScanArg(ex));
                }
            }
        }

        private static void SetWIAProperty(IProperties properties, object propName, object propValue)
        {
            Property prop = properties.get_Item(ref propName);
            prop.set_Value(ref propValue);
        }

        public string Scan()
        {
            try
            {
                if (ScanerEvent != null)
                {
                    ScanerEvent.Invoke(this, new ScanArg(3, ""));
                }

                var item = device.Items[1];
                

                //SetWIAProperty(item.Properties, "6146", 1); // 1 Color
                SetWIAProperty(item.Properties, "6147", dpi); // dpis 
                SetWIAProperty(item.Properties, "6148", dpi); // dpis 
                // This line throws the exception  
                //SetWIAProperty(item.Properties, "3097", 100); // page size 0=A4, 1=letter, 2=custom, 100=auto

                WIA.ImageFile image = (WIA.ImageFile)dialog.ShowTransfer(item, wiaFormatPNG);

                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".jpeg";

                string tmp = "img.tmp";
                if(File.Exists(tmp))
                    File.Delete(tmp);

                image.SaveFile(tmp);
                image = null;

                using (var img = Bitmap.FromFile(tmp))
                {
                    img.Save(Path.Combine(Dir, fileName), ImageFormat.Jpeg);
                }
                if (ScanerEvent != null)
                {
                    ScanerEvent.Invoke(this, new ScanArg(1, fileName));
                }

                return fileName;
            }
            catch (Exception ex)
            {
                if (ScanerEvent != null)
                {
                    ScanerEvent.Invoke(this, new ScanArg(ex));
                }

                return null;
            }
        }         
    }
}