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
	public abstract class BaseDatos:IDisposable
	{
		protected IDbConnection con;
		protected IDbCommand cmd;
		protected BaseDatos(IDbConnection con)
		{
			Assert.IsNotNull(con);
			this.con=con;
			cmd=this.con.CreateCommand();
		}
		public IDataReader ExecuteReader(string sentencia){
			Assert.IsNotNull(con);
			IDbCommand cmd_local=con.CreateCommand();
			cmd_local.CommandText=sentencia;
			return cmd_local.ExecuteReader();
		}
		public object ExecuteScalar(string sentencia){
			Assert.IsNotNull(cmd);
			cmd.CommandText=sentencia;
			return cmd.ExecuteScalar();
		}
		public int ExecuteNonQuery(string sentencia){
			Assert.IsNotNull(cmd);
			cmd.CommandText=sentencia;
			return cmd.ExecuteNonQuery();
		}
		public bool EliminarTablaSiExiste(string nombreTabla){
			Assert.IsNotNull(cmd);
			try{
				cmd.CommandText="DROP TABLE "+StuffTabla(nombreTabla);
				cmd.ExecuteNonQuery();
				return true;
			}catch(DbException ex){
				if(ex.ErrorCode==ErrorCode_NoExisteTabla){
					return false; // ok, no existía la tabla por eso salto la excepción
				}
				throw;
			}
		}
		public string StuffValor(object valor){
			if(valor==null){
				return "null";
			}
			if(valor.GetType()==typeof(String)){
				return "'"+Cadena.BuscarYReemplazar((string) valor,"'","''")+"'";
			}else if(valor.GetType()==typeof(double)){
				return ((double) valor).ToString(Cadena.FormatoPuntoDecimal);
			}else if(valor.GetType()==typeof(DateTime)){
				return StuffFecha((DateTime) valor);
			}else{
				return valor.ToString();
			}
		}
		public void Close(){
			con.Close();
			con=null;
			cmd=null;
		}
		public void Dispose(){
			Close();
		}
		public abstract int ErrorCode_NoExisteTabla{ get; }
		public abstract string StuffTabla(string nombreTabla);
		public abstract string StuffFecha(DateTime fecha);
		public virtual string StuffCampo(string nombreCampo){ return StuffTabla(nombreCampo); }
	}
	[TestFixture]
	public class ProbarBaseDatos{
		public static void ObjEnTodasLasBases(BaseDatos db){
			ObjOperacionesSimples(db);
			ObjUsarReceptor(db);
		}
		public static void ObjOperacionesSimples(BaseDatos db){
			IDataReader rdr=db.ExecuteReader("SELECT * FROM tablaexistente");
			rdr.Read();
			Assert.AreEqual("uno",rdr["texto"]);
			rdr.Close();
			Assert.AreEqual(0.625,db.ExecuteScalar("SELECT 10.0/16 FROM tablaexistente WHERE numero=1"));
			db.EliminarTablaSiExiste("nueva_tabla_prueba");
			db.ExecuteNonQuery(@"CREATE TABLE nueva_tabla_prueba (
				nombre varchar(100),
				numero integer,
				monto double precision,
				fecha date,
				primary key (nombre));
			");
			db.ExecuteNonQuery("INSERT INTO nueva_tabla_prueba (nombre) VALUES ('solo')");
			rdr=db.ExecuteReader("SELECT * FROM nueva_tabla_prueba");
			rdr.Read();
			Assert.AreEqual("solo",rdr["nombre"]);
			rdr.Close();
		}
		public static void ObjUsarReceptor(BaseDatos db){
			ReceptorSql receptor=new ReceptorSql(db,"nueva_tabla_prueba");
			using(InsertadorSql insertador=new InsertadorSql(receptor)){
				insertador["nombre"]="toto";
				insertador["numero"]=2;
				insertador["monto"]=3.1416;
				insertador["fecha"]=new DateTime(2001,12,20);
				insertador.InsertarSiHayCampos();
			}
			using(InsertadorSql insertador=new InsertadorSql(receptor)){
				insertador["nombre"]="tute";
				insertador["numero"]=3;
				insertador["monto"]=Math.Exp(1);
				insertador["fecha"]=new DateTime(1969,5,6);
				insertador.InsertarSiHayCampos();
			}
			IDataReader rdr=db.ExecuteReader("SELECT * FROM nueva_tabla_prueba ORDER BY nombre");
			rdr.Read();
			rdr.Read();
			Assert.AreEqual(3.1416,rdr["monto"]);
			Assert.AreEqual(new DateTime(2001,12,20),rdr["fecha"]);
			rdr.Read();
			Assert.AreEqual(Math.Exp(1),(double) rdr["monto"],0.00000000000001);
			Assert.AreEqual(new DateTime(1969,5,6),rdr["fecha"]);
		}
		/*
		public static void AuxEnTodasLasBases(IDbConnection con){
			AuxOperacionesSimples(con);
			// AuxUsarReceptor(con);
		}
		public static void AuxOperacionesSimples(IDbConnection con){
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
			}catch(DbException ex){
				if(ex.ErrorCode==BdAccess.ErrorCode_NoExisteTabla ||
				   ex.ErrorCode==PostgreSql.ErrorCode_NoExisteTabla){
				}else{
					Assert.Ignore("DROP TABLE cantó "+ex.ErrorCode+": "+ex.Message+" no es un error conocido");
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
		}
		public static void AuxUsarReceptor(IDbConnection con){
			ReceptorSql receptor=new ReceptorSql(con,"nueva_tabla_prueba");
			InsertadorSql insertador=new InsertadorSql(receptor);
			insertador["nombre"]="toto";
			insertador["numero"]=2;
			insertador["monto"]=3.1416;
			insertador["fecha"]=new DateTime(2001,12,20);
			insertador.InsertarSiHayCampos();
			IDbCommand cmd=con.CreateCommand();
			cmd.CommandText="SELECT * FROM nueva_tabla_prueba ORDER BY nombre";
			IDataReader rdr=cmd.ExecuteReader();
			rdr.Read();
			rdr.Read();
			Assert.AreEqual(3.1416,rdr["monto"]);
			Assert.AreEqual(new DateTime(2001,12,20),rdr["fecha"]);
		}
		*/
	}
}
