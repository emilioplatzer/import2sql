/*
 * Created by SharpDevelop.
 * User: Emilio
 * Date: 20/03/2008
 * Time: 02:57 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using System.Data.Odbc;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of PostgreSql.
	/// </summary>
	public class PostgreSql:BaseDatos
	{
		PostgreSql(OdbcConnection con)
			:base(con)
		{
		}
		/*
		public static OleDbConnection abrirPostgreOleDb(string Servidor,string Base, string Usuario, string Clave){
			OleDbConnection ConexionABase = new System.Data.OleDb.OleDbConnection();
			ConexionABase.ConnectionString="Provider=PostgreSQL OLE DB Provider;Data Source="+Servidor+";" +
				"Location="+Base+";User id="+Usuario+";Password="+Clave+";"; // "timeout=1000";
			ConexionABase.Open();
			return ConexionABase;
		}
		*/
		public static PostgreSql Abrir(string Servidor,string Base, string Usuario, string Clave){
			OdbcConnection ConexionABase = new OdbcConnection();
			ConexionABase.ConnectionString=@"DRIVER=PostgreSQL Unicode;UID="+Usuario+";PORT=5432;SERVER="+Servidor+";DATABASE="+Base+";PASSWORD="+Clave;
			ConexionABase.Open();
			return new PostgreSql(ConexionABase);
		}
		public override string ErrorCode_NoExisteTabla{ get{ return "ERROR [42P01]";}}
		public override string ErrorCode_NoExisteVista{ get{ return "ERROR [42P01]";}}
		public override string StuffTabla(string nombreTabla){
			return '"'+nombreTabla+'"';
		}
		public override string StuffFecha(DateTime fecha){
			return "'"+fecha.Year+"/"+fecha.Month+"/"+fecha.Day+"'";
		}

	}
	[TestFixture]
	public class ProbarPostgreSql{
		#if SinPostgre
		[Test]
		public void SinPostgre(){
			Controlar.Definido("SinPostgre");
		}
		#else
		[Test]
		public void SinPostgre(){
		}
		[Test]
		public void Conexion(){
			System.Windows.Forms.Application.OleRequired();
			PostgreSql db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
			ProbarBaseDatos.ObjEnTodasLasBases(db);
		}
		#endif
	}
}
