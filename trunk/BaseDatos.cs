/*
 * Created by SharpDevelop.
 * User: Gdibella
 * Date: 17/02/2008
 * Time: 09:51 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
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
	}
}
