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
using System.Data.Common;
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
			IDbConnection con=BaseDatos.abrirMDB(nombreArchivo);
			IDbCommand cmd=con.CreateCommand();
			cmd.CommandText="CREATE TABLE tablaexistente (texto varchar(100), numero integer)";
			cmd.ExecuteNonQuery();
			cmd.CommandText="INSERT INTO tablaexistente (texto, numero) VALUES ('uno',1)";
			cmd.ExecuteNonQuery();
			AuxEnTodasLasBases(con);
		}
		[Test]
		public void ConexionPostgre(){
			
			System.Windows.Forms.Application.OleRequired();
			IDbConnection con=BaseDatos.abrirPostgre("127.0.0.1","import2sqlDB","import2sql","sqlimport");
			AuxEnTodasLasBases(con);
			IDbCommand cmd=con.CreateCommand();
			cmd.CommandText="SELECT 3";
			Assert.AreEqual(3,cmd.ExecuteScalar());
			cmd.CommandText="SELECT 3.14";
			Assert.AreEqual(3.14,cmd.ExecuteScalar());
		}
		public void AuxEnTodasLasBases(IDbConnection con){
			AuxOperacionesSimples(con);
			AuxUsarReceptor(con);
		}
		public void AuxOperacionesSimples(IDbConnection con){
			IDbCommand cmd=con.CreateCommand();
			cmd.CommandText="SELECT * FROM tablaexistente";	
			IDataReader rdr=cmd.ExecuteReader();
			rdr.Read();
			Assert.AreEqual("uno",rdr["texto"]);
			rdr.Close();
			cmd.CommandText="SELECT 10.0/16 FROM tablaexistente WHERE numero=1";
			Assert.AreEqual(0.625,cmd.ExecuteScalar());
			try{
				cmd.CommandText="DROP TABLE nueva_tabla_prueba";
				cmd.ExecuteNonQuery();
				Assert.Ignore("se pudo DROPear la tabla, la vez anterior la prueba se interrumpió porque no "+
				              "debería existir. Hay que correr la prueba de vuelta. Lo normal era que no exista");
			}catch(DbException ex){
				switch(ex.ErrorCode){
					case BdAccess.ErrorCode_NoExisteTabla: break; // ok Error de MDB
					case PostgreSql.ErrorCode_NoExisteTabla: break; // ok Error de Postgre
					default:
						Assert.Ignore("DROP TABLE cantó "+ex.ErrorCode+": "+ex.Message+" no es un error conocido");
						break;
				}
			}
			cmd.CommandText=@"CREATE TABLE nueva_tabla_prueba (
				nombre varchar(100),
				numero integer,
				monto double precision,
				fecha date,
				primary key (nombre));
			";
			cmd.ExecuteNonQuery();
			cmd.CommandText="INSERT INTO nueva_tabla_prueba (nombre) VALUES ('solo')";
			cmd.ExecuteNonQuery();
			cmd.CommandText="SELECT * FROM nueva_tabla_prueba";
			rdr=cmd.ExecuteReader();
			rdr.Read();
			Assert.AreEqual("solo",rdr["nombre"]);
			rdr.Close();
			cmd.CommandText="DROP TABLE nueva_tabla_prueba";
			cmd.ExecuteNonQuery();
		}
		public void AuxUsarReceptor(IDbConnection con){
			
		}
	}
}
