/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:08 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
 */
using System;

namespace TodoASql
{
	class Program
	{
		public static void Main(string[] args)
		{
			// new ProbarBaseDatos().CreacionMdb();
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
					,"Error, no encontr� el directorio. "
					,System.Windows.Forms.MessageBoxButtons.OK
					,System.Windows.Forms.MessageBoxIcon.Error
				);
			
			} // */
			
			Console.WriteLine("Listo!");
			// Console.Write("Press any key to continue . . . ");
			// Console.ReadKey(true);
		}
	}
}