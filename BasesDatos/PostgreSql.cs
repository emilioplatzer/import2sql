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

using Comunes;

namespace BasesDatos
{
	public class PostgreSql:BaseDatos
	{
		PostgreSql(OdbcConnection con)
			:base(con)
		{
		}
		PostgreSql(){}
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
			#if SinPostgre
			Controlar.NoDefinido("SinPostgre");
			#endif
			{
				OdbcConnection ConexionABase = new OdbcConnection();
				ConexionABase.ConnectionString=@"DRIVER=PostgreSQL Unicode;UID="+Usuario+";PORT=5432;SERVER="+Servidor+";DATABASE="+Base+";PASSWORD="+Clave;
				ConexionABase.Open();
				return new PostgreSql(ConexionABase);
			}
		}
		public override string ErrorCode_NoExisteTabla{ get{ return "ERROR [42P01]";}}
		public override string ErrorCode_NoExisteVista{ get{ return "ERROR [42P01]";}}
		public override string StuffTabla(string nombreTabla){
			if(DebeStuffear(nombreTabla)){
				return '"'+nombreTabla+'"';
			}else{
				return nombreTabla;
			}
		}
		public override string StuffFecha(DateTime fecha){
			return "'"+fecha.Year+"/"+fecha.Month+"/"+fecha.Day+"'";
		}
		public override string StuffValor<T>(T valor){
			if(valor is String){
				return base.StuffValor((valor as string).Replace(@"\",@"\\"));
			}else{
				return base.StuffValor(valor);
			}
		}
		public static PostgreSql SinAbrir(){
			return new PostgreSql();
		}
		public override bool UpdateConJoin{ get{ return false; } }
		public override bool InternosForzarAs { get { return false; } }
		public override string MarcaMotor{ get{ return "PostgreSql"; }}
		public override bool SubSelectsDeUpdateViaVista { get { return false; } } 
		public override string OperadorToSql(OperadorBinario Operador){
			switch(Operador){
				case OperadorBinario.Concatenado: return "||";
				default:return base.OperadorToSql(Operador);
			}
		}
		public override string OperadorToSqlPrefijo(OperadorFuncion Operador){
			switch(Operador){
				case OperadorFuncion.LogE: return "LN(";
				case OperadorFuncion.Str: return "";
				case OperadorFuncion.Nvl: return "COALESCE(";
				case OperadorFuncion.PrimeraPalabra: return "PRIMERPALABRA(";
				case OperadorFuncion.SinPrimeraPalabra: return "SINPRIMERPALABRA(";
				case OperadorFuncion.Normalizar: return "NORMALIZAR(";
				/*
				case OperadorFuncion.PrimeraPalabra: return "TRANSLATE(SUBSTRING(TRIM(BOTH ";
				case OperadorFuncion.SinPrimeraPalabra: return "TRIM(BOTH ' ' FROM TRIM(BOTH '.' FROM REGEXP_REPLACE(";
				*/
				default: return base.OperadorToSqlPrefijo(Operador);
			}
		}
		public override string OperadorToSqlSufijo(OperadorFuncion Operador){
			switch(Operador){
				/*
				case OperadorFuncion.PrimeraPalabra: return ") FROM '^([^ .]+)(?:[ .]+|$)'),' .','')";
				case OperadorFuncion.SinPrimeraPalabra: return ",'^[ .]*[^ .]+(?:[ .]|$)','')))";
				*/
				case OperadorFuncion.Str: return "";
				default: return base.OperadorToSqlSufijo(Operador);
			}
		}
	}
	[TestFixture]
	public class ProbarPostgreSql{
		public static PostgreSql AbrirBase(){
			System.Windows.Forms.Application.OleRequired();
			return PostgreSql.Abrir("127.0.0.1","import2sqldb","import2sql","sqlimport");
		}
		#if SinPostgre
		[Test]
		public void A_SinPostgre(){
			Controlar.Definido("SinPostgre");
		}
		#else
		[Test]
		public void A_SinPostgre(){
			Controlar.NoDefinido("SinPostgre");
		}
		[Test]
		public void Conexion(){
			PostgreSql db=AbrirBase();
			ProbarBaseDatos.ObjEnTodasLasBases(db);
		}
		#endif
	}
}
