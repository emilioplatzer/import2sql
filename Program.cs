/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:08 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */
 
using System;
using Indices;
using Modelador;
using PrModelador;

namespace TodoASql
{
	class Program
	{
		public static void Main(string[] args)
		{
			// new prTabla().Periodos();
			// new PruebasReflexion().NombresMiembros();
		    new ProbarSqLite().ConexionSqLite();
		    // new ProcesoLevantarPlanillas().TraerPlanillasRecepcion();
		    /*
		    ProbarIndiceD3 p=new ProbarIndiceD3();
		    p.A01CalculosBase();
		    p.VerCanasta();
		    p.zReglasDeIntegridad();
		    // */ 
			// new ProbarPostgreSql().Conexion();
			// new ProbarBdAccess().Creacion();
			// new ProbarMatrizExcelASqlGenerica().trasvasar();
			// new probarLibroExcel().Rango();
			// new ProbarFormulario().FormDerivado();
			// new ProbarMailASql().Proceso();
			// UnProcesamiento.Ahora();
			// /*
			/*
			#if SinOffice
			#else
			ProbarMatrizExcelASql p=new ProbarMatrizExcelASql();
			p.crear();
			p.revisar();
			p.crearReceptor();
			p.trasvasar();
			p.trasvasarConParametros();
			#endif
			// */
			// PruebaFormularios.Primero();
			// new Pruebas().Proceso(); /*
			/*
			PruebasParametros.MostrarVariablesDelSistema();
			try{
				new MailASql().LoQueSeaNecesario();
				System.Windows.Forms.MessageBox.Show("Listo!");
			}catch(System.Data.OleDb.OleDbException e){
				System.Windows.Forms.MessageBox.Show(
					e.Message
					,"Error en el manejo de la base de datos"
					,System.Windows.Forms.MessageBoxButtons.OK
					,System.Windows.Forms.MessageBoxIcon.Error
				);
			}catch(System.IO.DirectoryNotFoundException e){
				System.Windows.Forms.MessageBox.Show(
					e.Message
					,"Error, no encontró el directorio. "
					,System.Windows.Forms.MessageBoxButtons.OK
					,System.Windows.Forms.MessageBoxIcon.Error
				);
			
			} // */
			
			Console.WriteLine("Listo!");
			Console.Write("presione cualquier tecla . . . ");
			Console.ReadKey(true);
		}
	}
}
