/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:10 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.OleDb;

namespace TodoASql
{
	/// <summary>
	/// Description of Procesar.
	/// </summary>
	public class MailASql
	{
		string ContenidoPlano;
		OleDbConnection ConexionABase;
		OleDbDataReader SelectAbierto;
		string DirectorioMails;
		string NombreTablaReceptora;
		public MailASql(string nombreMDB,string nombreTabla,string directorioMails)
		{
			this.NombreTablaReceptora=nombreTabla;
			abrirBase(nombreMDB);
			this.DirectorioMails=directorioMails;
		}
		string obtenerCampo(string campo,string proximoCampo){
			Regex r=new Regex(" *"+campo+"[ .]*:([^`]*?)("+proximoCampo+")", RegexOptions.Multiline);
			Match m=r.Match(ContenidoPlano);
			if(!m.Success | m.Groups.Count<=1){
				return "";
			}
			string rta=m.Groups[1].ToString();
			return rta.Trim(" \t\r\n.:-,=;".ToCharArray());
		}
		void abrirBase(string nombreMDB){
			ConexionABase = BaseDatos.abrirMDB(nombreMDB);
			OleDbCommand cmd = new OleDbCommand("SELECT * FROM ["+NombreTablaReceptora+"]",ConexionABase);
			SelectAbierto=cmd.ExecuteReader();
		}
		void leerMail(string nombreArchivo){
			ContenidoPlano=Otras.expandirSignoIgual(Otras.leerArchivoCompleto(nombreArchivo));
		}
		string stuff(string valor){
			return valor.Replace('"',' ')
				.Replace('\n',' ')
				.Replace('\r',' ')
				.Replace('\t',' ')
				.Substring(0,Otras.min(250,valor.Length)).Trim();
		}
		bool guardarMailEnBase(){
			StringBuilder campos=new StringBuilder();
			StringBuilder valores=new StringBuilder();
			string separador="";
			for(int i=1;i<SelectAbierto.FieldCount;i++){
				string nombreCampo=SelectAbierto.GetName(i);
				// 
				string proximoCampo=i<SelectAbierto.FieldCount-1
									?SelectAbierto.GetName(i+1)
									:"----";
				string valorCampo=obtenerCampo(nombreCampo,proximoCampo);
				if(valorCampo.Length>0){
					campos.Append(separador+"["+nombreCampo+"]");
					valores.Append(separador+'"'+stuff(valorCampo)+'"');
					separador=",";
				}
			}
			if(campos.Length>0){
				string sentencia="INSERT INTO ["+NombreTablaReceptora+@"] ("+campos.ToString()+") VALUES ("+
						valores.ToString()+")";
				OleDbCommand cmd = new OleDbCommand(sentencia,ConexionABase);
				Otras.escribirArchivo(System.Environment.GetEnvironmentVariable("TEMP")
				                      + @"query.sql"
				                      ,sentencia);
				cmd.ExecuteNonQuery();
				return true;
			}else{
				return false;
			}
		}
		void Uno(string nombreArchivo){
			System.Console.Write("Mail:"+nombreArchivo);
			leerMail(nombreArchivo);
			System.Console.Write(" leido");
			if (guardarMailEnBase()){
				System.Console.WriteLine(" procesado");
				File.Delete(nombreArchivo+".procesado");
				File.Move(nombreArchivo,nombreArchivo+".procesado");
			}else{
				System.Console.WriteLine(" ERROR, NO CONTIENE CAMPOS VALIDOS");
			}
		}
		public void LoQueSeaNecesario(){
			DirectoryInfo dir=new DirectoryInfo(DirectorioMails);
			System.Console.WriteLine("donde:"+DirectorioMails);
			FileInfo[] archivos=dir.GetFiles("*.eml");
			foreach(FileInfo archivo in archivos){
				Uno(archivo.FullName);
			}
		} 
		public void Close(){
			SelectAbierto.Close();
			ConexionABase.Close();
		}
	}
}
