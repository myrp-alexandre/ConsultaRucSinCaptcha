﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.IO;
//using System.Web;
//using MSXML2;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using Tesseract;
using System.Web;

namespace WSConsultaRuc.Clases
{
    public class Empresas
    {
        public enum Resul
        {
            Ok = 0,
            NoResul = 1,
            ErrorCapcha = 2,
            Error = 3,
        }
        private Resul state;
        private string _RazonSocial,_ActividadComercioExterior, _TripoComtribuyente, _NombreComercial, _Direccion, _FechaInscripcion, _EstadoContribuyente, _FechaInicioActividad, _CondicionContribuyente, _SistemaEmisionComprobante, _SistemaContabilidad;
        


        CookieContainer cokkie = new CookieContainer();

        private CookieContainer myCookie;

        public Image GetCapcha { get { return ReadCapcha(); } }
        public string RazonSocial { get { return _RazonSocial; } }
        public string NombreComercial { get { return _NombreComercial; } }
        public string TipoComtribuyente { get { return _TripoComtribuyente; } }
        public string Direccion { get { return _Direccion; } }
        public string FechaInscripcion { get { return _FechaInscripcion; } }
        public string EstadoContribuyente { get { return _EstadoContribuyente; } }
        public string FechaInicioActividad { get { return _FechaInicioActividad; } }
        public string CondicionContribuyente { get { return _CondicionContribuyente; } }
        public string SistemaEmisionComprobante { get { return _SistemaEmisionComprobante; } }
        public string SistemaContabilidad { get { return _SistemaContabilidad; } }
        public string ActividadComercioExterior { get { return _ActividadComercioExterior; } }

        public Resul GetResul { get { return state; } }
        TesseractEngine engine;

        public Empresas()
        {
            try
            {
                myCookie = null;
                myCookie = new CookieContainer();
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                ReadCapcha();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Boolean ValidarCertificado(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        //Aqui obtenemos el captcha
        private Image ReadCapcha()
        {
            try
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidarCertificado);
                //Esta es la direccion que les pase en el grupo de facebook para obtener el captcha
                HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create("http://www.sunat.gob.pe/cl-ti-itmrconsruc/captcha?accion=image&magic=2");
                myWebRequest.CookieContainer = myCookie;
                myWebRequest.Proxy = null;
                myWebRequest.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse myWebResponse = (HttpWebResponse)myWebRequest.GetResponse();
                Stream myImgStream = myWebResponse.GetResponseStream();
                //Modificación 1 ... Esta fue la primera modificación ... cree un mapa de bits que utilizaré como
                //parámetro para en fin ... mejor se los muestro xd
                Bitmap bm = new Bitmap(Image.FromStream(myImgStream));
                //quitamos el color a nuestro mapa de bits 
                qutarColor(bm);
                //Procesamos la imagen (separación de carácteres, alineación etc)
                //Y se devuelve la imagen lista para ser procesada por el OCR
                return (Image)PreProcessImage(bm);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //En este metodo es el que utiliza el tesseract ... se obtiene la imagen del captcha terminada
        // y devuelve el texto obtenido ...


        public string UseTesseract()
        {

            string text = String.Empty;
            //Recordemos que el metodo ( si ya obviaré las tildes ) ... 
            // el metodo ReadCapcha devuelve la imagen ya procesada ...
            using (Bitmap bm = new Bitmap(ReadCapcha()))
            {
                //Instanciamos el TesseractEngine declarado arriba !
                engine = new TesseractEngine(@"C:\Sistema\tessdata", "eng", EngineMode.Default);
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                Tesseract.Page p = engine.Process(bm);
                text = p.GetText().Trim().ToUpper().Replace(" ", "");
                //  Console.WriteLine("Text recognized: " + text);
            }
            //Retornamos luego del trabajo del OCR el texto obtenido 
            return text;
        }
        //En este metodo se procesa la imagen, se separan los caracteres de manera individual
        private static Bitmap PreProcessImage(Bitmap memStream)
        {
            Bitmap bm = memStream;
            // Flatten Image to Black and White
            qutarColor(bm);

            // We have a know 6 charcter captcha
            List<Rectangle> charcters = new List<Rectangle>();
            List<int> blackin_x = new List<int>();

            int x_max = bm.Width - 1;
            int y_max = bm.Height - 1;

            // Here we are going to scan through the columns to determine if there in any black in them (charcter)
            for (int temp_x = 0; temp_x <= x_max; temp_x++)
            {
                for (int temp_y = 0; temp_y <= y_max; temp_y++)
                {
                    if (bm.GetPixel(temp_x, temp_y).Name != "ffffffff")
                    {
                        blackin_x.Add(temp_x);
                        break;
                    }
                }
            }

            // Building inital rectangles with X Boundaries
            // This is where we are using our previous results to build the horiztonal boundaries of our charcters
            int temp_start = blackin_x[0];
            for (int temp_x = 0; temp_x < blackin_x.Count - 1; temp_x++)
            {
                if (temp_x == blackin_x.Count - 2) // handles the last iteration
                {
                    Rectangle r = new Rectangle();
                    r.X = temp_start;
                    r.Width = blackin_x[temp_x] - r.X + 2;

                    charcters.Add(r);
                }
                if (blackin_x[temp_x] - blackin_x[temp_x + 1] == -1)
                {
                    continue;
                }
                else
                {
                    Rectangle r = new Rectangle();
                    r.X = temp_start;
                    r.Width = blackin_x[temp_x] - r.X + 1;
                    temp_start = blackin_x[temp_x + 1];
                    charcters.Add(r);
                }

            }

            // Finish out by getting y boundaries
            for (int i = 0; i < charcters.Count; i++)
            {
                Rectangle r = charcters[i];

                for (int temp_y = 0; temp_y < y_max; temp_y++)
                {
                    if (r.Y == 0)
                    {
                        if (!IsRowWhite(bm, temp_y, r.X, r.X + r.Width - 1))
                            r.Y = temp_y;
                    }
                    else if (r.Height == 0)
                    {
                        if (IsRowWhite(bm, temp_y, r.X, r.X + r.Width - 1))
                            r.Height = temp_y - r.Y + 1;
                    }
                    else
                        break;

                }

                charcters[i] = r; // have to do this as rectangle is struct

            }

            int totalWidth = 1 + charcters.Sum(o => o.Width) + (charcters.Count * 2); // we need padding
            int totalHeight = charcters.Max(o => o.Height) + 2; // padding here too 
            int current_x = 1; // start off the left edge 1px

            Bitmap bmp = new Bitmap(totalWidth, totalHeight);
            Graphics g = Graphics.FromImage(bmp);

            // the following four lines are added to help image quality
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.High;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // take our four charcters and move them into a new bitmap 
            foreach (Rectangle r in charcters)
            {
                g.DrawImage(bm, current_x, 1, r, GraphicsUnit.Pixel);
                current_x += r.Width + 2;
            }

            //  bmp.Save(@"C:\postprocess.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            return bmp;
        }

        /// <summary>
        /// Determines whether the specified row in the bitmap contains white.
        /// </summary>
        /// <param name="bm">The Image.</param>
        /// <param name="temp_y">The temp_y.</param>
        /// <param name="x">The x.</param>
        /// <param name="width">The width.</param>
        /// <returns></returns>
        private static bool IsRowWhite(Bitmap bm, int temp_y, int x, int width)
        {
            for (int i = x; i < width; i++)
            {
                if (bm.GetPixel(i, temp_y).Name != "ffffffff")
                    return false;
            }
            return true;
        }
        // Aqui quitamos el color ... lo dejamos en blanco y negro (El captcha)
        public static void qutarColor(Bitmap bm)
        {
            for (int x = 0; x < bm.Width; x++)
                for (int y = 0; y < bm.Height; y++)
                {
                    Color pix = bm.GetPixel(x, y);
                    //Aqui puedes jugar con los valores del brillo yo he probado poco pero tu puedes cambiarlo
                    if (pix.GetBrightness() > 0.870f)
                    {
                        bm.SetPixel(x, y, Color.White);
                    }
                    else
                    {
                        bm.SetPixel(x, y, Color.Black);
                    }
                }
        }
        public void GetInfo(string numDni, string ImgCapcha)
        {
            string vRazonSocial = "",vActividadComercioExterior="", vCondicionContribuyente="",vEstadoContribuyente = "",vTipoComtribuyente = "",vDireccion = "", vSistemaEmisionComprobante="";
            string vFechaInscripcion = "",vNombreComercial="", vFechaInicioActividad= "",vSistemaContabilidad="";
            try
            {
                //A este link le pasamos los datos , RUC y valor del captcha
                string myUrl = String.Format("http://www.sunat.gob.pe/cl-ti-itmrconsruc/jcrS00Alias?accion=consPorRuc&nroRuc={0}&codigo={1}",
                                        numDni, ImgCapcha);
                HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(myUrl);
                myWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:23.0) Gecko/20100101 Firefox/23.0";
                myWebRequest.CookieContainer = myCookie;
                myWebRequest.Credentials = CredentialCache.DefaultCredentials;
                myWebRequest.Proxy = null;
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myWebRequest.GetResponse();
                Stream myStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myStream);
                //Leemos los datos
                string xDat = HttpUtility.HtmlDecode(myStreamReader.ReadToEnd());
                if (xDat.Length <= 635)
                {
                    return;
                }
                string[] tabla;
                xDat = xDat.Replace("     ", " ");
                xDat = xDat.Replace("    ", " ");
                xDat = xDat.Replace("   ", " ");
                xDat = xDat.Replace("  ", " ");
                xDat = xDat.Replace("( ", "(");
                xDat = xDat.Replace(" )", ")");
                //Lo convertimos a tabla o mejor dicho a un arreglo de string como se ve declarado arriba
                tabla = Regex.Split(xDat, "<td class");
                //separamos el arreglo que hasta ese momento tenia 1 solo item , y lo dividimos por la etiqueta tdclass
                //Esto lo hice porque cuando es persona el ruc empieza con 1 
                //y tiene un resultado distinto a cuando es empresa ...
                if (numDni.StartsWith("1"))
                {
                    //tabla[17] = tabla[17].Replace("=\"bg\" colspan=3>", "");
                    //tabla[17] = tabla[17].Replace("</td>\r\n </tr>\r\n<!-- SE COMENTO POR INDICACION DEL PASE PAS20134EA20000207 -->\r\n<!-- <tr> -->\r\n<!-- ", "");
                    //vRazonSocial = (string)tabla[1];
                    //vDireccion = (string)tabla[17];

                    //RAZON SOCIAL
                    tabla[1] = tabla[1].Replace("=\"bg\" colspan=3>" + numDni + " - ", "").Replace("</td>\r\n </tr>\r\n <tr>\r\n", "");

                    //TIPO CONTRIBUYENTE
                    tabla[3] = tabla[3].Replace("=\"bg\" colspan=3>", "").Replace("</td>\r\n </tr>\r\n <tr>\r\n", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //NOMBRE COMERCIAL
                    tabla[7] = tabla[7].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //DIRECCION
                    tabla[19] = tabla[19].Replace("=\"bg\" colspan=3>", "").Replace("</td>\r\n </tr>\r\n<!-- SE COMENTO POR INDICACION DEL PASE PAS20134EA20000207 -->\r\n<!-- <tr> -->\r\n<!-- ", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Replace("�", "Ñ");

                    //FECHA INSCRIPCION
                    tabla[9] = tabla[9].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Remove(10);

                    //ESTADO CONTRIBUYENTE
                    tabla[12] = tabla[12].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //FECHA INICIO ACTIVIDAD
                    tabla[10] = tabla[10].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //CONDICION CONTRIBUYENTE
                    tabla[15] = tabla[15].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //SISTEMA DE EMISION DE COMPROBANTE
                    tabla[21] = tabla[21].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //ACTIVIDAD DE COMERCIO EXTERIOR
                    tabla[23] = tabla[23].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //SISTEMA CONTABILIDAD
                    tabla[25] = tabla[25].Replace("=\"bg\" colspan=3>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Replace("<!--I#P_SNADE003-1#20141109#CFS-->", "").Replace("<!--", "");

                    //-------------------------------
                    vNombreComercial = tabla[7].ToString().Trim();
                    vRazonSocial = tabla[1].ToString().Trim();
                    vDireccion = tabla[19].ToString().Trim();
                    vTipoComtribuyente = tabla[3].Trim();
                    vFechaInscripcion = tabla[9].ToString().Trim();
                    vFechaInicioActividad = tabla[10].ToString().Trim();
                    vEstadoContribuyente = tabla[12].ToString().Trim();
                    vCondicionContribuyente = tabla[15].ToString().Trim();

                    vSistemaEmisionComprobante = tabla[21].ToString().Trim();
                    vActividadComercioExterior = tabla[23].ToString().Trim();
                    vSistemaContabilidad = tabla[25].ToString().Trim();
                }
                //RUC 20
                else if (numDni.StartsWith("2"))
                {
                    //RAZON SOCIAL
                    tabla[1] = tabla[1].Replace("=\"bg\" colspan=3>" + numDni + " - ", "").Replace("</td>\r\n </tr>\r\n <tr>\r\n", "");
                    
                    //NOMBRE COMERCIAL
                    tabla[5] = tabla[5].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>","").Replace("<tr>","");

                    //TIPO CONTRIBUYENTE
                    tabla[3] = tabla[3].Replace("=\"bg\" colspan=3>", "").Replace("</td>\r\n </tr>\r\n <tr>\r\n", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //DIRECCION
                    tabla[15] = tabla[15].Replace("=\"bg\" colspan=3>", "").Replace("</td>\r\n </tr>\r\n<!-- SE COMENTO POR INDICACION DEL PASE PAS20134EA20000207 -->\r\n<!-- <tr> -->\r\n<!-- ", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Replace("�","Ñ");

                    //FECHA INSCRIPCION
                    tabla[7] = tabla[7].Replace("=\"bg\" colspan=1>","").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Remove(10);

                    //ESTADO CONTRIBUYENTE
                    tabla[10] = tabla[10].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //FECHA INICIO ACTIVIDAD
                    tabla[8] = tabla[8].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //CONDICION CONTRIBUYENTE
                    tabla[13] = tabla[13].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //SISTEMA DE EMISION DE COMPROBANTE
                    tabla[17] = tabla[17].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //ACTIVIDAD DE COMERCIO EXTERIOR
                    tabla[19] = tabla[19].Replace("=\"bg\" colspan=1>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "");

                    //SISTEMA CONTABILIDAD
                    tabla[21] = tabla[21].Replace("=\"bg\" colspan=3>", "").Replace("</td>", "").Replace("</tr>", "").Replace("<tr>", "").Replace("<!--I#P_SNADE003-1#20141109#CFS-->","").Replace("<!--","");

                    //-------------------------------
                    vNombreComercial = tabla[5].ToString().Trim();
                    vRazonSocial = tabla[1].ToString().Trim();
                    vDireccion = tabla[15].ToString().Trim();
                    vTipoComtribuyente = tabla[3].Trim();
                    vFechaInscripcion = tabla[7].ToString().Trim();
                    vFechaInicioActividad = tabla[8].ToString().Trim();
                    vEstadoContribuyente = tabla[10].ToString().Trim();
                    vCondicionContribuyente = tabla[13].ToString().Trim();
                    vSistemaEmisionComprobante = tabla[17].ToString().Trim();
                    vActividadComercioExterior = tabla[19].ToString().Trim();
                    vSistemaContabilidad = tabla[21].ToString().Trim();
                }

                
                _RazonSocial = vRazonSocial;
                _NombreComercial = vNombreComercial;
                _Direccion = vDireccion;
                _TripoComtribuyente = vTipoComtribuyente;
                _FechaInscripcion = vFechaInscripcion;
                _FechaInicioActividad = vFechaInicioActividad;
                _EstadoContribuyente = vEstadoContribuyente;
                _CondicionContribuyente = vCondicionContribuyente;
                _SistemaEmisionComprobante = vSistemaEmisionComprobante;
                _ActividadComercioExterior = vActividadComercioExterior;
                _SistemaContabilidad = vSistemaContabilidad;
            }
            catch (Exception ex)
            {
                _RazonSocial = "Error";
                // throw ex;
            }
        }
    }
}