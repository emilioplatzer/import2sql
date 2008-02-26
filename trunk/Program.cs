/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:08 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */
using System;

namespace TodoASql
{
	class Program
	{
		public static void Main(string[] args)
		{
			// new Pruebas().Proceso(); /*
			try{
				ParametrosMailASql parametros=new ParametrosMailASql();
				parametros.LeerString(Archivo.Leer("dirbase.ini"),Parametros.Tipo.INI);
				new MailASql(parametros).LoQueSeaNecesario();
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
				
			// */
			Console.WriteLine("Listo!");
			
			// TODO: Implement Functionality Here
			
			// Console.Write("Press any key to continue . . . ");
			// Console.ReadKey(true);
		}
	}
}
