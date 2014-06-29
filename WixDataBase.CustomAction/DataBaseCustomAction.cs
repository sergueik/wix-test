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
Links: 
Debug: http://blog.torresdal.net/CommentView,guid,BFEBE347-AD82-4C76-A96E-1C22AA39EFC9.aspx
SearchAndRemplace: http://dotnetbyexample.blogspot.com/2010/11/wix-configurable-search-replace-custom.html
Pasar propiedades: http://www.progtown.com/topic242296-wixmsi-customaction-impersonation-and-obtaining-property.html
Web Installer: http://blogs.planetsoftware.com.au/paul/archive/2011/02/26/creating-a-web-application-installer-with-wix-3.5-and-visual-once-more.aspx
Tabas MSI: http://msdn.microsoft.com/en-us/library/windows/desktop/aa368295(v=vs.85).aspx
Serializar Json.Net: http://json.codeplex.com/
Progress bar: http://taocoyote.wordpress.com/2009/05/19/adding-managed-custom-actions-to-the-progressbar/
Secuencia ejecución: http://code.dblock.org/msi-property-patterns-upgrading-firstinstall-and-maintenance
Ejecutar acciones al desinstalar: http://stackoverflow.com/questions/320921/how-to-add-a-wix-custom-action-that-happens-only-on-uninstall-via-msi
Expresiones condicionales: http://msdn.microsoft.com/en-us/library/aa368012%28v=VS.85%29.aspx
WixUI_InstallMode: http://neilsleightholm.blogspot.com/2008/08/customised-uis-for-wix.html
========================================================================== */

/* ========================================================================== 
VARIABLES PARA INSTALAR:
$(DATABASE_MAILBOX): Buzon de correo, usado para configurar el servicio de mail.
$(DATABASE_MAILIP): Dirección IP del servidor de correo, usado para configurar el servicio de mail.
$(DATABASE_MAILPROFILENAME): Nombre de perfil de correo, usado para configurar el servicio de mail, recomendado el mismo nombre de instancia.
$(DATABASE_OPERATORMAILBOX): Buzones de correo para recibir notificaciones, para el operador: "OperadorWixDataBase".
$(DATABASE_PROXYPASSWORD): Contraseña del usuario de Windos usado para crar cuenta proxy.
$(DATABASE_PROXYWINDOWSUSER): Usuario de Windows usado para crar cuenta proxy.
$(DATABASE_NAME): Nombre de la base de datos  
========================================================================== */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using WixDataBase.CustomAction.UI;
using System.Windows.Forms;
using WixDataBase.CustomAction.Utilities;
using System.Data.SqlClient;
using System.Reflection;
using System.IO;
using System.Transactions;
using System.Net;

namespace WixDataBase.CustomAction
{
    public class DataBaseCustomAction
    {
        #region Fields
        /// <summary>
        /// Conexión al servidor SQL Server.
        /// </summary>
        private static SqlConnection m_sqlConectionMain = new SqlConnection();
        #endregion Fields

        #region Custom actions
        /// <summary>Ejecuta scripts en el servidor de base de datos SQL Server.
        /// Declare este método en una CustomAction con Execute="deferred" y agendelo en InstallExecuteSequence con After="..."
        /// después de invocar la CustomAction que prepara la colección de valores Session.CustomActionData.
        /// La colección de valores session[propertyName] no está disponible en CustomAction con Execute="deferred", 
        /// por tanto debe establecer la colección de valores Session.CustomActionData[propertyName] 
        /// en una CustomAction con Execute="immediate".
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <returns>Retorna estado de la ejecución.</returns>
        [CustomAction]
        public static ActionResult ExecuteSQLScripts(Session session)
        {
            try
            {
                // Active mensaje solo en desarrollo para depurar               
                //MessageBox.Show("ExecuteSQLScripts: Para depurar en VS .Net, en el menú Debug, 'Attach to process' ate los procesos: msiexec.exe y rundll32.exe.",
                //    "DEPURAR: ExecuteSQLScripts", MessageBoxButtons.OK, MessageBoxIcon.Information);

                MessageResult iResult;
                string sInstallLocation, sServidor, sBaseDatos, sMensaje;
                int iTotalTicks, iTickIncrement = 1;
                bool isCustomActionData = true;

                if (session == null)
                {
                    throw new ArgumentNullException("session");
                }

                sInstallLocation = GetSessionProperty(session, "INSTALLLOCATION", isCustomActionData);
                sServidor = GetSessionProperty(session, "DATABASE_SERVERNAME", isCustomActionData);
                sBaseDatos = GetSessionProperty(session, "DATABASE_NAME", isCustomActionData);
                List<DataBasePathTO> listPaths = GetCustomTableDataBasePaths(session, isCustomActionData);
                List<FeactureInstallTO> listF = session.CustomActionData.GetObject<List<FeactureInstallTO>>("DATABASE_FEACTURESCRIPTS");
                iTotalTicks = listF.Count;

                InstallUtilities.WriteLogInstall(session, "Iniciando ExecuteSQLScripts ...", null, true);
                iResult = InstallProgress.ResetProgress(session, iTotalTicks);
                if (iResult == MessageResult.Cancel)
                {
                    return ActionResult.UserExit;
                }

                sMensaje = "Ejecutando script en servidor SQL Server: " + sServidor;
                iResult = InstallProgress.DisplayStatusActionStart(session, sMensaje, sMensaje, "[1] / [2]: [3]");
                if (iResult == MessageResult.Cancel)
                {
                    return ActionResult.UserExit;
                }

                iResult = InstallProgress.NumberOfTicksPerActionData(session, iTickIncrement, true);
                if (iResult == MessageResult.Cancel)
                {
                    return ActionResult.UserExit;
                }

                for (int i = 0; i < listF.Count; i++)
                {
                    ExecuteSQLScript(session, listF[i].DirectoryPath, listF[i].FileName, isCustomActionData);
                    iResult = InstallProgress.DisplayActionData3(session, (i + 1).ToString(), iTotalTicks.ToString(), 
                        InstallUtilities.Right(Path.Combine(listF[i].DirectoryPath, listF[i].FileName), 200));
                    if (iResult == MessageResult.Cancel)
                    {
                        return ActionResult.UserExit;
                    }
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción al ejectar script de base de datos", ex, true);
                MessageBox.Show(ex.Message, "Error al ejecutar scripts", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ActionResult.Failure;
            }
        }

        /// <summary>
        /// Establece el valor predeterminado de la propiedad Sesion["DATABASE_MAILIP"], en caso que esté en blanco.
        /// Declare este método en una CustomAction con Execute="immediate" e invoquelo desde un botón con Event="DoAction".
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <returns>Retorna estado de la ejecución.</returns>
        [CustomAction]
        public static ActionResult SetDefaultIPAdress(Session session)
        {
            try
            {
                string sHostName, sIP, sIPCurrent;

                sIPCurrent = GetSessionProperty(session, "DATABASE_MAILIP", false);
                if (string.IsNullOrWhiteSpace(sIPCurrent))
                {
                    sHostName = System.Net.Dns.GetHostName();
                    sIP = System.Net.Dns.GetHostEntry(sHostName).AddressList[0].ToString();
                    SetSessionProperty(session, "DATABASE_MAILIP", sIP);
                }
 
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción al leer IP", ex, true);
                return ActionResult.Failure;
            }
        }

        /// <summary>Presenta formulario para configurar rutas de instalación. 
        /// Prepara la colección de valores Session.CustomActionData para ser utilizados en custom action "deferred" con los datos de:
        /// <para>Propiedades con información de conexión al servidor de base de datos</para>
        /// <para>Rutas de instalación tomadas de CustomTable Id="TABLE_DATABASE_PATHS"</para>
        /// <para>Scripts de base de datos a ser instalados, tomados de la tabla MSI: Feacture, usando archivos *.sql</para>
        /// <para></para>
        /// Declare este método en una CustomAction con Execute="immediate" y agendelo en InstallExecuteSequence con Before="InstallFinalize".
        /// El método agenda las siguientes custom action Wix con Execute="deferred"y agendadas en InstallExecuteSequence después de agendar esta acción: 
        /// <para>CA_DataBaseExecuteScripts</para>
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <returns>Retorna estado de la ejecución.</returns>
        [CustomAction]
        public static ActionResult SwhowPathInstall(Session session)
        {
            try
            {
                // Active mensaje solo en desarrollo para depurar
                //MessageBox.Show("SwhowPathInstall: Para depurar, en el menú Debug, 'Attach to process' ate los procesos: msiexec.exe y rundll32.exe.",
                //    "Depurar: SwhowPathInstall", MessageBoxButtons.OK, MessageBoxIcon.Information);

                bool isCustomActionData = false;
                List<DataBasePathTO> listPaths;
                List<FeactureInstallTO> listFeactureScripts;
                List<string> listFeactureNames = new List<string>();

                InstallUtilities.WriteLogInstall(session, "Iniciando SwhowPathInstall ...", null, true); 
                if (session == null)
                {
                    throw new ArgumentNullException("session");
                }

                // Rutas de instalación tomadas de CustomTable Id="TABLE_DATABASE_PATHS" y establecer valor predeterminado de rutas
                listPaths = GetCustomTableDataBasePaths(session, isCustomActionData);

                // Abre formulario al usuario para modificar rutas de instalación de base de datos.
                frmPaths frmA = new frmPaths(listPaths);
                if (frmA.ShowDialog() != DialogResult.OK)
                {
                    throw new InstallerException("Configuración de rutas de instalación no realizada");
                }

                // Preparar CustomActionData para deferred CustomAction 
                CustomActionData data = new CustomActionData();

                // Actualiza CustomTable Id="TABLE_DATABASE_PATHS" con las rutas modificadas
                //UpdateCustomTableDataBasePaths(session, listPaths, isCustomActionData);

                // Prepara lista de rutas a ser enviadas a CustomActionData para deferred CustomAction
                data.AddObject<List<DataBasePathTO>>("DATABASE_PATHS", listPaths);

                // Adicionar la lista de rutas como una propiedad de sesión
                // data.Add("DATABASE_PATHS", JsonConvert.SerializeObject(listPaths));

                // Para recuperar la propiedad serializada
                //List<DataBasePathTO> listPaths = JsonConvert.DeserializeObject<List<DataBasePathTO>>(session.CustomActionData["DATABASE_PATHS"]);

                // Prepara propiedades a ser enviadas a CustomActionData para deferred CustomAction
                SetCustomActionData(session, "INSTALLLOCATION", data);
                SetCustomActionData(session, "DATABASE_SERVERNAME", data);
                SetCustomActionData(session, "DATABASE_NAME", data);
                SetCustomActionData(session, "DATABASE_WINDOWSAUTHENTICATION", data);
                SetCustomActionData(session, "DATABASE_AUTHENTICATEDATABASE", data);
                SetCustomActionData(session, "DATABASE_EXECUTESCRIPTS", data);
                SetCustomActionData(session, "DATABASE_USERNAME", data);
                SetCustomActionData(session, "DATABASE_PASSWORD", data);
                SetCustomActionData(session, "DATABASE_PROXYPASSWORD", data);
                SetCustomActionData(session, "DATABASE_MAILPROFILENAME", data);
                SetCustomActionData(session, "DATABASE_MAILBOX", data);
                SetCustomActionData(session, "DATABASE_MAILIP", data);
                SetCustomActionData(session, "DATABASE_OPERATORNAMENAME", data);
                SetCustomActionData(session, "DATABASE_OPERATORMAILBOX", data);
                SetCustomActionData(session, "DATABASE_PROXYWINDOWSUSER", data);

                // Scripts de base de datos a ser instalados, tomados de la tabla MSI: Feacture, usando archivos *.sql
                foreach (FeatureInfo fi in session.Features)
                {
                    if (fi.RequestState == InstallState.Local || fi.RequestState == InstallState.Source || fi.RequestState == InstallState.Default)
                    {
                        listFeactureNames.Add(fi.Name);
                        InstallUtilities.WriteLogInstall(session, "FEATURE fi.Name: " + fi.Name + ", fi.CurrentState: " + fi.CurrentState + 
                            ", fi.RequestState:" + fi.RequestState, null, false);
                    }
                }
                listFeactureScripts = GetFeactureScriptDataBase(session, listFeactureNames);
                data.AddObject<List<FeactureInstallTO>>("DATABASE_FEACTURESCRIPTS", listFeactureScripts);

                // Adicionar todas las propiedades en una sola variable
                //session["CUSTOMACTIONDATA_PROPERTIES"] = data.ToString();

                // Agendar acciones deferred  
                session.DoAction("CA_DataBaseExecuteScripts", data);

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción al establecer rutas de instalación de bases de datos.", ex, true);
                return ActionResult.Failure;
            }
        }

        /// <summary>Realiza un test a la conexión con el origen de datos.</summary>
        /// Declare este método en una CustomAction con Execute="immediate" e invoquelo desde un botón con Event="DoAction".
        /// <param name="session">Sesión Windows Installer.</param>
        /// <returns>Retorna estado de la ejecución.</returns>
        [CustomAction]
        public static ActionResult TestSqlConnection(Session session)
        {
            try
            {
                if (session == null)
                {
                    throw new ArgumentNullException("session");
                }

                SetSessionProperty(session, "DATABASE_TEST_CONNECTION", "0");
                string sConnectionString = GetConnectionString(session, false);
                using (SqlConnection sqlConect = new SqlConnection(sConnectionString))
                {
                    sqlConect.Open();
                }
                SetSessionProperty(session, "DATABASE_TEST_CONNECTION", "1");

                MessageBox.Show("Test satisfactorio!", "Test autenticación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción al realizar test de conexión al servidor de base de datos.", ex, true);
                MessageBox.Show(ex.Message, "Test autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return ActionResult.Success;
        }
        #endregion Custom actions

        #region Private methods
        /// <summary>
        /// Retorna el ensamblado actual.
        /// </summary>
        /// <returns>Retorna el ensamblado actual.</returns>
        private static Assembly GetAssembly()
        { 
            if (InstallUtilities.AssemblyCurrent == null)
            {
                InstallUtilities.AssemblyCurrent = Assembly.GetExecutingAssembly();
            }
            return InstallUtilities.AssemblyCurrent;
        }

        /// <summary>
        /// Retorna la conexión al servidor SQL Server.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retorna la conexión al servidor SQL Server.</returns>
        private static SqlConnection GetConnection(Session session, bool isCustomActionData)
        {
            string sConnectionString;
            try
            {
                if (m_sqlConectionMain == null || m_sqlConectionMain.State == System.Data.ConnectionState.Closed)
                {
                    sConnectionString = GetConnectionString(session, isCustomActionData);

                    m_sqlConectionMain.ConnectionString = sConnectionString;
                    m_sqlConectionMain.Open();
                }
                return m_sqlConectionMain;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Retorna cadena de conexión usando los datos suministrados por el usuario.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retorna cadena de conexión</returns>
        private static string GetConnectionString(Session session, bool isCustomActionData)
        {
            string sConnectionString;

            if (GetSessionProperty(session, "DATABASE_WINDOWSAUTHENTICATION", isCustomActionData) == "1")
            {
                sConnectionString = string.Format("Integrated Security=SSPI;Persist Security Info=False;Data Source={0};",
                    GetSessionProperty(session, "DATABASE_SERVERNAME", isCustomActionData).Trim());
            }
            else
            {
                sConnectionString = string.Format("Persist Security Info=False;Data Source={0};User ID={1};Password={2};",
                    GetSessionProperty(session, "DATABASE_SERVERNAME", isCustomActionData), 
                    GetSessionProperty(session, "DATABASE_USERNAME", isCustomActionData), 
                    GetSessionProperty(session, "DATABASE_PASSWORD", isCustomActionData));
            }

            if (GetSessionProperty(session, "DATABASE_AUTHENTICATEDATABASE", isCustomActionData) == "1")
            {
                sConnectionString += string.Format("Initial Catalog={0};", GetSessionProperty(session, "DATABASE_NAME", isCustomActionData));
            }
            return sConnectionString;
        }

        /// <summary>
        /// Retorna el contenido del archivo script, almacenado como un recurso dentro del ensamblado.
        /// </summary>
        /// <param name="path">Carpeta donde se encuentra el archivo.</param>
        /// <param name="fileName">Nombre de archivo script.</param>
        /// <returns>Retorna texto con el script.</returns>
        private static string GetSQLScriptFromAssembly(string path, string fileName)
        {
            StreamReader reader = null;
            string sFile = null;
            try
            {
                Assembly asm = GetAssembly();

                // Revisar rutas de recursos
                //foreach (string s in asm.GetManifestResourceNames())
                //{
                //    Console.WriteLine(s);
                //}

                // Los nombres de carpetas no deben iniciar con numero, si lo hacen debe iniciar el nombre de la carpeta con guion bajo ( _ )
                sFile = asm.GetName().Name + "." + path.Replace("\\", ".") + "." + fileName;

                Stream strm = asm.GetManifestResourceStream(sFile);
                reader = new StreamReader(strm);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InstallerException("Error al leer el archivo: " + (string.IsNullOrEmpty(sFile) ? "" : sFile), ex);
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }
                catch
                { }
            }
        }

        /// <summary>
        /// Retorna el contenido del archivo script, almacenado en un archivo en disco.
        /// </summary>
        /// <param name="path">Carpeta donde se encuentra el archivo.</param>
        /// <param name="fileName">Nombre de archivo script.</param>
        /// <returns>Retorna texto con el script.</returns>
        private static string GetSQLScriptFromFile(string path, string fileName)
        {
            string f = Path.Combine(path, fileName);
            if (File.Exists(f))
            {
                TextReader r = new StreamReader(f, Encoding.Default); //System.Text.Encoding.GetEncoding(1252)
                try
                {
                    return r.ReadToEnd();
                }
                finally
                {
                    try
                    {
                        r.Close();
                    }
                    catch
                    { }
                }
            }
            else
            {
                throw new InstallerException("Archivo no encontrado: " + (string.IsNullOrEmpty(f) ? "" : f));
            }
        }

        /// <summary>
        /// Ejecuta el script en el origen de datos.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="path">Carpeta donde se encuentra el archivo.</param>
        /// <param name="fileName">Nombre de archivo script.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retorna <c>true</c> si el proceso es satisfactorio, de lo contrario <c>false</c>.</returns>
        private static bool ExecuteSQLScript(Session session, string path, string fileName, bool isCustomActionData)
        {
            return ExecuteSQLScript(session, path, fileName, true, isCustomActionData);
        }

        /// <summary>
        /// Ejecuta el script en el origen de datos.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="path">Carpeta donde se encuentra el archivo.</param>
        /// <param name="fileName">Nombre de archivo script.</param>
        /// <param name="usingTransaction">Indica si debe usar transacciones.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retorna <c>true</c> si el proceso es satisfactorio, de lo contrario <c>false</c>.</returns>
        private static bool ExecuteSQLScript(Session session, string path, string fileName, bool usingTransaction, bool isCustomActionData)
        {
            bool bOK = true;
            string sCommandText = "", sScript = null, sMessage;
            SqlCommand command = new SqlCommand();
            SqlConnection cnx;
            int i;
            try
            {
                InstallUtilities.WriteLogInstall(session, string.Format("Begin ExecuteSQL, Path: {0}, Filename: {1} ...", path, fileName), null, false);

                // Texto script
                sScript = GetSQLScriptFromFile(path, fileName).Trim();
                sScript = ReplaceVariables(session, sScript, isCustomActionData);
                if (string.IsNullOrWhiteSpace(sScript))
                {
                    return true;
                }

                cnx = GetConnection(session, isCustomActionData);
                command.CommandType = System.Data.CommandType.Text;
                command.CommandTimeout = 0;
                command.Connection = cnx;

                // Divide el archivo en lotes separado por GO y ejecuta cada fragmento
                string[] splits = new string[] { "GO\n", "GO\r\n", "GO\t", "GO \r\n", "GO \n", "GO  \r\n", "GO  \n", "GO   \n", "GO   \r\n", "GO    \r\n" };
                string[] sSQL = sScript.Split(splits, StringSplitOptions.RemoveEmptyEntries);

                if (usingTransaction)
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        foreach (string s in sSQL)
                        {
                            sCommandText = s.Trim();
                            if (sCommandText.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
                            {
                                sCommandText = sCommandText.Substring(0, sCommandText.Length - 2);
                            }

                            if (string.IsNullOrWhiteSpace(sCommandText))
                                continue;

                            command.CommandText = sCommandText;
                            command.ExecuteNonQuery();
                        }
                        scope.Complete();
                    }
                }
                else
                {
                    foreach (string s in sSQL)
                    {
                        sCommandText = s.Trim();
                        if (sCommandText.EndsWith("GO", StringComparison.OrdinalIgnoreCase))
                        {
                            sCommandText = sCommandText.Substring(0, sCommandText.Length - 2);
                        }

                        if (string.IsNullOrWhiteSpace(sCommandText))
                            continue;

                        command.CommandText = sCommandText;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                bOK = false;
                sMessage = "Exception al ejecutar script: " + Path.Combine(path, fileName);
                InstallUtilities.WriteLogInstall(session, sMessage, ex, true);
                InstallUtilities.WriteLogInstall(session, "COMANDO EJECUTADO:", null, false);
                InstallUtilities.WriteLogInstall(session, sCommandText, null, false);

                sCommandText = sCommandText.Length > 1000 ? sCommandText.Substring(0, 1000) + " ..." : sCommandText;
                sCommandText = sMessage + Environment.NewLine + Environment.NewLine +
                    " Excepción: " + ex.Message + Environment.NewLine + Environment.NewLine + sCommandText;

                MessageBox.Show(sCommandText, "Error al ejecutar script de base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (MessageBox.Show("¿Desea continuar ejecutando el siguiente script?" + Environment.NewLine + Environment.NewLine + 
                    "  Si responde No la instalación será abortada.", "Continuar instalación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    bOK = true;
                }
                else
                {
                    throw new InstallerException(sMessage + ". " + ex.Message);
                }
            }
            finally
            {
                command.Dispose();
            }

            return bOK;
        }

        /// <summary>
        /// Reemplaza las variables del script con los valores suministrados por el usuario.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="script">Texto con el script.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retorna texto script con variables reemplazadas.</returns>
        private static string ReplaceVariables(Session session, string script, bool isCustomActionData)
        {
            int i;
            string sPath;
            if (string.IsNullOrWhiteSpace(script))
                return script;

            script = script.Replace("$(DATABASE_NAME)", GetSessionProperty(session, "DATABASE_NAME", isCustomActionData));

            script = script.Replace("$(DATABASE_MAILPROFILENAME)", GetSessionProperty(session, "DATABASE_MAILPROFILENAME", isCustomActionData));
            script = script.Replace("$(DATABASE_MAILBOX)", GetSessionProperty(session, "DATABASE_MAILBOX", isCustomActionData));
            script = script.Replace("$(DATABASE_MAILIP)", GetSessionProperty(session, "DATABASE_MAILIP", isCustomActionData));

            script = script.Replace("$(DATABASE_OPERATORMAILBOX)", GetSessionProperty(session, "DATABASE_OPERATORMAILBOX", isCustomActionData));
            script = script.Replace("$(DATABASE_PROXYWINDOWSUSER)", GetSessionProperty(session, "DATABASE_PROXYWINDOWSUSER", isCustomActionData));
            script = script.Replace("$(DATABASE_PROXYPASSWORD)", GetSessionProperty(session, "DATABASE_PROXYPASSWORD", isCustomActionData));

            List<DataBasePathTO> listPaths = GetCustomTableDataBasePaths(session, isCustomActionData);
            for (i = 0; i < listPaths.Count; i++)
            {
                if (listPaths[i].Path.EndsWith("\\"))
                {
                    sPath = listPaths[i].Path.Substring(0, listPaths[i].Path.Length - 1);
                }
                else
                {
                    sPath = listPaths[i].Path;
                }
                script = script.Replace("$(" + listPaths[i].Name + ")", sPath);
            }

            return script;
        }
        #endregion Private methods

        #region Session methods
        /// <summary>
        /// Retoran rutas de instalación de base de datos almacenada en CutomTable.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        /// <returns>Retoran rutas de instalación de base de datos.</returns>
        private static List<DataBasePathTO> GetCustomTableDataBasePaths(Session session, bool isCustomActionData)
        {
            try
            {
                List<DataBasePathTO> listPaths;

                if (isCustomActionData)
                {
                    listPaths = session.CustomActionData.GetObject<List<DataBasePathTO>>("DATABASE_PATHS");
                }
                else
                {
                    listPaths = new List<DataBasePathTO>();
                    DataBasePathTO path;
                    string sPath;
                    using (Microsoft.Deployment.WindowsInstaller.View v = session.Database.OpenView
                        ("SELECT * FROM `TABLE_DATABASE_PATHS`"))
                    {
                        if (v != null)
                        {
                            v.Execute();
                            for (Record r = v.Fetch(); r != null; r = v.Fetch())
                            {
                                sPath = r.GetString(3);
                                if (string.IsNullOrWhiteSpace(sPath))
                                {
                                    sPath = GetSessionProperty(session, "INSTALLLOCATION", isCustomActionData);
                                }

                                path = new DataBasePathTO()
                                {
                                    Name = r.GetString(1),
                                    Description = r.GetString(2),
                                    Path = sPath
                                };
                                listPaths.Add(path);
                                r.Dispose();
                            }
                        }
                    }
                }

                if (listPaths == null || listPaths.Count == 0)
                {
                    throw new InstallerException("No configuradas rutas de instalación");
                }
                return listPaths;
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción, GetCustomTableDataBasePaths", ex, true);
                throw;
            }
        }

        /// <summary>
        /// Retorna datos de las tablas MSI: `FeatureComponents`, `Feature`, `Component`, `File`.
        /// Filtra los archivos instalados que sean scripts de base de datos (*.SQL).
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="listFeactureNames">Lista con nombres de carcaterístas a ser instaladas.</param>
        /// <returns>Retorna archivos scripts de base de datos a ser instalados.</returns>
        private static List<FeactureInstallTO> GetFeactureScriptDataBase(Session session, List<string> listFeactureNames)
        {
            try
            {
                List<FeactureInstallTO> listF;

                string sLevel = session["INSTALLLEVEL"];
                listF = new List<FeactureInstallTO>();
                FeactureInstallTO f;
                int i;
                string sFileName;
                string sQuery = "SELECT `Feature`.`Feature`, `Feature`.`Title`, `Feature`.`Display`, " +
                    " `Component`.`Directory_`, `File`.`FileName` " +
                    " FROM `FeatureComponents`, `Feature`, `Component`, `File` " +
                    " WHERE `FeatureComponents`.`Feature_` = `Feature`.`Feature` " +
                    " AND `FeatureComponents`.`Component_` = `Component`.`Component` " +
                    " AND `File`.`Component_` = `Component`.`Component` " +
                    " AND `Feature`.`RuntimeLevel` > 0 AND `Feature`.`Level` > 0 " +
                    // " AND `Feature`.`Level` <= " + sLevel +
                    " ORDER BY `Feature`.`Display`";
                using (Microsoft.Deployment.WindowsInstaller.View v = session.Database.OpenView(sQuery))
                {
                    if (v != null)
                    {
                        v.Execute();
                        for (Record r = v.Fetch(); r != null; r = v.Fetch())
                        {
                            if (listFeactureNames.Contains(r.GetString("Feature")) && r.GetString("FileName").ToUpper().EndsWith(".SQL"))
                            {
                                i = r.GetString("FileName").IndexOf("|");
                                if (i > 0)
                                {
                                    sFileName = r.GetString("FileName").Substring(i + 1);
                                }
                                else
                                {
                                    sFileName = r.GetString("FileName");
                                }

                                f = new FeactureInstallTO()
                                {
                                    Feature = r.GetString("Feature"),
                                    Title = r.GetString("Title"),
                                    DisplayOrder = r.GetInteger("Display"),
                                    FileName = sFileName,
                                    DirectoryPath = session.GetTargetPath(r.GetString("Directory_"))
                                };
                                listF.Add(f);
                            }
                            r.Dispose();
                        }
                    }
                }

                return listF;
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción, ReadFeactureScriptDataBase", ex, true);
                throw;
            }
        }

        /// <summary>
        /// Retorna el valor de una propiedad del objeto del objeto <paramref name="session"/>.
        /// El valor puede ser retornado de: session[propertyName] o session.CustomActionData[propertyName].
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="propertyName">Nombre de propiedad</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        private static string GetSessionProperty(Session session, string propertyName, bool isCustomActionData)
        {
            if (isCustomActionData)
            {
                return session.CustomActionData[propertyName];
            }
            else // if (session.GetMode(InstallRunMode.Scheduled))
            {
                return session[propertyName];
            }
        }

        /// <summary>
        /// Establecer elemento de la colección Session.CustomActionData del objeto <paramref name="session"/>, 
        /// utilizando el valor de la propiedad session[propertyName]. 
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="propertyName">Nombre de propiedad</param>
        private static void SetCustomActionData(Session session, string propertyName)
        {
            if (session.CustomActionData.ContainsKey(propertyName))
                session.CustomActionData[propertyName] = session[propertyName];
            else
                session.CustomActionData.Add(propertyName, session[propertyName]);
        }

        /// <summary>
        /// Establecer elemento de la colección CustomActionData: <paramref name="data"/>, 
        /// utilizando el valor de la propiedad session[propertyName]. 
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="propertyName">Nombre de propiedad</param>
        /// <param name="data">Colección de propiedades.</param>
        private static void SetCustomActionData(Session session, string propertyName, CustomActionData data)
        {
            if (data.ContainsKey(propertyName))
                data[propertyName] = session[propertyName];
            else
                data.Add(propertyName, session[propertyName]);
        }

        /// <summary>
        /// Establece el valor de una propiedad.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="propertyName">Nombre de propiedad</param>
        /// <param name="value">Valor de la propiedad.</param>
        private static void SetSessionProperty(Session session, string propertyName, string value)
        {
            try
            {
                session[propertyName] = value;
            }
            catch 
            {    }
        }

        /// <summary>
        /// Actualiza los datos almacenados en Session.CustomActionData, con los datos de la lista <paramref name="listPaths"/>.
        /// </summary>
        /// <param name="session">Sesión Windows Installer.</param>
        /// <param name="listPaths">Rutas de instalación de base de datos.</param>
        /// <param name="isCustomActionData">
        /// <para><c>true</c>: Indica retornar el valor almacenado en session.CustomActionData[propertyName]. Util para acciones deferred.</para>
        /// <para><c>false</c>: Indica retornar el valor almacenado en session[propertyName]. Util para acciones immediate.</para>
        /// </param>
        public static void UpdateCustomTableDataBasePaths(Session session, List<DataBasePathTO> listPaths, bool isCustomActionData)
        {
            try
            {
                Record record;
                Microsoft.Deployment.WindowsInstaller.View viewWI;

                if (listPaths == null || listPaths.Count < 0)
                    return;

                if (session == null)
                {
                    throw new ArgumentNullException("session");
                }

                if (isCustomActionData)
                {
                    if (session.CustomActionData.ContainsKey("DATABASE_PATHS"))
                    {
                        session.CustomActionData.Remove("DATABASE_PATHS");
                        session.CustomActionData.AddObject<List<DataBasePathTO>>("DATABASE_PATHS", listPaths);
                    }
                    else
                    {
                        session.CustomActionData.AddObject<List<DataBasePathTO>>("DATABASE_PATHS", listPaths);
                    }
                }
                else
                {
                    //TableInfo info = session.Database.Tables["TABLE_DATABASE_PATHS"];
                    //for (int i = 0; i < listPaths.Count; i++)
                    //{
                    //    record = session.Database.CreateRecord(info.Columns.Count);
                    //    record.FormatString = info.Columns.FormatString;
                    //    record.SetString(1, listPaths[i].Name);
                    //    record.SetString(2, listPaths[i].Description);
                    //    record.SetString(3, listPaths[i].Path);
                    //    session.Database.Execute(info.SqlInsertString + " TEMPORARY", record);
                    //}

                    // Otro metodo
                    //viewWI = session.Database.OpenView("DELETE FROM `TABLE_DATABASE_PATHS`");
                    //viewWI.Execute();
                    //viewWI.Close();

                    TableInfo info = session.Database.Tables["TABLE_DATABASE_PATHS"];
                    viewWI = session.Database.OpenView("SELECT * FROM TABLE_DATABASE_PATHS");
                    //viewWI.Execute();
                    for (int i = 0; i < listPaths.Count; i++)
                    {
//                        record = session.Database.CreateRecord(3);
                        record = session.Database.CreateRecord(info.Columns.Count);
                        record.FormatString = info.Columns.FormatString;

                        //record.SetString(1, listPaths[i].Name);
                        //record.SetString(2, listPaths[i].Description);
                        //record.SetString(3, listPaths[i].Path);

                        record[1] = listPaths[i].Name;
                        record[2] = listPaths[i].Description;
                        record[3] = listPaths[i].Path;

//                        viewWI.Modify(ViewModifyMode.Replace, record);
                        viewWI.Modify(ViewModifyMode.InsertTemporary, record);
                        record.Dispose();
                    }
                    viewWI.Close();
                }
            }
            catch (Exception ex)
            {
                InstallUtilities.WriteLogInstall(session, "Excepción al actualizar filas de CustomTable", ex, true);
                throw;
            }
        }
        #endregion Session methods
    }
}
