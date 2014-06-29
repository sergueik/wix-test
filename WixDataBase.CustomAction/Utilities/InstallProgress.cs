/* ========================================================================== 
Proyecto: Instalador
Empresa: 
Creado por: Hugo Gonzalez Olaya
Fecha creacion: 2012-01-31
Fecha Actualizacion: 2012-01-31
Descripcion: Instalador de aplicación.
========================================================================== */
/* ========================================================================== 
Referencias: 
Microsoft.Deployment.WindowsInstaller
System.Windows.Forms
========================================================================== 
Historial de versiones (Actualizado: 2011-01-31)
1.0.0	Creado por: Hugo González Olaya
========================================================================== */

/* ========================================================================== 
Links: Progress bar: http://taocoyote.wordpress.com/2009/05/19/adding-managed-custom-actions-to-the-progressbar/
========================================================================== */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace WixDataBase.CustomAction.Utilities
{
    /// <summary>
    /// Clase con métodos para presentar una barra progreso.
    /// </summary>
    public static class InstallProgress
    {
        /// <summary>
        /// Reinicia la barra de progreso a cero y establece un nuevo valor para el total de ticks.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="totalTicks">Número total de ticks.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult ResetProgress(Session session, int totalTicks)
        {
            Record record = new Record(4);
            record[1] = "0"; // Indica reinicia barra de progreso y establece número total de ticks de la barra
            
            record[2] = totalTicks.ToString(); // Total ticks

            // Mover barra de progreso 
            // 0: Hacia adelante de izquierda a derecha. 1: Hacia atras de derecha a izquierda)
            record[3] = "0";

            // 0: Ejecución en progreso, la UI calcula y presenta el tiempo restante. 
            // 1: Creando script de ejecución. La UI presenta un mensaje mientras el instalador termina de preparar la instalación.
            record[4] = "0";

            return session.Message(InstallMessage.Progress, record);
        }

        /// <summary>
        /// Establece la cantidad a incrementar en la barra de progreso para cada mensaje ActionData enviado.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="ticks">Número de ticks a ser adicionados a la barra de progreso.</param>
        /// <param name="increaseForActionData">Indica si aumenta la barra de progreso con cada mensaje enviado por ActionData:
        /// <para>False: Indica enviar el mensaje actual al reporte de progreso.</para>
        /// <para>True: Indica incrementar la barra de progreso, en cada ActionData, en el número de ticks espeficicados en <paramref name="ticks"/>.</para>
        /// </param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult NumberOfTicksPerActionData(Session session, int ticks, bool increaseForActionData)
        {
            Record record = new Record(3);
            record[1] = "1"; // Suministra información relacionada al mensaje de progreso enviados por la acción actual
            
            // ticks a incrementar en cada mensaje ActionData enviado por la custom action, este valor es ignorado si el campo 3 es 0
            record[2] = ticks.ToString();

            if (increaseForActionData)
            {
                record[3] = "1"; // Indica incrementar la barra de progreso, en cada ActionData, en el número de ticks espeficicados en el campo 2
            }
            else
            {
                record[3] = "0"; // Indica enviar el mensaje actual al reporte de progreso  
            }
            return session.Message(InstallMessage.Progress, record);
        }

        /// <summary>
        /// Presenta un mensaje ActionData en el dialogo e incrementa la barra de progreso.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="message">Mensaje a ser presentado.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult DisplayActionData(Session session, string message)
        {
            Record record = new Record(1);
            record[1] = message;
            return session.Message(InstallMessage.ActionData, record);
        }

        /// <summary>
        /// Presenta un mensaje ActionData en el dialogo e incrementa la barra de progreso.
        /// La plantilla debe establecida en el método <see cref="DisplayStatusActionStart()"/> debe ser similar a:
        /// "Script [1] / [2]"
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="parameter1">Parámetro 1.</param>
        /// <param name="parameter2">Parámetro 2.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult DisplayActionData2(Session session, string parameter1, string parameter2)
        {
            Record record = new Record(2);
            record[1] = parameter1;
            record[2] = parameter2;
            //record[3] = "Ejecutando la tarea [1] de [2]";
            return session.Message(InstallMessage.ActionData, record);
        }

        /// <summary>
        /// Presenta un mensaje ActionData en el dialogo e incrementa la barra de progreso.
        /// La plantilla debe establecida en el método <see cref="DisplayStatusActionStart()"/> debe ser similar a:
        /// "Script [1] / [2]: [3]"
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="parameter1">Parámetro 1.</param>
        /// <param name="parameter2">Parámetro 2.</param>
        /// <param name="parameter3">Parámetro 3.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult DisplayActionData3(Session session, string parameter1, string parameter2, string parameter3)
        {
            Record record = new Record(3);
            record[1] = parameter1;
            record[2] = parameter2;
            record[3] = parameter3;
            return session.Message(InstallMessage.ActionData, record);
        }

        /// <summary>
        /// Actualiza el mensaje de estado presentado al usuario
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="actionName">Nombre de la acción a ser ejecutado.</param>
        /// <param name="status">Texto con el estado. El texto es presentado en el control que se suscribe al evento "ActionText".</param>
        /// <param name="template">Plantilla del texto de avance de proceso. 
        /// El texto es presentado en un control que se suscribe al evento: ActionData
        /// Ejemplo: "Ejecutando la tarea [1] de [2]"</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult DisplayStatusActionStart(Session session, string actionName, string status, string template)
        {
                Record record = new Record(3);
                record[1] = actionName;
                record[2] = status;
                record[3] = template;
                return session.Message(InstallMessage.ActionStart, record);
        }

        /// <summary>
        /// Aumenta el valor actual de total ticks en la cantidad especificada.
        /// Puede usar este método el fase UI, por una custom action con el atributo Execute="immediate" 
        /// cuando prepara los scripts de ejecución.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="totalTicks">Número de ticks a ser adicionados.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult IncrementTotalTicks(Session session, int totalTicks)
        {
            Record record = new Record(2);
            record[1] = "3"; // Indica aumentar el número total de ticks de la barra de progreso.
            record[2] = totalTicks.ToString(); // Número de ticks a ser adicionados al total actual de la barra de progreso.
            return session.Message(InstallMessage.Progress, record);
        }

        /// <summary>
        /// Incrementa la barra de progreso en el número de ticks indicado.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="tickIncrement">Número de ticks de incremento.</param>
        /// <returns>Retorna resultado del manejador de mensajes.</returns>
        public static MessageResult IncrementProgress(Session session, int tickIncrement)
        {
            Record record = new Record(2);
            record[1] = "2"; // Indica incrementar la barra de progreso
            record[2] = tickIncrement.ToString(); // Número de ticks a ser incrementados a la barra de progreso.
            //record[3] = 0;
            return session.Message(InstallMessage.Progress, record);
        }

        public static void ThrowInstallError(Session session, string errorMessage)
        {
            Record record = new Record(1);
            record[1] = errorMessage;

            session.Message(InstallMessage.FatalExit, record);
        }

    }
}
