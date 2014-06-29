/* ========================================================================== 
Proyecto: Asistente.Browser
Empresa: 
Creado por: Hugo González Olaya
Fecha creacion: 2011-01-31
Fecha Actualizacion: 2011-01-31
Descripcion: Formulario para configurar rutas de instalación.
========================================================================== */
/* ========================================================================== 
Referencias: 
System.Configuration.Install
========================================================================== 
Historial de versiones (Actualizado: 2011-01-31)
1.0.0	Creado por: Hugo González Olaya
========================================================================== */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Microsoft.Deployment.WindowsInstaller;
using WixDataBase.CustomAction.Utilities;

namespace WixDataBase.CustomAction.UI
{
    /// <summary>
    /// Formulario para configurar rutas de instalación.
    /// </summary>
    public partial class frmPaths : Form
    {
        #region Fields
        /// <summary>
        /// Rutas de instalación.
        /// </summary>
        private List<DataBasePathTO> m_listPaths;

        /// <summary>
        /// Directorio actual.
        /// </summary>
        private string m_dirCurrent;
        #endregion Fields

        #region Constructs
        /// <summary>
        /// Constructor predeterminado, crea una nueva instancia.
        /// </summary>
        public frmPaths()
        {
            InitializeComponent();
         
            m_dirCurrent = Assembly.GetExecutingAssembly().Location;
        }

        /// <summary>
        /// Constructor, crea una nueva instancia.
        /// </summary>
        /// <param name="dataPaths">Rutas de instalación.</param>
        public frmPaths(List<DataBasePathTO> listPaths)
        {
            InitializeComponent();
            m_dirCurrent = Assembly.GetExecutingAssembly().Location;
            m_listPaths = listPaths;
            GetInstallProperties();
        }
        #endregion Constructs

        #region Private methods
        /// <summary>
        /// Recupera parámetros para el contexto de instalación y llena los controles.
        /// </summary>
        private void GetInstallProperties()
        {
            try
            {
                dgvPaths.Columns[0].DataPropertyName = "Name";
                dgvPaths.Columns[1].DataPropertyName = "Description";
                dgvPaths.Columns[2].DataPropertyName = "Path";

                this.dgvPaths.DataSource = m_listPaths;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Recuperar parámetros de instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Establece parámetros para el contexto de instalación.
        /// </summary>
        private bool SetParametersInstallContext()
        {
            try
            {
                string sPath, sPathFirts;
                sPathFirts = dgvPaths.Rows[0].Cells["dgvColPath"].Value.ToString().Trim();
                if (string.IsNullOrWhiteSpace(sPathFirts))
                {
                    throw new ArgumentOutOfRangeException("Debe establecer la primera ruta");
                }

                if (chkSamePath.Checked)
                {
                    for (int i = 0; i < m_listPaths.Count; i++)
                    {
                        m_listPaths[i].Path = sPathFirts;
                    }
                }
                else
                {
                    for (int i = 0; i < m_listPaths.Count; i++)
                    {
                        if (i == 0)
                        {
                            sPath = sPathFirts;
                        }
                        else
                        {
                            sPath = dgvPaths.Rows[i].Cells["dgvColPath"].Value.ToString().Trim();
                            if (string.IsNullOrWhiteSpace(sPath))
                            {
                                sPath = sPathFirts;
                            }
                        }

                        m_listPaths[i].Path = sPath;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Establecer parámetros de instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        #endregion Private methods

        #region Handle events
        /// <summary>
        /// Maneja el evento <seealso cref="Click"/>, disparado para aceptar el dialogo.
        /// </summary>
        /// <param name="sender">Objeto que dispara el evento.</param>
        /// <param name="e">Argumentos <seealso cref="EventArgs"/> con datos del evento.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (SetParametersInstallContext())
            {

                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Maneja el evento <seealso cref="Click"/>, disparado para cancelar el dialogo.
        /// </summary>
        /// <param name="sender">Objeto que dispara el evento.</param>
        /// <param name="e">Argumentos <seealso cref="EventArgs"/> con datos del evento.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Maneja el evento <seealso cref="CellClick"/> para presentar un cuadro de diálogo para seleccionar carpeta.
        /// </summary>
        /// <param name="sender">Objeto que dispara el evento.</param>
        /// <param name="e">Argumentos <seealso cref="DataGridViewCellEventArgs"/> con datos del evento.</param>
        private void dgvPaths_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string path;
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvPaths.Columns["dgvColPathDialog"].Index)
            {
                if (dgvPaths.Rows[e.RowIndex].Cells["dgvColPath"].Value == null || string.IsNullOrWhiteSpace(dgvPaths.Rows[e.RowIndex].Cells["dgvColPath"].Value.ToString()))
                {
                    path = m_dirCurrent;
                }
                else
                {
                    path = dgvPaths.Rows[e.RowIndex].Cells["dgvColPath"].Value.ToString();
                }

                dgvPaths.Rows[e.RowIndex].Cells["dgvColPath"].Value = InstallUtilities.GetFolder(path);
            }
        }
        #endregion Handle events
    }
}
