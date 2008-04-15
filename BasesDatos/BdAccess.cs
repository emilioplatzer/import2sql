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
	public class BdAccess:BaseDatos
	{
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
			return "["+nombreTabla+"]";
		}
		public override string StuffFecha(DateTime fecha){
			return "#"+fecha.Month+"/"+fecha.Day+"/"+fecha.Year+"#";
		}
		public override bool SoportaFkMixta {
			get { return false; }
		}
		public override string OperadorConcatenacionMedio{ 
			get { return " & "; }
		}
		public override bool UpdateConJoin{ get{ return true; } }
	}
	[TestFixture]
	public class ProbarBdAccess{
		[Test]
		public void Creacion(){
			string nombreArchivo="tempAccesABorrar.mdb";
			Archivo.Borrar(nombreArchivo);
			Assert.IsTrue(!Archivo.Existe(nombreArchivo),"no debería existir");
			Catalog cat=BdAccess.Crear(nombreArchivo);
			Assert.IsTrue(Archivo.Existe(nombreArchivo),"debería existir");
			BdAccess db=BdAccess.Abrir(nombreArchivo);
			db.ExecuteNonQuery("CREATE TABLE tablaexistente (texto varchar(100), numero integer)");
			db.ExecuteNonQuery("INSERT INTO tablaexistente (texto, numero) VALUES ('uno',1)");
			ProbarBaseDatos.ObjEnTodasLasBases(db);
		}
	}
}
