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
using System.Data;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of Procesar.
	/// </summary>
	public class MailASql
	{
		string ContenidoPlano;
		string DirectorioMails;
		ReceptorSql Receptor;
		/*
		public MailASql():this(new ParametrosMailASql(Parametros.LeerPorDefecto.SI)){
		}
		*/
		public MailASql(ParametrosMailASql parametros,ReceptorSql receptor){
			this.DirectorioMails=parametros.DirMailsAProcesar;
			this.Receptor=receptor;
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
		void LeerMail(string nombreArchivo){
			ContenidoPlano=Cadena.ExpandirSignoIgual(Archivo.Leer(nombreArchivo));
		}
		bool GuardarMailEnBase(){
			InsertadorSql insert=new InsertadorSql(Receptor);
			for(int i=1;i<Receptor.FieldCount;i++){
				string nombreCampo=Receptor.GetName(i);
				// 
				string proximoCampo=i<Receptor.FieldCount-1
									?Receptor.GetName(i+1)
									:"----";
				string valorCampo=ObtenerCampo(nombreCampo,proximoCampo);
				if(valorCampo.Length>0){
					insert[nombreCampo]=valorCampo;
				}
			}
			return insert.InsertarSiHayCampos();
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
			Assert.IsTrue(!Archivo.Existe(nombreArchivo),"no debería existir");
			BdAccess.Crear(nombreArchivo);
			Assert.IsTrue(Archivo.Existe(nombreArchivo),"debería existir");
			BdAccess db=BdAccess.Abrir(nombreArchivo);
			db.ExecuteNonQuery(@"
				CREATE TABLE Receptor(
				   id number,
				   Numero varchar(250),
				   Nombre varchar(250),
				   [Tipo de documento] varchar(250),
				   [Número de documento] varchar(250),
				   Nacimiento date,
				   Observaciones varchar(250)
				   )");
			System.IO.Directory.CreateDirectory(directorio);
			Archivo.Escribir(directorio+@"\unmail.eml",@"
				lleno de basura
				To:Numero@nombre.com
				From:<Tipo de documento>
				Subject: No encuentro mi número de documento
				
				content:quoted-printable
				Numero: 123 :: Nombre: Carlos Perez
				Tipo de documento: DNI Número de documento: 12.333.123.
				Nacimiento: 15/1/1991
				Observaciones: condicional");
			Archivo.Escribir(directorio+@"\otro mail.eml",@"
				lleno de basura
				To:Numero@nombre.com
				From:<Tipo de documento>
				Subject: No encuentro mi número de documento
				
				content:quoted-printable
				Numero: 124
				Nombre: María de las Mercedes
				Tipo de documento: DNI - Número de documento: 12345678
				Nacimiento: 10-8-71
				Observaciones: condicional");
			ParametrosMailASql parametros=new ParametrosMailASql(nombreArchivo,"receptor",directorio);
			ReceptorSql receptor=new ReceptorSql(parametros);
			MailASql procesador=new MailASql(parametros,receptor);
			procesador.LoQueSeaNecesario();
			receptor.Close();
			db=BdAccess.Abrir(nombreArchivo);
			string cantidadRegistros=db.ExecuteScalar("SELECT count(*) FROM Receptor").ToString();
			Assert.AreEqual("2",cantidadRegistros);
			IDataReader rdr=db.ExecuteReader("SELECT * FROM Receptor ORDER BY Numero");
			rdr.Read();
			Assert.AreEqual("123",rdr.GetValue(1));
			Assert.AreEqual(new DateTime(1991,1,15),rdr.GetDateTime(5));
			rdr.Read();
			Assert.AreEqual("María de las Mercedes",rdr.GetValue(2));
			Assert.AreEqual(new DateTime(1971,8,10),rdr.GetDateTime(5));
		}
	}
	public class ParametrosMailASql:Parametros,IParametorsReceptorSql{
		public string DirTemp;
		public string DirMailsAProcesar;
		string tablaReceptora; public string TablaReceptora{ get{ return tablaReceptora; }}
		string baseReceptora; public string BaseReceptora{ get{ return baseReceptora; }}
		public ParametrosMailASql(LeerPorDefecto queHacer):base(queHacer){
		}
		public ParametrosMailASql(string nombreMDB,string nombreTabla,string directorioMails)
			:base(LeerPorDefecto.NO)
		{
			this.DirTemp=System.Environment.GetEnvironmentVariable("TEMP");
			this.baseReceptora=nombreMDB;
			this.tablaReceptora=nombreTabla;
			this.DirMailsAProcesar=directorioMails;
		}
	}
}
