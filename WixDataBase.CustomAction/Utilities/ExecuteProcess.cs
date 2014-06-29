/* ========================================================================== 
Proyecto: Asistente.Browser
Empresa: 
Creado por: Hugo González Olaya
Fecha creacion: 2011-01-31
Fecha Actualizacion: 2011-01-31
Descripcion: Clase con utilidades para ejecutar proceso como *.EXE, *.MSI, *.BAT.
========================================================================== */
/* ========================================================================== 
Referencias: 
System.Configuration.Install
System.Deployment
System.Drawing
System.Windows.Forms 
========================================================================== 
Historial de versiones (Actualizado: 2011-01-31)
1.0.0	Creado por: Hugo González Olaya
========================================================================== */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace WixDataBase.CustomAction.Utilities
{
    /// <summary>
    /// Clase con utilidades para ejecutar proceso como *.EXE, *.MSI, *.BAT.
    /// Desde un instalador puede invocar otro instalador EXE o MSI.
    /// </summary>
    public class ExecuteProcess
    {
        #region Fields
        /// <summary>
        /// Ruta en la cual dejar información de log.
        /// </summary>
        private string m_pathLog = "";

        /// <summary>
        /// Localización del archivo a ser ejecutado.
        /// </summary>
        private string m_filePath = "";

        /// <summary>
        /// Proceso en el cual es ejecutado el ejecutable..
        /// </summary>
        private Process m_process = null;

        /// <summary>
        /// Espera para terminar los procesos en milesegundos (5 min = 300000, 2 min = 120000).
        /// </summary>
        private int m_waitForExit = 300000;

        /// <summary>
        /// Notificación al hilo que el proceso de instalación (Setup.exe) ha terminado.
        /// </summary>
        private AutoResetEvent m_setupCompleted = new AutoResetEvent(false);
        #endregion Fields

        #region Constructs
        /// <summary>
        /// Constructor, crea una nueva instancia.
        /// </summary>
        /// <param name="pathLog">Ruta en la cual dejar información de log.</param>
        /// <param name="filePaht">Localización del archivo a ser ejecutado.</param>
        public ExecuteProcess(string pathLog, string filePaht)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_filePath))
                    throw new ArgumentNullException("_filePath");

                m_pathLog = pathLog;
                m_filePath = filePaht;

                m_process.Exited += new EventHandler(ExecuteProcess_Exited);
                m_process.EnableRaisingEvents = true;

               // _process.WaitForExit(_waitForExit);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion Constructs

        #region Public methods
        /// <summary>
        /// Maneja el evento de notificación al hilo que el proceso de instalación (Setup.exe) ha terminado.
        /// </summary>
        /// <param name="sender">Objeto que dispara el evento..</param>
        /// <param name="e">Argumentos <seealso cref="EventArgs"/> con datos del evento..</param>
        public void setupExeExited(object sender, EventArgs e)
        {
            m_setupCompleted.Set();
        }

        /// <summary>
        /// Invoca el proceso utilizando la clase <seealso cref="System.Diagnostics.Process"/>.
        /// </summary>
        public Process Start()
        {
            return Start(null);
        }

        /// <summary>
        /// Invoca el proceso utilizando la clase <seealso cref="System.Diagnostics.Process"/>.
        /// </summary>
        /// <param name="arguments">Argumentos para ejecutar el archivo.</param>
        public Process Start(string arguments)
        {
            Process rtnProcess = null;

            try
            {
                if (string.IsNullOrWhiteSpace(m_filePath))
                    throw new ArgumentNullException("_filePath");

                if (null != arguments)
                {
                    rtnProcess = Process.Start(m_filePath, arguments);
                }
                else
                {
                    rtnProcess = Process.Start(m_filePath);
                }
            }
            catch 
            {
                throw;
            }

            return rtnProcess;
        }
        #endregion Public methods

        #region Handle events
        /// <summary>
        /// Maneja el evento <seealso cref="Exited"/> disparado al terminar el proceso.
        /// </summary>
        /// <param name="sender">Objeto que dispara el evento.</param>
        /// <param name="e">Argumentos <seealso cref="EventArgs"/> con datos del evento.</param>
        private void ExecuteProcess_Exited(object sender, EventArgs e)
        {
            // Do nothing.            
        }
        #endregion Handle events
    }
}
