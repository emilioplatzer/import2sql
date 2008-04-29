/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 23/03/2008
 * Hora: 10:41
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;
using System.Data.Odbc;
using NUnit.Framework;

using Comunes;

namespace BasesDatos
{
	public class SqLite:BaseDatos
	{
		SqLite(){}
		SqLite(OdbcConnection con)
			:base(con)
		{
		}
		public static SqLite Abrir(string Base){
			OdbcConnection ConexionABase = new OdbcConnection();
			ConexionABase.ConnectionString=@"DRIVER=SQLite3 ODBC Driver;DATABASE="+Base;
			ConexionABase.Open();
			return new SqLite(ConexionABase);
		}
		public static SqLite SinAbrir(){
			return new SqLite();
		}
		public override string ErrorCode_NoExisteTabla{ get{ return "ERROR [HY000]";}}
		public override string ErrorCode_NoExisteVista{ get{ return "ERROR [HY000]";}}
		public override bool UpdateSoloUnaTabla{ get{ return true; } }
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
		public override string OperadorConcatenacionMedio{ 
			get { return "||"; }
		}
		public override bool UpdateConJoin{ get{ return false; } }
		public override bool InternosForzarAs { get { return true; } }
		public override string FuncionLn{ get{ return "LOGoLN";} }
	}
	[TestFixture]
	public class ProbarSqLite{
		#if SinSqLite
		[Test]
		public void SinSqLite(){
			Controlar.Definido("SinSqLite");
		}
		#else
		[Test]
		public void SinSqLite(){
		}
		string nombreArchivo=Archivo.CarpetaActual()+"sqlite_prueba.db";
		public ProbarSqLite(){
			Archivo.Borrar(nombreArchivo);
			SqLite db=SqLite.Abrir(nombreArchivo);
			db.ExecuteNonQuery("CREATE TABLE tablaexistente (texto varchar(100), numero integer)");
			db.ExecuteNonQuery("INSERT INTO tablaexistente (texto, numero) VALUES ('uno',1)");
		}
		[Test]
		public void ConexionSqLite(){
			System.Windows.Forms.Application.OleRequired();
			SqLite db=SqLite.Abrir(nombreArchivo);
			ProbarBaseDatos.ObjEnTodasLasBases(db);
		}
		[Test]
		public void ElUpdate(){
			SqLite db=SqLite.Abrir(nombreArchivo);
			db.ExecuteNonQuery(@"
				CREATE TABLE nodes(
				  id integer primary key,
				  father_id integer,
				  depth integer);
			");
			db.ExecuteNonQuery(@"
			    INSERT into nodes (id,father_id,depth) VALUES (1,null,1);
			");
			db.ExecuteNonQuery(@"
			    INSERT into nodes (id,father_id,depth) VALUES (11,1,null);
			");
			db.ExecuteNonQuery(@"
			  update nodes set depth=
			    (select f.depth+1
			       from nodes as f
			       where f.id = father_id)
			    where depth is null;
			");
			db.ExecuteNonQuery(@" 
			  update nodes set depth=depth+10
			    where not exists (select id from nodes where id=1);
			");
		}
		#endif
	}
}
