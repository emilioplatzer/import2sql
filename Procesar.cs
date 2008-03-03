/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:10 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.OleDb;
using NUnit.Framework;

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
		public MailASql():this(new ParametrosMailASql(Parametros.LeerPorDefecto.SI)){
		}
		public MailASql(ParametrosMailASql parametros){
			this.DirectorioMails=parametros.DirMailsAProcesar;
			this.NombreTablaReceptora=parametros.TablaReceptora;
			AbrirBase(parametros.BaseReceptora);
		}
		string ObtenerCampo(string campo,string proximoCampo){
			Regex r=new Regex(" *"+campo+"[ .]*:([^`]*?)("+proximoCampo+")", RegexOptions.Multiline);
			Match m=r.Match(ContenidoPlano);
			if(!m.Success | m.Groups.Count<=1){
				return "";
			}
			string rta=m.Groups[1].ToString();
			return rta.Trim(" \t\r\n.:-,=;".ToCharArray());
		}
		void AbrirBase(string nombreMDB){
			ConexionABase = BaseDatos.abrirMDB(nombreMDB);
			OleDbCommand cmd = new OleDbCommand("SELECT * FROM ["+NombreTablaReceptora+"]",ConexionABase);
			SelectAbierto=cmd.ExecuteReader();
		}
		void LeerMail(string nombreArchivo){
			ContenidoPlano=Cadena.ExpandirSignoIgual(Archivo.Leer(nombreArchivo));
		}
		bool GuardarMailEnBase(){
			StringBuilder campos=new StringBuilder();
			StringBuilder valores=new StringBuilder();
			Separador coma=new Separador(",");
			for(int i=1;i<SelectAbierto.FieldCount;i++){
				string nombreCampo=SelectAbierto.GetName(i);
				// 
				string proximoCampo=i<SelectAbierto.FieldCount-1
									?SelectAbierto.GetName(i+1)
									:"----";
				string valorCampo=ObtenerCampo(nombreCampo,proximoCampo);
				if(valorCampo.Length>0){
					campos.Append(coma+"["+nombreCampo+"]");
					valores.Append(coma.mismo()+'"'+Cadena.SacarComillas(valorCampo)+'"');
				}
			}
			if(campos.Length>0){
				string sentencia="INSERT INTO ["+NombreTablaReceptora+@"] ("+campos.ToString()+") VALUES ("+
						valores.ToString()+")";
				OleDbCommand cmd = new OleDbCommand(sentencia,ConexionABase);
				Archivo.Escribir(System.Environment.GetEnvironmentVariable("TEMP")
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
			LeerMail(nombreArchivo);
			System.Console.Write(" leido");
			if (GuardarMailEnBase()){
				System.Console.WriteLine(" procesado");
				File.Delete(nombreArchivo+".procesado");
				File.Move(nombreArchivo,nombreArchivo+".procesado");
			}else{
				System.Console.WriteLine(" ERROR, NO CONTIENE CAMPOS VALIDOS");
			}
		}
		public void LoQueSeaNecesario(){
			DirectoryInfo dir=new DirectoryInfo(DirectorioMails);
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
	[TestFixture]
	public class ProbarMailASql
	{
		public ProbarMailASql()
		{
		}
		[Test]
		public void Proceso(){
			string nombreArchivo="tempAccesABorrar2.mdb";
			string directorio="temp_borrar";
			Archivo.Borrar(nombreArchivo);
			Assert.IsTrue(!Archivo.Existe(nombreArchivo),"no deber�a existir");
			BaseDatos.CrearMDB(nombreArchivo);
			Assert.IsTrue(Archivo.Existe(nombreArchivo),"deber�a existir");
			OleDbConnection con=BaseDatos.abrirMDB(nombreArchivo);
			string sentencia=@"
				CREATE TABLE Receptor(
				   id number,
				   Numero varchar(250),
				   Nombre varchar(250),
				   [Tipo de documento] varchar(250),
				   [N�mero de documento] varchar(250),
				   Nacimiento date,
				   Observaciones varchar(250)
				   )";
			OleDbCommand com=new OleDbCommand(sentencia,con);
			com.ExecuteNonQuery();
			con.Close();
			System.IO.Directory.CreateDirectory(directorio);
			Archivo.Escribir(directorio+@"\unmail.eml",@"
				lleno de basura
				To:Numero@nombre.com
				From:<Tipo de documento>
				Subject: No encuentro mi n�mero de documento
				
				content:quoted-printable
				Numero: 123 :: Nombre: Carlos Perez
				Tipo de documento: DNI N�mero de documento: 12.333.123.
				Nacimiento: 15/1/1991
				Observaciones: condicional");
			Archivo.Escribir(directorio+@"\otro mail.eml",@"
				lleno de basura
				To:Numero@nombre.com
				From:<Tipo de documento>
				Subject: No encuentro mi n�mero de documento
				
				content:quoted-printable
				Numero: 124
				Nombre: Mar�a de las Mercedes
				Tipo de documento: DNI - N�mero de documento: 12345678
				Nacimiento: 10-8-71
				Observaciones: condicional");
			MailASql procesador=new MailASql(new ParametrosMailASql(nombreArchivo,"receptor",directorio));
			procesador.LoQueSeaNecesario();
			procesador.Close();
			con=BaseDatos.abrirMDB(nombreArchivo);
			sentencia="SELECT count(*) FROM Receptor";
			com=new OleDbCommand(sentencia,con);
			string cantidadRegistros=com.ExecuteScalar().ToString();
			Assert.AreEqual("2",cantidadRegistros);
			sentencia="SELECT * FROM Receptor ORDER BY Numero";
			com=new OleDbCommand(sentencia,con);
			OleDbDataReader rdr=com.ExecuteReader();
			rdr.Read();
			Assert.AreEqual("123",rdr.GetValue(1));
			Assert.AreEqual(new DateTime(1991,1,15),rdr.GetDateTime(5));
			rdr.Read();
			Assert.AreEqual("Mar�a de las Mercedes",rdr.GetValue(2));
			Assert.AreEqual(new DateTime(1971,8,10),rdr.GetDateTime(5));
		}
	}
	public class ParametrosMailASql:Parametros{
		public string DirTemp;
		public string DirMailsAProcesar;
		public string TablaReceptora;
		public string BaseReceptora;
		public ParametrosMailASql(LeerPorDefecto queHacer):base(queHacer){
			/*
			string dirBase=System.Environment.GetEnvironmentVariable("MAIL2ACCESS_DIR");
			if(dirBase==null){
				string[] parametros=System.Environment.GetCommandLineArgs();
				if(parametros.Length>1 && parametros[0]=="DIR"){
					dirBase=parametros[1];
				}else{
					string nombreArchivoDirBase="dirbase.ini";
					if(Archivo.Existe(nombreArchivoDirBase)){
						dirBase=Archivo.Leer(nombreArchivoDirBase);
					}
				}
			}
			this.BaseReceptora=dirBase+@"\ServEsp.mdb";
			this.TablaReceptora="MOCs";
		    this.DirMailsAProcesar=dirBase+@"\MailsAProcesar";
			this.DirTemp=System.Environment.GetEnvironmentVariable("TEMP");
			*/
		}
		public ParametrosMailASql(string nombreMDB,string nombreTabla,string directorioMails)
			:base(LeerPorDefecto.NO)
		{
			this.DirTemp=System.Environment.GetEnvironmentVariable("TEMP");
			this.BaseReceptora=nombreMDB;
			this.TablaReceptora=nombreTabla;
			this.DirMailsAProcesar=directorioMails;
		}
	}
}