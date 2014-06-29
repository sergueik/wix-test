/* ========================================================================== 
Proyecto: Asistente.Browser
Empresa: 
Creado por: Hugo González Olaya
Fecha creacion: 2011-01-31
Fecha Actualizacion: 2011-01-31
Descripcion: Clase con utilidades.
========================================================================== */
/* ========================================================================== 
Referencias: 
========================================================================== 
Historial de versiones (Actualizado: 2011-01-31)
1.0.0	Creado por: Hugo González Olaya
========================================================================== */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;

namespace WixDataBase.CustomAction.Utilities
{
    /// <summary>
    /// Clase con utilidades.
    /// </summary>
    public static class InstallUtilities
    {
        #region Fields
        /// <summary>
        /// Ensamblado actual
        /// </summary>
        public static Assembly AssemblyCurrent;
        #endregion Fields

        #region Methods
        /// <summary>
        /// Abre cuadro de diálogo para seleccionar nombre de carpeta.
        /// </summary>
        /// <param name="folderDefault">Carpeta predeterminada.</param>
        /// <returns>Retorna nombre de carpeta.</returns>
        public static string GetFolder(string folderDefault)
        {
            string sPath = folderDefault;
            var t = new Thread((ThreadStart)(() =>
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.RootFolder = System.Environment.SpecialFolder.MyComputer;
                fbd.Description = "Seleccione una carpeta";
                fbd.SelectedPath = folderDefault;
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    sPath = fbd.SelectedPath;
                }
                else
                {
                    return;
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            return sPath;
        }

        /// <summary>
        /// Retorna un número de caracteres desde la derecha de un texto.
        /// </summary>
        /// <param name="text">Texto.</param>
        /// <param name="length">Número de caracteres a ser retornados.</param>
        /// <returns>Retorna un número de caracteres desde la derecha de un texto.</returns>
        public static string Right(string text, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            else if (length == 0 || text.Length == 0)
            {
                return "";
            }
            else if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            else if (text.Length <= length)
            {
                return text;
            }
            else
            {
                return text.Substring(text.Length - length, length);
            }

        }


        public static IPAddress GetExternalIp()
        {

            string whatIsMyIp = "http://whatismyip.com";

            string getIpRegex = @"(?<=<TITLE>.*)\d*\.\d*\.\d*\.\d*(?=</TITLE>)";

            WebClient wc = new WebClient();

            UTF8Encoding utf8 = new UTF8Encoding();

            string requestHtml = "";

            try
            {

                requestHtml = utf8.GetString(wc.DownloadData(whatIsMyIp));

            }

            catch (WebException we)
            {

                // do something with exception  

                Console.Write(we.ToString());

            }

            Regex r = new Regex(getIpRegex);

            Match m = r.Match(requestHtml);

            IPAddress externalIp = null;

            if (m.Success)
            {

                externalIp = IPAddress.Parse(m.Value);

            }

            return externalIp;

        }

        /// <summary>
        /// Escribe un mensaje en log de instalación.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="message">Mensaje.</param>
        /// <param name="ex">Excepción.</param>
        /// <param name="includeLine">Indica si incluye texto para separador de mensajes al inicio.</param>
        public static void WriteLogInstall(Session session, string message, Exception ex, bool includeLine)
        {
            if (session != null)
            {
                if (includeLine)
                {
                    session.Log(DateTime.Now.ToString("yyy-MM-dd HH:mm:ss") + ": ============================================================" );
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    session.Log(message);
                }
                if (ex != null)
                {
                    session.Log("Excepción:");
                    session.Log(ex.Message);
                }
            }
        }
        #endregion Methods
    }

    /// <summary>
    /// Rutas de instalación de archivos de base de datos.
    /// Utilizado en scripts de base de datos al crear base de datos.
    /// </summary>
    public class DataBasePathTO
    {
        /// <summary>
        /// Nombre de variable en script de base de datos a ser reemplazado con la ruta suministrada: <see cref="Path"/>:
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Descripción a ser presentada al usuario en el formulario de rutas.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Ruta de instalación de archivo de base de datos.
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Caracteristica a ser instalada.
    /// </summary>
    public class FeactureInstallTO
    {
        /// <summary>
        /// Nombre de caracteristica
        /// </summary>
        public string Feature { get; set; }

        /// <summary>
        /// Titulo de característica.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Ruta del directorio donde se encuentra el archivo instalado.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Nombre del archivo instalado.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Orden de presentación en el formulario de caracteristicas.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
