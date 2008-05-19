/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 21/03/2008
 * Time: 10:43 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using System.Data.OleDb;
using ADOX;
using NUnit.Framework;

using Comunes;

namespace BasesDatos
{
	public class BdAccess:BaseDatos{
		BdAccess(OleDbConnection con)
			:base(con)
		{
		}
		BdAccess(){}
		public static ADOX.CatalogClass Crear(string nombreArchivo){
			ADOX.CatalogClass cat=new CatalogClass();
			cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;" +
				   "Data Source="+nombreArchivo+";" +
				   "Jet OLEDB:Engine Type=5");
			return cat;
		}
		public static BdAccess Abrir(string nombreArchivo){
			OleDbConnection ConexionABase = new System.Data.OleDb.OleDbConnection();
			ConexionABase.ConnectionString = 
				@"PROVIDER=Microsoft.Jet.OLEDB.4.0;Data Source="+nombreArchivo;
			ConexionABase.Open();
			return new BdAccess(ConexionABase);
		}
		public static BdAccess SinAbrir(){
			return new BdAccess();
		}
		public override string ErrorCode_NoExisteTabla{ get{ return "La tabla";}}
		public override string ErrorCode_NoExisteVista{ get{ return "No se puede encontrar";}}
		public override string StuffTabla(string nombreTabla){
			if(DebeStuffear(nombreTabla)){
				return "["+nombreTabla.Replace(".","_").Replace("[","(").Replace("]",")")+"]";
			}else{
				return nombreTabla;
			}
		}
		public override string StuffFecha(DateTime fecha){
			return "#"+fecha.Month+"/"+fecha.Day+"/"+fecha.Year+"#";
		}
		public override bool SoportaFkMixta {
			get { return false; }
		}
		public override bool UpdateConJoin{ get{ return true; } }
		public override bool UpdateSelectSumViaDSum{ get{ return true; } }
		public override bool InternosForzarAs { get { return false; } }
		public override string MarcaMotor{ get{ return "MS Access"; }}
		public override bool SubSelectsDeUpdateViaVista { get { return true; } } 
		public override string OperadorToSql(OperadorBinario Operador){
			switch(Operador){
				case OperadorBinario.Concatenado: return " & ";
				default:return base.OperadorToSql(Operador);
			}
		}
		public override string OperadorToSqlPrefijo(OperadorFuncion Operador){
			switch(Operador){
				case OperadorFuncion.LogE: return "LOG(";
				case OperadorFuncion.Str: return "STR(";
				case OperadorFuncion.Nvl: return "NZ(";
				default: return base.OperadorToSqlPrefijo(Operador);
			}
		}
	}
	[TestFixture]
	public class ProbarBdAccess{
		public static BdAccess AbrirBase(int version){
			// Pendiente
			// Tuve que poner la version porque no cierra bien la base.
			// Después tengo que investigar eso!
			string nombreArchivo="tempAccesABorrar"+version.ToString()+".mdb";
			if(version>0){
				Archivo.Borrar(nombreArchivo);
				Assert.IsTrue(!Archivo.Existe(nombreArchivo),"no debería existir");
				Catalog cat=BdAccess.Crear(nombreArchivo);
			}
			Assert.IsTrue(Archivo.Existe(nombreArchivo),"debería existir");
			return BdAccess.Abrir(nombreArchivo);
		}
		[Test]
		public void Creacion(){
			BdAccess db=AbrirBase(1);
			db.ExecuteNonQuery("CREATE TABLE tablaexistente (texto varchar(100), numero integer)");
			db.ExecuteNonQuery("INSERT INTO tablaexistente (texto, numero) VALUES ('uno',1)");
			ProbarBaseDatos.ObjEnTodasLasBases(db);
		}
	}
}
