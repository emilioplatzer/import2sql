/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 06/06/2008
 * Hora: 11:33
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;

namespace Interactivo
{
	public delegate BaseDatos ValidadorLogin(DatosLogin datos);
	public class DatosLogin{
		public string Nombre_Usuario="import2sql";
		[TipoClave] public string Clave;
		public string Base="import2sqldb";
		public string Servidor="127.0.0.1";
	}
	public class FormLogin:Formulario{
		DatosLogin datos=new DatosLogin();
		ValidadorLogin validador;
		BaseDatos db;
		public FormLogin(ValidadorLogin validador){
			this.validador=validador;
			GenerarDesdeObjeto(datos);
			btnTomar.Text="Entrar";
			AcceptButton=btnTomar;
			ShowDialog();
		}
		public FormLogin()
			:this(ValidadorLoginPostgre)
		{}
		public static BaseDatos ValidadorLoginPostgre(DatosLogin datos){
			BaseDatos db=null;
			try{
				db=PostgreSql.Abrir(datos.Servidor,datos.Base,datos.Nombre_Usuario,datos.Clave);
			}catch(System.Data.Odbc.OdbcException ex){
				System.Windows.Forms.DialogResult res=
					System.Windows.Forms.MessageBox.Show(
						"El sistema informa: "+ex.Message,
						"No se puede conectar a la base de datos",
						System.Windows.Forms.MessageBoxButtons.RetryCancel);
				if(res==System.Windows.Forms.DialogResult.Cancel){
					System.Windows.Forms.Application.Exit();
				}
			}
			return db;
		}
		public override bool Validar(){
			if(datos.Clave==null || datos.Clave==""){
				Controls["txt_Clave"].Focus();
				return false;
			}
			if(datos.Base==null || datos.Base==""){
				Controls["txt_Base"].Focus();
				return false;
			}
			if(datos.Servidor==null || datos.Servidor==""){
				Controls["txt_Servidor"].Focus();
				return false;
			}
			db=validador(datos);
			return db!=null;
		}
		public BaseDatos BaseAbierta(){
			return db;
		}
	}
}
