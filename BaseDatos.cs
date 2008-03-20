/*
 * Created by SharpDevelop.
 * User: Gdibella
 * Date: 17/02/2008
 * Time: 09:51 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using NUnit.Framework;
using ADOX;

namespace TodoASql
{
	/// <summary>
	/// Description of BaseDatos.
	/// </summary>
	public class BaseDatos
	{
		public BaseDatos()
		{
		}
		public static ADOX.CatalogClass CrearMDB(string nombreArchivo){
			ADOX.CatalogClass cat=new CatalogClass();
			cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;" +
				   "Data Source="+nombreArchivo+";" +
				   "Jet OLEDB:Engine Type=5");
			return cat;
		}
		public static OleDbConnection abrirMDB(string nombreArchivo){
			OleDbConnection ConexionABase = new System.Data.OleDb.OleDbConnection();
			ConexionABase.ConnectionString = 
				@"PROVIDER=Microsoft.Jet.OLEDB.4.0;Data Source="+nombreArchivo;
			ConexionABase.Open();
			return ConexionABase;
		}
		public static OleDbConnection abrirPostgreOleDb(string Servidor,string Base, string Usuario, string Clave){
			OleDbConnection ConexionABase = new System.Data.OleDb.OleDbConnection();
			ConexionABase.ConnectionString="Provider=PostgreSQL OLE DB Provider;Data Source="+Servidor+";" +
				"Location="+Base+";User id="+Usuario+";Password="+Clave+";"; // "timeout=1000";
			ConexionABase.Open();
			return ConexionABase;
		}
		public static OdbcConnection abrirPostgre(string Servidor,string Base, string Usuario, string Clave){
			OdbcConnection ConexionABase = new OdbcConnection();
			// ConexionABase.ConnectionString=Archivo.Leer(@"e:\hecho\cs\import2sql\soporte\PostgreODBC.dsn");
			ConexionABase.ConnectionString=@"DRIVER=PostgreSQL Unicode;UID=import2sql;PORT=5432;SERVER=127.0.0.1;DATABASE=import2sqlDB;PASSWORD=sqlimport";
			ConexionABase.Open();
			return ConexionABase;
		}
	}
	[TestFixture]
	public class ProbarBaseDatos{
		[Test]
		public void CreacionMdb(){
			string nombreArchivo="tempAccesABorrar.mdb";
			Archivo.Borrar(nombreArchivo);
			Assert.IsTrue(!Archivo.Existe(nombreArchivo),"no debería existir");
			Catalog cat=BaseDatos.CrearMDB(nombreArchivo);
			Assert.IsTrue(Archivo.Existe(nombreArchivo),"debería existir");
		}
		[Test]
		public void ConexionPostgre(){
			
			System.Windows.Forms.Application.OleRequired();
			// OleDbConnection 
			IDbConnection con=BaseDatos.abrirPostgre("127.0.0.1","import2sqlDB","import2sql","sqlimport");
			// OleDbCommand com=new OleDbCommand("select 3",con);
			// OleDbDataReader rdr=com.ExecuteReader();
			// Assert.AreEqual(3,com.ExecuteScalar());
			// OleDbCommand com=new OleDbCommand("select * from \"TablaExistente\"",con);
			// OleDbCommand com=new OleDbCommand("select * from TablaExistente",con);
			// OleDbCommand 
			IDbCommand com=con.CreateCommand();
			com.CommandText="select * from tablaexistente";	
			//OleDbDataREader
			IDataReader rdr=com.ExecuteReader();
			rdr.Read();
			Assert.AreEqual("uno",rdr["texto"]);
			rdr.Close();
			com.CommandText="select 3";
			Assert.AreEqual(3,com.ExecuteScalar());
			com.CommandText="select 3.14";
			Assert.AreEqual(3.14,com.ExecuteScalar());
		}
	}
}
